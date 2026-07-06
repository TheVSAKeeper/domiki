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
            var domikManager = new DomikManager(uow, uow.Context, GetCalculator(calculatorJustFinishMode), resourceManager, playerResourceManager, workerManager, weatherManager, villageLevelCalculator);
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
            var orderManager = new OrderManager(uow, uow.Context, GetCalculator(calculatorJustFinishMode), resourceManager, playerResourceManager, villageLevelCalculator);
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

        public WeatherManager GetWeatherManager(UnitOfWork uow, bool calculatorJustFinishMode = true)
        {
            var resourceManager = new ResourceManager(uow.Context);
            return new WeatherManager(uow.Context, uow, GetCalculator(calculatorJustFinishMode), resourceManager);
        }

        private ICalculator GetCalculator(bool justFinishMode = true)
        {
            return new TestCalculator(() => GetUow(), (UnitOfWork uow) => { return new CalculatorTick(GetDomikManager(uow), GetOrderManager(uow), GetWeatherManager(uow)); }, justFinishMode);
        }

        public static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json")
               .AddEnvironmentVariables()
                .Build();
            return config;
        }
    }
}
