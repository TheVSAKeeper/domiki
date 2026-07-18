using Domiki.Web.Core.Scheduling;
using Domiki.Web.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Domiki.Web.Tests;

/// <summary>
/// Обсчитывает событие синхронно сразу при регистрации. Исключения: события внутри Defer()-блока и OrderExpire –
/// заказы не авто-протухают, их финиширует только явный вызов (как в legacy-харнессе, где OrderManager жил с
/// justFinishMode=false).
/// </summary>
public sealed class TestCalculator : ICalculator
{
    private static readonly AsyncLocal<bool> _deferred = new();

    private readonly IServiceProvider _serviceProvider;

    public TestCalculator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static IDisposable Defer()
    {
        _deferred.Value = true;
        return new DeferScope();
    }

    public void Insert(CalculateInfo calcDate)
    {
        if (_deferred.Value || calcDate.Type == CalculateTypes.OrderExpire)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<UnitOfWork>();
        var calculatorTick = scope.ServiceProvider.GetRequiredService<CalculatorTick>();
        calculatorTick.Calculate(DateTimeHelper.GetNowDate().AddYears(217), calcDate);
        uow.Context.SaveChanges();
        uow.Commit();
    }

    public void Remove(int playerId, long objectId, CalculateTypes type)
    {
    }

    public void Reschedule(int playerId, long objectId, CalculateTypes type, DateTime newDate)
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
