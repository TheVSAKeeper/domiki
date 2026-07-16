using Domiki.Web.Activities.Models;
using Domiki.Web.Core.Models;
using Domiki.Web.Economy.Models;
using Domiki.Web.Reference;
using Domiki.Web.Reference.Models;

namespace Domiki.Web.Tests;

public sealed class FoodReferenceTests
{
    /// <summary>
    /// Рецепт выпечки хлеба превращает 2 муки в 4 хлеба, без монет на входе.
    /// </summary>
    [Test]
    public void BreadReceiptResourcesTest()
    {
        var receipt = App.Act<ResourceManager, Receipt[]>(m => m.GetReceipts()).Single(x => x.Id == ReceiptIds.MakeBread);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.LogicName, Is.EqualTo("make_bread"));
            Assert.That(receipt.InputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Flour, 2)]));
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Bread, 4)]));
        }
    }

    /// <summary>
    /// Дубрава торгует хлебом как вторичным ресурсом спроса, а её чертёж открывает пекарню при репутации 25.
    /// </summary>
    [Test]
    public void DubravaFoodProfileAndBakeryBlueprintTest()
    {
        var dubrava = App.Act<ResourceManager, Neighbor[]>(m => m.GetNeighbors()).Single(x => x.Id == 5);
        var blueprint = App.Act<ResourceManager, Blueprint[]>(m => m.GetBlueprints()).Single(x => x.Id == BlueprintIds.Bakery);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(dubrava.LogicName, Is.EqualTo("dubrava"));
            Assert.That(dubrava.SecondaryResourceTypeId, Is.EqualTo(ResourceIds.Bread));
            Assert.That(blueprint.DomikTypeId, Is.EqualTo(DomikIds.Bakery));
            Assert.That(blueprint.ReputationThreshold, Is.EqualTo(25));
            Assert.That(blueprint.NeighborId, Is.EqualTo(dubrava.Id));
        }
    }

    /// <summary>
    /// Добыча зерна не требует монет на входе и даёт 1 зерно за цикл.
    /// </summary>
    [Test]
    public void GrainReceiptHasNoCoinInputTest()
    {
        var receipt = App.Act<ResourceManager, Receipt[]>(m => m.GetReceipts()).Single(x => x.Id == ReceiptIds.GrainDig);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.LogicName, Is.EqualTo("grain_dig"));
            Assert.That(receipt.InputResources.Any(x => x.Type.Id == ResourceIds.Coin), Is.False);
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Grain, 1)]));
        }
    }

    /// <summary>
    /// Апгрейд мельницы на 1 уровне требует 150 монет и 1 жёрнов, на 3 уровне – 1 жёрнов, на 5 уровне – 2 жёрнова;
    /// апгрейд пекарни на 1 уровне требует 10 золота, на 4 уровне – 10 посуды.
    /// </summary>
    /// <param name="domikTypeId">Тип постройки (мельница или пекарня).</param>
    /// <param name="level">Уровень постройки.</param>
    /// <param name="resourceTypeId">Проверяемый тип ресурса в стоимости апгрейда.</param>
    /// <param name="expected">Ожидаемое количество ресурса.</param>
    [TestCase(DomikIds.Mill, 1, ResourceIds.Coin, 150)]
    [TestCase(DomikIds.Mill, 1, ResourceIds.Millstone, 1)]
    [TestCase(DomikIds.Mill, 3, ResourceIds.Millstone, 1)]
    [TestCase(DomikIds.Mill, 5, ResourceIds.Millstone, 2)]
    [TestCase(DomikIds.Bakery, 1, ResourceIds.Gold, 10)]
    [TestCase(DomikIds.Bakery, 4, ResourceIds.Dishes, 10)]
    public void FoodBuildingUpgradeCostsTest(int domikTypeId, int level, int resourceTypeId, int expected)
    {
        var domikType = App.Act<ResourceManager, DomikType[]>(m => m.GetDomikTypes()).Single(x => x.Id == domikTypeId);
        var resources = domikType.Levels.Single(x => x.Value == level).Resources;

        Assert.That(resources.Single(x => x.Type.Id == resourceTypeId).Value, Is.EqualTo(expected));
    }

    /// <summary>
    /// Рыночная стоимость зерна, муки и хлеба: зерно – 10, мука – 35, хлеб – 20.
    /// </summary>
    /// <param name="resourceTypeId">Тип ресурса.</param>
    /// <param name="expected">Ожидаемая рыночная стоимость.</param>
    [TestCase(ResourceIds.Grain, 10)]
    [TestCase(ResourceIds.Flour, 35)]
    [TestCase(ResourceIds.Bread, 20)]
    public void MarketValueTest(int resourceTypeId, int expected)
    {
        Assert.That(ResourceManager.GetMarketValue(resourceTypeId), Is.EqualTo(expected));
    }
}
