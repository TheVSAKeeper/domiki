namespace Domiki.Web.Core.Scheduling;

public class CalculatorBackgroundService : BackgroundService
{
    private readonly ICalculator _calculator;
    private IServiceProvider _serviceProvider;

    public CalculatorBackgroundService(IServiceProvider serviceProvider, ICalculator calculator)
    {
        _serviceProvider = serviceProvider;
        _calculator = calculator;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _calculator.CheckInit();
        return Task.FromResult(0);
    }
}
