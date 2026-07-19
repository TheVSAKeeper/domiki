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

namespace Domiki.Web.Tests;

[SetUpFixture]
public sealed class TestAppSetup
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        App.Initialize();
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

    public static string RunId { get; } = Guid.NewGuid().ToString("N")[..8];

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

    public static HttpClient Client()
    {
        return _factory!.CreateClient();
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
            builder.UseSetting("ConnectionStrings:DefaultConnection", config.ConnectionStrings.DefaultConnection);
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
            context.Database.ExecuteSqlRaw($"DELETE FROM {QuoteTable(entityType)} AS t WHERE {predicate}", new object[] { playerIds });
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
