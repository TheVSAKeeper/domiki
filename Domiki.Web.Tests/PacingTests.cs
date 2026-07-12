using Domiki.Web.Business;
using Domiki.Web.Business.Core;

namespace Domiki.Web.Tests
{
    public class PacingTests : TestBase
    {
        [TestCase(6, 1, 53)]
        [TestCase(6, 4, 280)]
        [TestCase(6, 9, 945)]
        [TestCase(7, 1, 53)]
        [TestCase(7, 4, 280)]
        [TestCase(7, 9, 945)]
        public void ProcessedResourceOrderUsesNormalizedQuantityAndRewardTest(int resourceTypeId, int expectedQuantity, int expectedRewardCoins)
        {
            var playerId = GetPlayerId();
            var tier = OrderManager.Tiers.Single(candidate => OrderManager.GetOrderQuantity(candidate, resourceTypeId) == expectedQuantity);
            var quantity = OrderManager.GetOrderQuantity(tier, resourceTypeId);
            var rewardCoins = (int)Math.Round(quantity * ResourceManager.GetMarketValue(resourceTypeId) * tier.DemandMultiplier, MidpointRounding.AwayFromZero);
            var orderId = CreateOrder(playerId, resourceTypeId, quantity, rewardCoins, tier.RewardGold, tier.RewardReputation);
            GrantResource(playerId, resourceTypeId, quantity);
            var coinsBefore = GetResource(playerId, 1);

            CompleteOrder(playerId, orderId);

            Assert.That(quantity, Is.EqualTo(expectedQuantity));
            Assert.That(rewardCoins, Is.EqualTo(expectedRewardCoins));
            Assert.That(GetResource(playerId, 1) - coinsBefore, Is.EqualTo(rewardCoins));
        }

        [TestCase(32, 2)]
        [TestCase(33, 3)]
        [TestCase(34, 4)]
        public void BuyReceiptWritesOffCoinsAndProducesResourceTest(int receiptId, int resourceTypeId)
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 7);
            GrantResource(playerId, 1, 20);
            UpgradeDomik(playerId, 3);
            GrantResource(playerId, 1, 505);
            GrantResource(playerId, 2, 15);
            GrantResource(playerId, 3, 15);
            UpgradeDomik(playerId, 3);
            var coinsBefore = GetResource(playerId, 1);
            var resourceBefore = GetResource(playerId, resourceTypeId);

            StartManufacture(playerId, 3, receiptId);

