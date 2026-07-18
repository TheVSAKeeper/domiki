using Domiki.Web.Economy;
using Domiki.Web.Infrastructure;
using Domiki.Web.Village;
using System.Diagnostics.CodeAnalysis;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Domiki.Web.Core.Scheduling;

public class Calculator : ICalculator
{
    private const int PoisonThreshold = 10;
    private const int PoisonRetrySeconds = 300;
    private const int MaxEventsPerTick = 100;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Calculator> _logger;
    private List<CalculateInfo>? _datas;
    private Timer t = null!;
    private bool _isInit;
    private CalculateInfo? _failingEvent;
    private int _failingCount;

    public Calculator(IServiceProvider serviceProvider, ILogger<Calculator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void Insert(CalculateInfo cData)
    {
        if (_datas == null)
        {
            Init();
        }

        _logger.LogInformation("Calculator - add data: " + cData.PlayerId + " - " + cData.ObjectId + " - " + cData.Type);
        var index = _datas.FindIndex(x => x.Date > cData.Date);
        if (index == -1)
        {
            _datas.Add(cData);
        }
        else
        {
            _datas.Insert(index, cData);
        }

        MinDateForTest = _datas[0].Date;
    }

    public void Remove(int playerId, long objectId, CalculateTypes type)
    {
        if (_datas == null)
        {
            Init();
        }

        var index = _datas.FindIndex(x => x.ObjectId == objectId && x.Type == type && x.PlayerId == playerId);
        if (index == -1)
        {
            return;
        }

        _logger.LogInformation("Calculator - remove data: " + playerId + " - " + objectId + " - " + type);
        _datas.RemoveAt(index);
        if (index == 0)
        {
            MinDateForTest = _datas.Count > 0 ? _datas[0].Date : null;
        }
    }

    public void Reschedule(int playerId, long objectId, CalculateTypes type, DateTime newDate)
    {
        if (_datas == null)
        {
            Init();
        }

        var index = _datas.FindIndex(x => x.ObjectId == objectId && x.Type == type && x.PlayerId == playerId);
        if (index == -1)
        {
            return;
        }

        var cData = _datas[index];
        _datas.RemoveAt(index);
        cData.Date = newDate;

        var insertIndex = _datas.FindIndex(x => x.Date > cData.Date);
        if (insertIndex == -1)
        {
            _datas.Add(cData);
        }
        else
        {
            _datas.Insert(insertIndex, cData);
        }

        _logger.LogInformation("Calculator - reschedule data: " + playerId + " - " + objectId + " - " + type + " -> " + newDate);
        MinDateForTest = _datas.Count > 0 ? _datas[0].Date : null;
    }

    public void CheckInit()
    {
        //_isInit = true;
        if (_datas == null && !_isInit)
        {
            _isInit = true;
            Init();
            if (_datas.Count > 0)
            {
                MinDateForTest = _datas[0].Date;
            }

            _logger.LogInformation("Calculator - init: count = " + _datas.Count + " , min date = " + MinDateForTest);
        }
    }

    internal void DrainDue(DateTime date, int budget)
    {
        while (MinDateForTest != null && MinDateForTest <= date && budget-- > 0)
        {
            RunDue(date);
        }
    }

    internal void RunDue(DateTime date)
    {
        if (_datas!.Count == 0)
        {
            MinDateForTest = null;
            return;
        }

        var calcDate = _datas[0];
        try
        {
            var startDate = DateTime.Now;
            var result = ProcessEvent(date, calcDate);
            var time = (DateTime.Now - startDate).TotalMilliseconds;
            _logger.LogInformation("Calculator - tick success: " + calcDate.PlayerId + " - " + calcDate.ObjectId + " - " + calcDate.Type + " " + time + "ms");
            if (result)
            {
                _datas.Remove(calcDate);
                MinDateForTest = _datas.Count > 0 ? _datas[0].Date : null;
                _logger.LogInformation("Calculator - tick remove data: " + calcDate.PlayerId + " - " + calcDate.ObjectId + " - " + calcDate.Type);
            }

            _failingEvent = null;
            _failingCount = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Calculator - tick trable: " + calcDate.PlayerId + " - " + calcDate.ObjectId + " - " + calcDate.Type + " | message: " + ex.Message);
            if (ReferenceEquals(_failingEvent, calcDate))
            {
                _failingCount++;
            }
            else
            {
                _failingEvent = calcDate;
                _failingCount = 1;
            }

            if (_failingCount >= PoisonThreshold)
            {
                _datas.RemoveAt(0);
                MinDateForTest = _datas.Count > 0 ? _datas[0].Date : null;
                _failingEvent = null;
                _failingCount = 0;
                calcDate.Date = date.AddSeconds(PoisonRetrySeconds);
                _logger.LogError("Calculator - poison event deferred " + PoisonRetrySeconds + "s: " + calcDate.PlayerId + " - " + calcDate.ObjectId + " - " + calcDate.Type);
                Insert(calcDate);
            }
        }
    }

    internal void SeedForTest(IEnumerable<CalculateInfo> events)
    {
        _datas = events.OrderBy(x => x.Date).ToList();
        MinDateForTest = _datas.Count > 0 ? _datas[0].Date : null;
    }

    protected virtual bool ProcessEvent(DateTime date, CalculateInfo calcDate)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var calculatorTick = scope.ServiceProvider.GetRequiredService<CalculatorTick>();
            var uow = scope.ServiceProvider.GetRequiredService<UnitOfWork>();
            var result = calculatorTick.Calculate(date, calcDate);
            uow.Context.SaveChanges();
            uow.Commit();

            if (result)
            {
                var pushSender = scope.ServiceProvider.GetRequiredService<PushSender>();
                var broker = scope.ServiceProvider.GetRequiredService<GameStateBroker>();
                switch (calcDate.Type)
                {
                    case CalculateTypes.Domiks:
                        pushSender.Notify(calcDate.PlayerId, calcDate.PushTitle ?? "Домики", calcDate.PushBody ?? "Домик достроен – загляни в деревню", "/domiki-page");
                        break;

                    case CalculateTypes.Manufacture:
                        pushSender.Notify(calcDate.PlayerId, calcDate.PushTitle ?? "Домики", calcDate.PushBody ?? "Производство завершено – товары готовы", "/domiki-page");
                        break;

                    case CalculateTypes.Expedition when calcDate.PushBody != null:
                        pushSender.Notify(calcDate.PlayerId, calcDate.PushTitle ?? "Домики", calcDate.PushBody, "/domiki-page");
                        break;

                    case CalculateTypes.Errand when calcDate.PushBody != null:
                        pushSender.Notify(calcDate.PlayerId, calcDate.PushTitle ?? "Домики", calcDate.PushBody, "/domiki-page");
                        break;
                }

                switch (calcDate.Type)
                {
                    case CalculateTypes.Domiks:
                    case CalculateTypes.Manufacture:
                    case CalculateTypes.OrderExpire:
                    case CalculateTypes.Expedition:
                    case CalculateTypes.Errand:
                        broker.Publish(calcDate.PlayerId, GameStateScopes.State);
                        break;

                    case CalculateTypes.TradeLotExpire:
                        broker.Publish(calcDate.PlayerId, GameStateScopes.State);
                        broker.Broadcast(GameStateScopes.Market);
                        break;

                    case CalculateTypes.WeatherRotation:
                        broker.Broadcast(GameStateScopes.State);
                        break;
                }
            }

            return result;
        }
    }

    private void Tick(object? sender, ElapsedEventArgs e)
    {
        if (MinDateForTest == null)
        {
            return;
        }

        var date = DateTimeHelper.GetNowDate();
        if (MinDateForTest > date)
        {
            return;
        }

        t.Stop();
        try
        {
            DrainDue(date, MaxEventsPerTick);
        }
        finally
        {
            t.Start();
        }
    }

    [MemberNotNull(nameof(_datas))]
    private void Init()
    {
        _datas = GetCalculateDates();

        var th = new Thread(Execute);
        th.Start();
    }

    private void Execute()
    {
        t = new();
        t.Interval = 25;
        t.Elapsed += Tick;
        t.Start();
    }

    private List<CalculateInfo> GetCalculateDates()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var vasv = scope.ServiceProvider.GetRequiredService<CalculatorTick>();
            var uow = scope.ServiceProvider.GetRequiredService<UnitOfWork>();
            var dates = new List<CalculateInfo>();
            var dbDomiks = uow.Context.Domiks.Where(s => s.UpgradeSeconds != null).ToList();
            foreach (var dbStorage in dbDomiks)
            {
                var compliteDate = dbStorage.UpgradeCalculateDate!.Value.AddSeconds(dbStorage.UpgradeSeconds!.Value);
                dates.Add(new()
                {
                    PlayerId = dbStorage.PlayerId,
                    ObjectId = dbStorage.Id,
                    Date = compliteDate,
                    Type = CalculateTypes.Domiks,
                });
            }

            var dbManufactures = uow.Context.Manufactures.ToList();
            foreach (var dbManufacture in dbManufactures)
            {
                dates.Add(new()
                {
                    PlayerId = dbManufacture.DomikPlayerId,
                    ObjectId = dbManufacture.Id,
                    Date = dbManufacture.FinishDate,
                    Type = CalculateTypes.Manufacture,
                });
            }

            var dbOrders = uow.Context.Orders.ToList();
            foreach (var dbOrder in dbOrders)
            {
                dates.Add(new()
                {
                    PlayerId = dbOrder.PlayerId,
                    ObjectId = dbOrder.Id,
                    Date = dbOrder.ExpireDate,
                    Type = CalculateTypes.OrderExpire,
                });
            }

            var dbExpeditions = uow.Context.Expeditions.ToList();
            foreach (var dbExpedition in dbExpeditions)
            {
                dates.Add(new()
                {
                    PlayerId = dbExpedition.PlayerId,
                    ObjectId = dbExpedition.Id,
                    Date = dbExpedition.FinishDate,
                    Type = CalculateTypes.Expedition,
                });
            }

            var dbTradeLots = uow.Context.TradeLots.ToList();
            foreach (var dbTradeLot in dbTradeLots)
            {
                dates.Add(new()
                {
                    PlayerId = dbTradeLot.SellerId,
                    ObjectId = dbTradeLot.Id,
                    Date = dbTradeLot.ExpireDate,
                    Type = CalculateTypes.TradeLotExpire,
                });
            }

            var dbErrands = uow.Context.Errands.Where(x => x.ResolvedDate == null).ToList();
            foreach (var dbErrand in dbErrands)
            {
                dates.Add(dbErrand.AcceptDate == null
                    ? new()
                    {
                        PlayerId = dbErrand.PlayerId,
                        ObjectId = dbErrand.Id,
                        Date = dbErrand.ExpireDate,
                        Type = CalculateTypes.Errand,
                        PushBody = null,
                    }
                    : new()
                    {
                        PlayerId = dbErrand.PlayerId,
                        ObjectId = dbErrand.Id,
                        Date = dbErrand.FinishDate!.Value,
                        Type = CalculateTypes.Errand,
                        PushBody = ErrandManager.ErrandResolvedPushBody,
                    });
            }

            var now = DateTimeHelper.GetNowDate();
            var weatherManager = scope.ServiceProvider.GetRequiredService<WeatherManager>();
            weatherManager.EnsureWeatherSchedule(now);
            var currentWeatherPeriod = uow.Context.WeatherPeriods.Single(x => x.StartDate <= now && now < x.EndDate);
            dates.Add(new()
            {
                PlayerId = 0,
                ObjectId = currentWeatherPeriod.Id,
                Date = currentWeatherPeriod.EndDate,
                Type = CalculateTypes.WeatherRotation,
            });

            uow.Commit();

            dates = dates.OrderBy(x => x.Date).ToList();
            return dates;
        }
    }

    internal IReadOnlyList<CalculateInfo> PendingForTest => _datas!;

    internal DateTime? MinDateForTest { get; private set; }
}
