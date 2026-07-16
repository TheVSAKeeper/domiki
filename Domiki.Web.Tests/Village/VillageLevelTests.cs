using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    public class VillageLevelTests : TestBase
    {
        /// <summary>
        /// Уровень деревни растёт от числа построек, поселившихся трудяг и репутации у соседей – каждый фактор вносит свой вклад с собственным весом.
        /// </summary>
        [Test]
        public void VillageLevelGrowsFromBuildingsResidentsAndReputationTest()
        {
            var playerId = GetPlayerId();

            var initial = GetVillageLevel(playerId);
            Assert.That(initial.Level, Is.EqualTo(4));

            GrantDomik(playerId, 3, 2);
            var withBarracks = GetVillageLevel(playerId);
            Assert.That(withBarracks.Buildings, Is.EqualTo(3));
            Assert.That(withBarracks.Residents, Is.EqualTo(2));
            Assert.That(withBarracks.Reputation, Is.EqualTo(0));
            Assert.That(withBarracks.Comfort, Is.EqualTo(0));
            Assert.That(withBarracks.Level, Is.EqualTo(7));

            GrantReputation(playerId, 4, 10);
            var withReputation = GetVillageLevel(playerId);
            Assert.That(withReputation.Reputation, Is.EqualTo(1));
            Assert.That(withReputation.Level, Is.EqualTo(12));
        }

        /// <summary>
        /// Вклад уюта в уровень деревни ограничен потолком в 50 очков, сколько бы уюта игрок ни накопил.
        /// </summary>
        /// <param name="comfort">Накопленный уют.</param>
        /// <param name="expectedContribution">Ожидаемый вклад уюта в уровень деревни.</param>
        [TestCase(49, 49)]
        [TestCase(50, 50)]
        [TestCase(80, 50)]
        public void ComfortContributionToLevelIsCappedTest(int comfort, int expectedContribution)
        {
            Assert.That(VillageLevelCalculator.ComputeLevel(0, 0, 0, comfort), Is.EqualTo(expectedContribution));
        }

        /// <summary>
        /// Постройка, открываемая по уровню обжитости деревни, недоступна для покупки до достижения порога и становится доступной после.
        /// </summary>
        [Test]
        public void BuyDomikGateBlocksBeforeThresholdAndAllowsAfterTest()
        {
            var playerId = GetPlayerId();

            var ex = Assert.Throws<BusinessException>(() => BuyDomik(playerId, 3));
            Assert.That(ex.Message, Is.EqualTo("Откроется при обжитости 6"));

            GrantDomik(playerId, 3, 2);
            GrantDomik(playerId, 4, 2);

            Assert.DoesNotThrow(() => BuyDomik(playerId, 3));
        }

        /// <summary>
        /// Пока порог уровня деревни не достигнут, доска заказов предлагает заказы только от соседей с нулевым уровнем открытия.
        /// </summary>
        [Test]
        public void OrderBoardUsesOnlyUnlockedNeighborsBeforeThresholdTest()
        {
            var playerId = GetPlayerId();

            var orders = GetOrders(playerId);

            Assert.That(orders, Is.Not.Empty);
            Assert.That(orders.All(x => x.Neighbor.UnlockLevel == 0), Is.True);
        }

        /// <summary>
        /// После достижения порога обжитости (8) доска заказов может использовать соседей, открытых вплоть до этого уровня, но не выше.
        /// </summary>
        [Test]
        public void OrderBoardCanUseUnlockedNeighborsAfterThresholdTest()
        {
            var playerId = GetPlayerId();
            GrantDomik(playerId, 3, 2);
            BuyDomik(playerId, 3);

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

        /// <summary>
        /// До порога умного автоподбора автоматический выбор трудяги на производство берёт того, у кого меньше id, игнорируя пригодность черты характера.
        /// </summary>
        [Test]
        public void AutoWorkerSelectionIsByIdBeforeSmartAutoThresholdTest()
        {
            var playerId = GetPlayerId();
            GrantDomik(playerId, 3, 2);

            var workers = GetWorkers(playerId);
            var weakWorker = workers[0];
            var strongWorker = workers[1];
            SetWorkerTrait(weakWorker.Id, 1);
            SetWorkerTrait(strongWorker.Id, 3);

            StartManufacture(playerId, 2, 1, false);

            var busyWorker = GetWorkers(playerId).Single(x => x.ManufactureId != null);
            Assert.That(busyWorker.Id, Is.EqualTo(weakWorker.Id));
        }

        /// <summary>
        /// После порога умного автоподбора автоматический выбор трудяги на производство берёт того, чья черта характера лучше всего подходит под работу, а не с меньшим id.
        /// </summary>
        [Test]
        public void AutoWorkerSelectionIsByFitnessAfterSmartAutoThresholdTest()
        {
            var playerId = GetPlayerId();
            GrantDomik(playerId, 3, 2);
            GrantDomik(playerId, 4, 2);

            var workers = GetWorkers(playerId);
            var weakWorker = workers[0];
            var strongWorker = workers[1];
            SetWorkerTrait(weakWorker.Id, 1);
            SetWorkerTrait(strongWorker.Id, 3);

            StartManufacture(playerId, 2, 1, false);

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
