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

        [Test]
        public void FinishManufactureIncrementsWorkerSkillUsesTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 5);

            StartManufacture(playerId, 2, 1, false);
            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            FinishManufacture(playerId, manufacture.Id, manufacture.FinishDate.AddSeconds(1));

            var skill = GetWorkers(playerId).Single().Skills.Single();
            Assert.That(skill.DomikTypeId, Is.EqualTo(5));
            Assert.That(skill.Uses, Is.EqualTo(1));
            Assert.That(skill.BonusPercent, Is.EqualTo(WorkerSkillCalculator.GetBonusPercent(1)));
        }

        [Test]
        public void WorkerSkillShortensManufactureDurationTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 5);
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, 1);
            SetWorkerSkill(worker.Id, 5, 10);

            var start = DateTimeHelper.GetNowDate();
            StartManufacture(playerId, 2, 14, false);

            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            Assert.That((manufacture.FinishDate - start).TotalSeconds, Is.EqualTo(26208).Within(2));
        }

        [Test]
        public void WorkerSkillBonusHasCapTest()
        {
            Assert.That(WorkerSkillCalculator.GetBonusPercent(10000), Is.EqualTo(15));
        }

        [Test]
        public void WorkerSkillBonusHasDiminishingReturnsTest()
        {
            var earlyGain = WorkerSkillCalculator.GetBonusPercent(2) - WorkerSkillCalculator.GetBonusPercent(1);
            var lateGain = WorkerSkillCalculator.GetBonusPercent(41) - WorkerSkillCalculator.GetBonusPercent(40);

            Assert.That(earlyGain, Is.GreaterThan(lateGain));
        }

        [Test]
        public void WorkerSkillForOtherDomikTypeDoesNotShortenManufactureDurationTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 6);
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, 1);
            SetWorkerSkill(worker.Id, 5, 10);

            var start = DateTimeHelper.GetNowDate();
            StartManufacture(playerId, 2, 16, false);

            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            Assert.That((manufacture.FinishDate - start).TotalSeconds, Is.EqualTo(28800).Within(2));
        }

        [Test]
        public void WorkerTraitAndSkillStackMultiplicativelyTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 5);
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, 3);
            SetWorkerSkill(worker.Id, 5, 10);

            var start = DateTimeHelper.GetNowDate();
            StartManufacture(playerId, 2, 14, false);

            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            Assert.That((manufacture.FinishDate - start).TotalSeconds, Is.EqualTo(20967).Within(2));
        }


        [TestCase(25, 60)]
        public void FinishManufactureAccumulatesWorkerWorkedSecondsTest(int receiptId, int expectedWorkedSeconds)
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 7);
            GrantResource(playerId, 6, 1);
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, 1);

            StartManufacture(playerId, 2, receiptId, true);

            worker = GetWorkers(playerId).Single();
            Assert.That(worker.WorkedSeconds, Is.EqualTo(expectedWorkedSeconds));
            Assert.That(worker.RestUntil, Is.Null);
        }

        [TestCase(14)]
        public void FinishManufactureFatigueThresholdSendsWorkerToRestTest(int receiptId)
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 5);
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, 1);

            StartManufacture(playerId, 2, receiptId, true);

            worker = GetWorkers(playerId).Single();
            Assert.That(worker.WorkedSeconds, Is.EqualTo(0));
            Assert.That(worker.RestUntil, Is.Not.Null);
            Assert.That(worker.RestUntil, Is.GreaterThan(DateTimeHelper.GetNowDate()));
        }

        [TestCase(14)]
        public void RestingWorkerDoesNotStartManufactureTest(int receiptId)
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 5);
            var worker = GetWorkers(playerId).Single();
            SetWorkerRest(worker.Id, DateTimeHelper.GetNowDate().AddHours(1));

            var ex = Assert.Throws<BusinessException>(() => StartManufacture(playerId, 2, receiptId, false));
            Assert.That(ex.Message, Is.EqualTo("Недостаточно трудяг"));
        }

        [TestCase(14)]
        public void WorkerWithExpiredRestUntilStartsManufactureTest(int receiptId)
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 5);
            var worker = GetWorkers(playerId).Single();
            SetWorkerRest(worker.Id, DateTimeHelper.GetNowDate().AddSeconds(-1));

            StartManufacture(playerId, 2, receiptId, false);

            worker = GetWorkers(playerId).Single();
            Assert.That(worker.ManufactureId, Is.Not.Null);
        }

        [TestCase(18)]
        public void SonyaWorkerDoesNotAccumulateFatigueTest(int receiptId)
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 5);
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, 4);
            SetWorkerWorked(worker.Id, 0);

            StartManufacture(playerId, 2, receiptId, true);

            worker = GetWorkers(playerId).Single();
            Assert.That(worker.WorkedSeconds, Is.EqualTo(0));
            Assert.That(worker.RestUntil, Is.Null);
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

        private void GrantResource(int playerId, int resourceTypeId, int value)
        {
            using (var uow = GetUow())
            {
                var resource = uow.Context.Resources.SingleOrDefault(x => x.PlayerId == playerId && x.TypeId == resourceTypeId);
                if (resource == null)
                {
                    resource = new Domiki.Web.Data.Resource
                    {
                        PlayerId = playerId,
                        TypeId = resourceTypeId,
                    };
                    uow.Context.Resources.Add(resource);
                }

                resource.Value += value;
                uow.Context.SaveChanges();
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

        private void SetWorkerWorked(int workerId, int workedSeconds)
        {
            using (var uow = GetUow())
            {
                var worker = uow.Context.Workers.Single(x => x.Id == workerId);
                worker.WorkedSeconds = workedSeconds;
                uow.Commit();
            }
        }

        private void SetWorkerRest(int workerId, DateTime? restUntil)
        {
            using (var uow = GetUow())
            {
                var worker = uow.Context.Workers.Single(x => x.Id == workerId);
                worker.RestUntil = restUntil;
                uow.Commit();
            }
        }

        private void SetWorkerSkill(int workerId, int domikTypeId, int uses)
        {
            using (var uow = GetUow())
            {
                var skill = uow.Context.WorkerSkills.SingleOrDefault(x => x.WorkerId == workerId && x.DomikTypeId == domikTypeId);
                if (skill == null)
                {
                    uow.Context.WorkerSkills.Add(new Domiki.Web.Data.WorkerSkill
                    {
                        WorkerId = workerId,
                        DomikTypeId = domikTypeId,
                        Uses = uses,
                    });
                }
                else
                {
                    skill.Uses = uses;
                }

                uow.Commit();
            }
        }
    }
}
