namespace Domiki.Web.Business.Core
{
    public class CalculatorTick
    {
        private DomikManager _domikManager;
        private OrderManager _orderManager;

        public CalculatorTick(DomikManager domikManager, OrderManager orderManager)
        {
            _domikManager = domikManager;
            _orderManager = orderManager;
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
            }
            return false;
        }
    }
}
