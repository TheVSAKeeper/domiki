using Domiki.Web.Activities;
using Domiki.Web.Economy;
using Domiki.Web.Village;

namespace Domiki.Web.Core.Scheduling;

public class CalculatorTick
{
    private readonly DomikManager _domikManager;
    private readonly OrderManager _orderManager;
    private readonly WeatherManager _weatherManager;
    private readonly ExpeditionManager _expeditionManager;
    private readonly MarketManager _marketManager;

    public CalculatorTick(DomikManager domikManager, OrderManager orderManager, WeatherManager weatherManager, ExpeditionManager expeditionManager, MarketManager marketManager)
    {
        _domikManager = domikManager;
        _orderManager = orderManager;
        _weatherManager = weatherManager;
        _expeditionManager = expeditionManager;
        _marketManager = marketManager;
    }

    public bool Calculate(DateTime date, CalculateInfo calcInfo)
    {
        switch (calcInfo.Type)
        {
            case CalculateTypes.Domiks:
                return _domikManager.FinishDomik(date, calcInfo);

            case CalculateTypes.Manufacture:
                return _domikManager.FinishManufacture(date, calcInfo);

            case CalculateTypes.OrderExpire:
                return _orderManager.FinishOrder(date, calcInfo);

            case CalculateTypes.WeatherRotation:
                return _weatherManager.RotateWeather(date);

            case CalculateTypes.Expedition:
                return _expeditionManager.FinishExpedition(date, calcInfo);

            case CalculateTypes.TradeLotExpire:
                return _marketManager.FinishTradeLot(date, calcInfo);
        }

        return false;
    }
}
