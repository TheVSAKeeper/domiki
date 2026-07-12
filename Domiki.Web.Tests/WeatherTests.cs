using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    [NonParallelizable]
    public class WeatherTests : TestBase
    {
        [TearDown]
        public void TearDown()
        {
            ClearWeatherSchedule();
        }

        private const int ClearWeatherTypeId = 1;
        private const int RainWeatherTypeId = 2;
        private const int DroughtWeatherTypeId = 3;
        private const int ClayMineDomikTypeId = 5;
        private const int LumberMillDomikTypeId = 6;
        private const int StoneMineDomikTypeId = 3;
        private const int ClayDig8hReceiptId = 14;
        private const int WoodDig8hReceiptId = 16;
        private const int StoneDig8hReceiptId = 15;
        private const int ClayResourceTypeId = 4;
        private const int WoodResourceTypeId = 3;

        [TestCase(RainWeatherTypeId, ClayMineDomikTypeId, ClayDig8hReceiptId, 150)]
        [TestCase(RainWeatherTypeId, LumberMillDomikTypeId, WoodDig8hReceiptId, 75)]
        [TestCase(DroughtWeatherTypeId, LumberMillDomikTypeId, WoodDig8hReceiptId, 150)]
        [TestCase(DroughtWeatherTypeId, ClayMineDomikTypeId, ClayDig8hReceiptId, 75)]
        public void StartManufactureAppliesWeatherOutputPercentTest(int weatherTypeId, int domikTypeId, int receiptId, int expectedOutputPercent)
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, domikTypeId);
            SetWeather(weatherTypeId);

            StartManufacture(playerId, 2, receiptId, false);

            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            Assert.That(GetManufactureOutputPercent(manufacture.Id), Is.EqualTo(expectedOutputPercent));
        }

        [TestCase(ClearWeatherTypeId, ClayMineDomikTypeId, ClayDig8hReceiptId)]
        [TestCase(ClearWeatherTypeId, LumberMillDomikTypeId, WoodDig8hReceiptId)]
        [TestCase(RainWeatherTypeId, StoneMineDomikTypeId, StoneDig8hReceiptId)]
        [TestCase(DroughtWeatherTypeId, StoneMineDomikTypeId, StoneDig8hReceiptId)]
        public void StartManufactureWithoutWeatherEffectKeepsOutputPercent100Test(int weatherTypeId, int domikTypeId, int receiptId)
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 2);
            BuyDomik(playerId, domikTypeId);
            SetWeather(weatherTypeId);

            StartManufacture(playerId, 3, receiptId, false);

            var manufacture = GetDomiks(playerId).First(x => x.Id == 3).Manufactures.Single();
            Assert.That(GetManufactureOutputPercent(manufacture.Id), Is.EqualTo(100));
        }

        [Test]
        public void FinishManufactureGrantsBonusOutputUnderRainAtClayMineTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, ClayMineDomikTypeId);
            SetWeather(RainWeatherTypeId);

            StartManufacture(playerId, 2, ClayDig8hReceiptId, false);
            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            FinishManufacture(playerId, manufacture.Id, manufacture.FinishDate.AddSeconds(1));

            var clay = GetResources(playerId).First(x => x.Type.Id == ClayResourceTypeId);
            Assert.That(clay.Value, Is.EqualTo(12));
        }

        [Test]
        public void FinishManufactureCutsOutputUnderRainAtLumberMillTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, LumberMillDomikTypeId);
            SetWeather(RainWeatherTypeId);

            StartManufacture(playerId, 2, WoodDig8hReceiptId, false);
            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            FinishManufacture(playerId, manufacture.Id, manufacture.FinishDate.AddSeconds(1));

            var wood = GetResources(playerId).First(x => x.Type.Id == WoodResourceTypeId);
            Assert.That(wood.Value, Is.EqualTo(6));
        }

        [Test]
        public void FinishManufactureMaxGuardPreventsZeroGrantTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, ClayMineDomikTypeId);
            SetWeather(ClearWeatherTypeId);

            StartManufacture(playerId, 2, ClayDig8hReceiptId, false);
            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            SetManufactureOutputPercent(manufacture.Id, 1);

            FinishManufacture(playerId, manufacture.Id, manufacture.FinishDate.AddSeconds(1));

            var clay = GetResources(playerId).First(x => x.Type.Id == ClayResourceTypeId);
            Assert.That(clay.Value, Is.EqualTo(1));
        }

        [Test]
        public void FinishManufactureUsesOutputPercentFixedAtStartNotAtFinishTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, ClayMineDomikTypeId);
            SetWeather(RainWeatherTypeId);

            StartManufacture(playerId, 2, ClayDig8hReceiptId, false);
            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();

            SetWeather(ClearWeatherTypeId);
            FinishManufacture(playerId, manufacture.Id, manufacture.FinishDate.AddSeconds(1));

            var clay = GetResources(playerId).First(x => x.Type.Id == ClayResourceTypeId);
            Assert.That(clay.Value, Is.EqualTo(12));
        }

        [Test]
        public void GetWeatherReturnsCurrentAndContiguousForecastTest()
        {
            ClearWeatherSchedule();
            var now = DateTimeHelper.GetNowDate();
            EnsureWeatherSchedule();

            var weather = GetWeather();

            Assert.That(weather.Current, Is.Not.Null);
            Assert.That(weather.Current.StartDate, Is.LessThanOrEqualTo(now));
            Assert.That(weather.Current.EndDate, Is.GreaterThan(now));
            Assert.That(weather.Forecast.Length, Is.EqualTo(2));
            Assert.That(weather.Forecast[0].StartDate, Is.EqualTo(weather.Current.EndDate));
            Assert.That(weather.Forecast[1].StartDate, Is.EqualTo(weather.Forecast[0].EndDate));
            Assert.That(weather.Forecast[1].EndDate, Is.EqualTo(weather.Current.StartDate.AddSeconds(WeatherManager.ForecastHorizonSeconds)));
        }

        [Test]
        public void EnsureWeatherScheduleFromEmptyCoversForecastHorizonTest()
        {
            ClearWeatherSchedule();
            var now = DateTimeHelper.GetNowDate();

            EnsureWeatherSchedule();

            var periods = GetWeatherPeriods();
            Assert.That(periods.Length, Is.GreaterThan(0));
            Assert.That(periods.First().StartDate, Is.EqualTo(now));
            Assert.That(periods.Last().EndDate, Is.GreaterThanOrEqualTo(now.AddSeconds(WeatherManager.ForecastHorizonSeconds)));
            Assert.That(periods.Any(x => x.StartDate <= now && now < x.EndDate), Is.True);
        }

        [Test]
        public void EnsureWeatherScheduleFromPartialScheduleExtendsTailTest()
        {
            ClearWeatherSchedule();
            var now = DateTimeHelper.GetNowDate();
            InsertWeatherPeriod(ClearWeatherTypeId, now, now.AddSeconds(WeatherManager.WeatherPeriodSeconds));

            EnsureWeatherSchedule();

            var periods = GetWeatherPeriods();
            Assert.That(periods.Last().EndDate, Is.GreaterThanOrEqualTo(now.AddSeconds(WeatherManager.ForecastHorizonSeconds)));
            for (var i = 1; i < periods.Length; i++)
            {
                Assert.That(periods[i].StartDate, Is.EqualTo(periods[i - 1].EndDate));
            }
        }

        [Test]
        public void EnsureWeatherScheduleFromStaleTailContinuesForwardThroughNowTest()
        {
            ClearWeatherSchedule();
            var now = DateTimeHelper.GetNowDate();
            InsertWeatherPeriod(ClearWeatherTypeId, now.AddHours(-16), now.AddHours(-8));

            EnsureWeatherSchedule();

            var periods = GetWeatherPeriods();
            Assert.That(periods.Last().EndDate, Is.GreaterThanOrEqualTo(now.AddSeconds(WeatherManager.ForecastHorizonSeconds)));
            Assert.That(periods.Any(x => x.StartDate <= now && now < x.EndDate), Is.True);
        }

        private int GetPlayerId()
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var playerId = domikManager.GetPlayerId("testUser_" + Guid.NewGuid());
                uow.Commit();
                return playerId;
            }
        }

        private Domik[] GetDomiks(int playerId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var domiks = domikManager.GetDomiks(playerId).ToArray();
                uow.Commit();
                return domiks;
            }
        }

        private Resource[] GetResources(int playerId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var resources = domikManager.GetResources(playerId).ToArray();
                uow.Commit();
                return resources;
            }
        }

        private void BuyDomik(int playerId, int domikTypeId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                domikManager.BuyDomik(playerId, domikTypeId);
                uow.Commit();
            }
        }

        private void StartManufacture(int playerId, int domikId, int receiptId, bool calculatorJustFinishMode)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow, calculatorJustFinishMode);
                domikManager.StartManufacture(playerId, domikId, receiptId);
                uow.Commit();
            }
        }

        private void FinishManufacture(int playerId, int manufactureId, DateTime date)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var result = domikManager.FinishManufacture(date, new CalculateInfo
                {
                    PlayerId = playerId,
                    ObjectId = manufactureId,
                    Date = date,
                    Type = CalculateTypes.Manufacture,
                });
                Assert.That(result, Is.True);
                uow.Commit();
            }
        }

        private int GetManufactureOutputPercent(int manufactureId)
        {
            using (var uow = GetUow())
            {
                return uow.Context.Manufactures.Single(x => x.Id == manufactureId).OutputPercent;
            }
        }

        private void SetManufactureOutputPercent(int manufactureId, int percent)
        {
            using (var uow = GetUow())
            {
                var manufacture = uow.Context.Manufactures.Single(x => x.Id == manufactureId);
                manufacture.OutputPercent = percent;
                uow.Commit();
            }
        }

        private void SetWeather(int weatherTypeId)
        {
            ClearWeatherSchedule();
            var now = DateTimeHelper.GetNowDate();
            InsertWeatherPeriod(weatherTypeId, now, now.AddSeconds(WeatherManager.WeatherPeriodSeconds));
        }

        private void ClearWeatherSchedule()
        {
            using (var uow = GetUow())
            {
                uow.Context.WeatherPeriods.RemoveRange(uow.Context.WeatherPeriods);
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private void InsertWeatherPeriod(int weatherTypeId, DateTime startDate, DateTime endDate)
        {
            using (var uow = GetUow())
            {
                uow.Context.WeatherPeriods.Add(new Domiki.Web.Data.WeatherPeriod
                {
                    WeatherTypeId = weatherTypeId,
                    StartDate = startDate,
                    EndDate = endDate,
                });
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private Domiki.Web.Data.WeatherPeriod[] GetWeatherPeriods()
        {
            using (var uow = GetUow())
            {
                return uow.Context.WeatherPeriods.OrderBy(x => x.StartDate).ToArray();
            }
        }

        private void EnsureWeatherSchedule()
        {
            using (var uow = GetUow())
            {
                var weatherManager = GetWeatherManager(uow);
                weatherManager.EnsureWeatherSchedule(DateTimeHelper.GetNowDate());
                uow.Commit();
            }
        }

        private WeatherState GetWeather()
        {
            using (var uow = GetUow())
            {
                var weatherManager = GetWeatherManager(uow);
                var weather = weatherManager.GetWeather(DateTimeHelper.GetNowDate());
                uow.Commit();
                return weather;
            }
        }
    }
}
