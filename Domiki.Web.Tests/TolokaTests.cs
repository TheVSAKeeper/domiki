using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    [NonParallelizable]
    public class TolokaTests : TestBase
    {
        private const int BridgeTolokaTypeId = 1;
        private const int GranaryTolokaTypeId = 2;
        private const int StoneResourceTypeId = 2;
        private const int ClayMineDomikTypeId = 5;
        private const int ClayDig8hReceiptId = 14;
        private const int RainWeatherTypeId = 2;
        private const int ClearWeatherTypeId = 1;
        private const int FountainDecorTypeId = 4;
        private const int GatheringDomikTypeId = 10;

        [SetUp]
        public void SetUp()
        {
            ResetToloka();
            ClearWeatherSchedule();
        }

        [TearDown]
        public void TearDown()
        {
            ResetToloka();
            ClearWeatherSchedule();
        }

        [Test]
        public void GetTolokaForNewPlayerReturnsNullTest()
        {
            var playerId = GetPlayerId();

            var toloka = GetToloka(playerId);

            Assert.That(toloka, Is.Null);
        }

        [Test]
        public void GetTolokaWithBuildingReturnsActiveTest()
        {
            var playerId = GetUnlockedPlayerId();

            var toloka = GetToloka(playerId);

            Assert.That(toloka.Active, Is.Not.Null);
            Assert.That(toloka.Active.TolokaType.LogicName, Is.EqualTo("bridge"));
            Assert.That(toloka.MyContribution, Is.EqualTo(0));
        }

        [TestCase(1, 8)]
        [TestCase(2, 10)]
        [TestCase(5, 16)]
        public void GatheringLevelControlsBuffHoursTest(int level, int expectedHours)
        {
            var playerId = GetUnlockedPlayerId();
            SetGatheringLevel(playerId, level);

            var state = GetToloka(playerId)!;

            Assert.That(state.BuffHours, Is.EqualTo(expectedHours));
            Assert.That(state.NextBuffHours, Is.EqualTo(level < 5 ? expectedHours + 2 : (int?)null));
        }

        [Test]
        public void ContributeWithoutBuildingThrowsAndDoesNotChangeTolokaTest()
        {
            var playerId = GetPlayerId();
            GrantResource(playerId, StoneResourceTypeId, 100);

            var ex = Assert.Throws<BusinessException>(() => Contribute(playerId, 50));

            Assert.That(ex.Message, Is.EqualTo("Нужна Сходня"));
            Assert.That(GetActiveToloka().Collected, Is.EqualTo(0));
            Assert.That(GetResources(playerId).Single(x => x.Type.Id == StoneResourceTypeId).Value, Is.EqualTo(100));
        }

        [Test]
        public void ContributeWritesOffResourceAndIncreasesCollectedAndMineTest()
        {
            var playerId = GetUnlockedPlayerId();
            GrantResource(playerId, StoneResourceTypeId, 100);

            Contribute(playerId, 40);

            var toloka = GetToloka(playerId);
            Assert.That(toloka.Active.Collected, Is.EqualTo(40));
            Assert.That(toloka.MyContribution, Is.EqualTo(40));
            Assert.That(GetResources(playerId).Single(x => x.Type.Id == StoneResourceTypeId).Value, Is.EqualTo(60));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ContributeInvalidAmountThrowsAndDoesNotChangeTolokaTest(int amount)
        {
            var playerId = GetUnlockedPlayerId();
            GrantResource(playerId, StoneResourceTypeId, 100);

            var ex = Assert.Throws<BusinessException>(() => Contribute(playerId, amount));

            Assert.That(ex.Message, Is.EqualTo("Неверное количество"));
            Assert.That(GetActiveToloka().Collected, Is.EqualTo(0));
        }

        [Test]
        public void ContributeWithoutResourcesThrowsAndDoesNotChangeTolokaTest()
        {
            var playerId = GetUnlockedPlayerId();

            var ex = Assert.Throws<BusinessException>(() => Contribute(playerId, 50));

            Assert.That(ex.Message, Does.StartWith("Недостаточно "));
            Assert.That(GetActiveToloka().Collected, Is.EqualTo(0));
        }

        [Test]
        public void ContributeCompletesTolokaAndSeedsNextActiveTest()
        {
            var playerId = GetUnlockedPlayerId();
            GrantResource(playerId, StoneResourceTypeId, 100);
            SetActiveTolokaCollected(1960);
            var activeId = GetActiveToloka().Id;

            Contribute(playerId, 50);

            using (var uow = GetUow())
            {
                var completed = uow.Context.Tolokas.Single(x => x.Id == activeId);
                Assert.That(completed.CompletedDate, Is.Not.Null);
                Assert.That(completed.Collected, Is.EqualTo(2010));
                Assert.That(uow.Context.Tolokas.Count(x => x.CompletedDate == null), Is.EqualTo(1));
                Assert.That(uow.Context.TolokaContributions.Single(x => x.TolokaId == activeId && x.PlayerId == playerId).Value, Is.EqualTo(50));
            }
        }

        [Test]
        public void StartManufactureAppliesTolokaBuffInsideWindowTest()
        {
            var playerId = GetPlayerId();
            CompleteTolokaWithContribution(playerId, DateTimeHelper.GetNowDate());
            BuyDomik(playerId, 2);
            BuyDomik(playerId, ClayMineDomikTypeId);
            SetWeather(ClearWeatherTypeId);

            StartManufacture(playerId, 2, ClayDig8hReceiptId);

            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            Assert.That(GetManufactureOutputPercent(manufacture.Id), Is.EqualTo(100 + TolokaManager.TolokaBuffPercent));
        }

        [Test]
        public void StartManufactureDoesNotApplyTolokaBuffOutsideWindowTest()
        {
            var playerId = GetPlayerId();
            CompleteTolokaWithContribution(playerId, DateTimeHelper.GetNowDate().AddHours(-9));
            BuyDomik(playerId, 2);
            BuyDomik(playerId, ClayMineDomikTypeId);
            SetWeather(ClearWeatherTypeId);

            StartManufacture(playerId, 2, ClayDig8hReceiptId);

            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            Assert.That(GetManufactureOutputPercent(manufacture.Id), Is.EqualTo(100));
        }

        [Test]
        public void StartManufactureStacksTolokaBuffWithWeatherTest()
        {
            var playerId = GetPlayerId();
            CompleteTolokaWithContribution(playerId, DateTimeHelper.GetNowDate());
            BuyDomik(playerId, 2);
            BuyDomik(playerId, ClayMineDomikTypeId);
            SetWeather(RainWeatherTypeId);

            StartManufacture(playerId, 2, ClayDig8hReceiptId);

            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            Assert.That(GetManufactureOutputPercent(manufacture.Id), Is.EqualTo(188));
        }

        [Test]
        public void TwoPlayersContributeToOneTolokaAndBothGetBuffTest()
        {
            var firstPlayerId = GetUnlockedPlayerId();
            var secondPlayerId = GetUnlockedPlayerId();
            GrantResource(firstPlayerId, StoneResourceTypeId, 1000);
            GrantResource(secondPlayerId, StoneResourceTypeId, 1000);
            SetActiveTolokaCollected(1900);

            Contribute(firstPlayerId, 50);
            Contribute(secondPlayerId, 50);

            using (var uow = GetUow())
            {
                var completed = uow.Context.Tolokas.Single(x => x.CompletedDate != null);
                Assert.That(completed.Collected, Is.EqualTo(2000));
            }
            Assert.That(HasActiveBuff(firstPlayerId), Is.True);
            Assert.That(HasActiveBuff(secondPlayerId), Is.True);
        }

        [Test]
        public async Task ConcurrentContributesKeepExactCollectedSumTest()
        {
            var firstPlayerId = GetUnlockedPlayerId();
            var secondPlayerId = GetUnlockedPlayerId();
            GrantResource(firstPlayerId, StoneResourceTypeId, 1000);
            GrantResource(secondPlayerId, StoneResourceTypeId, 1000);

            await Task.WhenAll(
                Task.Run(() => Contribute(firstPlayerId, 70)),
                Task.Run(() => Contribute(secondPlayerId, 80)));

            Assert.That(GetActiveToloka().Collected, Is.EqualTo(150));
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

        private int GetUnlockedPlayerId()
        {
            var playerId = GetPlayerId();
            GrantDecor(playerId, FountainDecorTypeId, 3);
            BuyDomik(playerId, GatheringDomikTypeId);
            return playerId;
        }

        private TolokaState? GetToloka(int playerId)
        {
            using (var uow = GetUow())
            {
                var manager = GetTolokaManager(uow);
                var toloka = manager.GetToloka(DateTimeHelper.GetNowDate(), playerId);
                uow.Commit();
                return toloka;
            }
        }

        private void Contribute(int playerId, int amount)
        {
            using (var uow = GetUow())
            {
                var manager = GetTolokaManager(uow);
                manager.Contribute(playerId, amount, DateTimeHelper.GetNowDate());
                uow.Commit();
            }
        }

        private bool HasActiveBuff(int playerId)
        {
            using (var uow = GetUow())
            {
                var manager = GetTolokaManager(uow);
                var result = manager.HasActiveBuff(playerId, DateTimeHelper.GetNowDate());
                uow.Commit();
                return result;
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

        private void SetGatheringLevel(int playerId, int level)
        {
            using (var uow = GetUow())
            {
                uow.Context.Domiks.Single(x => x.PlayerId == playerId && x.TypeId == GatheringDomikTypeId).Level = level;
                uow.Commit();
            }
        }

        private void StartManufacture(int playerId, int domikId, int receiptId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow, false);
                domikManager.StartManufacture(playerId, domikId, receiptId);
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

        private void GrantResource(int playerId, int resourceTypeId, int value)
        {
            using (var uow = GetUow())
            {
                var resource = uow.Context.Resources.SingleOrDefault(x => x.PlayerId == playerId && x.TypeId == resourceTypeId);
                if (resource == null)
                {
                    resource = new Domiki.Web.Data.Resource { PlayerId = playerId, TypeId = resourceTypeId };
                    uow.Context.Resources.Add(resource);
                }

                resource.Value += value;
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private void GrantDecor(int playerId, int decorTypeId, int count)
        {
            using (var uow = GetUow())
            {
                var decor = uow.Context.PlayerDecors.SingleOrDefault(x => x.PlayerId == playerId && x.DecorTypeId == decorTypeId);
                if (decor == null)
                {
                    decor = new Domiki.Web.Data.PlayerDecor { PlayerId = playerId, DecorTypeId = decorTypeId };
                    uow.Context.PlayerDecors.Add(decor);
                }

                decor.Count += count;
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private Domiki.Web.Data.Toloka GetActiveToloka()
        {
            using (var uow = GetUow())
            {
                return uow.Context.Tolokas.Single(x => x.CompletedDate == null);
            }
        }

        private void SetActiveTolokaCollected(int collected)
        {
            using (var uow = GetUow())
            {
                var toloka = uow.Context.Tolokas.Single(x => x.CompletedDate == null);
                toloka.Collected = collected;
                uow.Commit();
            }
        }

        private void CompleteTolokaWithContribution(int playerId, DateTime completedDate)
        {
            using (var uow = GetUow())
            {
                var active = uow.Context.Tolokas.Single(x => x.CompletedDate == null);
                active.Collected = 2000;
                active.CompletedDate = completedDate;
                uow.Context.TolokaContributions.Add(new Domiki.Web.Data.TolokaContribution
                {
                    TolokaId = active.Id,
                    PlayerId = playerId,
                    Value = 1,
                });
                uow.Context.Tolokas.Add(new Domiki.Web.Data.Toloka
                {
                    TolokaTypeId = GranaryTolokaTypeId,
                    Collected = 0,
                    StartDate = completedDate,
                    CompletedDate = null,
                });
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private void ResetToloka()
        {
            using (var uow = GetUow())
            {
                uow.Context.TolokaContributions.RemoveRange(uow.Context.TolokaContributions);
                uow.Context.Tolokas.RemoveRange(uow.Context.Tolokas);
                uow.Context.SaveChanges();
                uow.Context.Tolokas.Add(new Domiki.Web.Data.Toloka
                {
                    TolokaTypeId = BridgeTolokaTypeId,
                    Collected = 0,
                    StartDate = DateTimeHelper.GetNowDate(),
                    CompletedDate = null,
                });
                uow.Context.SaveChanges();
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
    }
}
