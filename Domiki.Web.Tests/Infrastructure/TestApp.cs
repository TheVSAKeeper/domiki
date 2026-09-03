using System.Globalization;
using Domiki.Web.Core.Scheduling;
using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Domiki.Web.Tests;

[SetUpFixture]
public sealed class TestAppSetup
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        App.Initialize();
        App.SweepAbandoned();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        App.Cleanup();
    }
}

public static class App
{
    private static WebApplicationFactory<Program>? _factory;

    private static int _clientCount;

    private const string RunStampFormat = "yyyyMMddHHmmss";

    private const string TestUserPrefix = "testUser_";

    /// <summary>
    /// Возраст прогона, после которого его игроки считаются брошенными. С запасом больше самого долгого прогона.
    /// </summary>
    private static readonly TimeSpan AbandonedAfter = TimeSpan.FromHours(3);

    /// <summary>
    /// Метка прогона в имени тестового игрока. Начинается со штампа времени старта, поэтому брошенных игроков
    /// прерванного прогона можно отличить от игроков соседнего живого прогона – см. <see cref="SweepAbandoned"/>.
    /// </summary>
    public static string RunId { get; } =
        DateTime.UtcNow.ToString(RunStampFormat) + "-" + Guid.NewGuid().ToString("N")[..6];

    /// <summary>
    /// Число повторов в конкурентных стресс-тестах. По умолчанию 25 – гонка на блокировке строки игрока
    /// воспроизводится в первом же десятке итераций, а каждая итерация стоит десяти сериализованных транзакций.
    /// Полный объём задаётся переменной окружения DOMIKI_STRESS_ITERATIONS.
    /// </summary>
    public static int StressIterations { get; } =
        int.TryParse(Environment.GetEnvironmentVariable("DOMIKI_STRESS_ITERATIONS"), out var iterations) && iterations > 0
            ? iterations
            : 25;

    public static IServiceProvider Services => _factory!.Services;

    public static AppScope Scope()
    {
        return new(Services.CreateScope());
    }

    public static void Act<T>(Action<T> act) where T : notnull
    {
        using var scope = Scope();
        act(scope.Get<T>());
        scope.Commit();
    }

    public static TResult Act<T, TResult>(Func<T, TResult> act) where T : notnull
    {
        using var scope = Scope();
        var result = act(scope.Get<T>());
        scope.Commit();
        return result;
    }

