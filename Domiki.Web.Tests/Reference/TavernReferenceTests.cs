using Domiki.Web.Core.Models;
using Domiki.Web.Reference;
using Domiki.Web.Reference.Models;

namespace Domiki.Web.Tests;

/// <summary>
/// Справочные данные Корчмы и сыра.
/// </summary>
public sealed class TavernReferenceTests
{
    /// <summary>
    /// Сыр является едой с рыночной стоимостью 25 монет.
    /// </summary>
    [Test]
    public void CheeseFoodAndMarketValueTest()
    {
        var cheese = App.Act<ResourceManager, ResourceType[]>(m => m.GetResourceTypes()).Single(x => x.Id == ResourceIds.Cheese);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cheese.LogicName, Is.EqualTo("cheese"));
            Assert.That(cheese.IsFood, Is.True);
            Assert.That(ResourceManager.GetMarketValue(cheese.Id), Is.EqualTo(25));
        }
    }

    /// <summary>
    /// Корчма открывается на 16 уровне деревни, доступна в одном экземпляре, имеет три уровня и не производит рецептов.
    /// </summary>
    [Test]
    public void TavernLevelsAndNoReceiptsTest()
    {
        var tavern = App.Act<ResourceManager, DomikType[]>(m => m.GetDomikTypes()).Single(x => x.Id == DomikIds.Tavern);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tavern.UnlockLevel, Is.EqualTo(16));
            Assert.That(tavern.MaxCount, Is.EqualTo(1));
            Assert.That(tavern.Levels, Has.Length.EqualTo(3));
            Assert.That(tavern.Levels.All(x => x.Receipts.Length == 0), Is.True);
        }
    }

    /// <summary>
    /// Овчарня всех уровней варит сыр из двух зёрен и выдаёт два сыра.
    /// </summary>
    [Test]
    public void SheepfoldMakesCheeseTest()
    {
        var receipt = App.Act<ResourceManager, Receipt[]>(m => m.GetReceipts()).Single(x => x.Id == ReceiptIds.MakeCheese);
        var sheepfold = App.Act<ResourceManager, DomikType[]>(m => m.GetDomikTypes()).Single(x => x.Id == DomikIds.Sheepfold);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.InputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Grain, 2)]));
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Cheese, 2)]));
            Assert.That(sheepfold.Levels.All(x => x.Receipts.Any(y => y.Id == receipt.Id)), Is.True);
        }
    }

    /// <summary>
    /// Рецепт «Продать сыр» меняет один сыр на 25 монет и доступен во всех уровнях Лавки.
    /// </summary>
    [Test]
    public void SellCheeseReceiptTest()
    {
        var receipt = App.Act<ResourceManager, Receipt[]>(m => m.GetReceipts()).Single(x => x.Id == ReceiptIds.SellCheese);
        var market = App.Act<ResourceManager, DomikType[]>(m => m.GetDomikTypes()).Single(x => x.Id == DomikIds.Market);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.InputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Cheese, 1)]));
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Coin, 25)]));
            Assert.That(market.Levels.All(x => x.Receipts.Any(y => y.Id == receipt.Id)), Is.True);
        }
    }
}
