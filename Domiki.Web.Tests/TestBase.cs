using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Domiki.Web.Tests
{
    public class TestBase
    {
        protected Settings _options;

        public TestBase()
        {
            var config = InitConfiguration();
            _options = config.Get<Settings>();
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
            var tolokaManager = new TolokaManager(uow, uow.Context, resourceManager, playerResourceManager, villageLevelCalculator, GetSeasonManager(uow));
            var domikManager = new DomikManager(uow, uow.Context, GetCalculator(calculatorJustFinishMode), resourceManager, playerResourceManager, workerManager, weatherManager, villageLevelCalculator, blueprintManager, tolokaManager);
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
            var orderManager = new OrderManager(uow, uow.Context, GetCalculator(calculatorJustFinishMode), resourceManager, playerResourceManager, villageLevelCalculator, GetSeasonManager(uow));
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
            return new ExpeditionManager(uow, uow.Context, GetCalculator(calculatorJustFinishMode), resourceManager, playerResourceManager, workerManager, GetSeasonManager(uow));
        }

        public TolokaManager GetTolokaManager(UnitOfWork uow)
        {
            var resourceManager = new ResourceManager(uow.Context);
            var playerResourceManager = new PlayerResourceManager(uow.Context, resourceManager);
            var workerManager = new WorkerManager(uow.Context, resourceManager, playerResourceManager);
            var villageLevelCalculator = new VillageLevelCalculator(uow.Context, resourceManager, workerManager);
            return new TolokaManager(uow, uow.Context, resourceManager, playerResourceManager, villageLevelCalculator, GetSeasonManager(uow));
        }

        public SeasonManager GetSeasonManager(UnitOfWork uow)
        {
            return new SeasonManager(uow.Context);
        }

        public MarketManager GetMarketManager(UnitOfWork uow, bool calculatorJustFinishMode = false)
        {
            var resourceManager = new ResourceManager(uow.Context);
            var playerResourceManager = new PlayerResourceManager(uow.Context, resourceManager);
            var workerManager = new WorkerManager(uow.Context, resourceManager, playerResourceManager);
            var villageLevelCalculator = new VillageLevelCalculator(uow.Context, resourceManager, workerManager);
            return new MarketManager(uow, uow.Context, GetCalculator(calculatorJustFinishMode), resourceManager, playerResourceManager, villageLevelCalculator);
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
            var tolokaManager = new TolokaManager(uow, uow.Context, resourceManager, playerResourceManager, villageLevelCalculator, GetSeasonManager(uow));
            var domikManager = new DomikManager(uow, uow.Context, GetCalculator(true), resourceManager, playerResourceManager, workerManager, weatherManager, villageLevelCalculator, blueprintManager, tolokaManager);
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
