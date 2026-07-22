using Domiki.Web.Core.Models;
using Domiki.Web.Reference;
using Domiki.Web.Reference.Models;
using Domiki.Web.Village.Models;

namespace Domiki.Web.Tests;

public sealed class SickTypeReferenceTests
{
    /// <summary>
    /// Каждая погодная хворь привязана к своему виду погоды, а жар единственный не защищается плащом.
    /// </summary>
    /// <param name="sickTypeId">Тип хвори.</param>
    /// <param name="weatherTypeId">Погода, вызывающая хворь.</param>
    /// <param name="cloakProtects">Бережёт ли плащ от хвори.</param>
    [TestCase(SickTypeIds.Cold, WeatherIds.Rain, true)]
    [TestCase(SickTypeIds.Heatstroke, WeatherIds.Drought, false)]
    [TestCase(SickTypeIds.Chill, WeatherIds.Frost, true)]
    [TestCase(SickTypeIds.Lumbago, WeatherIds.Wind, true)]
    public void SickTypesHaveWeatherAndCloakRulesTest(int sickTypeId, int weatherTypeId, bool cloakProtects)
    {
        var sickType = App.Act<ResourceManager, SickType[]>(m => m.GetSickTypes()).Single(x => x.Id == sickTypeId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sickType.WeatherTypeId, Is.EqualTo(weatherTypeId));
            Assert.That(sickType.CloakProtects, Is.EqualTo(cloakProtects));
        }
    }

    /// <summary>
    /// Мастерская на каждом уровне шьёт один плащ из двух сукон за четыре часа.
    /// </summary>
    /// <param name="level">Уровень мастерской.</param>
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void WorkshopSewsCloakAtEveryLevelTest(int level)
    {
        var receipt = App.Act<ResourceManager, Receipt[]>(m => m.GetReceipts()).Single(x => x.Id == ReceiptIds.SewCloak);
        var workshop = App.Act<ResourceManager, DomikType[]>(m => m.GetDomikTypes()).Single(x => x.Id == DomikIds.Workshop);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.InputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Cloth, 2)]));
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Cloak, 1)]));
            Assert.That(receipt.DurationSeconds, Is.EqualTo(14400));
            Assert.That(receipt.PlodderCount, Is.EqualTo(1));
            Assert.That(workshop.Levels.Single(x => x.Value == level).Receipts.Select(x => x.Id), Does.Contain(ReceiptIds.SewCloak));
        }
    }

    /// <summary>
    /// Рыночная стоимость плаща равна 120 монетам.
    /// </summary>
    [Test]
    public void CloakMarketValueTest()
    {
        Assert.That(ResourceManager.GetMarketValue(ResourceIds.Cloak), Is.EqualTo(120));
    }
}
