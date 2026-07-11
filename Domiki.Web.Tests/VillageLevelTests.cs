using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    public class VillageLevelTests : TestBase
    {
        [Test]
        public void VillageLevelGrowsFromBuildingsResidentsAndReputationTest()
        {
            var playerId = GetPlayerId();

            var initial = GetVillageLevel(playerId);
            Assert.That(initial.Level, Is.EqualTo(0));

            BuyDomik(playerId, 2);
            var withBarracks = GetVillageLevel(playerId);
            Assert.That(withBarracks.Buildings, Is.EqualTo(1));
            Assert.That(withBarracks.Residents, Is.EqualTo(1));
            Assert.That(withBarracks.Reputation, Is.EqualTo(0));
            Assert.That(withBarracks.Comfort, Is.EqualTo(0));
            Assert.That(withBarracks.Level, Is.EqualTo(3));

            GrantReputation(playerId, 4, 10);
            var withReputation = GetVillageLevel(playerId);
            Assert.That(withReputation.Reputation, Is.EqualTo(1));
            Assert.That(withReputation.Level, Is.EqualTo(8));
        }

        [TestCase(49, 49)]
        [TestCase(50, 50)]
        [TestCase(80, 50)]
        public void ComfortContributionToLevelIsCappedTest(int comfort, int expectedContribution)
        {
            Assert.That(VillageLevelCalculator.ComputeLevel(0, 0, 0, comfort), Is.EqualTo(expectedContribution));
        }

        [Test]
        public void BuyDomikGateBlocksBeforeThresholdAndAllowsAfterTest()
        {
            var playerId = GetPlayerId();

            var ex = Assert.Throws<BusinessException>(() => BuyDomik(playerId, 3));
            Assert.That(ex.Message, Is.EqualTo("Откроется при обжитости 3"));

            BuyDomik(playerId, 2);

            Assert.DoesNotThrow(() => BuyDomik(playerId, 3));
        }

        [Test]
        public void OrderBoardUsesOnlyUnlockedNeighborsBeforeThresholdTest()
        {
            var playerId = GetPlayerId();

            var orders = GetOrders(playerId);

            Assert.That(orders, Is.Not.Empty);
            Assert.That(orders.All(x => x.Neighbor.UnlockLevel == 0), Is.True);
        }

        [Test]
        public void OrderBoardCanUseUnlockedNeighborsAfterThresholdTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            GrantReputation(playerId, 4, 10);

            var sawGatedNeighbor = false;
            for (var i = 0; i < 40 && !sawGatedNeighbor; i++)
            {
                ClearOrders(playerId);
                var orders = GetOrders(playerId);
                Assert.That(orders.All(x => x.Neighbor.UnlockLevel <= 8), Is.True);
                sawGatedNeighbor = orders.Any(x => x.Neighbor.UnlockLevel > 0);
            }

            Assert.That(sawGatedNeighbor, Is.True);
        }

        [Test]
        public void AutoWorkerSelectionIsByIdBeforeSmartAutoThresholdTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 5);

            var workers = GetWorkers(playerId);
            var weakWorker = workers[0];
            var strongWorker = workers[1];
            SetWorkerTrait(weakWorker.Id, 1);
            SetWorkerTrait(strongWorker.Id, 3);

            StartManufacture(playerId, 3, 1, false);

            var busyWorker = GetWorkers(playerId).Single(x => x.ManufactureId != null);
            Assert.That(busyWorker.Id, Is.EqualTo(weakWorker.Id));
        }

        [Test]
        public void AutoWorkerSelectionIsByFitnessAfterSmartAutoThresholdTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 5);

            var workers = GetWorkers(playerId);
            var weakWorker = workers[0];
            var strongWorker = workers[1];
            SetWorkerTrait(weakWorker.Id, 1);
            SetWorkerTrait(strongWorker.Id, 3);

            StartManufacture(playerId, 4, 1, false);

            var busyWorker = GetWorkers(playerId).Single(x => x.ManufactureId != null);
            Assert.That(busyWorker.Id, Is.EqualTo(strongWorker.Id));
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

        private Order[] GetOrders(int playerId)
        {
            using (var uow = GetUow())
            {
                var orderManager = GetOrderManager(uow);
                var orders = orderManager.GetOrders(playerId).ToArray();
                uow.Commit();
                return orders;
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

        private void BuyDomik(int playerId, int domikTypeId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                domikManager.BuyDomik(playerId, domikTypeId);
                uow.Commit();
            }
        }

        private void StartManufacture(int playerId, int domikId, int receiptId, bool useOptional)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow, false);
                domikManager.StartManufacture(playerId, domikId, receiptId, useOptional);
                uow.Commit();
            }
        }

        private void GrantReputation(int playerId, int neighborId, int points)
        {
            using (var uow = GetUow())
            {
                var reputation = uow.Context.NeighborReputations.SingleOrDefault(x => x.PlayerId == playerId && x.NeighborId == neighborId);
                if (reputation == null)
                {
                    reputation = new Domiki.Web.Data.NeighborReputation { PlayerId = playerId, NeighborId = neighborId };
                    uow.Context.NeighborReputations.Add(reputation);
                }

                reputation.Points += points;
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private void ClearOrders(int playerId)
        {
            using (var uow = GetUow())
            {
                var orders = uow.Context.Orders.Where(x => x.PlayerId == playerId).ToArray();
                uow.Context.Orders.RemoveRange(orders);
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
    }
}
