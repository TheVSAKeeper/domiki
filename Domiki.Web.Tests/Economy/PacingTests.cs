using Domiki.Web.Data.Entities;
using Domiki.Web.Economy;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;

namespace Domiki.Web.Tests;

public sealed class PacingTests
{
    /// <summary>
    /// Заказы, сгенерированные доской (включая заказы на передельные ресурсы), сохраняют количество и награду строго по
    /// формуле нормировки тира.
    /// </summary>
    [Test]
    public void CreateOrderPersistsNormalizedQuantityAndRewardTest()
    {
        var player = TestPlayer.Create()
            .WithDecor(DecorIds.Fountain, 4)
            .WithBlueprint(BlueprintIds.Pottery)
            .WithResource(ResourceIds.Coin, 600)
            .Buy(DomikIds.Pottery);

        for (var attempt = 0; attempt < 25; attempt++)
        {
            player.EnsureOrderBoard();
            var orders = LoadOrders(player.Id);
            foreach (var (order, resource) in orders)
            {
                var tier = OrderManager.Tiers.Single(candidate => candidate.RewardReputation == order.RewardReputation);
                var capacity = player.Capacity(resource.ResourceTypeId);
                var expectedQuantity = OrderManager.GetEffectiveQuantity(tier, resource.ResourceTypeId, capacity);
                var expectedReward = (int)Math.Round(expectedQuantity * ResourceManager.GetMarketValue(resource.ResourceTypeId) * tier.DemandMultiplier, MidpointRounding.AwayFromZero);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(resource.Value, Is.EqualTo(expectedQuantity));
                    Assert.That(order.RewardCoins, Is.EqualTo(expectedReward));
                }
            }

            if (orders.Any(x => x.Resource.ResourceTypeId is ResourceIds.Brick or ResourceIds.Board))
            {
                return;
            }

            ClearOrders(player.Id);
        }

        Assert.Fail("Заказ на передел не сгенерирован за 25 переборов доски");
    }

    /// <summary>
    /// Объём и денежная награда заказа на передельный ресурс нормированы по тиру спроса и одинаковы для равнозначных типов
    /// ресурса (6 и 7).
    /// </summary>
    /// <param name="resourceTypeId">Тип запрашиваемого ресурса.</param>
    /// <param name="expectedQuantity">Ожидаемое нормированное количество ресурса.</param>
    /// <param name="expectedRewardCoins">Ожидаемая награда в монетах.</param>
    [TestCase(ResourceIds.Brick, 1, 53)]
    [TestCase(ResourceIds.Brick, 4, 280)]
    [TestCase(ResourceIds.Brick, 9, 945)]
    [TestCase(ResourceIds.Board, 1, 53)]
    [TestCase(ResourceIds.Board, 4, 280)]
    [TestCase(ResourceIds.Board, 9, 945)]
    public void ProcessedResourceOrderUsesNormalizedQuantityAndRewardTest(int resourceTypeId, int expectedQuantity, int expectedRewardCoins)
    {
        var player = TestPlayer.Create();
        var tier = OrderManager.Tiers.Single(candidate => OrderManager.GetOrderQuantity(candidate, resourceTypeId) == expectedQuantity);
        var quantity = OrderManager.GetOrderQuantity(tier, resourceTypeId);
        var rewardCoins = (int)Math.Round(quantity * ResourceManager.GetMarketValue(resourceTypeId) * tier.DemandMultiplier, MidpointRounding.AwayFromZero);
        var orderId = CreateManualOrder(player.Id, 1, resourceTypeId, quantity, rewardCoins, tier.RewardGold, tier.RewardReputation);
        player.WithResource(resourceTypeId, quantity);
        var coinsBefore = player.Resource(ResourceIds.Coin);

        player.CompleteOrder(orderId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(quantity, Is.EqualTo(expectedQuantity));
            Assert.That(rewardCoins, Is.EqualTo(expectedRewardCoins));
            Assert.That(player.Resource(ResourceIds.Coin) - coinsBefore, Is.EqualTo(rewardCoins));
        }
    }

    /// <summary>
    /// Запуск рецепта на рыночном дворе списывает ровно 45 монет и производит 1 единицу указанного ресурса.
    /// </summary>
    /// <param name="receiptId">Идентификатор рецепта.</param>
    /// <param name="resourceTypeId">Тип производимого ресурса.</param>
    [TestCase(ReceiptIds.BuyStone, ResourceIds.Stone)]
    [TestCase(ReceiptIds.BuyWood, ResourceIds.Wood)]
    [TestCase(ReceiptIds.BuyClay, ResourceIds.Clay)]
    public void BuyReceiptWritesOffCoinsAndProducesResourceTest(int receiptId, int resourceTypeId)
    {
        const int purchaseCost = 45;
        const int producedAmount = 1;

        var player = TestPlayer.Create()
            .Buy(DomikIds.Market)
            .WithResource(ResourceIds.Coin, 20);

        var marketId = player.DomikId(DomikIds.Market);
        player.Upgrade(marketId);
        player.WithResource(ResourceIds.Coin, 515)
            .WithResource(ResourceIds.Stone, 15)
            .WithResource(ResourceIds.Wood, 15);
        player.Upgrade(marketId);
        var coinsBefore = player.Resource(ResourceIds.Coin);
        var resourceBefore = player.Resource(resourceTypeId);

        player.StartManufacture(marketId, receiptId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(coinsBefore - player.Resource(ResourceIds.Coin), Is.EqualTo(purchaseCost));
            Assert.That(player.Resource(resourceTypeId) - resourceBefore, Is.EqualTo(producedAmount));
        }
    }

    private static int CreateManualOrder(int playerId, int neighborId, int resourceTypeId, int quantity, int rewardCoins, int rewardGold, int rewardReputation)
    {
        using var scope = App.Scope();
        var now = DateTimeHelper.GetNowDate();
        var order = new Order
        {
            PlayerId = playerId,
            NeighborId = neighborId,
            CreateDate = now,
            ExpireDate = now.AddHours(4),
            RewardCoins = rewardCoins,
            RewardGold = rewardGold,
            RewardReputation = rewardReputation,
        };

        scope.Context.Orders.Add(order);
        scope.Context.SaveChanges();
        scope.Context.OrderResources.Add(new()
        {
            OrderId = order.Id,
            ResourceTypeId = resourceTypeId,
            Value = quantity,
        });

        scope.Commit();
        return order.Id;
    }

    private static (Order Order, OrderResource Resource)[] LoadOrders(int playerId)
    {
        return App.Read(context => context.Orders.Where(x => x.PlayerId == playerId)
            .Join(context.OrderResources, order => order.Id, resource => resource.OrderId, (order, resource) => new { order, resource })
            .ToArray()
            .Select(x => (x.order, x.resource))
            .ToArray());
    }

    private static void ClearOrders(int playerId)
    {
        using var scope = App.Scope();
        var orders = scope.Context.Orders.Where(x => x.PlayerId == playerId).ToArray();
        var orderIds = orders.Select(x => x.Id).ToArray();
        var resources = scope.Context.OrderResources.Where(x => orderIds.Contains(x.OrderId)).ToArray();
        scope.Context.OrderResources.RemoveRange(resources);
        scope.Context.Orders.RemoveRange(orders);
        scope.Commit();
    }
}
