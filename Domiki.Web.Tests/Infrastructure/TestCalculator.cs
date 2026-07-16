using Domiki.Web.Core.Scheduling;
using Domiki.Web.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Domiki.Web.Tests;

public class TestCalculator : ICalculator
{
    private static readonly AsyncLocal<bool> _deferred = new();

    private readonly Func<UnitOfWork>? _uowFactory;
    private readonly Func<UnitOfWork, CalculatorTick>? _calculatorTickFactory;
    private readonly IServiceProvider? _serviceProvider;

    /// <summary>
    /// Все события обсчитываются моментально.
    /// </summary>
    private readonly bool _justFinishMode;

    public TestCalculator(Func<UnitOfWork> uowFactory, Func<UnitOfWork, CalculatorTick> calculatorTickFactory, bool justFinishMode = true)
    {
        _uowFactory = uowFactory;
        _calculatorTickFactory = calculatorTickFactory;
        _justFinishMode = justFinishMode;
    }

    public TestCalculator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _justFinishMode = true;
    }

    public static IDisposable Defer()
    {
        _deferred.Value = true;
        return new DeferScope();
    }

    public void Insert(CalculateInfo calcDate)
    {
        if (!_justFinishMode || _deferred.Value)
        {
            return;
        }

        if (_serviceProvider != null)
        {
            using var scope = _serviceProvider.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<UnitOfWork>();
            var calculatorTick = scope.ServiceProvider.GetRequiredService<CalculatorTick>();
            calculatorTick.Calculate(DateTimeHelper.GetNowDate().AddYears(217), calcDate);
            uow.Context.SaveChanges();
            uow.Commit();
            return;
        }

        using var legacyUow = _uowFactory!();
        var legacyCalculatorTick = _calculatorTickFactory!(legacyUow);
        legacyCalculatorTick.Calculate(DateTimeHelper.GetNowDate().AddYears(217), calcDate);
        legacyUow.Context.SaveChanges();
        legacyUow.Commit();
    }

    public void Remove(int playerId, long objectId, CalculateTypes type)
    {
    }

    public void CheckInit()
    {
    }

    private sealed class DeferScope : IDisposable
    {
        public void Dispose()
        {
            _deferred.Value = false;
        }
    }
}
