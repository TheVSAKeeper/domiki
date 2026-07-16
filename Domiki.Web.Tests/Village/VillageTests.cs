using Domiki.Web.Infrastructure;
using Domiki.Web.Village.Models;

namespace Domiki.Web.Tests;

public class VillageTests : TestBase
{
    /// <summary>
    /// Деревня нового игрока стартует без имени и с гербом по умолчанию – иконка и цвет оба равны 0.
    /// </summary>
    [Test]
    public void GetVillageNewPlayerReturnsEmptyNameAndDefaultCrestTest()
    {
        var playerId = GetPlayerId();

        var village = GetVillage(playerId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(village.VillageName, Is.Null);
            Assert.That(village.CrestIcon, Is.Zero);
            Assert.That(village.CrestColor, Is.Zero);
        }
    }

    /// <summary>
    /// Имена деревень уникальны среди игроков: попытка занять уже занятое имя падает исключением и не меняет имя и герб
    /// второго игрока.
    /// </summary>
    [Test]
    public void SetVillageDuplicateNameThrowsAndKeepsStateTest()
    {
        var firstPlayerId = GetPlayerId();
        var secondPlayerId = GetPlayerId();
        var takenName = TestVillageName("Тихий Бор");
        var keptName = TestVillageName("Ясная Поляна");
        SetVillage(firstPlayerId, takenName, 1, 2);
        SetVillage(secondPlayerId, keptName, 3, 4);

        Assert.Throws<BusinessException>(() => SetVillage(secondPlayerId, takenName, 5, 6));

        var village = GetVillage(secondPlayerId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(village.VillageName, Is.EqualTo(keptName));
            Assert.That(village.CrestIcon, Is.EqualTo(3));
            Assert.That(village.CrestColor, Is.EqualTo(4));
        }
    }

    /// <summary>
    /// Установка имени деревни нормализует пробелы (обрезка по краям, схлопывание повторов) и сохраняет выбранный герб.
    /// </summary>
    [Test]
    public void SetVillageValidSavesNormalizedNameAndCrestTest()
    {
        var playerId = GetPlayerId();
        var name = TestVillageName("Тихая Долина");

        SetVillage(playerId, "  " + name.Replace(" ", "   ") + "  ", 2, 3);

        var village = GetVillage(playerId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(village.VillageName, Is.EqualTo(name));
            Assert.That(village.CrestIcon, Is.EqualTo(2));
            Assert.That(village.CrestColor, Is.EqualTo(3));
        }
    }

    public static IEnumerable<TestCaseData> InvalidLengthNames()
    {
        yield return new TestCaseData("Аб").SetName("TooShortVillageName");
        yield return new TestCaseData("1234567890123456789012345").SetName("TooLongVillageName");
        yield return new TestCaseData("   ").SetName("BlankVillageName");
    }

    public static IEnumerable<TestCaseData> InvalidCharacterNames()
    {
        yield return new TestCaseData("Лес_Дом").SetName("UnderscoreVillageName");
        yield return new TestCaseData("Лес!").SetName("BangVillageName");
        yield return new TestCaseData("Лес/Дом").SetName("SlashVillageName");
        yield return new TestCaseData("Лес\tДом").SetName("TabVillageName");
    }

    public static IEnumerable<TestCaseData> BlacklistedNames()
    {
        yield return new TestCaseData("Админово").SetName("AdminVillageName");
        yield return new TestCaseData("Модераторск").SetName("ModeratorVillageName");
        yield return new TestCaseData("Домикино").SetName("DomikiVillageName");
        yield return new TestCaseData("fucktown").SetName("ProfanityLatinVillageName");
        yield return new TestCaseData("сукадеревня").SetName("ProfanityCyrillicVillageName");
    }

    /// <summary>
    /// Имя деревни должно соответствовать ограничениям по длине, набору символов и списку запрещённых/нецензурных слов –
    /// нарушение любого из них падает исключением, и деревня остаётся без имени.
    /// </summary>
    /// <param name="name">Проверяемое (некорректное) имя деревни.</param>
    [TestCaseSource(nameof(InvalidLengthNames))]
    [TestCaseSource(nameof(InvalidCharacterNames))]
    [TestCaseSource(nameof(BlacklistedNames))]
    public void SetVillageInvalidNameThrowsTest(string name)
    {
        var playerId = GetPlayerId();

        Assert.Throws<BusinessException>(() => SetVillage(playerId, name, 0, 0));

        var village = GetVillage(playerId);
        Assert.That(village.VillageName, Is.Null);
    }

    /// <summary>
    /// Индекс иконки и цвета герба должны попадать в допустимый диапазон [0, 7] – выход за него падает исключением, и деревня
    /// остаётся без имени.
    /// </summary>
    /// <param name="crestIcon">Проверяемый индекс иконки герба.</param>
    /// <param name="crestColor">Проверяемый индекс цвета герба.</param>
    [TestCase(-1, 0)]
    [TestCase(8, 0)]
    [TestCase(0, -1)]
    [TestCase(0, 8)]
    public void SetVillageInvalidCrestThrowsTest(int crestIcon, int crestColor)
    {
        var playerId = GetPlayerId();

        Assert.Throws<BusinessException>(() => SetVillage(playerId, "Берёзки", crestIcon, crestColor));

        var village = GetVillage(playerId);
        Assert.That(village.VillageName, Is.Null);
    }

    private int GetPlayerId()
    {
        using var uow = GetUow();
        var domikManager = GetDomikManager(uow);
        var playerId = domikManager.GetPlayerId("testUser_" + Guid.NewGuid());
        uow.Commit();
        return playerId;
    }

    private VillageState GetVillage(int playerId)
    {
        using var uow = GetUow();
        var domikManager = GetDomikManager(uow);
        var village = domikManager.GetVillage(playerId);
        uow.Commit();
        return village;
    }

    private void SetVillage(int playerId, string name, int crestIcon, int crestColor)
    {
        using var uow = GetUow();
        var domikManager = GetDomikManager(uow);
        domikManager.SetVillageIdentity(playerId, name, crestIcon, crestColor);
        uow.Commit();
    }
}
