using Domiki.Web.Infrastructure;

namespace Domiki.Web.Tests;

public sealed class CountGateTests
{
    /// <summary>
    /// Постройка без ворот обжитости ограничена только своим максимальным количеством и пропадает из списка доступных после
    /// покупки лимита.
    /// </summary>
    [Test]
    public void DomikTypeWithoutGatesIsBoundedOnlyByMaxCountTest()
    {
        var player = TestPlayer.Create();

        var available = player.PurchaseAvailableDomiks();
        var market = available.First(x => x.Type.Id == DomikIds.Market);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(market.AvailableCount, Is.EqualTo(1));
            Assert.That(market.NextCountGateLevel, Is.Null);
        }

        player.Buy(DomikIds.Market);

        available = player.PurchaseAvailableDomiks();
        Assert.That(available.Any(x => x.Type.Id == DomikIds.Market), Is.False);
    }

    /// <summary>
    /// Постройки, полученные сверх текущего лимита обжитости, у игрока не отбираются, а доступное для покупки количество не
    /// уходит в минус.
    /// </summary>
    [Test]
    public void GrandfatheredOwnershipIsNotClippedAndAvailableCountNeverNegativeTest()
    {
        const int ownedCount = 4;
        const int nextGateLevel = 24;

        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3);

        Assert.That(player.Domiks().Count(x => x.Type.Id == DomikIds.Barrack), Is.EqualTo(ownedCount));

        var available = player.PurchaseAvailableDomiks();
        var barak = available.First(x => x.Type.Id == DomikIds.Barrack);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(barak.AvailableCount, Is.Zero);
            Assert.That(barak.NextCountGateLevel, Is.EqualTo(nextGateLevel));
        }

        var ex = Assert.Throws<BusinessException>(() => player.Buy(DomikIds.Barrack));
        Assert.That(ex.Message, Is.EqualTo($"Постройка «Артельная изба» откроется при обжитости {nextGateLevel}"));

        Assert.That(player.Domiks().Count(x => x.Type.Id == DomikIds.Barrack), Is.EqualTo(ownedCount));
    }

    /// <summary>
    /// Второй экземпляр каменоломни открывается только при обжитости 12, ниже – покупка запрещена с понятным сообщением.
    /// </summary>
    [Test]
    public void StoneMineSecondInstanceGateTest()
    {
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 500);

        SetVillageLevel(player, 6);
        player.Buy(DomikIds.StoneMine);

        SetVillageLevel(player, 11);
        var ex = Assert.Throws<BusinessException>(() => player.Buy(DomikIds.StoneMine));
        Assert.That(ex.Message, Is.EqualTo("Постройка «Каменоломня» откроется при обжитости 12"));

        SetVillageLevel(player, 12);
        Assert.DoesNotThrow(() => player.Buy(DomikIds.StoneMine));
    }

    /// <summary>
    /// Покупка очередного экземпляра постройки, привязанного к порогу обжитости, падает исключением, пока порог не достигнут.
    /// </summary>
    /// <param name="domikTypeId">Тип постройки.</param>
    /// <param name="gateLevel">Обжитость, открывающая покупку.</param>
    /// <param name="ownsFirstInstance">Есть ли у игрока предыдущий экземпляр постройки.</param>
    [TestCase(DomikIds.Barrack, 5, true)]
    [TestCase(DomikIds.ClayMine, 8, true)]
    [TestCase(DomikIds.LumberMill, 8, false)]
    public void BuyNextGatedInstanceThrowsBelowThresholdTest(int domikTypeId, int gateLevel, bool ownsFirstInstance)
    {
        var player = TestPlayer.Create();
        if (!ownsFirstInstance)
        {
            player.WithDomik(domikTypeId);
        }

        SetVillageLevel(player, gateLevel - 1);

        var name = player.DomikTypes().First(x => x.Id == domikTypeId).Name;
        var ex = Assert.Throws<BusinessException>(() => player.Buy(domikTypeId));
        Assert.That(ex.Message, Is.EqualTo($"Постройка «{name}» откроется при обжитости {gateLevel}"));
    }

    /// <summary>
    /// По достижении требуемой обжитости покупка очередного экземпляра постройки проходит без ошибок.
    /// </summary>
    /// <param name="domikTypeId">Тип постройки.</param>
    /// <param name="gateLevel">Обжитость, открывающая покупку.</param>
    /// <param name="ownsFirstInstance">Есть ли у игрока предыдущий экземпляр постройки.</param>
    [TestCase(DomikIds.Barrack, 5, true)]
    [TestCase(DomikIds.ClayMine, 8, true)]
    [TestCase(DomikIds.LumberMill, 8, false)]
    public void BuyNextGatedInstanceSucceedsAtThresholdTest(int domikTypeId, int gateLevel, bool ownsFirstInstance)
    {
        var player = TestPlayer.Create();
        if (!ownsFirstInstance)
        {
            player.WithDomik(domikTypeId);
        }

        SetVillageLevel(player, gateLevel);

        Assert.DoesNotThrow(() => player.Buy(domikTypeId));
    }

    private static void SetVillageLevel(TestPlayer player, int target)
    {
        while (player.GetVillageLevel().Level < target)
        {
            player.WithDomik(DomikIds.Market);
        }
    }
}
