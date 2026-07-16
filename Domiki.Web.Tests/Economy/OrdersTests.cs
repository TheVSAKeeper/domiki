using Domiki.Web.Economy.Models;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference.Models;
using Domiki.Web.Village;
using Order = Domiki.Web.Data.Entities.Order;

namespace Domiki.Web.Tests;

public sealed class OrdersTests
{
    /// <summary>
    /// Выполнить чужой заказ нельзя: бросается ошибка, а заказ остаётся на доске исходного игрока.
    /// </summary>
    [Test]
    public void CompleteForeignOrderThrowsTest()
    {
        var firstPlayer = TestPlayer.Create();

        var secondPlayer = TestPlayer.Create();

        var order = firstPlayer.Orders().First();
        var need = order.Resources.Single();
        secondPlayer.WithResource(need.Type.Id, need.Value);

        Assert.Throws<BusinessException>(() => secondPlayer.CompleteOrder(order.Id));

        Assert.That(firstPlayer.Orders().Any(x => x.Id == order.Id), Is.True);
    }

    /// <summary>
    /// Выполнение несуществующего заказа бросает ошибку.
    /// </summary>
    [Test]
    public void CompleteMissingOrderThrowsTest()
    {
        var player = TestPlayer.Create();

        Assert.Throws<BusinessException>(() => player.CompleteOrder(int.MaxValue));
    }

    /// <summary>
    /// Освободившийся после выполнения заказа слот доски не заполняется заново при повторных запросах доски.
    /// </summary>
    [Test]
    public void CompleteOrderHoldsSlotOnRepeatedFetchTest()
    {
        const int expectedOrderCount = 2;

        var player = TestPlayer.Create();
        var order = player.Orders().First();
        var need = order.Resources.Single();
        player.WithResource(need.Type.Id, need.Value);

        player.CompleteOrder(order.Id);

        var orders = player.Orders();
        Assert.That(orders.Count, Is.EqualTo(expectedOrderCount));
        Assert.That(orders.Any(x => x.Id == order.Id), Is.False);
    }

