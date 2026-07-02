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
            var domikManager = new DomikManager(uow, uow.Context, GetCalculator(calculatorJustFinishMode), new ResourceManager(uow.Context));
            return domikManager;
        }

        public ResourceManager GetResourceManager(UnitOfWork uow)
        {
            var manager = new ResourceManager(uow.Context);
            return manager;
        }

        private ICalculator GetCalculator(bool justFinishMode = true)
        {
            return new TestCalculator(() => GetUow(), (UnitOfWork uow) => { return new CalculatorTick(GetDomikManager(uow)); }, justFinishMode);
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
