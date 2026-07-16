using Domiki.Web.Activities.Models;
using Domiki.Web.Core.Models;
using Domiki.Web.Economy.Models;
using Domiki.Web.Reference;

namespace Domiki.Web.Tests;

public sealed class ReceiptReferenceTests
{
    /// <summary>
    /// Чертежи открываются по возрастающей лестнице репутации: гончарня с 15, камнерез с 20, мастерская с 30.
    /// </summary>
    [Test]
    public void BlueprintLadderTest()
    {
        var blueprints = App.Act<ResourceManager, Blueprint[]>(m => m.GetBlueprints());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(blueprints.Single(x => x.LogicName == "pottery").ReputationThreshold, Is.EqualTo(15));
            Assert.That(blueprints.Single(x => x.LogicName == "stonecutter").ReputationThreshold, Is.EqualTo(20));
            Assert.That(blueprints.Single(x => x.LogicName == "workshop").ReputationThreshold, Is.EqualTo(30));
        }
    }

    /// <summary>
    /// У каждого из пяти соседей закреплён вторичный ресурс: глинищи – 12, каменка – 10, заречье – 2, боровое – 9, дубрава –
    /// 15.
    /// </summary>
    [Test]
    public void SecondaryProfilesTest()
    {
        var neighbors = App.Act<ResourceManager, Neighbor[]>(m => m.GetNeighbors());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(neighbors.Single(x => x.LogicName == "glinischi").SecondaryResourceTypeId, Is.EqualTo(ResourceIds.Dishes));
            Assert.That(neighbors.Single(x => x.LogicName == "kamenka").SecondaryResourceTypeId, Is.EqualTo(ResourceIds.Block));
            Assert.That(neighbors.Single(x => x.LogicName == "zarechye").SecondaryResourceTypeId, Is.EqualTo(ResourceIds.Stone));
            Assert.That(neighbors.Single(x => x.LogicName == "borovoe").SecondaryResourceTypeId, Is.EqualTo(ResourceIds.Furniture));
            Assert.That(neighbors.Single(x => x.LogicName == "dubrava").SecondaryResourceTypeId, Is.EqualTo(ResourceIds.Bread));
        }
    }

    /// <summary>
    /// Рецепт привязан к конкретному типу постройки и открывается только с нужного уровня; 8-часовые варианты рецепта
    /// требуют более высокого уровня, чем их базовые аналоги.
    /// </summary>
    /// <param name="domikTypeId">Тип постройки.</param>
    /// <param name="level">Уровень постройки.</param>
    /// <param name="receiptId">Проверяемый рецепт.</param>
    /// <param name="expected">Ожидается ли привязка.</param>
    [TestCase(DomikIds.Pottery, 1, ReceiptIds.MakeBrick, true)]
    [TestCase(DomikIds.Pottery, 2, ReceiptIds.MakeBrick8h, true)]
    [TestCase(DomikIds.Pottery, 1, ReceiptIds.MakeBrick8h, false)]
    [TestCase(DomikIds.Forge, 1, ReceiptIds.MakeBrick, false)]
    [TestCase(DomikIds.Forge, 5, ReceiptIds.MakeBrick8h, false)]
    [TestCase(DomikIds.Forge, 1, ReceiptIds.MakeTool, true)]
    [TestCase(DomikIds.Forge, 3, ReceiptIds.MakeTool8h, true)]
    [TestCase(DomikIds.Forge, 2, ReceiptIds.MakeTool8h, false)]
    [TestCase(DomikIds.Stonecutter, 1, ReceiptIds.MakeBlock, true)]
    [TestCase(DomikIds.Stonecutter, 1, ReceiptIds.MakeBlock8h, false)]
    [TestCase(DomikIds.Stonecutter, 2, ReceiptIds.MakeBlock8h, true)]
    [TestCase(DomikIds.Stonecutter, 3, ReceiptIds.MakeMillstone, true)]
    [TestCase(DomikIds.Market, 1, ReceiptIds.SellBlock, true)]
    [TestCase(DomikIds.Market, 1, ReceiptIds.SellDishes, true)]
    [TestCase(DomikIds.Market, 4, ReceiptIds.SellBlockX10, false)]
    [TestCase(DomikIds.Market, 5, ReceiptIds.SellBlockX10, true)]
    [TestCase(DomikIds.Market, 5, ReceiptIds.SellDishesX10, true)]
    public void ReceiptBindingTest(int domikTypeId, int level, int receiptId, bool expected)
    {
        var domikType = App.Act<ResourceManager, DomikType[]>(m => m.GetDomikTypes()).First(x => x.Id == domikTypeId);
        var receiptIds = domikType.Levels.First(x => x.Value == level).Receipts.Select(x => x.Id);

        Assert.That(receiptIds.Contains(receiptId), Is.EqualTo(expected));
    }

    /// <summary>
    /// Рыночная стоимость блока, жёрнова и посуды: блок – 35, жёрнов – 150, посуда – 45.
    /// </summary>
    /// <param name="resourceTypeId">Тип ресурса.</param>
    /// <param name="expected">Ожидаемая рыночная стоимость.</param>
    [TestCase(ResourceIds.Block, 35)]
    [TestCase(ResourceIds.Millstone, 150)]
    [TestCase(ResourceIds.Dishes, 45)]
    public void MarketValueTest(int resourceTypeId, int expected)
    {
        Assert.That(ResourceManager.GetMarketValue(resourceTypeId), Is.EqualTo(expected));
    }
}
