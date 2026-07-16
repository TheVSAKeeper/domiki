using Domiki.Web.Core.Models;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Reference.Models;

namespace Domiki.Web.Tests;

public sealed class BalanceRecalibrationTests
{
    /// <summary>
    /// Черта характера и наработанный навык рабочего множат длительность производства уже после применения бонуса инструмента.
    /// </summary>
    [Test]
    public void DurationMultiplierUsesTraitsAndSkillAfterToolTest()
    {
        const int expectedDurationSeconds = 19584;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.ClayMine)
            .WithResource(ResourceIds.Tool, 1);

        var clayMineId = player.DomikId(DomikIds.ClayMine);
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, 3)
            .SetWorkerSkill(worker.Id, DomikIds.ClayMine, 100);

        var start = DateTimeHelper.GetNowDate();
        using (App.PendingEvents())
        {
            player.StartManufacture(clayMineId, ReceiptIds.ClayDig8h, [worker.Id], true);
        }

        var manufacture = player.Manufacture(clayMineId);
        // 8ч база (28800с), умноженная на модификаторы черты характера и наработанного навыка рабочего после бонуса инструмента
        Assert.That((manufacture.FinishDate - start).TotalSeconds, Is.EqualTo(expectedDurationSeconds).Within(1));
    }

    /// <summary>
    /// После рекалибровки золотая шахта сохраняет рецепт добычи золота, но теряет рецепты 17 и 21, а рынок теряет рецепты 8 и
    /// 12.
    /// </summary>
    [Test]
    public void GoldMineAndMarketReceiptsAreRecalibratedTest()
    {
        var types = GetDomikTypes();

        var goldMineReceiptIds = types.Single(x => x.Id == DomikIds.GoldMine)
            .Levels
            .SelectMany(x => x.Receipts)
            .Select(x => x.Id)
            .ToArray();

        var marketReceiptIds = types.Single(x => x.Id == DomikIds.Market)
            .Levels
            .SelectMany(x => x.Receipts)
            .Select(x => x.Id)
            .ToArray();

        Assert.That(goldMineReceiptIds, Does.Contain(ReceiptIds.GoldDig));
        Assert.That(goldMineReceiptIds, Does.Not.Contain(17));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(goldMineReceiptIds, Does.Not.Contain(21));
            Assert.That(marketReceiptIds, Does.Not.Contain(8));
        }

        Assert.That(marketReceiptIds, Does.Not.Contain(12));
    }

    /// <summary>
    /// Рецепт обжига кирпича в гончарной мастерской тратит 16 глины и выдаёт 8 кирпичей за смену.
    /// </summary>
    [Test]
    public void PotteryBrickShiftConsumesClayAndProducesBricksTest()
    {
        const int consumedClay = 16;
        const int producedBricks = 8;

        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 700)
            .WithBlueprint(BlueprintIds.Pottery)
            .WithDomiks(DomikIds.Barrack, 3)
            .Buy(DomikIds.Pottery);

        var potteryId = player.DomikId(DomikIds.Pottery);
        player.Upgrade(potteryId);
        player.WithResource(ResourceIds.Clay, consumedClay);
        var before = player.Resources();

        player.StartManufacture(potteryId, ReceiptIds.MakeBrick8h);

        var after = player.Resources();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ResourceValue(before, ResourceIds.Clay) - ResourceValue(after, ResourceIds.Clay), Is.EqualTo(consumedClay));
            Assert.That(ResourceValue(after, ResourceIds.Brick) - ResourceValue(before, ResourceIds.Brick), Is.EqualTo(producedBricks));
        }
    }

    /// <summary>
    /// Имена трудяг уникальны в пределах одного игрока – при найме не выдаются повторы.
    /// </summary>
    [Test]
    public void WorkerNamesAreUniquePerPlayerTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 4);

        var workers = player.Workers();

        Assert.That(workers.Select(x => x.Name).Distinct().Count(), Is.EqualTo(workers.Count));
    }

    /// <summary>
    /// Стоимость улучшения до заданного уровня в монетах одинакова для всех обычных построек (кроме золотой шахты, рынка и
    /// построек 9–11).
    /// </summary>
    /// <param name="level">Целевой уровень постройки.</param>
    /// <param name="expectedCoins">Ожидаемая стоимость улучшения в монетах.</param>
    [TestCase(4, 1500)]
    [TestCase(5, 9000)]
    public void UpgradeCoinCostsAreRecalibratedTest(int level, int expectedCoins)
    {
        var types = GetDomikTypes();

        foreach (var type in types)
        {
            var typeLevel = type.Levels.SingleOrDefault(x => x.Value == level);
            if (typeLevel == null)
            {
                continue;
            }

            if (type.Id is DomikIds.MarketYard or DomikIds.Gathering or DomikIds.ScoutHut)
            {
                continue;
            }

            if (type.Id is DomikIds.GoldMine or DomikIds.Market)
            {
                continue;
            }

            var coinCost = typeLevel.Resources.Single(x => x.Type.Id == ResourceIds.Coin).Value;
            Assert.That(coinCost, Is.EqualTo(expectedCoins), $"domik type {type.Id}");
        }
    }

    /// <summary>
    /// Золотая шахта и рынок улучшаются по собственной, отличной от прочих построек, кривой стоимости в монетах.
    /// </summary>
    /// <param name="level">Целевой уровень постройки.</param>
    /// <param name="expectedCoins">Ожидаемая стоимость улучшения в монетах.</param>
    [TestCase(2, 150)]
    [TestCase(3, 450)]
    [TestCase(4, 2200)]
    [TestCase(5, 13000)]
    public void GoldMineAndMarketUpgradeCoinCostsAreReshapedTest(int level, int expectedCoins)
    {
        var types = GetDomikTypes();

        foreach (var typeId in new[] { DomikIds.GoldMine, DomikIds.Market })
        {
            var typeLevel = types.Single(x => x.Id == typeId).Levels.Single(x => x.Value == level);
            var coinCost = typeLevel.Resources.Single(x => x.Type.Id == ResourceIds.Coin).Value;
            Assert.That(coinCost, Is.EqualTo(expectedCoins), $"domik type {typeId}");
        }
    }

    private static DomikType[] GetDomikTypes()
    {
        return App.Act<ResourceManager, DomikType[]>(m => m.GetDomikTypes());
    }

    private static int ResourceValue(IReadOnlyList<Resource> resources, int typeId)
    {
        return resources.FirstOrDefault(x => x.Type.Id == typeId)?.Value ?? 0;
    }
}
