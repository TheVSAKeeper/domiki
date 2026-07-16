using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    [NonParallelizable]
    public class MedicineTests : TestBase
    {
        [TearDown]
        public void TearDown()
        {
            ClearWeatherSchedule();
        }

        private const int ClearWeatherTypeId = 1;
        private const int RainWeatherTypeId = 2;
        private const int ClayMineDomikTypeId = 5;
        private const int LumberMillDomikTypeId = 6;
        private const int ClayDig8hReceiptId = 14;
        private const int WoodDig8hReceiptId = 16;
        private const int OrdinaryTraitId = 1;
        private const int SonyaTraitId = 4;
        private const int HardyTraitId = 6;

        [TestCase(RainWeatherTypeId, ClayMineDomikTypeId, ClayDig8hReceiptId, true, DomikManager.SickChancePercent)]
        [TestCase(ClearWeatherTypeId, ClayMineDomikTypeId, ClayDig8hReceiptId, true, 0)]
        [TestCase(RainWeatherTypeId, LumberMillDomikTypeId, WoodDig8hReceiptId, true, 0)]
        [TestCase(RainWeatherTypeId, ClayMineDomikTypeId, ClayDig8hReceiptId, false, 0)]
        public void StartManufactureFixesSickChanceFromWeatherAndVillageTest(int weatherTypeId, int domikTypeId, int receiptId, bool highVillage, int expectedSickChance)
        {
            var playerId = GetPlayerId();
            GrantDomik(playerId, 20, domikTypeId, 1);
            if (highVillage)
            {
                RaiseVillageToSickGate(playerId);
            }
            else
            {
                Assert.That(GetVillageLevel(playerId), Is.LessThan(DomikManager.SickMinVillageLevel));
            }
            SetWeather(weatherTypeId);

            StartManufacture(playerId, 20, receiptId, false);

            var manufacture = GetDomiks(playerId).First(x => x.Id == 20).Manufactures.Single();
            Assert.That(GetManufactureSickChance(manufacture.Id), Is.EqualTo(expectedSickChance));
        }

        [Test]
        public void SickChanceStaysFixedAtStartWhenWeatherChangesTest()
        {
            var playerId = GetPlayerId();
            GrantDomik(playerId, 20, ClayMineDomikTypeId, 1);
            RaiseVillageToSickGate(playerId);
            SetWeather(RainWeatherTypeId);

            StartManufacture(playerId, 20, ClayDig8hReceiptId, false);
            var manufacture = GetDomiks(playerId).First(x => x.Id == 20).Manufactures.Single();

            SetWeather(ClearWeatherTypeId);

            Assert.That(GetManufactureSickChance(manufacture.Id), Is.EqualTo(DomikManager.SickChancePercent));
        }

        [TestCase(100, true)]
        [TestCase(0, false)]
        public void FinishManufactureRollSendsWorkerToSickTest(int sickChance, bool expectSick)
        {
            var playerId = GetPlayerId();
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, OrdinaryTraitId);

            StartManufacture(playerId, 2, ClayDig8hReceiptId, false);
            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            SetManufactureSickChance(manufacture.Id, sickChance);
            var finishDate = manufacture.FinishDate.AddSeconds(1);
            FinishManufacture(playerId, manufacture.Id, finishDate);

            worker = GetWorkers(playerId).Single();
            if (expectSick)
            {
                Assert.That(worker.SickUntil, Is.Not.Null);
                Assert.That(worker.RestUntil, Is.EqualTo(worker.SickUntil));
                Assert.That((worker.SickUntil.Value - finishDate).TotalSeconds, Is.EqualTo(DomikManager.SickDurationSeconds).Within(2));
            }
            else
            {
                Assert.That(worker.SickUntil, Is.Null);
            }
        }

        [TestCase(-1, false)]
        [TestCase(-25, true)]
        public void FinishManufactureRespectsSickImmunityTest(int priorSickOffsetHours, bool expectResick)
        {
            var playerId = GetPlayerId();
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, OrdinaryTraitId);
            var priorSickUntil = DateTimeHelper.GetNowDate().AddHours(priorSickOffsetHours);
            SetWorkerSick(worker.Id, priorSickUntil);

            StartManufacture(playerId, 2, ClayDig8hReceiptId, false);
            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            SetManufactureSickChance(manufacture.Id, 100);
            var finishDate = manufacture.FinishDate.AddSeconds(1);
            FinishManufacture(playerId, manufacture.Id, finishDate);

            worker = GetWorkers(playerId).Single();
            if (expectResick)
            {
                Assert.That(worker.SickUntil, Is.GreaterThan(finishDate));
            }
            else
            {
                Assert.That(worker.SickUntil, Is.EqualTo(priorSickUntil));
            }
        }

        [TestCase(SonyaTraitId)]
        [TestCase(HardyTraitId)]
        public void ImmuneTraitsDoNotGetSickTest(int traitId)
        {
            var playerId = GetPlayerId();
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, traitId);

            StartManufacture(playerId, 2, ClayDig8hReceiptId, false);
            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            SetManufactureSickChance(manufacture.Id, 100);
            FinishManufacture(playerId, manufacture.Id, manufacture.FinishDate.AddSeconds(1));

            worker = GetWorkers(playerId).Single();
            Assert.That(worker.SickUntil, Is.Null);
        }

        [Test]
        public void FinishManufactureDoesNotExceedMaxSickPerPlayerTest()
        {
            var playerId = GetPlayerId();
            var defaultWorker = GetWorkers(playerId).Single();
            SetWorkerTrait(defaultWorker.Id, OrdinaryTraitId);
            var alreadySickA = InsertWorker(playerId, "Хворый-А");
            var alreadySickB = InsertWorker(playerId, "Хворый-Б");
            var future = DateTimeHelper.GetNowDate().AddHours(24);
            SetWorkerSick(alreadySickA, future);
            SetWorkerSick(alreadySickB, future);

            StartManufacture(playerId, 2, ClayDig8hReceiptId, false, new[] { defaultWorker.Id });
            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            SetManufactureSickChance(manufacture.Id, 100);
            FinishManufacture(playerId, manufacture.Id, manufacture.FinishDate.AddSeconds(1));

            var worker = GetWorkers(playerId).Single(x => x.Id == defaultWorker.Id);
            Assert.That(worker.SickUntil, Is.Null);
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

        private Worker[] GetWorkers(int playerId)
        {
            using (var uow = GetUow())
            {
                var workerManager = GetWorkerManager(uow);
                var workers = workerManager.GetWorkers(playerId).ToArray();
                uow.Commit();
                return workers;
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

        private void StartManufacture(int playerId, int domikId, int receiptId, bool calculatorJustFinishMode, int[]? workerIds = null)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow, calculatorJustFinishMode);
                domikManager.StartManufacture(playerId, domikId, receiptId, workerIds: workerIds);
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

        private int GetManufactureSickChance(int manufactureId)
        {
            using (var uow = GetUow())
            {
                return uow.Context.Manufactures.Single(x => x.Id == manufactureId).SickChance;
            }
        }

        private void SetManufactureSickChance(int manufactureId, int chance)
        {
            using (var uow = GetUow())
            {
                var manufacture = uow.Context.Manufactures.Single(x => x.Id == manufactureId);
                manufacture.SickChance = chance;
                uow.Commit();
            }
        }

        private int GetVillageLevel(int playerId)
        {
            using (var uow = GetUow())
            {
                var calculator = GetVillageLevelCalculator(uow);
                var level = calculator.GetLevel(playerId).Level;
                uow.Commit();
                return level;
            }
        }

        private void RaiseVillageToSickGate(int playerId)
        {
            var nextId = 30;
            while (GetVillageLevel(playerId) < DomikManager.SickMinVillageLevel)
            {
                GrantDomik(playerId, nextId++, ClayMineDomikTypeId, 1);
            }
        }

        private void SetWorkerTrait(int workerId, int traitId)
        {
            using (var uow = GetUow())
            {
                var worker = uow.Context.Workers.Single(x => x.Id == workerId);
                worker.TraitId = traitId;
                uow.Commit();
            }
        }

        private void SetWorkerSick(int workerId, DateTime sickUntil)
        {
            using (var uow = GetUow())
            {
                var worker = uow.Context.Workers.Single(x => x.Id == workerId);
                worker.SickUntil = sickUntil;
                worker.RestUntil = sickUntil;
                uow.Commit();
            }
        }

        private int InsertWorker(int playerId, string name)
        {
            using (var uow = GetUow())
            {
                var worker = new Domiki.Web.Data.Worker { PlayerId = playerId, Name = name, TraitId = OrdinaryTraitId };
                uow.Context.Workers.Add(worker);
                uow.Context.SaveChanges();
                uow.Commit();
                return worker.Id;
            }
        }

        private void SetWeather(int weatherTypeId)
        {
            ClearWeatherSchedule();
            var now = DateTimeHelper.GetNowDate();
            using (var uow = GetUow())
            {
                uow.Context.WeatherPeriods.Add(new Domiki.Web.Data.WeatherPeriod
                {
                    WeatherTypeId = weatherTypeId,
                    StartDate = now,
                    EndDate = now.AddSeconds(WeatherManager.WeatherPeriodSeconds),
                });
                uow.Context.SaveChanges();
                uow.Commit();
            }
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
    }
}
