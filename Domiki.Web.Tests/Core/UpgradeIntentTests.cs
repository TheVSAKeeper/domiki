using Domiki.Web.Core;
using Domiki.Web.Economy;
using Domiki.Web.Reference;
using System.Text.Json;
using PlayerEventType = Domiki.Web.Data.Entities.PlayerEventType;

namespace Domiki.Web.Tests;

/// <summary>
/// Задумка: пометка постройки, на улучшение которой игрок копит, и её заповедь от нарядов.
/// </summary>
public sealed class UpgradeIntentTests
{
    private const int PotteryDomikId = 4;
    private const int ClayPerCycle = 2;
    private const int ClayMineNextLevel = 4;

    /// <summary>
    /// Задумка заповедует по каждому ресурсу цены столько, сколько уже накоплено, и никогда больше нужного.
    /// </summary>
    /// <param name="stockShare">Доля цены уровня, которая есть на складе: половина против двойного запаса.</param>
    [TestCase(0.5)]
    [TestCase(2.0)]
    public void IntentReservesAccruedShareTest(double stockShare)
    {
        const int clayMineDomikId = 4;

        var clayCost = UpgradeCost(DomikIds.ClayMine, ClayMineNextLevel, ResourceIds.Clay);
        var stock = (int)(clayCost * stockShare);

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.ElderHouse, 3)
            .WithDomik(DomikIds.ClayMine, ClayMineNextLevel - 1)
            .WithResource(ResourceIds.Clay, stock);

        player.SetUpgradeIntent(clayMineDomikId);

        Assert.That(player.IntentReserve(ResourceIds.Clay), Is.EqualTo(Math.Min(stock, clayCost)));
    }

    /// <summary>
    /// Заповедь задумки останавливает наряд, не списывая припас, и называет копилку в событии
    /// <see cref="PlayerEventType.ManufactureReserveHeld"/>.
    /// </summary>
    [Test]
    public void IntentStopsNaryadTest()
    {
        const int startClay = 4;

        const int clayMineDomikId = 6;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.Pottery, 3)
            .WithDomik(DomikIds.ElderHouse, 3)
            .WithDomik(DomikIds.ClayMine, ClayMineNextLevel - 1)
            .WithResource(ResourceIds.Clay, startClay);

        player.SetUpgradeIntent(clayMineDomikId);

        using (App.PendingEvents())
        {
            player.StartManufacture(PotteryDomikId, ReceiptIds.MakeDishes, autoRepeat: true);
        }

        var manufacture = player.Manufacture(PotteryDomikId);
        player.FinishManufacture(manufacture.Id, manufacture.FinishDate.AddSeconds(1));

        var events = App.Read(context => context.PlayerEvents
            .Where(x => x.PlayerId == player.Id && x.Type == PlayerEventType.ManufactureReserveHeld)
            .Select(x => x.Data)
            .ToList());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(JsonDocument.Parse(events[0]).RootElement.GetProperty("intent").GetBoolean(), Is.True);
            Assert.That(player.Resource(ResourceIds.Clay), Is.EqualTo(startClay - ClayPerCycle));
        }
    }

    /// <summary>
    /// Заповедь задумки открывается заповедным ларём: без третьего уровня Избы старосты копилка ничего не удерживает.
    /// </summary>
    [Test]
    public void IntentReservesNeedElderHouseTest()
    {
        const int startClay = 4;

        const int clayMineDomikId = 5;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.Pottery, 3)
            .WithDomik(DomikIds.ClayMine, ClayMineNextLevel - 1)
            .WithResource(ResourceIds.Clay, startClay);

        player.SetUpgradeIntent(clayMineDomikId);

        using (App.PendingEvents())
        {
            player.StartManufacture(PotteryDomikId, ReceiptIds.MakeDishes, autoRepeat: true);
        }

        var manufacture = player.Manufacture(PotteryDomikId);
        player.FinishManufacture(manufacture.Id, manufacture.FinishDate.AddSeconds(1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.IntentReserve(ResourceIds.Clay), Is.Zero);
            Assert.That(player.Resource(ResourceIds.Clay), Is.EqualTo(startClay - ClayPerCycle * 2));
        }
    }

    /// <summary>
    /// Задумка уходит сама, когда улучшение начато: цель достигнута, копилке больше нечего держать.
    /// </summary>
    [Test]
    public void IntentClearsOnUpgradeTest()
    {
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, UpgradeCost(DomikIds.ClayMine, 2, ResourceIds.Coin));

        player.SetUpgradeIntent(StartingDomikIds.ClayMine);

        using (App.PendingEvents())
        {
            player.Upgrade(StartingDomikIds.ClayMine);
        }

        Assert.That(player.UpgradeIntent(), Is.Null);
    }

    /// <summary>
    /// Задумать можно только достроенную постройку: возводимая ещё не имеет уровня, с которого копить.
    /// </summary>
    [Test]
    public void IntentRejectsBuildingDomikTest()
    {
        const int buildingDomikId = 3;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.ClayMine, 0);

        Assert.That(Throws.Business(() => player.SetUpgradeIntent(buildingDomikId)).Message, Does.Contain("возводится"));
    }

    /// <summary>
    /// Задумать идущее улучшение нельзя: копить уже не на что, цена списана.
    /// </summary>
    [Test]
    public void IntentRejectsUpgradingDomikTest()
    {
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, UpgradeCost(DomikIds.ClayMine, 2, ResourceIds.Coin));

        using (App.PendingEvents())
        {
            player.Upgrade(StartingDomikIds.ClayMine);

            Assert.That(Throws.Business(() => player.SetUpgradeIntent(StartingDomikIds.ClayMine)).Message, Does.Contain("уже улучшается"));
        }
    }

    private static int UpgradeCost(int domikTypeId, int level, int resourceTypeId)
    {
        return App.Act<ResourceManager, int>(manager => manager.GetDomikTypes()
            .First(x => x.Id == domikTypeId)
            .Levels.First(x => x.Value == level)
            .Resources.Where(x => x.Type.Id == resourceTypeId)
            .Sum(x => x.Value));
    }
}

file static class UpgradeIntentTestsActs
{
    public static TestPlayer SetUpgradeIntent(this TestPlayer p, int? domikId)
    {
        App.Act<DomikManager>(m => m.SetUpgradeIntent(p.Id, domikId));
        return p;
    }

    public static int? UpgradeIntent(this TestPlayer p)
    {
        return App.Act<DomikManager, int?>(m => m.GetUpgradeIntent(p.Id));
    }

    public static int IntentReserve(this TestPlayer p, int resourceTypeId)
    {
        return App.Act<ElderHouseManager, int>(m => m.GetIntentReserves(p.Id)
            .Where(x => x.ResourceTypeId == resourceTypeId)
            .Sum(x => x.Reserve));
    }
}
