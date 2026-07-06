using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    public class Stage1BalanceRecalibrationTests : TestBase
    {
        [Test]
        public void GoldMineAndMarketReceiptsAreRecalibratedTest()
        {
            var types = GetDomikTypes();

            var goldMineReceiptIds = types.Single(x => x.Id == 4).Levels
                .SelectMany(x => x.Receipts)
                .Select(x => x.Id)
                .ToArray();
            var marketReceiptIds = types.Single(x => x.Id == 7).Levels
                .SelectMany(x => x.Receipts)
                .Select(x => x.Id)
                .ToArray();

            Assert.That(goldMineReceiptIds, Does.Contain(3));
            Assert.That(goldMineReceiptIds, Does.Not.Contain(17));
            Assert.That(goldMineReceiptIds, Does.Not.Contain(21));
            Assert.That(marketReceiptIds, Does.Not.Contain(8));
            Assert.That(marketReceiptIds, Does.Not.Contain(12));
        }

        [TestCase(4, 1500)]
        [TestCase(5, 9000)]
        public void UpgradeCoinCostsAreRecalibratedTest(int level, int expectedCoins)
        {
            var types = GetDomikTypes();

            foreach (var type in types)
            {
                var coinCost = type.Levels.Single(x => x.Value == level).Resources.Single(x => x.Type.Id == 1).Value;
                Assert.That(coinCost, Is.EqualTo(expectedCoins), $"domik type {type.Id}");
            }
        }

        [Test]
        public void ForgeBrickShiftConsumesClayAndProducesBricksTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 1);
            GrantResource(playerId, 4, 16);
            var before = GetResources(playerId);

            StartManufacture(playerId, 4, 27, true);

            var after = GetResources(playerId);
            Assert.That(ResourceValue(before, 4) - ResourceValue(after, 4), Is.EqualTo(16));
            Assert.That(ResourceValue(after, 6) - ResourceValue(before, 6), Is.EqualTo(8));
        }

        [Test]
        public void DurationMultiplierClampUsesBaseReceiptDurationTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 5);
            GrantResource(playerId, 8, 1);
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, 3);
            SetWorkerSkill(worker.Id, 5, 100);
            var start = DateTimeHelper.GetNowDate();

            StartManufacture(playerId, 2, 14, false, true, new[] { worker.Id });

            var manufacture = GetManufactures(playerId).Single();
            var durationSeconds = (manufacture.FinishDate - start).TotalSeconds;
            Assert.That(durationSeconds, Is.GreaterThanOrEqualTo(Math.Ceiling(28800 * 0.6)));
            Assert.That(durationSeconds, Is.LessThanOrEqualTo(Math.Ceiling(28800 * 0.6) + 1));
        }

        [Test]
        public void WorkerNamesAreUniquePerPlayerTest()
        {
            var playerId = GetPlayerId();
            for (var i = 0; i < 5; i++)
            {
                BuyDomik(playerId, 2);
            }

            var workers = GetWorkers(playerId);

            Assert.That(workers.Select(x => x.Name).Distinct().Count(), Is.EqualTo(workers.Length));
        }

        private DomikType[] GetDomikTypes()
        {
            using (var uow = GetUow())
            {
                var resourceManager = GetResourceManager(uow);
                var result = resourceManager.GetDomikTypes();
                uow.Commit();
                return result;
            }
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

        private Domiki.Web.Data.Manufacture[] GetManufactures(int playerId)
        {
            using (var uow = GetUow())
            {
                var manufactures = uow.Context.Manufactures.Where(x => x.DomikPlayerId == playerId).ToArray();
                uow.Commit();
                return manufactures;
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

        private void StartManufacture(int playerId, int domikId, int receiptId, bool calculatorJustFinishMode, bool useOptional = false, int[]? workerIds = null)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow, calculatorJustFinishMode);
                domikManager.StartManufacture(playerId, domikId, receiptId, useOptional, workerIds);
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

        private int ResourceValue(Resource[] resources, int typeId)
        {
            return resources.FirstOrDefault(x => x.Type.Id == typeId)?.Value ?? 0;
        }
    }
}
