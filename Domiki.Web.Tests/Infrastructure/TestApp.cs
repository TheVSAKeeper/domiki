using Domiki.Web.Core.Scheduling;
using Domiki.Web.Data;
using Domiki.Web.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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
        var config = TestBase.InitConfiguration().Get<Settings>()!;
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
                context.Database.ExecuteSqlInterpolated($"DELETE FROM \"Players\" WHERE \"Id\" = ANY({playerIds})");
                context.Database.ExecuteSqlInterpolated($"DELETE FROM \"Domiks\" WHERE \"PlayerId\" = ANY({playerIds})");
                context.Database.ExecuteSqlInterpolated($"DELETE FROM \"PlayerGoals\" WHERE \"PlayerId\" = ANY({playerIds})");
                context.Database.ExecuteSqlInterpolated($"DELETE FROM \"PlayerEvents\" WHERE \"PlayerId\" = ANY({playerIds})");
                context.Database.ExecuteSqlInterpolated($"DELETE FROM \"PlayerPushSubscriptions\" WHERE \"PlayerId\" = ANY({playerIds})");
            }
        }

        _factory.Dispose();
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