            Assert.That(coinsBefore - GetResource(playerId, 1), Is.EqualTo(35));
            Assert.That(GetResource(playerId, resourceTypeId) - resourceBefore, Is.EqualTo(1));
        }

        [Test]
        public void CreateOrderPersistsNormalizedQuantityAndRewardTest()
        {
            var playerId = GetPlayerId();
            GrantDecor(playerId, 4, 4);
            GrantBlueprint(playerId, 3);
            GrantResource(playerId, 1, 600);
            BuyDomik(playerId, 13);
            for (var attempt = 0; attempt < 25; attempt++)
            {
                EnsureOrderBoard(playerId);
                var orders = LoadOrders(playerId);
                foreach (var (order, resource) in orders)
                {
                    var tier = OrderManager.Tiers.Single(candidate => candidate.RewardReputation == order.RewardReputation);
                    var expectedQuantity = OrderManager.GetOrderQuantity(tier, resource.ResourceTypeId);
                    var expectedReward = (int)Math.Round(expectedQuantity * ResourceManager.GetMarketValue(resource.ResourceTypeId) * tier.DemandMultiplier, MidpointRounding.AwayFromZero);
                    Assert.That(resource.Value, Is.EqualTo(expectedQuantity));
                    Assert.That(order.RewardCoins, Is.EqualTo(expectedReward));
                }

                if (orders.Any(x => x.Resource.ResourceTypeId is 6 or 7))
                {
                    return;
                }

                ClearOrders(playerId);
            }

            Assert.Fail("Заказ на передел не сгенерирован за 25 переборов доски");
        }

        private int GetPlayerId()
        {
            using var uow = GetUow();
            var playerId = GetDomikManager(uow).GetPlayerId("testUser_" + Guid.NewGuid());
            uow.Commit();
            return playerId;
        }

        private void BuyDomik(int playerId, int typeId)
        {
            using var uow = GetUow();
            GetDomikManager(uow).BuyDomik(playerId, typeId);
            uow.Commit();
        }

        private void UpgradeDomik(int playerId, int domikId)
        {
            using var uow = GetUow();
            GetDomikManager(uow).UpgradeDomik(playerId, domikId);
            uow.Commit();
        }

        private void StartManufacture(int playerId, int domikId, int receiptId)
        {
            using var uow = GetUow();
            GetDomikManager(uow).StartManufacture(playerId, domikId, receiptId);
            uow.Commit();
        }

        private void CompleteOrder(int playerId, int orderId)
        {
            using var uow = GetUow();
            GetOrderManager(uow).CompleteOrder(playerId, orderId);
            uow.Commit();
        }

        private int CreateOrder(int playerId, int resourceTypeId, int quantity, int rewardCoins, int rewardGold, int rewardReputation)
        {
            using var uow = GetUow();
            var now = DateTimeHelper.GetNowDate();
            var order = new Domiki.Web.Data.Order
            {
                PlayerId = playerId,
                NeighborId = 1,
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
                Value = quantity,
            });
            uow.Commit();
            return order.Id;
        }

        private void GrantBlueprint(int playerId, int blueprintId)
        {
            using var uow = GetUow();
            uow.Context.PlayerBlueprints.Add(new Domiki.Web.Data.PlayerBlueprint { PlayerId = playerId, BlueprintId = blueprintId });
            uow.Context.SaveChanges();
            uow.Commit();
        }

        private void GrantDecor(int playerId, int decorTypeId, int count)
        {
            using var uow = GetUow();
            var decor = uow.Context.PlayerDecors.SingleOrDefault(candidate => candidate.PlayerId == playerId && candidate.DecorTypeId == decorTypeId);
            if (decor == null)
            {
                decor = new Domiki.Web.Data.PlayerDecor { PlayerId = playerId, DecorTypeId = decorTypeId };
                uow.Context.PlayerDecors.Add(decor);
            }

            decor.Count += count;
            uow.Context.SaveChanges();
            uow.Commit();
        }

        private void EnsureOrderBoard(int playerId)
        {
            using var uow = GetUow();
            GetOrderManager(uow).EnsureOrderBoard(playerId);
            uow.Commit();
        }

        private (Domiki.Web.Data.Order Order, Domiki.Web.Data.OrderResource Resource)[] LoadOrders(int playerId)
        {
            using var uow = GetUow();
            var orders = uow.Context.Orders.Where(candidate => candidate.PlayerId == playerId)
                .Join(uow.Context.OrderResources, order => order.Id, resource => resource.OrderId, (order, resource) => new { order, resource })
                .ToArray()
                .Select(x => (x.order, x.resource))
                .ToArray();
            uow.Commit();
            return orders;
        }

        private void ClearOrders(int playerId)
        {
            using var uow = GetUow();
            var orders = uow.Context.Orders.Where(candidate => candidate.PlayerId == playerId).ToArray();
            var orderIds = orders.Select(order => order.Id).ToArray();
            var resources = uow.Context.OrderResources.Where(candidate => orderIds.Contains(candidate.OrderId)).ToArray();
            uow.Context.OrderResources.RemoveRange(resources);
            uow.Context.Orders.RemoveRange(orders);
            uow.Context.SaveChanges();
            uow.Commit();
        }

        private void GrantResource(int playerId, int resourceTypeId, int value)
        {
            using var uow = GetUow();
            var resource = uow.Context.Resources.SingleOrDefault(candidate => candidate.PlayerId == playerId && candidate.TypeId == resourceTypeId);
            if (resource == null)
            {
                resource = new Domiki.Web.Data.Resource { PlayerId = playerId, TypeId = resourceTypeId };
                uow.Context.Resources.Add(resource);
            }

            resource.Value += value;
            uow.Context.SaveChanges();
            uow.Commit();
        }

        private int GetResource(int playerId, int resourceTypeId)
        {
            using var uow = GetUow();
            var value = uow.Context.Resources.SingleOrDefault(candidate => candidate.PlayerId == playerId && candidate.TypeId == resourceTypeId)?.Value ?? 0;
            uow.Commit();
            return value;
        }
    }
}
