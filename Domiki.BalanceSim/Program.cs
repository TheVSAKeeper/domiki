using Domiki.BalanceSim;
using Domiki.Web.Business.Core;
using Domiki.Web.Data;
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
var report = new BalanceSimulator(data).Run();
Console.WriteLine(new BalanceReport(data, report).Render());
