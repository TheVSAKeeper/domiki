using Domiki.Web.Activities;
using Domiki.Web.Activities.Models;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Tests;

public sealed class BlueprintsTests
{
    private const int BorovoeId = 2;

    /// <summary>
    /// Покупка каменотёсни без владения её чертежом бросает исключение с упоминанием чертежа.
    /// </summary>
    [Test]
    public void BuyStonecutterWithoutBlueprintThrowsTest()
    {
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 1000);

        var ex = Throws.Business(() => player.Buy(DomikIds.Stonecutter));

        Assert.That(ex.Message, Does.Contain("чертёж"));
    }

    /// <summary>
    /// Чертёж фактически выдаётся лениво, в момент покупки постройки, а не заранее при достижении порога репутации.
    /// </summary>
    [Test]
    public void BuyWorkshopLazilyGrantsBlueprintTest()
    {
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 1000)
            .WithReputation(BorovoeId, 30);

        Assert.That(player.BlueprintCount(BlueprintIds.Workshop), Is.Zero);

        player.Buy(DomikIds.Workshop);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.BlueprintCount(BlueprintIds.Workshop), Is.EqualTo(1));
            Assert.That(player.Domiks().Count(x => x.Type.Id == DomikIds.Workshop), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// При достаточной репутации чертёж считается выданным и покупка постройки проходит успешно.
    /// </summary>
    [Test]
    public void BuyWorkshopWithBlueprintSucceedsTest()
    {
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 1000)
            .WithReputation(BorovoeId, 30);

        player.Buy(DomikIds.Workshop);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Domiks().Count(x => x.Type.Id == DomikIds.Workshop), Is.EqualTo(1));
            Assert.That(player.BlueprintCount(BlueprintIds.Workshop), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Покупка постройки без нужного чертежа бросает исключение и не трогает ресурсы игрока.
    /// </summary>
    [Test]
    public void BuyWorkshopWithoutBlueprintThrowsAndKeepsResourcesTest()
    {
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 1000)
            .WithReputation(BorovoeId, 29);

        var before = player.Resources();
        var ex = Throws.Business(() => player.Buy(DomikIds.Workshop));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("Нужен чертёж (репутация Боровое 30)"));
            Assert.That(player.Resources().Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo(before.Select(x => (x.Type.Id, x.Value))));
            Assert.That(player.Domiks().Count(x => x.Type.Id == DomikIds.Workshop), Is.Zero);
        }
    }

    /// <summary>
    /// Рецепт мастерской превращает доски в мебель, а рецепт рынка продаёт мебель за монеты.
    /// </summary>
    [Test]
    public void FurnitureRecipeAndSaleTest()
    {
        const int furnitureSaleCoins = 95;

        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 700)
            .WithReputation(BorovoeId, 30)
            .WithResource(ResourceIds.Board, 2);

        player.Buy(DomikIds.Barrack).Buy(DomikIds.Market).Buy(DomikIds.Workshop);
        var workshopId = player.DomikId(DomikIds.Workshop);

        player.StartManufacture(workshopId, ReceiptIds.MakeFurniture);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Resource(ResourceIds.Board), Is.Zero);
            Assert.That(player.Resource(ResourceIds.Furniture), Is.EqualTo(1));
        }

        var coinBeforeSell = player.Resource(ResourceIds.Coin);
        var marketId = player.Domiks().First(x => x.Type.Id == DomikIds.Market && x.Level > 0).Id;
        player.StartManufacture(marketId, ReceiptIds.SellFurniture);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Resource(ResourceIds.Furniture), Is.Zero);
            Assert.That(player.Resource(ResourceIds.Coin) - coinBeforeSell, Is.EqualTo(furnitureSaleCoins));
        }
    }

    /// <summary>
    /// Новый игрок видит чертёж мастерской заблокированным, привязанным к соседу Боровое, при нулевой репутации.
    /// </summary>
    [Test]
    public void GetBlueprintsNewPlayerReturnsWorkshopBlueprintLockedTest()
    {
        var player = TestPlayer.Create();

        var blueprint = player.Blueprints().Single(x => x.Blueprint.Id == BlueprintIds.Workshop);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(blueprint.Blueprint.DomikTypeId, Is.EqualTo(DomikIds.Workshop));
            Assert.That(blueprint.Neighbor.Id, Is.EqualTo(BorovoeId));
            Assert.That(blueprint.CurrentReputation, Is.Zero);
            Assert.That(blueprint.Owned, Is.False);
        }
    }

    /// <summary>
    /// Повторная выдача уже выданного чертежа идемпотентна: возвращает false и не меняет владение.
    /// </summary>
    [Test]
    public void GrantBlueprintIsIdempotentTest()
    {
        var player = TestPlayer.Create();

        var first = player.GrantBlueprint(BlueprintIds.Stonecutter);
        var second = player.GrantBlueprint(BlueprintIds.Stonecutter);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(player.OwnsBlueprint(BlueprintIds.Stonecutter), Is.True);
        }
    }

    /// <summary>
    /// Порог репутации выдаёт чертёж ровно один раз, повторное чтение не дублирует владение.
    /// </summary>
    /// <param name="reputation">Накопленная репутация с соседом.</param>
    /// <param name="expectedOwned">Ожидается ли владение чертежом.</param>
    [TestCase(29, false)]
    [TestCase(30, true)]
    [TestCase(31, true)]
    public void ReputationThresholdGrantsBlueprintOnceTest(int reputation, bool expectedOwned)
    {
        var player = TestPlayer.Create()
            .WithReputation(BorovoeId, reputation);

        var first = player.Blueprints().Single(x => x.Blueprint.Id == BlueprintIds.Workshop);
        var second = player.Blueprints().Single(x => x.Blueprint.Id == BlueprintIds.Workshop);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.Owned, Is.EqualTo(expectedOwned));
            Assert.That(second.Owned, Is.EqualTo(expectedOwned));
            Assert.That(player.BlueprintCount(BlueprintIds.Workshop), Is.EqualTo(expectedOwned ? 1 : 0));
        }
    }
}

file static class BlueprintsTestsActs
{
    public static IReadOnlyList<PlayerBlueprint> Blueprints(this TestPlayer p)
    {
        return App.Act<BlueprintManager, IReadOnlyList<PlayerBlueprint>>(m => m.GetBlueprints(p.Id).ToList());
    }

    public static bool GrantBlueprint(this TestPlayer p, int blueprintId)
    {
        return App.Act<BlueprintManager, bool>(m => m.GrantBlueprint(p.Id, blueprintId));
    }

    public static bool OwnsBlueprint(this TestPlayer p, int blueprintId)
    {
        return App.Act<BlueprintManager, bool>(m => m.IsOwned(p.Id, blueprintId));
    }

    public static int BlueprintCount(this TestPlayer p, int blueprintId)
    {
        return App.Read(context => context.PlayerBlueprints.Count(x => x.PlayerId == p.Id && x.BlueprintId == blueprintId));
    }
}
