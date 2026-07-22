using Domiki.Web.Reference;
using Domiki.Web.Village.Models;

namespace Domiki.Web.Tests;

public sealed class ArtisanDecorReferenceTests
{
    /// <summary>
    /// Заезжие мастера продают по одной монетной вещи в каждой ступени лестницы, а уют даёт только резная калитка.
    /// </summary>
    /// <param name="decorTypeId">Тип украшения.</param>
    /// <param name="coinCost">Стоимость в монетах.</param>
    /// <param name="requiresDecorTypeId">Тип требуемого предыдущего украшения, либо 0 для первой ступени.</param>
    /// <param name="comfortPoints">Уют от одного экземпляра.</param>
    [TestCase(DecorIds.CarvedGate, 600, 0, 2)]
    [TestCase(DecorIds.CraneWell, 4000, DecorIds.CarvedGate, 0)]
    [TestCase(DecorIds.Gazebo, 25000, DecorIds.CraneWell, 0)]
    [TestCase(DecorIds.CarpPond, 70000, DecorIds.Gazebo, 0)]
    public void ArtisanDecorLadderTest(int decorTypeId, int coinCost, int requiresDecorTypeId, int comfortPoints)
    {
        var decor = App.Act<ResourceManager, DecorType[]>(m => m.GetDecorTypes()).Single(x => x.Id == decorTypeId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(decor.Cost.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Coin, coinCost)]));
            Assert.That(decor.MaxCount, Is.EqualTo(1));
            Assert.That(decor.RequiresDecorTypeId, Is.EqualTo(requiresDecorTypeId == 0 ? null : requiresDecorTypeId));
            Assert.That(decor.ComfortPoints, Is.EqualTo(comfortPoints));
        }
    }

    /// <summary>
    /// Прежний декор сохраняет неограниченное число экземпляров и не требует другого украшения.
    /// </summary>
    [Test]
    public void ExistingDecorRemainsUnlimitedTest()
    {
        var decor = App.Act<ResourceManager, DecorType[]>(m => m.GetDecorTypes()).Where(x => x.Id < DecorIds.CarvedGate);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(decor.Select(x => x.MaxCount), Is.All.Null);
            Assert.That(decor.Select(x => x.RequiresDecorTypeId), Is.All.Null);
        }
    }
}
