using Domiki.Web.Core.Models;
using Domiki.Web.Reference;
using Domiki.Web.Reference.Models;

namespace Domiki.Web.Tests;

public sealed class WoolReferenceTests
{
    /// <summary>
    /// Пасти и стричь овец доступны на всех уровнях овчарни, ткачество – на всех уровнях мастерской, а продажа шерсти и сукна – на всех уровнях рынка.
    /// </summary>
    /// <param name="domikTypeId">Тип постройки.</param>
    /// <param name="level">Уровень постройки.</param>
    /// <param name="receiptId">Проверяемый рецепт.</param>
    [TestCase(DomikIds.Sheepfold, 1, ReceiptIds.SheepGraze)]
    [TestCase(DomikIds.Sheepfold, 2, ReceiptIds.SheepGraze)]
    [TestCase(DomikIds.Sheepfold, 3, ReceiptIds.SheepGraze)]
    [TestCase(DomikIds.Sheepfold, 4, ReceiptIds.SheepGraze)]
    [TestCase(DomikIds.Sheepfold, 5, ReceiptIds.SheepGraze)]
    [TestCase(DomikIds.Sheepfold, 1, ReceiptIds.SheepShear)]
    [TestCase(DomikIds.Sheepfold, 2, ReceiptIds.SheepShear)]
    [TestCase(DomikIds.Sheepfold, 3, ReceiptIds.SheepShear)]
    [TestCase(DomikIds.Sheepfold, 4, ReceiptIds.SheepShear)]
    [TestCase(DomikIds.Sheepfold, 5, ReceiptIds.SheepShear)]
    [TestCase(DomikIds.Workshop, 1, ReceiptIds.MakeCloth)]
    [TestCase(DomikIds.Workshop, 2, ReceiptIds.MakeCloth)]
    [TestCase(DomikIds.Workshop, 3, ReceiptIds.MakeCloth)]
    [TestCase(DomikIds.Workshop, 4, ReceiptIds.MakeCloth)]
    [TestCase(DomikIds.Workshop, 5, ReceiptIds.MakeCloth)]
    [TestCase(DomikIds.Market, 1, ReceiptIds.SellWool)]
    [TestCase(DomikIds.Market, 2, ReceiptIds.SellWool)]
    [TestCase(DomikIds.Market, 3, ReceiptIds.SellWool)]
    [TestCase(DomikIds.Market, 4, ReceiptIds.SellWool)]
    [TestCase(DomikIds.Market, 5, ReceiptIds.SellWool)]
    [TestCase(DomikIds.Market, 1, ReceiptIds.SellCloth)]
    [TestCase(DomikIds.Market, 2, ReceiptIds.SellCloth)]
    [TestCase(DomikIds.Market, 3, ReceiptIds.SellCloth)]
    [TestCase(DomikIds.Market, 4, ReceiptIds.SellCloth)]
    [TestCase(DomikIds.Market, 5, ReceiptIds.SellCloth)]
    public void ReceiptsAreBoundAtEveryLevelTest(int domikTypeId, int level, int receiptId)
    {
        AssertBinding(domikTypeId, level, receiptId);
    }

    /// <summary>
    /// Выпас овец не требует ресурсов и даёт 3 шерсти за 3 часа.
    /// </summary>
    [Test]
    public void SheepGrazeReceiptTest()
    {
        var receipt = App.Act<ResourceManager, Receipt[]>(m => m.GetReceipts()).Single(x => x.Id == ReceiptIds.SheepGraze);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.InputResources, Is.Empty);
            Assert.That(receipt.OptionalInputResources, Is.Empty);
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Wool, 3)]));
            Assert.That(receipt.DurationSeconds, Is.EqualTo(10800));
        }
    }

    /// <summary>
    /// Стрижка овец превращает 2 зерна в 4 шерсти за 2 часа.
    /// </summary>
    [Test]
    public void SheepShearReceiptTest()
    {
        var receipt = App.Act<ResourceManager, Receipt[]>(m => m.GetReceipts()).Single(x => x.Id == ReceiptIds.SheepShear);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.InputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Grain, 2)]));
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Wool, 4)]));
            Assert.That(receipt.DurationSeconds, Is.EqualTo(7200));
        }
    }

    /// <summary>
    /// Ткачество превращает 2 шерсти в 1 сукно.
    /// </summary>
    [Test]
    public void MakeClothReceiptTest()
    {
        var receipt = App.Act<ResourceManager, Receipt[]>(m => m.GetReceipts()).Single(x => x.Id == ReceiptIds.MakeCloth);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.InputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Wool, 2)]));
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Cloth, 1)]));
        }
    }

    /// <summary>
    /// Продажа шерсти и сукна на рынке выдаёт фиксированное число монет.
    /// </summary>
    /// <param name="receiptId">Проверяемый рецепт.</param>
    /// <param name="inputResourceTypeId">Тип входного ресурса.</param>
    /// <param name="inputValue">Количество входного ресурса.</param>
    /// <param name="outputValue">Количество получаемых монет.</param>
    [TestCase(ReceiptIds.SellWool, ResourceIds.Wool, 1, 10)]
    [TestCase(ReceiptIds.SellCloth, ResourceIds.Cloth, 1, 40)]
    public void SellReceiptsHaveExpectedCoinOutputTest(int receiptId, int inputResourceTypeId, int inputValue, int outputValue)
    {
        var receipt = App.Act<ResourceManager, Receipt[]>(m => m.GetReceipts()).Single(x => x.Id == receiptId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.InputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(inputResourceTypeId, inputValue)]));
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Coin, outputValue)]));
        }
    }

    /// <summary>
    /// Рыночная стоимость шерсти – 10, сукна – 40.
    /// </summary>
    /// <param name="resourceTypeId">Тип ресурса.</param>
    /// <param name="expected">Ожидаемая рыночная стоимость.</param>
    [TestCase(ResourceIds.Wool, 10)]
    [TestCase(ResourceIds.Cloth, 40)]
    public void MarketValueTest(int resourceTypeId, int expected)
    {
        Assert.That(ResourceManager.GetMarketValue(resourceTypeId), Is.EqualTo(expected));
    }

    /// <summary>
    /// Овчарня открывается на 14 уровне деревни и доступна игроку в одном экземпляре.
    /// </summary>
    [Test]
    public void SheepfoldGateTest()
    {
        var sheepfold = App.Act<ResourceManager, DomikType[]>(m => m.GetDomikTypes()).Single(x => x.Id == DomikIds.Sheepfold);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sheepfold.UnlockLevel, Is.EqualTo(14));
            Assert.That(sheepfold.MaxCount, Is.EqualTo(1));
        }
    }

    private static void AssertBinding(int domikTypeId, int level, int receiptId)
    {
        var domikType = App.Act<ResourceManager, DomikType[]>(m => m.GetDomikTypes()).Single(x => x.Id == domikTypeId);
        var receiptIds = domikType.Levels.Single(x => x.Value == level).Receipts.Select(x => x.Id);

        Assert.That(receiptIds, Does.Contain(receiptId));
    }
}
