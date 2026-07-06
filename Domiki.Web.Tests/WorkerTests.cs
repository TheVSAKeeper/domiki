using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    public class WorkerTests : TestBase
    {
        [Test]
        public void GetWorkersMatchesBedCapacityTest()
        {
            var playerId = GetPlayerId();

            Assert.That(GetWorkers(playerId).Length, Is.EqualTo(0));

            BuyDomik(playerId, 2);

            var workers = GetWorkers(playerId);
            Assert.That(workers.Length, Is.EqualTo(1));
            Assert.That(workers.Single().ManufactureId, Is.Null);
        }

        [Test]
        public void UpgradeBarracksAddsWorkerTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            Assert.That(GetWorkers(playerId).Length, Is.EqualTo(1));

            UpgradeDomik(playerId, 1);

            Assert.That(GetWorkers(playerId).Length, Is.EqualTo(2));
        }

        [Test]
        public void StartManufactureAssignsAndFinishReleasesWorkerTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 5);

            StartManufacture(playerId, 2, 1, false);

            var busyWorker = GetWorkers(playerId).Single();
            Assert.That(busyWorker.ManufactureId, Is.Not.Null);

            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            FinishManufacture(playerId, manufacture.Id, manufacture.FinishDate.AddSeconds(1));

            Assert.That(GetWorkers(playerId).Single().ManufactureId, Is.Null);
        }

        [Test]
        public void StartManufactureWithoutFreeWorkersThrowsTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 5);
            BuyDomik(playerId, 5);
            StartManufacture(playerId, 2, 1, false);

            var ex = Assert.Throws<BusinessException>(() => StartManufacture(playerId, 3, 1, false));
            Assert.That(ex.Message, Is.EqualTo("Недостаточно трудяг"));
        }

        [Test]
        public void WorkerTraitShortensManufactureDurationTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 5);
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, 3);

            var start = DateTimeHelper.GetNowDate();
            StartManufacture(playerId, 2, 14, false);

            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            Assert.That((manufacture.FinishDate - start).TotalSeconds, Is.EqualTo(23040).Within(2));
        }

        [Test]
        public void GroupRecipeAssignsReceiptPlodderCountWorkersTest()
        {
            var playerId = GetPlayerId();
            for (var i = 0; i < 5; i++)
            {
                BuyDomik(playerId, 2);
            }
            BuyDomik(playerId, 5);
            UpgradeDomik(playerId, 6);

            StartManufacture(playerId, 6, 2, false);

            var manufacture = GetDomiks(playerId).First(x => x.Id == 6).Manufactures.Single();
            var workers = GetWorkers(playerId);
            Assert.That(workers.Count(x => x.ManufactureId == manufacture.Id), Is.EqualTo(5));
            Assert.That(workers.Count(x => x.ManufactureId == null), Is.EqualTo(0));
        }

        [Test]
        public void ConcurrentGetWorkersDoesNotThrowTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            Assert.That(GetWorkers(playerId).Length, Is.EqualTo(1));

            var errorCount = 0;
            Parallel.ForEach(Enumerable.Range(0, 16), _ =>
            {
                try
                {
                    Assert.That(GetWorkers(playerId).Length, Is.EqualTo(1));
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref errorCount);
                }
            });

            Assert.That(errorCount, Is.EqualTo(0));
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

        private void BuyDomik(int playerId, int domikTypeId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                domikManager.BuyDomik(playerId, domikTypeId);
                uow.Commit();
            }
        }

        private void UpgradeDomik(int playerId, int domikId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                domikManager.UpgradeDomik(playerId, domikId);
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

        private void SetWorkerTrait(int workerId, int traitId)
        {
            using (var uow = GetUow())
            {
                var worker = uow.Context.Workers.Single(x => x.Id == workerId);
                worker.TraitId = traitId;
                uow.Commit();
            }
        }
    }
}
