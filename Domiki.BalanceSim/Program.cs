using Domiki.BalanceSim;
using Domiki.Web.Data.Entities;
using Domiki.Web.Data;
using Domiki.Web.Reference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Не задана ConnectionStrings:DefaultConnection");
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql(connectionString)
    .Options;

using var context = new ApplicationDbContext(options);
var resources = new ResourceManager(context);
var data = SimulationData.Load(resources);
var simulator = new BalanceSimulator(data);
if (args.Contains("--ftue", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine(new FtueReport(simulator.RunFtue()).Render());
}
else
{
    var report = simulator.Run();
    Console.WriteLine(new BalanceReport(data, report).Render());
}