    /// <summary>
    /// Выполнение заказа при достаточных ресурсах списывает нужный ресурс, начисляет монеты, золото и репутацию соседа, а слот
    /// на доске остаётся пустым (не перезаполняется сразу).
    /// </summary>
    [Test]
    public void CompleteOrderWithEnoughResourcesWritesOffRewardsAndHoldsSlotTest()
    {
        const int expectedOrderCount = 2;

        var player = TestPlayer.Create();
        var order = player.Orders().First();
        var need = order.Resources.Single();
        player.WithResource(need.Type.Id, need.Value);
        var beforeResources = player.Resources();
        var beforeReputation = player.Reputation().First(x => x.Neighbor.Id == order.Neighbor.Id).Points;

        player.CompleteOrder(order.Id);

        var afterResources = player.Resources();
        var afterReputation = player.Reputation().First(x => x.Neighbor.Id == order.Neighbor.Id).Points;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ResourceValue(beforeResources, need.Type.Id) - ResourceValue(afterResources, need.Type.Id), Is.EqualTo(need.Value));
            Assert.That(ResourceValue(afterResources, ResourceIds.Coin) - ResourceValue(beforeResources, ResourceIds.Coin), Is.EqualTo(order.RewardCoins));
            Assert.That(ResourceValue(afterResources, ResourceIds.Gold) - ResourceValue(beforeResources, ResourceIds.Gold), Is.EqualTo(order.RewardGold));
            Assert.That(afterReputation - beforeReputation, Is.EqualTo(order.RewardReputation));
        }

        var orders = player.Orders();
        Assert.That(orders.Count, Is.EqualTo(expectedOrderCount));
        Assert.That(orders.Any(x => x.Id == order.Id), Is.False);
    }

    /// <summary>
    /// Выполнение заказа без нужных ресурсов бросает ошибку и не меняет ни ресурсы, ни репутацию, ни доску заказов.
    /// </summary>
    [Test]
    public void CompleteOrderWithoutResourcesThrowsAndKeepsStateTest()
    {
        const int expectedOrderCount = 3;

        var player = TestPlayer.Create();
        var order = player.Orders().First();
        var beforeResources = player.Resources();
        var beforeReputation = player.Reputation().Select(x => (x.Neighbor.Id, x.Points)).ToArray();

        Assert.Throws<BusinessException>(() => player.CompleteOrder(order.Id));

        var afterResources = player.Resources();
        var afterReputation = player.Reputation().Select(x => (x.Neighbor.Id, x.Points)).ToArray();
        var orders = player.Orders();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo(beforeResources.Select(x => (x.Type.Id, x.Value))));
            Assert.That(afterReputation, Is.EquivalentTo(beforeReputation));
            Assert.That(orders.Count, Is.EqualTo(expectedOrderCount));
        }

        Assert.That(orders.Any(x => x.Id == order.Id), Is.True);
    }

    /// <summary>
    /// Параллельные запросы доски заказов одного игрока сериализуются без ошибок конкурентности и сходятся на трёх заказах.
    /// </summary>
    [Test]
    public void ConcurrentGetOrdersSerializesWithoutConcurrencyExceptionTest()
    {
        const int expectedOrderCount = 3;

        for (var i = 1; i <= 30; i++)
        {
            var player = TestPlayer.Create();
            var numbers = Enumerable.Range(0, 8).ToList();

            Assert.DoesNotThrow(() => Parallel.ForEach(numbers, _ => player.Orders()), "iteration " + i);

            Assert.That(player.Orders().Count, Is.EqualTo(expectedOrderCount), "iteration " + i);
        }
    }

    /// <summary>
    /// Просроченный заказ, снятый планировщиком, убирается с доски, а его слот остаётся пустым (не заполняется сразу).
    /// </summary>
    [Test]
    public void FinishOrderRemovesExpiredOrderAndHoldsSlotTest()
    {
        const int expectedOrderCount = 2;

        var player = TestPlayer.Create();
        var order = player.Orders().First();

        player.FinishOrder(order.Id, order.ExpireDate.AddSeconds(1));

        var orders = player.Orders();
        Assert.That(orders.Count, Is.EqualTo(expectedOrderCount));
        Assert.That(orders.Any(x => x.Id == order.Id), Is.False);
    }

    /// <summary>
    /// Доска заказов нового игрока изначально заполнена ровно тремя заказами, каждый на один вид ресурса.
    /// </summary>
    [Test]
    public void GetOrdersNewPlayerCreatesThreeOrdersTest()
    {
        const int expectedOrderCount = 3;

        var player = TestPlayer.Create();

        var orders = player.Orders();

        Assert.That(orders.Count, Is.EqualTo(expectedOrderCount));
        Assert.That(orders.All(x => x.Resources.Length == 1), Is.True);
    }

    /// <summary>
    /// Игрок без единой постройки всё равно получает полную доску из трёх заказов.
    /// </summary>
    [Test]
    public void NewPlayerWithoutBuildingsStillGetsOrdersTest()
    {
        const int expectedOrderCount = 3;

        var player = TestPlayer.Create();

        var orders = player.Orders();

        Assert.That(orders.Count, Is.EqualTo(expectedOrderCount));
    }

    /// <summary>
    /// После истечения задержки на пополнение доска заказов снова заполняется до трёх заказов.
    /// </summary>
    [Test]
    public void OrderBoardRefillsAfterDelayElapsesTest()
    {
        const int expectedOrderCount = 3;

        var player = TestPlayer.Create();
        var order = player.Orders().First();
        var need = order.Resources.Single();
        player.WithResource(need.Type.Id, need.Value);

        player.CompleteOrder(order.Id);
        SetOrderRefillAt(player.Id, DateTimeHelper.GetNowDate().AddSeconds(-1));

        var orders = player.Orders();
        Assert.That(orders.Count, Is.EqualTo(expectedOrderCount));
    }

    /// <summary>
    /// Доска заказов просит только те ресурсы, которые деревня действительно способна производить.
    /// </summary>
    [Test]
    public void OrdersOnlyAskProducibleResourcesTest()
    {
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 10000);

        RaiseVillageLevelTo(player.Id, 10);
        player.Buy(DomikIds.ClayMine).Buy(DomikIds.LumberMill);

        for (var i = 0; i < 15; i++)
        {
            var orders = player.Orders();
            var resourceTypeIds = orders.SelectMany(x => x.Resources).Select(x => x.Type.Id).Distinct().ToArray();
            Assert.That(resourceTypeIds, Is.SubsetOf(new[] { ResourceIds.Wood, ResourceIds.Clay }));
            ClearOrders(player.Id);
        }
    }

    /// <summary>
    /// Репутация одного и того же соседа суммируется при выполнении нескольких его заказов подряд.
    /// </summary>
    [Test]
    public void ReputationAccumulatesForSameNeighborTest()
    {
        const int neighborId = 1;
        const int resourceTypeId = ResourceIds.Brick;
        const int expectedReputation = 7;

        var player = TestPlayer.Create();
        var firstOrderId = CreateManualOrder(player.Id, neighborId, resourceTypeId, 2, 100, 1, 3);
        var secondOrderId = CreateManualOrder(player.Id, neighborId, resourceTypeId, 3, 150, 2, 4);
        player.WithResource(resourceTypeId, 5);

        player.CompleteOrder(firstOrderId);
        player.CompleteOrder(secondOrderId);

        var reputation = player.Reputation().First(x => x.Neighbor.Id == neighborId);
        Assert.That(reputation.Points, Is.EqualTo(expectedReputation));
    }

    private static void RaiseVillageLevelTo(int playerId, int targetLevel)
    {
        var current = App.Act<VillageLevelCalculator, int>(m => m.GetLevel(playerId).Level);
        if (current >= targetLevel)
        {
            return;
        }

        var milestones = (int)Math.Ceiling((targetLevel - current) / (double)VillageLevelCalculator.ReputationWeight);
        GrantReputation(playerId, 1, milestones * VillageLevelCalculator.ReputationPointsPerMilestone);
    }

    private static void GrantReputation(int playerId, int neighborId, int points)
    {
        using var scope = App.Scope();
        var reputation = scope.Context.NeighborReputations.SingleOrDefault(x => x.PlayerId == playerId && x.NeighborId == neighborId);
        if (reputation == null)
        {
            reputation = new()
            {
                PlayerId = playerId,
                NeighborId = neighborId,
            };

            scope.Context.NeighborReputations.Add(reputation);
        }

        reputation.Points += points;
        scope.Commit();
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

    private static void SetOrderRefillAt(int playerId, DateTime value)
    {
        using var scope = App.Scope();
        var player = scope.Context.Players.Single(x => x.Id == playerId);
        player.NextOrderRefillAt = value;
        scope.Commit();
    }

    private static int CreateManualOrder(int playerId, int neighborId, int resourceTypeId, int value, int rewardCoins, int rewardGold, int rewardReputation)
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
            Value = value,
        });

        scope.Commit();
        return order.Id;
    }

    private static int ResourceValue(IReadOnlyList<Resource> resources, int typeId)
    {
        return resources.FirstOrDefault(x => x.Type.Id == typeId)?.Value ?? 0;
    }
}
