using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    public class OrdersTests : TestBase
    {
        [Test]
        public void GetOrdersNewPlayerCreatesThreeOrdersTest()
        {
            var playerId = GetPlayerId();

            var orders = GetOrders(playerId);

            Assert.That(orders.Length, Is.EqualTo(3));
            Assert.That(orders.All(x => x.Resources.Length == 1), Is.True);
        }

        [Test]
        public void CompleteOrderWithEnoughResourcesWritesOffRewardsAndHoldsSlotTest()
        {
            var playerId = GetPlayerId();
            var order = GetOrders(playerId).First();
            var need = order.Resources.Single();
            GrantResource(playerId, need.Type.Id, need.Value);
            var beforeResources = GetResources(playerId);
            var beforeReputation = GetReputation(playerId).First(x => x.Neighbor.Id == order.Neighbor.Id).Points;

            CompleteOrder(playerId, order.Id);

            var afterResources = GetResources(playerId);
            var afterReputation = GetReputation(playerId).First(x => x.Neighbor.Id == order.Neighbor.Id).Points;
            Assert.That(ResourceValue(beforeResources, need.Type.Id) - ResourceValue(afterResources, need.Type.Id), Is.EqualTo(need.Value));
            Assert.That(ResourceValue(afterResources, 1) - ResourceValue(beforeResources, 1), Is.EqualTo(order.RewardCoins));
            Assert.That(ResourceValue(afterResources, 5) - ResourceValue(beforeResources, 5), Is.EqualTo(order.RewardGold));
            Assert.That(afterReputation - beforeReputation, Is.EqualTo(order.RewardReputation));

            var orders = GetOrders(playerId);
            Assert.That(orders.Length, Is.EqualTo(2));
            Assert.That(orders.Any(x => x.Id == order.Id), Is.False);
        }

        [Test]
        public void CompleteOrderHoldsSlotOnRepeatedFetchTest()
        {
            var playerId = GetPlayerId();
            var order = GetOrders(playerId).First();
            var need = order.Resources.Single();
            GrantResource(playerId, need.Type.Id, need.Value);

            CompleteOrder(playerId, order.Id);

            var orders = GetOrders(playerId);
            Assert.That(orders.Length, Is.EqualTo(2));
            Assert.That(orders.Any(x => x.Id == order.Id), Is.False);
        }

        [Test]
        public void OrderBoardRefillsAfterDelayElapsesTest()
        {
            var playerId = GetPlayerId();
            var order = GetOrders(playerId).First();
            var need = order.Resources.Single();
            GrantResource(playerId, need.Type.Id, need.Value);

            CompleteOrder(playerId, order.Id);
            SetOrderRefillAt(playerId, DateTimeHelper.GetNowDate().AddSeconds(-1));

            var orders = GetOrders(playerId);
            Assert.That(orders.Length, Is.EqualTo(3));
        }

        [Test]
        public void CompleteOrderWithoutResourcesThrowsAndKeepsStateTest()
        {
            var playerId = GetPlayerId();
            var order = GetOrders(playerId).First();
            var beforeResources = GetResources(playerId);
            var beforeReputation = GetReputation(playerId).Select(x => (x.Neighbor.Id, x.Points)).ToArray();

            Assert.Throws<BusinessException>(() => CompleteOrder(playerId, order.Id));

            var afterResources = GetResources(playerId);
            var afterReputation = GetReputation(playerId).Select(x => (x.Neighbor.Id, x.Points)).ToArray();
            var orders = GetOrders(playerId);
            Assert.That(afterResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo(beforeResources.Select(x => (x.Type.Id, x.Value))));
            Assert.That(afterReputation, Is.EquivalentTo(beforeReputation));
            Assert.That(orders.Length, Is.EqualTo(3));
            Assert.That(orders.Any(x => x.Id == order.Id), Is.True);
        }

        [Test]
        public void CompleteForeignOrderThrowsTest()
        {
            var firstPlayerId = GetPlayerId();
            var secondPlayerId = GetPlayerId();
            var order = GetOrders(firstPlayerId).First();
            var need = order.Resources.Single();
            GrantResource(secondPlayerId, need.Type.Id, need.Value);

            Assert.Throws<BusinessException>(() => CompleteOrder(secondPlayerId, order.Id));

            Assert.That(GetOrders(firstPlayerId).Any(x => x.Id == order.Id), Is.True);
        }

        [Test]
        public void CompleteMissingOrderThrowsTest()
        {
            var playerId = GetPlayerId();

            Assert.Throws<BusinessException>(() => CompleteOrder(playerId, int.MaxValue));
        }

        [Test]
        public void FinishOrderRemovesExpiredOrderAndHoldsSlotTest()
        {
            var playerId = GetPlayerId();
            var order = GetOrders(playerId).First();

            FinishOrder(playerId, order.Id, order.ExpireDate.AddSeconds(1));

            var orders = GetOrders(playerId);
            Assert.That(orders.Length, Is.EqualTo(2));
            Assert.That(orders.Any(x => x.Id == order.Id), Is.False);
        }

        [Test]
        public void ReputationAccumulatesForSameNeighborTest()
        {
            var playerId = GetPlayerId();
            var neighborId = 1;
            var resourceTypeId = 6;
            var firstOrderId = CreateManualOrder(playerId, neighborId, resourceTypeId, 2, 100, 1, 3);
            var secondOrderId = CreateManualOrder(playerId, neighborId, resourceTypeId, 3, 150, 2, 4);
            GrantResource(playerId, resourceTypeId, 5);

            CompleteOrder(playerId, firstOrderId);
            CompleteOrder(playerId, secondOrderId);

            var reputation = GetReputation(playerId).First(x => x.Neighbor.Id == neighborId);
            Assert.That(reputation.Points, Is.EqualTo(7));
        }

        [Test]
        public void OrdersOnlyAskProducibleResourcesTest()
        {
            var playerId = GetPlayerId();
            GrantResource(playerId, 1, 10000);
            RaiseVillageLevelTo(playerId, 10);
            BuyDomik(playerId, 5);
            BuyDomik(playerId, 6);

            for (var i = 0; i < 15; i++)
            {
                var orders = GetOrders(playerId).ToArray();
                var resourceTypeIds = orders.SelectMany(x => x.Resources).Select(x => x.Type.Id).Distinct().ToArray();
                Assert.That(resourceTypeIds, Is.SubsetOf(new[] { 3, 4 }));
                DeleteAllOrders(playerId);
            }
        }

        [Test]
        public void NewPlayerWithoutBuildingsStillGetsOrdersTest()
        {
            var playerId = GetPlayerId();

            var orders = GetOrders(playerId).ToArray();

            Assert.That(orders.Length, Is.EqualTo(3));
        }

        [Test]
        public void ConcurrentGetOrdersSerializesWithoutConcurrencyExceptionTest()
        {
            for (var i = 1; i <= 30; i++)
            {
                var playerId = GetPlayerId();
                var numbers = Enumerable.Range(0, 8).ToList();

                Assert.DoesNotThrow(() => Parallel.ForEach(numbers, _ => GetOrders(playerId)), "iteration " + i);

                Assert.That(GetOrders(playerId).Length, Is.EqualTo(3), "iteration " + i);
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

        private NeighborReputation[] GetReputation(int playerId)
        {
            using (var uow = GetUow())
            {
                var orderManager = GetOrderManager(uow);
                var reputation = orderManager.GetReputation(playerId).ToArray();
                uow.Commit();
                return reputation;
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

        private void CompleteOrder(int playerId, int orderId)
        {
            using (var uow = GetUow())
            {
                var orderManager = GetOrderManager(uow);
                orderManager.CompleteOrder(playerId, orderId);
                uow.Commit();
            }
        }

        private void FinishOrder(int playerId, int orderId, DateTime date)
        {
            using (var uow = GetUow())
            {
                var orderManager = GetOrderManager(uow);
                var result = orderManager.FinishOrder(date, new CalculateInfo
                {
                    PlayerId = playerId,
                    ObjectId = orderId,
                    Date = date,
                    Type = CalculateTypes.OrderExpire,
                });
                Assert.That(result, Is.True);
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

        private void RaiseVillageLevelTo(int playerId, int targetLevel)
        {
            var current = GetVillageLevel(playerId).Level;
            if (current >= targetLevel)
            {
                return;
            }

            var milestones = (int)Math.Ceiling((targetLevel - current) / (double)VillageLevelCalculator.ReputationWeight);
            GrantReputation(playerId, 1, milestones * VillageLevelCalculator.ReputationPointsPerMilestone);
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

        private void DeleteAllOrders(int playerId)
        {
            using (var uow = GetUow())
            {
                var dbOrders = uow.Context.Orders.Where(x => x.PlayerId == playerId).ToArray();
                uow.Context.Orders.RemoveRange(dbOrders);
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private void SetOrderRefillAt(int playerId, DateTime value)
        {
            using (var uow = GetUow())
            {
                var player = uow.Context.Players.Single(x => x.Id == playerId);
                player.NextOrderRefillAt = value;
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private void GrantResource(int playerId, int typeId, int value)
        {
            using (var uow = GetUow())
            {
                var resource = uow.Context.Resources.FirstOrDefault(x => x.PlayerId == playerId && x.TypeId == typeId);
                if (resource == null)
                {
                    resource = new Domiki.Web.Data.Resource { PlayerId = playerId, TypeId = typeId };
                    uow.Context.Resources.Add(resource);
                }

                resource.Value += value;
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private int CreateManualOrder(int playerId, int neighborId, int resourceTypeId, int value, int rewardCoins, int rewardGold, int rewardReputation)
        {
            using (var uow = GetUow())
            {
                var now = DateTimeHelper.GetNowDate();
                var order = new Domiki.Web.Data.Order
                {
                    PlayerId = playerId,
                    NeighborId = neighborId,
                    CreateDate = now,
                    ExpireDate = now.AddHours(4),
                    RewardCoins = rewardCoins,
                    RewardGold = rewardGold,
                    RewardReputation = rewardReputation,
                };
                uow.Context.Orders.Add(order);
                uow.Context.SaveChanges();
                uow.Context.OrderResources.Add(new Domiki.Web.Data.OrderResource
                {
                    OrderId = order.Id,
                    ResourceTypeId = resourceTypeId,
                    Value = value,
                });
                uow.Commit();
                return order.Id;
            }
        }

        private int ResourceValue(Resource[] resources, int typeId)
        {
            return resources.FirstOrDefault(x => x.Type.Id == typeId)?.Value ?? 0;
        }
    }
}
