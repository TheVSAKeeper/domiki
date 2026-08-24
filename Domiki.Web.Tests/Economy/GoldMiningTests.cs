using Domiki.Web.Core;
using Domiki.Web.Infrastructure;
using System.Text.Json;
using Order = Domiki.Web.Data.Entities.Order;
using PlayerEventType = Domiki.Web.Data.Entities.PlayerEventType;

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
    /// Золотая шахта выдаёт не больше уровня шахты золота в сутки, а при выбранной жиле новая смена не запускается вовсе.
    /// </summary>
    /// <param name="mineLevel">Уровень золотой шахты.</param>
    [TestCase(1)]
    [TestCase(3)]
    [TestCase(5)]
    public void GoldMineGrantsAtMostMineLevelPerDayTest(int mineLevel)
    {
        AssumeFarFromUtcMidnight();

        var player = TestPlayer.Create()
            .WithGoldMine(mineLevel);

        var goldMineId = player.DomikId(DomikIds.GoldMine);
        var beforeGold = player.Resource(ResourceIds.Gold);

        for (var i = 0; i < mineLevel; i++)
        {
            player.StartManufacture(goldMineId, ReceiptIds.GoldDig);
        }

        var afterGold = player.Resource(ResourceIds.Gold);
        SetGoldMinedDate(player.Id, DateTimeHelper.GetNowDate().Date);
        var blocked = Assert.Throws<BusinessException>(() => player.StartManufacture(goldMineId, ReceiptIds.GoldDig));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterGold - beforeGold, Is.EqualTo(mineLevel));
            Assert.That(GetGoldMinedToday(player.Id), Is.EqualTo(mineLevel));
            Assert.That(blocked.Message, Does.StartWith("Жила на сегодня выбрана"));
        }
    }

    /// <summary>
    /// Наряд на добычу золота при выбранной жиле не возобновляется: рудник и трудяга свободны, в журнале своя запись о жиле,
    /// а не «наряд заглох».
    /// </summary>
    [Test]
    public void GoldDigAutoRepeatStopsWhenVeinSpentTest()
    {
        const int mineLevel = 1;

        AssumeFarFromUtcMidnight();

        var player = TestPlayer.Create()
            .WithGoldMine(mineLevel);

        var goldMineId = player.DomikId(DomikIds.GoldMine);
        var beforeGold = player.Resource(ResourceIds.Gold);

        player.StartManufacture(goldMineId, ReceiptIds.GoldDig, autoRepeat: true);

        var capEvents = App.Read(context => context.PlayerEvents.Where(x => x.PlayerId == player.Id && x.Type == PlayerEventType.ManufactureGoldCapReached).ToList());
        var repeatFailedCount = App.Read(context => context.PlayerEvents.Count(x => x.PlayerId == player.Id && x.Type == PlayerEventType.ManufactureRepeatFailed));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Resource(ResourceIds.Gold) - beforeGold, Is.EqualTo(mineLevel));
            Assert.That(player.GoldManufactureCount(goldMineId), Is.Zero);
            Assert.That(player.Workers().All(x => x.ManufactureId == null), Is.True);
            Assert.That(capEvents, Has.Count.EqualTo(1));
            Assert.That(repeatFailedCount, Is.Zero);
        }

        using var data = JsonDocument.Parse(capEvents[0].Data);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(data.RootElement.GetProperty("mined").GetInt32(), Is.EqualTo(mineLevel));
            Assert.That(data.RootElement.GetProperty("cap").GetInt32(), Is.EqualTo(mineLevel));
        }
    }

    /// <summary>
    /// Пока идущая золотая смена заберёт остаток жилы, вторая золотая смена в том же руднике не запускается.
    /// </summary>
    [Test]
    public void GoldDigStartBlockedWhenRemainderReservedByRunningShiftTest()
    {
        const int mineLevel = 3;
        const int minedAlready = 2;

        AssumeFarFromUtcMidnight();

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack, 2)
            .WithDomik(DomikIds.GoldMine, mineLevel);

        var goldMineId = player.DomikId(DomikIds.GoldMine);
        SetGoldMined(player.Id, DateTimeHelper.GetNowDate().Date, minedAlready);

        using (App.PendingEvents())
        {
            player.StartManufacture(goldMineId, ReceiptIds.GoldDig);
            var blocked = Assert.Throws<BusinessException>(() => player.StartManufacture(goldMineId, ReceiptIds.GoldDig));
            Assert.That(blocked.Message, Does.Contain("на вороте"));
        }
    }

    /// <summary>
    /// Счётчик добычи общий на двор, поэтому идущая смена одного рудника занимает остаток жилы и для второго рудника.
    /// </summary>
    [Test]
    public void GoldDigStartBlockedWhenRemainderReservedByAnotherMineTest()
    {
        const int mineLevel = 3;
        const int minedAlready = 2;

        AssumeFarFromUtcMidnight();

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack, 2)
            .WithDomik(DomikIds.GoldMine, mineLevel)
            .WithDomik(DomikIds.GoldMine, mineLevel);

        var mineIds = player.Domiks().Where(x => x.Type.Id == DomikIds.GoldMine).Select(x => x.Id).ToList();
        SetGoldMined(player.Id, DateTimeHelper.GetNowDate().Date, minedAlready);

        using (App.PendingEvents())
        {
            player.StartManufacture(mineIds[0], ReceiptIds.GoldDig);
            var blocked = Assert.Throws<BusinessException>(() => player.StartManufacture(mineIds[1], ReceiptIds.GoldDig));
            Assert.That(blocked.Message, Does.Contain("на вороте"));
        }
    }

    /// <summary>
    /// Ускорение золотой смены при выбранной жиле запрещено – золото за ускорение не списывается впустую.
    /// </summary>
    [Test]
    public void GoldDigHurryBlockedWhenVeinSpentTest()
    {
        const int mineLevel = 1;
        const int goldStock = 5;

        var player = TestPlayer.Create()
            .WithGoldMine(mineLevel)
            .WithResource(ResourceIds.Gold, goldStock);

        var goldMineId = player.DomikId(DomikIds.GoldMine);
        var beforeGold = player.Resource(ResourceIds.Gold);

        using (App.PendingEvents())
        {
            player.StartManufacture(goldMineId, ReceiptIds.GoldDig);
            var manufactureId = player.Manufacture(goldMineId).Id;
            SetGoldMined(player.Id, DateTimeHelper.GetNowDate().Date, mineLevel);

            var blocked = Assert.Throws<BusinessException>(() => player.HurryManufacture(manufactureId));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(blocked.Message, Does.StartWith("Жила на сегодня выбрана"));
                Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(beforeGold));
            }
        }
    }

    /// <summary>
    /// Снимок добычи за сегодня отдаёт счётчик текущих суток, а за прошлые сутки – ноль, хотя сам счётчик в БД не обнулён.
    /// </summary>
    [Test]
    public void GoldMinedTodaySnapshotIsZeroForPastDateTest()
    {
        const int expectedMinedToday = 1;

        var player = TestPlayer.Create()
            .WithGoldMine(1);

        var goldMineId = player.DomikId(DomikIds.GoldMine);
        player.StartManufacture(goldMineId, ReceiptIds.GoldDig);
        SetGoldMinedDate(player.Id, DateTimeHelper.GetNowDate().Date);
        var minedToday = App.Act<DomikManager, int>(m => m.GetGoldMinedToday(player.Id));

        SetGoldMinedDate(player.Id, DateTimeHelper.GetNowDate().AddDays(-1).Date);
        var minedYesterdayView = App.Act<DomikManager, int>(m => m.GetGoldMinedToday(player.Id));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(minedToday, Is.EqualTo(expectedMinedToday));
            Assert.That(minedYesterdayView, Is.Zero);
            Assert.That(GetGoldMinedToday(player.Id), Is.EqualTo(expectedMinedToday));
        }
    }

    private static void AssumeFarFromUtcMidnight()
    {
        Assume.That(DateTimeHelper.GetNowDate().Hour, Is.LessThan(21), "у полуночи UTC смена переваливает в новые сутки и жила считается заново");
    }

    private static int GetGoldMinedToday(int playerId)
    {
        return App.Read(context => context.Players.Single(x => x.Id == playerId).GoldMinedToday);
    }

    private static void SetGoldMined(int playerId, DateTime date, int mined)
    {
        using var scope = App.Scope();
        var player = scope.Context.Players.Single(x => x.Id == playerId);
        player.GoldMinedDate = date;
        player.GoldMinedToday = mined;
        scope.Commit();
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

    public static int GoldManufactureCount(this TestPlayer player, int domikId)
    {
        return App.Read(context => context.Manufactures.Count(x => x.DomikPlayerId == player.Id && x.DomikId == domikId));
    }
}
