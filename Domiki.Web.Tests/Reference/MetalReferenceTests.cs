using Domiki.Web.Activities.Models;
using Domiki.Web.Core.Models;
using Domiki.Web.Reference;
using Domiki.Web.Reference.Models;
using Domiki.Web.Village.Models;

namespace Domiki.Web.Tests;

public sealed class MetalReferenceTests
{
    /// <summary>
    /// Кузница открывается только с 20 уровня игрока.
    /// </summary>
    [Test]
    public void ForgeUnlockLevelIsTwentyTest()
    {
        var forge = App.Act<ResourceManager, DomikType[]>(m => m.GetDomikTypes()).Single(x => x.Id == DomikIds.Forge);

        Assert.That(forge.UnlockLevel, Is.EqualTo(20));
    }

    /// <summary>
    /// Фонарь даёт 5 очков уюта и стоит 10 железа и 4 доски.
    /// </summary>
    [Test]
    public void LanternHasExpectedComfortAndCostTest()
    {
        var lantern = App.Act<ResourceManager, DecorType[]>(m => m.GetDecorTypes()).Single(x => x.Id == DecorIds.Lantern);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(lantern.LogicName, Is.EqualTo("lantern"));
            Assert.That(lantern.ComfortPoints, Is.EqualTo(5));
            Assert.That(lantern.Cost.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Iron, 10), (ResourceIds.Board, 4)]));
        }
    }

    /// <summary>
    /// Постройка добычи руды называется «Рудник».
    /// </summary>
    [Test]
    public void MineIsRenamedTest()
    {
        var mine = App.Act<ResourceManager, DomikType[]>(m => m.GetDomikTypes()).Single(x => x.Id == DomikIds.GoldMine);

        Assert.That(mine.Name, Is.EqualTo("Рудник"));
    }

    /// <summary>
    /// Базовая добыча руды не требует ни обязательных, ни опциональных ресурсов на входе.
    /// </summary>
    [Test]
    public void OreDigHasNoInputsTest()
    {
        var receipt = App.Act<ResourceManager, Receipt[]>(m => m.GetReceipts()).Single(x => x.Id == ReceiptIds.OreDig);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.InputResources, Is.Empty);
            Assert.That(receipt.OptionalInputResources, Is.Empty);
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Ore, 1)]));
        }
    }

    /// <summary>
    /// У руды и железа закреплены логические имена и рыночная стоимость: руда – 10, железо – 35.
    /// </summary>
    /// <param name="resourceTypeId">Тип ресурса.</param>
    /// <param name="logicName">Ожидаемое логическое имя ресурса.</param>
    /// <param name="marketValue">Ожидаемая рыночная стоимость.</param>
    [TestCase(ResourceIds.Ore, "ore", 10)]
    [TestCase(ResourceIds.Iron, "iron", 35)]
    public void MetalResourceTypesAndMarketValuesTest(int resourceTypeId, string logicName, int marketValue)
    {
        var resourceType = App.Act<ResourceManager, ResourceType[]>(m => m.GetResourceTypes()).Single(x => x.Id == resourceTypeId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resourceType.LogicName, Is.EqualTo(logicName));
            Assert.That(ResourceManager.GetMarketValue(resourceTypeId), Is.EqualTo(marketValue));
        }
    }

    /// <summary>
    /// Изготовление инструмента тратит железо и доски поровну, без кирпича на входе.
    /// </summary>
    /// <param name="receiptId">Проверяемый рецепт изготовления инструмента.</param>
    /// <param name="value">Количество каждого входного ресурса и полученного инструмента.</param>
    [TestCase(ReceiptIds.MakeTool, 1)]
    [TestCase(ReceiptIds.MakeTool8h, 8)]
    public void ToolReceiptsRequireIronAndBoardsTest(int receiptId, int value)
    {
        var receipt = App.Act<ResourceManager, Receipt[]>(m => m.GetReceipts()).Single(x => x.Id == receiptId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.InputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Iron, value), (ResourceIds.Board, value)]));
            Assert.That(receipt.InputResources.Any(x => x.Type.Id == ResourceIds.Brick), Is.False);
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Tool, value)]));
        }
    }

    /// <summary>
    /// Плавка железа привязана к конкретным уровням кузницы: рецепт 62 доступен на 1 и 2 уровнях, рецепт 63 – только с 3
    /// уровня.
    /// </summary>
    /// <param name="level">Уровень кузницы.</param>
    /// <param name="receiptId">Проверяемый рецепт плавки железа.</param>
    /// <param name="expected">Ожидается ли привязка.</param>
    [TestCase(1, ReceiptIds.MakeIron, true)]
    [TestCase(2, ReceiptIds.MakeIron, true)]
    [TestCase(3, ReceiptIds.MakeIron8h, true)]
    [TestCase(2, ReceiptIds.MakeIron8h, false)]
    public void IronReceiptsHaveExpectedForgeBindingsTest(int level, int receiptId, bool expected)
    {
        AssertBinding(DomikIds.Forge, level, receiptId, expected);
    }

    /// <summary>
    /// Рецепты добычи руды в шахте не имеют уровневого гейта и доступны с первого уровня постройки.
    /// </summary>
    /// <param name="level">Проверяемый уровень шахты.</param>
    /// <param name="receiptId">Проверяемый рецепт добычи руды.</param>
    [TestCase(1, ReceiptIds.OreDig)]
    [TestCase(5, ReceiptIds.OreDig)]
    [TestCase(1, ReceiptIds.OreDig8h)]
    [TestCase(5, ReceiptIds.OreDig8h)]
    [TestCase(1, ReceiptIds.OreDig24h)]
    [TestCase(5, ReceiptIds.OreDig24h)]
    public void OreReceiptsHaveExpectedMineBindingsTest(int level, int receiptId)
    {
        AssertBinding(DomikIds.GoldMine, level, receiptId, true);
    }

    /// <summary>
    /// Долгие смены добычи руды дают опциональный вход инструмента с бонусом +40% к выходу.
    /// </summary>
    /// <param name="receiptId">Проверяемый рецепт долгой смены добычи руды.</param>
    /// <param name="outputValue">Количество монет на входе и руды на выходе.</param>
    [TestCase(ReceiptIds.OreDig8h, 8)]
    [TestCase(ReceiptIds.OreDig24h, 24)]
    public void OreShiftsHaveOptionalToolTest(int receiptId, int outputValue)
    {
        var receipt = App.Act<ResourceManager, Receipt[]>(m => m.GetReceipts()).Single(x => x.Id == receiptId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.OptionalInputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Tool, 1)]));
            Assert.That(receipt.InputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Coin, outputValue)]));
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Ore, outputValue)]));
            Assert.That(receipt.OutputBonusPercent, Is.EqualTo(40));
        }
    }

    /// <summary>
    /// Оптовая продажа руды открывается только на пятом уровне рынка.
    /// </summary>
    /// <param name="level">Уровень рынка.</param>
    /// <param name="receiptId">Проверяемый рецепт оптовой продажи руды.</param>
    /// <param name="expected">Ожидается ли привязка.</param>
    [TestCase(5, ReceiptIds.SellOreX10, true)]
    [TestCase(4, ReceiptIds.SellOreX10, false)]
    public void OreBulkSellIsBoundToMarketLevelFiveTest(int level, int receiptId, bool expected)
    {
        AssertBinding(DomikIds.Market, level, receiptId, expected);
    }

    /// <summary>
    /// Цепочка переделов руда→железо и прямая продажа руды/железа на рынке идут по фиксированным курсам обмена.
    /// </summary>
    /// <param name="receiptId">Проверяемый рецепт.</param>
    /// <param name="inputResourceTypeId">Тип входного ресурса.</param>
    /// <param name="inputValue">Количество входного ресурса.</param>
    /// <param name="outputResourceTypeId">Тип выходного ресурса.</param>
    /// <param name="outputValue">Количество выходного ресурса.</param>
    [TestCase(ReceiptIds.MakeIron, ResourceIds.Ore, 2, ResourceIds.Iron, 1)]
    [TestCase(ReceiptIds.MakeIron8h, ResourceIds.Ore, 16, ResourceIds.Iron, 8)]
    [TestCase(ReceiptIds.SellOre, ResourceIds.Ore, 1, ResourceIds.Coin, 10)]
    [TestCase(ReceiptIds.SellIron, ResourceIds.Iron, 1, ResourceIds.Coin, 35)]
    [TestCase(ReceiptIds.SellIronX10, ResourceIds.Iron, 10, ResourceIds.Coin, 350)]
    [TestCase(ReceiptIds.SellOreX10, ResourceIds.Ore, 10, ResourceIds.Coin, 100)]
    public void MetalReceiptsHaveExpectedInputsAndOutputsTest(int receiptId, int inputResourceTypeId, int inputValue, int outputResourceTypeId, int outputValue)
    {
        var receipt = App.Act<ResourceManager, Receipt[]>(m => m.GetReceipts()).Single(x => x.Id == receiptId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.InputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(inputResourceTypeId, inputValue)]));
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(outputResourceTypeId, outputValue)]));
        }
    }

    /// <summary>
    /// Руда входит в добычу обеих экспедиций.
    /// </summary>
    /// <param name="expeditionTypeId">Тип экспедиции.</param>
    [TestCase(1)]
    [TestCase(2)]
    public void OreIsInExpeditionResourceLootTest(int expeditionTypeId)
    {
        var expedition = App.Act<ResourceManager, ExpeditionType[]>(m => m.GetExpeditionTypes()).Single(x => x.Id == expeditionTypeId);

        Assert.That(expedition.Loot.Any(x => x.Kind == Domiki.Web.Data.Entities.ExpeditionLootKind.Resource && x.ResourceTypeId == ResourceIds.Ore), Is.True);
    }

    private static void AssertBinding(int domikTypeId, int level, int receiptId, bool expected)
    {
        var domikType = App.Act<ResourceManager, DomikType[]>(m => m.GetDomikTypes()).Single(x => x.Id == domikTypeId);
        var receiptIds = domikType.Levels.Single(x => x.Value == level).Receipts.Select(x => x.Id);

        Assert.That(receiptIds.Contains(receiptId), Is.EqualTo(expected));
    }
}
