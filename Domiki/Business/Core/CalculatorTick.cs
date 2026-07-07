namespace Domiki.Web.Business.Core
{
    public class CalculatorTick
    {
        private DomikManager _domikManager;
        private OrderManager _orderManager;
        private WeatherManager _weatherManager;
        private ExpeditionManager _expeditionManager;

        public CalculatorTick(DomikManager domikManager, OrderManager orderManager, WeatherManager weatherManager, ExpeditionManager expeditionManager)
        {
            _domikManager = domikManager;
            _orderManager = orderManager;
            _weatherManager = weatherManager;
            _expeditionManager = expeditionManager;
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
            }
            return false;
        }
    }
}
