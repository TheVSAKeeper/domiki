using Domiki.Web.Infrastructure;
using Domiki.Web.Village;

namespace Domiki.Web.Tests;

public sealed class DecorTests
{
    private const int ZarechieNeighborId = 1;
    private const int FatigueThresholdSeconds = 8 * 3600;
    private const int RestSeconds = 2 * 3600;

    /// <summary>
    /// Без достаточного количества ресурсов покупка декора падает с ошибкой «Недостаточно...», а владение и уют не меняются.
    /// </summary>
    [Test]
    public void BuyDecorWithoutResourcesThrowsAndDoesNotChangeOwnedTest()
    {
        var player = TestPlayer.Create();

        var ex = Throws.Business(() => player.BuyDecor(DecorIds.Fence));

        Assert.That(ex.Message, Does.StartWith("Недостаточно "));
        var decor = player.Decor();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(decor.Owned, Is.Empty);
            Assert.That(decor.Comfort, Is.Zero);
        }
    }

    /// <summary>
    /// Декор, открываемый репутацией с соседом, нельзя купить без нужной репутации – ошибка упоминает «репутац».
    /// </summary>
    [Test]
    public void BuyGatedDecorWithoutReputationThrowsTest()
    {
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Brick, 20)
            .WithResource(ResourceIds.Block, 10);

        var ex = Throws.Business(() => player.BuyDecor(DecorIds.BrickArch));

        Assert.That(ex.Message, Does.Contain("репутац"));
    }

    /// <summary>
    /// При достижении нужного порога репутации с соседом декор, требующий репутацию, покупается успешно.
    /// </summary>
    [Test]
    public void BuyGatedDecorWithReputationSucceedsTest()
    {
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Brick, 20)
            .WithResource(ResourceIds.Block, 10)
            .WithReputation(ZarechieNeighborId, 30);

        player.BuyDecor(DecorIds.BrickArch);

        Assert.That(player.Decor().Owned.Single(x => x.DecorTypeId == DecorIds.BrickArch).Count, Is.EqualTo(1));
    }

    /// <summary>
    /// Декор, помеченный как непокупаемый (идол на тропе), нельзя купить – падает с ошибкой «Этот декор нельзя купить»,
    /// владение не меняется.
    /// </summary>
    [Test]
    public void BuyNonPurchasableDecorThrowsAndDoesNotChangeOwnedTest()
    {
        var player = TestPlayer.Create();

        var ex = Throws.Business(() => player.BuyDecor(DecorIds.TrailIdol));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("Этот декор нельзя купить"));
            Assert.That(player.Decor().Owned, Is.Empty);
        }
    }

    /// <summary>
    /// Покупка декора по несуществующему типу падает с ошибкой «Декор не найден».
    /// </summary>
    [Test]
    public void BuyUnknownDecorThrowsTest()
    {
        var player = TestPlayer.Create();

        var ex = Throws.Business(() => player.BuyDecor(999));

        Assert.That(ex.Message, Is.EqualTo("Декор не найден"));
    }

    /// <summary>
    /// Уют от владения декором вносит вклад в уровень деревни с фиксированным весом (VillageLevelCalculator.ComfortWeight).
    /// </summary>
    [Test]
    public void ComfortIncreasesVillageLevelTest()
    {
        var player = TestPlayer.Create();
        var before = player.GetVillageLevel();

        player.WithDecor(DecorIds.Fountain, 2);

        var after = player.GetVillageLevel();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(after.Comfort, Is.EqualTo(16));
            Assert.That(after.Level, Is.EqualTo(before.Level + 16 * VillageLevelCalculator.ComfortWeight));
        }
    }

    /// <summary>
    /// Сокращение отдыха от уюта ограничено сверху: сколько бы уюта ни было, отдых не сокращается больше чем наполовину.
    /// </summary>
    [Test]
    public void ComfortRestReductionIsCappedAtHalfTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.LumberMill);

        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, 1);
        player.WithDecor(DecorIds.Fountain, 10);
        player.SetWorkerWorked(worker.Id, FatigueThresholdSeconds - RestSeconds);

        using (App.PendingEvents())
        {
            player.StartManufacture(3, ReceiptIds.WoodDig8h);
        }

        var manufacture = player.Manufacture(3);
        var finishDate = manufacture.FinishDate.AddSeconds(1);
        player.FinishManufacture(manufacture.Id, finishDate);

        worker = player.Workers().Single();
        Assert.That(worker.RestUntil, Is.EqualTo(finishDate.AddSeconds(RestSeconds / 2)));
    }

    /// <summary>
    /// Уют сокращает время отдыха уставшего трудяги на процент, зависящий от накопленного уюта (здесь – на 8%, отдых
    /// сокращается до 92%).
    /// </summary>
    [Test]
    public void ComfortShortensWorkerRestTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.LumberMill);

        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, 1);
        player.WithDecor(DecorIds.Fountain, 1);
        player.SetWorkerWorked(worker.Id, FatigueThresholdSeconds - RestSeconds);

        using (App.PendingEvents())
        {
            player.StartManufacture(3, ReceiptIds.WoodDig8h);
        }

        var manufacture = player.Manufacture(3);
        var finishDate = manufacture.FinishDate.AddSeconds(1);
        player.FinishManufacture(manufacture.Id, finishDate);

        worker = player.Workers().Single();
        Assert.That(worker.RestUntil, Is.EqualTo(finishDate.AddSeconds(RestSeconds * 92 / 100)));
    }

    /// <summary>
    /// Новый игрок видит полный каталог декора с правильной покупаемостью (идол и баннер странника не продаются) и стартует
    /// без декора и с нулевым уютом.
    /// </summary>
    [Test]
    public void GetDecorForNewPlayerReturnsTypesAndZeroComfortTest()
    {
        var player = TestPlayer.Create();

        var decor = player.Decor();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(decor.Types.Select(x => x.LogicName), Is.EquivalentTo(["fence", "flowerbed", "garden", "fountain", "bench", "trail_idol", "wanderer_banner", "brick_arch", "lantern"]));
            Assert.That(decor.Types.Where(x => x.LogicName is "trail_idol" or "wanderer_banner").All(x => !x.IsPurchasable), Is.True);
            Assert.That(decor.Types.Where(x => x.LogicName is not ("trail_idol" or "wanderer_banner")).All(x => x.IsPurchasable), Is.True);
            Assert.That(decor.Owned, Is.Empty);
            Assert.That(decor.Comfort, Is.Zero);
        }
    }

    /// <summary>
    /// Выдача декора напрямую через менеджера суммирует количество при повторных вызовах, а не перезаписывает его.
    /// </summary>
    [Test]
    public void GrantDecorViaManagerIncrementsPlayerDecorTest()
    {
        var player = TestPlayer.Create();

        player.GrantDecorViaManager(DecorIds.TrailIdol, 1);
        player.GrantDecorViaManager(DecorIds.TrailIdol, 2);

        Assert.That(player.Decor().Owned.Single(x => x.DecorTypeId == DecorIds.TrailIdol).Count, Is.EqualTo(3));
    }

    /// <summary>
    /// Трудяга, ещё не достигший порога усталости, не уходит отдыхать вовсе, независимо от уровня уюта.
    /// </summary>
    [Test]
    public void NoFatigueWorkerDoesNotRestRegardlessComfortTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.LumberMill);

        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, 4);
        player.WithDecor(DecorIds.Fountain, 10);
        player.SetWorkerWorked(worker.Id, FatigueThresholdSeconds);

        using (App.PendingEvents())
        {
            player.StartManufacture(3, ReceiptIds.WoodDig8h);
        }

        var manufacture = player.Manufacture(3);
        player.FinishManufacture(manufacture.Id, manufacture.FinishDate.AddSeconds(1));

        worker = player.Workers().Single();
        Assert.That(worker.RestUntil, Is.Null);
    }

    /// <summary>
    /// Покупка декора списывает его стоимость в ресурсах и добавляет к общему уюту фиксированное количество очков за
    /// экземпляр.
    /// </summary>
    /// <param name="decorTypeId">Тип декора.</param>
    /// <param name="comfortPoints">Уют, который даёт один экземпляр декора.</param>
    [TestCase(DecorIds.Fence, 2)]
    [TestCase(DecorIds.Flowerbed, 3)]
    [TestCase(DecorIds.Garden, 5)]
    [TestCase(DecorIds.Fountain, 8)]
    public void BuyDecorWritesOffResourcesAndIncreasesComfortTest(int decorTypeId, int comfortPoints)
    {
        const int multiplier = 2;

        var player = TestPlayer.Create();
        var type = player.Decor().Types.Single(x => x.Id == decorTypeId);
        foreach (var cost in type.Cost)
        {
            player.WithResource(cost.Type.Id, cost.Value * multiplier);
        }

        var before = type.Cost.ToDictionary(x => x.Type.Id, x => player.Resource(x.Type.Id));

        player.BuyDecor(decorTypeId);
        player.BuyDecor(decorTypeId);

        var decor = player.Decor();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(decor.Owned.Single(x => x.DecorTypeId == decorTypeId).Count, Is.EqualTo(multiplier));
            Assert.That(decor.Comfort, Is.EqualTo(comfortPoints * multiplier));
        }

        foreach (var cost in type.Cost)
        {
            Assert.That(player.Resource(cost.Type.Id), Is.EqualTo(before[cost.Type.Id] - cost.Value * multiplier));
        }
    }
}
