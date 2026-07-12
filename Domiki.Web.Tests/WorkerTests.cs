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

            Assert.That(GetWorkers(playerId).Length, Is.EqualTo(1));

            GrantDomik(playerId, 3, 2);

            var workers = GetWorkers(playerId);
            Assert.That(workers.Length, Is.EqualTo(2));
            Assert.That(workers.All(x => x.ManufactureId == null), Is.True);
        }

        [Test]
        public void UpgradeBarracksAddsWorkerTest()
        {
            var playerId = GetPlayerId();
            GrantDomik(playerId, 3, 2);
            Assert.That(GetWorkers(playerId).Length, Is.EqualTo(2));

            UpgradeDomik(playerId, 1);

            Assert.That(GetWorkers(playerId).Length, Is.EqualTo(3));
        }

        [Test]
        public void StartManufactureAssignsAndFinishReleasesWorkerTest()
        {
            var playerId = GetPlayerId();

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
            GrantDomik(playerId, 3, 5);
            StartManufacture(playerId, 2, 1, false);

            var ex = Assert.Throws<BusinessException>(() => StartManufacture(playerId, 3, 1, false));
            Assert.That(ex.Message, Is.EqualTo("Недостаточно трудяг"));
        }

        [Test]
        public void WorkerTraitShortensManufactureDurationTest()
        {
            var playerId = GetPlayerId();
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
            GrantResource(playerId, 1, 100);
            for (var i = 0; i < 4; i++)
            {
                GrantDomik(playerId, 3 + i, 2);
            }
            GrantDomik(playerId, 7, 5);
            UpgradeDomik(playerId, 7);

            StartManufacture(playerId, 7, 2, false);

            var manufacture = GetDomiks(playerId).First(x => x.Id == 7).Manufactures.Single();
            var workers = GetWorkers(playerId);
            Assert.That(workers.Count(x => x.ManufactureId == manufacture.Id), Is.EqualTo(5));
            Assert.That(workers.Count(x => x.ManufactureId == null), Is.EqualTo(0));
        }

        [Test]
        public void ConcurrentGetWorkersDoesNotThrowTest()
        {
            var playerId = GetPlayerId();
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
            BuyDomik(playerId, 6);
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, 1);
            SetWorkerSkill(worker.Id, 5, 10);

            var start = DateTimeHelper.GetNowDate();
            StartManufacture(playerId, 3, 16, false);

            var manufacture = GetDomiks(playerId).First(x => x.Id == 3).Manufactures.Single();
            Assert.That((manufacture.FinishDate - start).TotalSeconds, Is.EqualTo(28800).Within(2));
        }

        [Test]
        public void WorkerTraitAndSkillStackMultiplicativelyTest()
        {
            var playerId = GetPlayerId();
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, 3);
            SetWorkerSkill(worker.Id, 5, 10);

            var start = DateTimeHelper.GetNowDate();
            StartManufacture(playerId, 2, 14, false);

            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            Assert.That((manufacture.FinishDate - start).TotalSeconds, Is.EqualTo(20967).Within(2));
        }


        [Test]
        public void TradeManufactureDoesNotAccumulateFatigueTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 7);
            GrantResource(playerId, 6, 1);
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, 1);

            StartManufacture(playerId, 3, 25, true);

            worker = GetWorkers(playerId).Single();
            Assert.That(worker.WorkedSeconds, Is.EqualTo(0));
            Assert.That(worker.RestUntil, Is.Null);
        }

        [Test]
        public void FinishManufactureAccumulatesActualWorkerWorkedSecondsTest()
        {
            var playerId = GetPlayerId();
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, 3);

            StartManufacture(playerId, 2, 14, true);

            worker = GetWorkers(playerId).Single();
            Assert.That(worker.WorkedSeconds, Is.EqualTo(23040));
            Assert.That(worker.RestUntil, Is.Null);
        }

        [TestCase(14)]
        public void FinishManufactureFatigueThresholdSendsWorkerToRestTest(int receiptId)
        {
            var playerId = GetPlayerId();
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
            var worker = GetWorkers(playerId).Single();
            SetWorkerRest(worker.Id, DateTimeHelper.GetNowDate().AddHours(1));

            var ex = Assert.Throws<BusinessException>(() => StartManufacture(playerId, 2, receiptId, false));
            Assert.That(ex.Message, Is.EqualTo("Недостаточно трудяг"));
        }

        [TestCase(14)]
        public void WorkerWithExpiredRestUntilStartsManufactureTest(int receiptId)
        {
            var playerId = GetPlayerId();
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
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, 4);
            SetWorkerWorked(worker.Id, 0);

            StartManufacture(playerId, 2, receiptId, true);

            worker = GetWorkers(playerId).Single();
            Assert.That(worker.WorkedSeconds, Is.EqualTo(0));
            Assert.That(worker.RestUntil, Is.Null);
        }

        [Test]
        public void SonyaTraitLengthensManufactureDurationByTwentyFivePercentTest()
        {
            var playerId = GetPlayerId();
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, 4);

            var start = DateTimeHelper.GetNowDate();
            StartManufacture(playerId, 2, 14, false);

            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            Assert.That((manufacture.FinishDate - start).TotalSeconds, Is.EqualTo(36000).Within(2));
        }

        [Test]
        public void AutoSelectsWorkerWithBestFitnessOverFirstByIdTest()
        {
            var playerId = GetPlayerId();
            GrantDomik(playerId, 3, 2);
            GrantDomik(playerId, 4, 2);
            GrantDomik(playerId, 5, 5);

            var workers = GetWorkers(playerId);
            var weakWorker = workers[0];
            var strongWorker = workers[1];
            SetWorkerTrait(weakWorker.Id, 1);
            SetWorkerTrait(strongWorker.Id, 3);

            StartManufacture(playerId, 5, 1, false);

            var busyWorker = GetWorkers(playerId).Single(x => x.ManufactureId != null);
            Assert.That(busyWorker.Id, Is.EqualTo(strongWorker.Id));
        }

        [Test]
        public void ManualSelectionAssignsExplicitWorkerTest()
        {
            var playerId = GetPlayerId();
            GrantDomik(playerId, 3, 2);
            GrantDomik(playerId, 4, 5);

            var workers = GetWorkers(playerId);
            var weakWorker = workers[0];
            var strongWorker = workers[1];
            SetWorkerTrait(weakWorker.Id, 1);
            SetWorkerTrait(strongWorker.Id, 3);

            StartManufacture(playerId, 4, 1, false, new[] { weakWorker.Id });

            var busyWorker = GetWorkers(playerId).Single(x => x.ManufactureId != null);
            Assert.That(busyWorker.Id, Is.EqualTo(weakWorker.Id));
        }

        [Test]
        public void EmptyManualSelectionFallsBackToAutoTest()
        {
            var playerId = GetPlayerId();

            StartManufacture(playerId, 2, 1, false, Array.Empty<int>());

            var busyWorker = GetWorkers(playerId).Single();
            Assert.That(busyWorker.ManufactureId, Is.Not.Null);
        }

        [Test]
        public void ManualSelectionWithWrongCountThrowsTest()
        {
            var playerId = GetPlayerId();
            GrantDomik(playerId, 3, 2);
            GrantDomik(playerId, 4, 5);

            var workerIds = GetWorkers(playerId).Select(x => x.Id).ToArray();

            var ex = Assert.Throws<BusinessException>(() => StartManufacture(playerId, 4, 1, false, workerIds));
            Assert.That(ex.Message, Is.EqualTo("Неверное число трудяг"));
        }

        [Test]
        public void ManualSelectionWithDuplicateWorkerThrowsTest()
        {
            var playerId = GetPlayerId();
            GrantResource(playerId, 1, 100);
            for (var i = 0; i < 4; i++)
            {
                GrantDomik(playerId, 3 + i, 2);
            }
            GrantDomik(playerId, 7, 5);
            UpgradeDomik(playerId, 7);

            var workerId = GetWorkers(playerId).First().Id;
            var workerIds = Enumerable.Repeat(workerId, 5).ToArray();

            var ex = Assert.Throws<BusinessException>(() => StartManufacture(playerId, 7, 2, false, workerIds));
            Assert.That(ex.Message, Is.EqualTo("Дублирующиеся трудяги"));
        }

        [Test]
        public void ManualSelectionForGroupRecipeReservesExactWorkersTest()
        {
            var playerId = GetPlayerId();
            GrantResource(playerId, 1, 200);
            for (var i = 0; i < 4; i++)
            {
                GrantDomik(playerId, 3 + i, 2);
            }
            UpgradeDomik(playerId, 1);
            GrantDomik(playerId, 7, 5);
            UpgradeDomik(playerId, 7);

            var workers = GetWorkers(playerId);
            var chosen = workers.Take(5).Select(x => x.Id).ToArray();
            var excludedWorkerId = workers[5].Id;

            StartManufacture(playerId, 7, 2, false, chosen);

            var updatedWorkers = GetWorkers(playerId);
            Assert.That(updatedWorkers.Where(x => chosen.Contains(x.Id)).All(x => x.ManufactureId != null), Is.True);
            Assert.That(updatedWorkers.Single(x => x.Id == excludedWorkerId).ManufactureId, Is.Null);
        }

        [Test]
        public void ManualSelectionWithForeignWorkerThrowsTest()
        {
            var playerId = GetPlayerId();

            var otherPlayerId = GetPlayerId();
            var foreignWorkerId = GetWorkers(otherPlayerId).Single().Id;

            var ex = Assert.Throws<BusinessException>(() => StartManufacture(playerId, 2, 1, false, new[] { foreignWorkerId }));
            Assert.That(ex.Message, Is.EqualTo("Трудяга недоступен"));
        }

        [Test]
        public void ManualSelectionWithBusyWorkerThrowsTest()
        {
            var playerId = GetPlayerId();
            GrantDomik(playerId, 3, 2);
            GrantDomik(playerId, 4, 5);

            var busyWorkerId = GetWorkers(playerId)[0].Id;
            StartManufacture(playerId, 2, 1, false, new[] { busyWorkerId });

            var ex = Assert.Throws<BusinessException>(() => StartManufacture(playerId, 4, 1, false, new[] { busyWorkerId }));
            Assert.That(ex.Message, Is.EqualTo("Трудяга недоступен"));
        }

        [Test]
        public void ManualSelectionWithRestingWorkerThrowsTest()
        {
            var playerId = GetPlayerId();
            GrantDomik(playerId, 3, 2);

            var restingWorkerId = GetWorkers(playerId)[0].Id;
            SetWorkerRest(restingWorkerId, DateTimeHelper.GetNowDate().AddHours(1));

            var ex = Assert.Throws<BusinessException>(() => StartManufacture(playerId, 2, 1, false, new[] { restingWorkerId }));
            Assert.That(ex.Message, Is.EqualTo("Трудяга недоступен"));
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
