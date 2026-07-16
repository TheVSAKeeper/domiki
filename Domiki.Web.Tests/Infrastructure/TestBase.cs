using Domiki.Web.Activities;
using Domiki.Web.Core.Scheduling;
using Domiki.Web.Core;
using Domiki.Web.Data.Entities;
using Domiki.Web.Data;
using Domiki.Web.Economy;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village;
using Domiki.Web.Workers;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Domiki.Web.Tests
{
    public class TestBase
    {
        protected Settings _options;
        private readonly GameStateBroker _broker = new();

        private static bool _migrated;
        private static readonly object _migrateLock = new object();

        private static readonly string[] VillageNameRoots =
        {
            "Заречье", "Полянка", "Боровое", "Ключи", "Лесное", "Озерки", "Холмы", "Родники", "Выселки", "Гари", "Луговое", "Верховье",
        };

        protected static string TestVillageName() =>
            TestVillageName(VillageNameRoots[Random.Shared.Next(VillageNameRoots.Length)]);

        protected static string TestVillageName(string prefix) =>
            $"{prefix}-{Guid.NewGuid().ToString("N")[..6]}";

        public TestBase()
        {
            var config = InitConfiguration();
            _options = config.Get<Settings>();
            EnsureMigrated();
        }

        private void EnsureMigrated()
        {
            if (_migrated)
            {
                return;
            }

            lock (_migrateLock)
            {
                if (_migrated)
                {
                    return;
                }

                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseNpgsql(_options.ConnectionStrings.DefaultConnection);
                optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
                using var context = new ApplicationDbContext(optionsBuilder.Options);
                context.Database.Migrate();
                _migrated = true;
            }
        }

        public void GrantDomik(int playerId, int id, int typeId, int level = 1)
        {
            using var uow = GetUow();
            uow.Context.Domiks.Add(new Domik { PlayerId = playerId, Id = id, TypeId = typeId, Level = level });
            uow.Context.SaveChanges();
            uow.Commit();
        }

        public void MuteFtue(int playerId)
        {
            using var uow = GetUow();
            var completed = uow.Context.PlayerGoals.Where(x => x.PlayerId == playerId).Select(x => x.GoalId).ToHashSet();
            uow.Context.PlayerGoals.AddRange(uow.Context.StarterGoals.Select(x => x.Id).ToArray()
                .Where(x => !completed.Contains(x))
                .Select(goalId => new PlayerGoal { PlayerId = playerId, GoalId = goalId, CompleteDate = DateTimeHelper.GetNowDate() }));
            uow.Context.Players.Single(x => x.Id == playerId).ZealCharges = 0;
            uow.Commit();
        }

        public UnitOfWork GetUow()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(_options.ConnectionStrings.DefaultConnection);
            var context = new ApplicationDbContext(optionsBuilder.Options);
            var uow = new UnitOfWork(context);
            return uow;
        }

        public DomikManager GetDomikManager(UnitOfWork uow, bool calculatorJustFinishMode = true)
        {
            var resourceManager = new ResourceManager(uow.Context);
            var playerResourceManager = new PlayerResourceManager(uow.Context, resourceManager);
            var workerManager = new WorkerManager(uow.Context, resourceManager, playerResourceManager);
            var weatherManager = GetWeatherManager(uow, calculatorJustFinishMode);
            var villageLevelCalculator = new VillageLevelCalculator(uow.Context, resourceManager, workerManager);
            var blueprintManager = new BlueprintManager(uow.Context, resourceManager, playerResourceManager);
            var playerEventManager = GetPlayerEventManager(uow);
            var tolokaManager = new TolokaManager(uow, uow.Context, resourceManager, playerResourceManager, GetSeasonManager(uow), playerEventManager, _broker);
            var goalManager = new GoalManager(uow.Context, resourceManager, playerResourceManager, villageLevelCalculator, playerEventManager);
            var domikManager = new DomikManager(uow, uow.Context, GetCalculator(calculatorJustFinishMode), resourceManager, playerResourceManager, workerManager, weatherManager, villageLevelCalculator, blueprintManager, tolokaManager, playerEventManager, goalManager);
            return domikManager;
        }

        public WorkerManager GetWorkerManager(UnitOfWork uow)
        {
            var resourceManager = new ResourceManager(uow.Context);
            var playerResourceManager = new PlayerResourceManager(uow.Context, resourceManager);
            return new WorkerManager(uow.Context, resourceManager, playerResourceManager);
        }

        public OrderManager GetOrderManager(UnitOfWork uow, bool calculatorJustFinishMode = false)
        {
            var resourceManager = new ResourceManager(uow.Context);
            var playerResourceManager = new PlayerResourceManager(uow.Context, resourceManager);
            var workerManager = new WorkerManager(uow.Context, resourceManager, playerResourceManager);
            var villageLevelCalculator = new VillageLevelCalculator(uow.Context, resourceManager, workerManager);
            var playerEventManager = GetPlayerEventManager(uow);
            var tolokaManager = new TolokaManager(uow, uow.Context, resourceManager, playerResourceManager, GetSeasonManager(uow), playerEventManager, _broker);
            var goalManager = new GoalManager(uow.Context, resourceManager, playerResourceManager, villageLevelCalculator, playerEventManager);
            var orderManager = new OrderManager(uow, uow.Context, GetCalculator(calculatorJustFinishMode), resourceManager, playerResourceManager, workerManager, villageLevelCalculator, GetSeasonManager(uow), tolokaManager, goalManager);
            return orderManager;
        }

        public VillageLevelCalculator GetVillageLevelCalculator(UnitOfWork uow)
        {
            var resourceManager = new ResourceManager(uow.Context);
            var playerResourceManager = new PlayerResourceManager(uow.Context, resourceManager);
            var workerManager = new WorkerManager(uow.Context, resourceManager, playerResourceManager);
            return new VillageLevelCalculator(uow.Context, resourceManager, workerManager);
        }

        public ResourceManager GetResourceManager(UnitOfWork uow)
        {
            var manager = new ResourceManager(uow.Context);
            return manager;
        }

        public BlueprintManager GetBlueprintManager(UnitOfWork uow)
        {
            var resourceManager = new ResourceManager(uow.Context);
            var playerResourceManager = new PlayerResourceManager(uow.Context, resourceManager);
            return new BlueprintManager(uow.Context, resourceManager, playerResourceManager);
        }

        public ExpeditionManager GetExpeditionManager(UnitOfWork uow, bool calculatorJustFinishMode = true)
        {
            var resourceManager = new ResourceManager(uow.Context);
            var playerResourceManager = new PlayerResourceManager(uow.Context, resourceManager);
            var workerManager = new WorkerManager(uow.Context, resourceManager, playerResourceManager);
            var decorManager = new DecorManager(uow, uow.Context, resourceManager, playerResourceManager);
            return new ExpeditionManager(uow, uow.Context, GetCalculator(calculatorJustFinishMode), resourceManager, playerResourceManager, workerManager, GetSeasonManager(uow), GetPlayerEventManager(uow), decorManager, GetBlueprintManager(uow));
        }

        public TolokaManager GetTolokaManager(UnitOfWork uow)
        {
            var resourceManager = new ResourceManager(uow.Context);
            var playerResourceManager = new PlayerResourceManager(uow.Context, resourceManager);
            return new TolokaManager(uow, uow.Context, resourceManager, playerResourceManager, GetSeasonManager(uow), GetPlayerEventManager(uow), _broker);
        }

        public SeasonManager GetSeasonManager(UnitOfWork uow)
        {
            return new SeasonManager(uow.Context);
        }

        public PlayerEventManager GetPlayerEventManager(UnitOfWork uow)
        {
            return new PlayerEventManager(uow.Context);
        }

        public PushManager GetPushManager(UnitOfWork uow)
        {
            return new PushManager(uow.Context);
        }

        public MarketManager GetMarketManager(UnitOfWork uow, bool calculatorJustFinishMode = false)
        {
            var resourceManager = new ResourceManager(uow.Context);
            var playerResourceManager = new PlayerResourceManager(uow.Context, resourceManager);
            return new MarketManager(uow, uow.Context, GetCalculator(calculatorJustFinishMode), resourceManager, playerResourceManager, GetPlayerEventManager(uow), _broker);
        }

        public DecorManager GetDecorManager(UnitOfWork uow)
        {
            var resourceManager = new ResourceManager(uow.Context);
            var playerResourceManager = new PlayerResourceManager(uow.Context, resourceManager);
            return new DecorManager(uow, uow.Context, resourceManager, playerResourceManager);
        }

        public WorldManager GetWorldManager(UnitOfWork uow)
        {
            var resourceManager = new ResourceManager(uow.Context);
            var playerResourceManager = new PlayerResourceManager(uow.Context, resourceManager);
            var workerManager = new WorkerManager(uow.Context, resourceManager, playerResourceManager);
            var villageLevelCalculator = new VillageLevelCalculator(uow.Context, resourceManager, workerManager);
            var weatherManager = GetWeatherManager(uow);
            var blueprintManager = new BlueprintManager(uow.Context, resourceManager, playerResourceManager);
            var playerEventManager = GetPlayerEventManager(uow);
            var tolokaManager = new TolokaManager(uow, uow.Context, resourceManager, playerResourceManager, GetSeasonManager(uow), playerEventManager, _broker);
            var goalManager = new GoalManager(uow.Context, resourceManager, playerResourceManager, villageLevelCalculator, playerEventManager);
            var domikManager = new DomikManager(uow, uow.Context, GetCalculator(true), resourceManager, playerResourceManager, workerManager, weatherManager, villageLevelCalculator, blueprintManager, tolokaManager, playerEventManager, goalManager);
            return new WorldManager(uow.Context, villageLevelCalculator, domikManager, resourceManager, GetSeasonManager(uow));
        }

        public WeatherManager GetWeatherManager(UnitOfWork uow, bool calculatorJustFinishMode = true)
        {
            var resourceManager = new ResourceManager(uow.Context);
            return new WeatherManager(uow.Context, uow, GetCalculator(calculatorJustFinishMode), resourceManager);
        }

        private ICalculator GetCalculator(bool justFinishMode = true)
        {
            return new TestCalculator(() => GetUow(), (UnitOfWork uow) => { return new CalculatorTick(GetDomikManager(uow), GetOrderManager(uow), GetWeatherManager(uow), GetExpeditionManager(uow), GetMarketManager(uow)); }, justFinishMode);
        }

        public static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json")
               .AddJsonFile("appsettings.Development.json", optional: true)
               .AddEnvironmentVariables()
                .Build();
            return config;
        }
    }
}
