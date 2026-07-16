using Domiki.Web.Infrastructure;
using Order = Domiki.Web.Data.Entities.Order;

namespace Domiki.Web.Tests;

public sealed class GoldMiningTests
{
    /// <summary>
    /// Суточный лимит добычи золота сбрасывается с наступлением следующего дня.
    /// </summary>
    [Test]
    public void GoldMineResetsCapOnNextDayTest()
    {
        const int expectedGold = 2;
        const int expectedMinedToday = 1;

        var player = TestPlayer.Create()
            .WithGoldMine(1);

        var goldMineId = player.DomikId(DomikIds.GoldMine);
        player.StartManufacture(goldMineId, ReceiptIds.GoldDig);
        SetGoldMinedDate(player.Id, DateTimeHelper.GetNowDate().AddDays(-1).Date);
        player.StartManufacture(goldMineId, ReceiptIds.GoldDig);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(expectedGold));
            Assert.That(GetGoldMinedToday(player.Id), Is.EqualTo(expectedMinedToday));
        }
    }

    /// <summary>
    /// Золото за выполнение заказов не ограничено суточным лимитом золотой шахты.
    /// </summary>
    [Test]
    public void OrderGoldIsNotLimitedByGoldMineCapTest()
    {
        const int rewardGold = 7;
        const int expectedMinedToday = 1;

        var player = TestPlayer.Create()
            .WithGoldMine(1);

        var goldMineId = player.DomikId(DomikIds.GoldMine);
        player.StartManufacture(goldMineId, ReceiptIds.GoldDig);
        var orderId = CreateManualOrder(player.Id, 1, ResourceIds.Coin, 1, 0, rewardGold, 1);
        var beforeGold = player.Resource(ResourceIds.Gold);

        player.CompleteOrder(orderId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Resource(ResourceIds.Gold) - beforeGold, Is.EqualTo(rewardGold));
            Assert.That(GetGoldMinedToday(player.Id), Is.EqualTo(expectedMinedToday));
        }
    }

    /// <summary>
    /// Золотая шахта выдаёт не больше уровня шахты золота в сутки, сколько бы раз её ни запускали.
    /// </summary>
    /// <param name="mineLevel">Уровень золотой шахты.</param>
    [TestCase(1)]
    [TestCase(3)]
    [TestCase(5)]
    public void GoldMineGrantsAtMostMineLevelPerDayTest(int mineLevel)
    {
        var player = TestPlayer.Create()
            .WithGoldMine(mineLevel);

        var goldMineId = player.DomikId(DomikIds.GoldMine);
        var beforeGold = player.Resource(ResourceIds.Gold);

        for (var i = 0; i < mineLevel + 2; i++)
        {
            player.StartManufacture(goldMineId, ReceiptIds.GoldDig);
        }

        var afterGold = player.Resource(ResourceIds.Gold);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterGold - beforeGold, Is.EqualTo(mineLevel));
            Assert.That(GetGoldMinedToday(player.Id), Is.EqualTo(mineLevel));
        }
    }

    private static int GetGoldMinedToday(int playerId)
    {
        return App.Read(context => context.Players.Single(x => x.Id == playerId).GoldMinedToday);
    }

    private static void SetGoldMinedDate(int playerId, DateTime value)
    {
        using var scope = App.Scope();
        var player = scope.Context.Players.Single(x => x.Id == playerId);
        player.GoldMinedDate = value;
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
}

file static class GoldMiningTestsActs
{
    public static TestPlayer WithGoldMine(this TestPlayer player, int level)
    {
        return player.WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.GoldMine, level);
    }
}
