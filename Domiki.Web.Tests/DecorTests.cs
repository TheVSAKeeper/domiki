using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    public class DecorTests : TestBase
    {
        private const int FenceDecorTypeId = 1;
        private const int FlowerbedDecorTypeId = 2;
        private const int GardenDecorTypeId = 3;
        private const int FountainDecorTypeId = 4;
        private const int WoodResourceTypeId = 3;
        private const int StoneResourceTypeId = 2;
        private const int ClayResourceTypeId = 4;
        private const int BrickResourceTypeId = 6;
        private const int BoardResourceTypeId = 7;
        private const int ToolResourceTypeId = 8;
        private const int BarracksDomikTypeId = 2;
        private const int LumberMillDomikTypeId = 6;
        private const int WoodDig8hReceiptId = 16;
        private const int FatigueThresholdSeconds = 8 * 3600;
        private const int RestSeconds = 2 * 3600;

        [Test]
        public void GetDecorForNewPlayerReturnsTypesAndZeroComfortTest()
        {
            var playerId = GetPlayerId();

            var decor = GetDecor(playerId);

            Assert.That(decor.Types.Select(x => x.LogicName), Is.EquivalentTo(new[] { "fence", "flowerbed", "garden", "fountain" }));
            Assert.That(decor.Owned, Is.Empty);
            Assert.That(decor.Comfort, Is.EqualTo(0));
        }

        [TestCase(FenceDecorTypeId, 2)]
        [TestCase(FlowerbedDecorTypeId, 3)]
        [TestCase(GardenDecorTypeId, 5)]
        [TestCase(FountainDecorTypeId, 8)]
        public void BuyDecorWritesOffResourcesAndIncreasesComfortTest(int decorTypeId, int comfortPoints)
        {
            var playerId = GetPlayerId();
            GrantDecorCost(playerId, decorTypeId, 2);
            var before = GetResources(playerId);

            BuyDecor(playerId, decorTypeId);
            BuyDecor(playerId, decorTypeId);

            var after = GetResources(playerId);
            var decor = GetDecor(playerId);
            var type = decor.Types.Single(x => x.Id == decorTypeId);
            Assert.That(decor.Owned.Single(x => x.DecorTypeId == decorTypeId).Count, Is.EqualTo(2));
            Assert.That(decor.Comfort, Is.EqualTo(comfortPoints * 2));

            foreach (var cost in type.Cost)
            {
                Assert.That(GetResourceValue(after, cost.Type.Id), Is.EqualTo(GetResourceValue(before, cost.Type.Id) - cost.Value * 2));
            }
        }

        [Test]
        public void BuyDecorWithoutResourcesThrowsAndDoesNotChangeOwnedTest()
        {
            var playerId = GetPlayerId();

            var ex = Assert.Throws<BusinessException>(() => BuyDecor(playerId, FenceDecorTypeId));

            Assert.That(ex.Message, Does.StartWith("Недостаточно "));
            var decor = GetDecor(playerId);
            Assert.That(decor.Owned, Is.Empty);
            Assert.That(decor.Comfort, Is.EqualTo(0));
        }

        [Test]
        public void BuyUnknownDecorThrowsTest()
        {
            var playerId = GetPlayerId();

            var ex = Assert.Throws<BusinessException>(() => BuyDecor(playerId, 999));

            Assert.That(ex.Message, Is.EqualTo("Декор не найден"));
        }

        [Test]
        public void ComfortIncreasesVillageLevelTest()
        {
            var playerId = GetPlayerId();
            var before = GetVillageLevel(playerId);
            GrantDecor(playerId, FountainDecorTypeId, 2);

            var after = GetVillageLevel(playerId);

            Assert.That(after.Comfort, Is.EqualTo(16));
            Assert.That(after.Level, Is.EqualTo(before.Level + 16 * VillageLevelCalculator.ComfortWeight));
        }

        [Test]
        public void ComfortShortensWorkerRestTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, BarracksDomikTypeId);
            BuyDomik(playerId, LumberMillDomikTypeId);
            var worker = GetWorkers(playerId).Single();
            GrantDecor(playerId, FountainDecorTypeId, 1);
            SetWorkerWorked(worker.Id, FatigueThresholdSeconds - RestSeconds);

            StartManufacture(playerId, 2, WoodDig8hReceiptId);
            var manufacture = GetDomiks(playerId).Single(x => x.Id == 2).Manufactures.Single();
            var finishDate = manufacture.FinishDate.AddSeconds(1);
            FinishManufacture(playerId, manufacture.Id, finishDate);

            worker = GetWorkers(playerId).Single();
            Assert.That(worker.RestUntil, Is.EqualTo(finishDate.AddSeconds(RestSeconds * 92 / 100)));
        }

        [Test]
        public void ComfortRestReductionIsCappedAtHalfTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, BarracksDomikTypeId);
            BuyDomik(playerId, LumberMillDomikTypeId);
            var worker = GetWorkers(playerId).Single();
            GrantDecor(playerId, FountainDecorTypeId, 10);
            SetWorkerWorked(worker.Id, FatigueThresholdSeconds - RestSeconds);

            StartManufacture(playerId, 2, WoodDig8hReceiptId);
            var manufacture = GetDomiks(playerId).Single(x => x.Id == 2).Manufactures.Single();
            var finishDate = manufacture.FinishDate.AddSeconds(1);
            FinishManufacture(playerId, manufacture.Id, finishDate);

            worker = GetWorkers(playerId).Single();
            Assert.That(worker.RestUntil, Is.EqualTo(finishDate.AddSeconds(RestSeconds / 2)));
        }

        [Test]
        public void NoFatigueWorkerDoesNotRestRegardlessComfortTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, BarracksDomikTypeId);
            BuyDomik(playerId, LumberMillDomikTypeId);
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, 4);
            GrantDecor(playerId, FountainDecorTypeId, 10);
            SetWorkerWorked(worker.Id, FatigueThresholdSeconds);

            StartManufacture(playerId, 2, WoodDig8hReceiptId);
            var manufacture = GetDomiks(playerId).Single(x => x.Id == 2).Manufactures.Single();
            FinishManufacture(playerId, manufacture.Id, manufacture.FinishDate.AddSeconds(1));

            worker = GetWorkers(playerId).Single();
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

        private DecorState GetDecor(int playerId)
        {
            using (var uow = GetUow())
            {
                var manager = GetDecorManager(uow);
                var decor = manager.GetDecor(playerId);
                uow.Commit();
                return decor;
            }
        }

        private VillageLevel GetVillageLevel(int playerId)
        {
            using (var uow = GetUow())
            {
                var calculator = GetVillageLevelCalculator(uow);
                var level = calculator.GetLevel(playerId);
                uow.Commit();
                return level;
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

        private void BuyDecor(int playerId, int decorTypeId)
        {
            using (var uow = GetUow())
            {
                var manager = GetDecorManager(uow);
                manager.BuyDecor(playerId, decorTypeId);
                uow.Commit();
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

        private void StartManufacture(int playerId, int domikId, int receiptId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow, false);
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

        private void GrantDecorCost(int playerId, int decorTypeId, int multiplier)
        {
            var decor = GetDecor(playerId);
            var type = decor.Types.Single(x => x.Id == decorTypeId);
            foreach (var cost in type.Cost)
            {
                GrantResource(playerId, cost.Type.Id, cost.Value * multiplier);
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

        private int GetResourceValue(Resource[] resources, int resourceTypeId)
        {
            return resources.FirstOrDefault(x => x.Type.Id == resourceTypeId)?.Value ?? 0;
        }
    }
}
