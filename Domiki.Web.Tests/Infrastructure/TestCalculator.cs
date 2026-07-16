using Domiki.Web.Core.Scheduling;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Tests
{
    public class TestCalculator : ICalculator
    {
        private Func<UnitOfWork> _uowFactory;
        private Func<UnitOfWork, CalculatorTick> _calculatorTickFactory;
        /// <summary>
        /// Все события обсчитываются моментально.
        /// </summary>
        private bool _justFinishMode;

        public TestCalculator(Func<UnitOfWork> uowFactory, Func<UnitOfWork, CalculatorTick> calculatorTickFactory, bool justFinishMode = true)
        {
            _uowFactory = uowFactory;
            _calculatorTickFactory = calculatorTickFactory;
            _justFinishMode = justFinishMode;
        }

        public void Insert(CalculateInfo calcDate)
        {
            if (!_justFinishMode)
            {
                return;
            }

            using (var uow = _uowFactory())
            {
                CalculatorTick calculatorTick = _calculatorTickFactory(uow);
                calculatorTick.Calculate(DateTimeHelper.GetNowDate().AddYears(217), calcDate);
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        public void Remove(int playerId, long objectId, CalculateTypes type)
        {
        }

        public void CheckInit()
        {
        }
    }
}