    public static TResult Read<TResult>(Func<ApplicationDbContext, TResult> read)
    {
        using var scope = Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    /// <summary>
    /// HTTP-клиент к тестовому хосту. Каждому клиенту выдаётся собственный адрес в X-Forwarded-For: по этому заголовку
    /// ограничитель частоты демо-входа делит разделы, и с общим адресом десяток тестов с входом выжигал бы лимит
    /// (10 запросов на 5 минут) на весь оставшийся прогон.
    /// </summary>
    public static HttpClient Client()
    {
        var client = _factory!.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", NextClientAddress());
        return client;
    }

    private static string NextClientAddress()
    {
        var index = Interlocked.Increment(ref _clientCount);
        return string.Create(CultureInfo.InvariantCulture, $"10.{index >> 16 & 0xFF}.{index >> 8 & 0xFF}.{index & 0xFF}");
    }

    public static IDisposable PendingEvents()
    {
        return TestCalculator.Defer();
    }

    internal static void Initialize()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", true)
            .AddEnvironmentVariables()
            .Build()
            .Get<Settings>()!;
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", PinPool(config.ConnectionStrings.DefaultConnection));
            builder.UseSetting("Demo:UserName", "demo-tester");
            builder.UseSetting("Demo:Email", "demo-tester@test.local");
            builder.UseSetting("Demo:Password", "Demo#Test1");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICalculator>();
                services.AddSingleton<ICalculator>(sp => new TestCalculator(sp));
            });
        });
    }

    /// <summary>
    /// Закрепляет пул соединений на весь прогон: физическое подключение к Postgres на порядки дороже запроса
    /// (проброшенный порт Docker отдаёт первый обмен на новом сокете за сотни миллисекунд, тогда как запрос
    /// по живому соединению – за доли), а пересоздание тысяч соединений изнашивает релей. Минимум держит
    /// сокеты открытыми под всплески <c>Parallel.ForEach</c> в конкурентных тестах, срок простоя снят,
    /// чтобы пул не подрезал их между тестами.
    /// </summary>
    /// <param name="connectionString">Исходная строка подключения из конфигурации.</param>
    private static string PinPool(string connectionString)
    {
        return new NpgsqlConnectionStringBuilder(connectionString)
        {
            MinPoolSize = 8,
            MaxPoolSize = 32,
            ConnectionIdleLifetime = 3600,
        }.ConnectionString;
    }

    /// <summary>
    /// Убирает игроков брошенных прогонов. Прогон, прерванный по Ctrl+C или упавший вместе с процессом, не доходит
    /// до <see cref="Cleanup"/> и оставляет своих игроков в dev-БД навсегда – накопленные тысячи засоряют выборки
    /// и очередь планировщика. Удаляются только игроки, чей штамп времени в имени старше <see cref="AbandonedAfter"/>,
    /// поэтому игроки соседнего живого прогона остаются нетронутыми.
    /// </summary>
    internal static void SweepAbandoned()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var threshold = DateTime.UtcNow - AbandonedAfter;
        var abandoned = context.Players
            .Where(player => EF.Functions.Like(player.AspNetUserId, "testUser_%"))
            .Select(player => new { player.Id, player.AspNetUserId })
            .ToArray()
            .Where(player => IsAbandoned(player.AspNetUserId, threshold))
            .Select(player => player.Id)
            .ToArray();

        if (abandoned.Length > 0)
        {
            DeletePlayers(context, abandoned);
        }
    }

    /// <summary>
    /// Считает игрока брошенным: либо его прогон стартовал раньше порога, либо имя вообще без штампа времени
    /// (формат до введения штампа – такой игрок заведомо остался от давнего прогона).
    /// </summary>
    /// <param name="aspNetUserId">Имя пользователя тестового игрока – <c>testUser_&lt;RunId&gt;_&lt;Guid&gt;</c>.</param>
    /// <param name="threshold">Время, раньше которого стартовавший прогон считается брошенным.</param>
    private static bool IsAbandoned(string aspNetUserId, DateTime threshold)
    {
        var tail = aspNetUserId.AsSpan(TestUserPrefix.Length);
        return tail.Length < RunStampFormat.Length
               || !DateTime.TryParseExact(tail[..RunStampFormat.Length], RunStampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var started)
               || started < threshold;
    }

    internal static void Cleanup()
    {
        if (_factory == null)
        {
            return;
        }

        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pattern = $"testUser_{RunId}%";
            var playerIds = context.Players
                .Where(p => EF.Functions.Like(p.AspNetUserId, pattern))
                .Select(p => p.Id)
                .ToArray();

            if (playerIds.Length > 0)
            {
                DeletePlayers(context, playerIds);
            }
        }

        _factory.Dispose();
    }

    private static void DeletePlayers(ApplicationDbContext context, int[] playerIds)
    {
        var model = context.Model;
        var playerType = model.FindEntityType(typeof(Player))!;

        var owned = new HashSet<IEntityType> { playerType };
        for (var grew = true; grew;)
        {
            grew = false;
            foreach (var entityType in model.GetEntityTypes())
            {
                if (!owned.Contains(entityType) && OwnershipEdges(entityType, playerType).Any(edge => owned.Contains(edge.Principal)))
                {
                    owned.Add(entityType);
                    grew = true;
                }
            }
        }

        foreach (var entityType in OrderDependentsFirst(owned, playerType))
        {
            var aliasCounter = 0;
            var predicate = OwnedPredicate(entityType, playerType, owned, "t", ref aliasCounter);
#pragma warning disable EF1002
            context.Database.ExecuteSqlRaw($"DELETE FROM {QuoteTable(entityType)} AS t WHERE {predicate}", new object[] { playerIds });
#pragma warning restore EF1002
        }
    }

    private static List<(List<(string Dependent, string Principal)> Columns, IEntityType Principal)> OwnershipEdges(IEntityType entityType, IEntityType playerType)
    {
        var edges = new List<(List<(string, string)>, IEntityType)>();
        var hasModeledPlayerFk = false;
        foreach (var fk in entityType.GetForeignKeys())
        {
            var columns = new List<(string, string)>();
            for (var i = 0; i < fk.Properties.Count; i++)
            {
                columns.Add((Column(fk.Properties[i], entityType), Column(fk.PrincipalKey.Properties[i], fk.PrincipalEntityType)));
            }

            edges.Add((columns, fk.PrincipalEntityType));
            hasModeledPlayerFk |= fk.PrincipalEntityType == playerType;
        }

        if (!hasModeledPlayerFk)
        {
            var playerIdProperty = entityType.FindProperty("PlayerId");
            if (playerIdProperty is { ClrType: var clrType } && clrType == typeof(int))
            {
                var principalColumn = Column(playerType.FindPrimaryKey()!.Properties[0], playerType);
                edges.Add((new List<(string, string)> { (Column(playerIdProperty, entityType), principalColumn) }, playerType));
            }
        }

        return edges;
    }

    private static string OwnedPredicate(IEntityType entityType, IEntityType playerType, ISet<IEntityType> owned, string alias, ref int aliasCounter)
    {
        if (entityType == playerType)
        {
            var idColumn = Column(playerType.FindPrimaryKey()!.Properties[0], playerType);
            return $"{alias}.\"{idColumn}\" = ANY({{0}})";
        }

        var clauses = new List<string>();
        foreach (var edge in OwnershipEdges(entityType, playerType))
        {
            if (edge.Principal == entityType || !owned.Contains(edge.Principal))
            {
                continue;
            }

            var principalAlias = "p" + aliasCounter++;
            var joins = edge.Columns.Select(column => $"{principalAlias}.\"{column.Principal}\" = {alias}.\"{column.Dependent}\"");
            var inner = OwnedPredicate(edge.Principal, playerType, owned, principalAlias, ref aliasCounter);
            clauses.Add($"EXISTS (SELECT 1 FROM {QuoteTable(edge.Principal)} AS {principalAlias} WHERE {string.Join(" AND ", joins)} AND ({inner}))");
        }

        return string.Join(" OR ", clauses);
    }

    private static IReadOnlyList<IEntityType> OrderDependentsFirst(ISet<IEntityType> owned, IEntityType playerType)
    {
        var indegree = owned.ToDictionary(entityType => entityType, _ => 0);
        foreach (var entityType in owned)
        {
            foreach (var edge in OwnershipEdges(entityType, playerType))
            {
                if (owned.Contains(edge.Principal) && edge.Principal != entityType)
                {
                    indegree[edge.Principal]++;
                }
            }
        }

        var queue = new Queue<IEntityType>(owned.Where(entityType => indegree[entityType] == 0));
        var order = new List<IEntityType>();
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            order.Add(node);
            foreach (var edge in OwnershipEdges(node, playerType))
            {
                if (owned.Contains(edge.Principal) && edge.Principal != node && --indegree[edge.Principal] == 0)
                {
                    queue.Enqueue(edge.Principal);
                }
            }
        }

        if (order.Count != owned.Count)
        {
            throw new InvalidOperationException("FK-цикл среди таблиц игрока – порядок удаления не построен");
        }

        return order;
    }

    private static string Column(IProperty property, IEntityType owner)
    {
        var store = StoreObjectIdentifier.Table(owner.GetTableName()!, owner.GetSchema());
        return property.GetColumnName(store) ?? property.Name;
    }

    private static string QuoteTable(IEntityType entityType)
    {
        var schema = entityType.GetSchema();
        var table = entityType.GetTableName()!;
        return schema == null ? $"\"{table}\"" : $"\"{schema}\".\"{table}\"";
    }
}

public sealed class AppScope : IDisposable
{
    private readonly IServiceScope _scope;

    internal AppScope(IServiceScope scope)
    {
        _scope = scope;
    }

    public ApplicationDbContext Context => Get<ApplicationDbContext>();

    public IServiceProvider Services => _scope.ServiceProvider;

    public T Get<T>() where T : notnull
    {
        return _scope.ServiceProvider.GetRequiredService<T>();
    }

    public void Commit()
    {
        Get<UnitOfWork>().Commit();
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}
