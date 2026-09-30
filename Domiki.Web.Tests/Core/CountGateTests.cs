using Domiki.Web.Infrastructure;

namespace Domiki.Web.Tests;

public sealed class CountGateTests
{
    private const int MaxDomikLevel = 5;

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

        var ex = Throws.Business(() => player.Buy(DomikIds.Barrack));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo($"Постройка «Артельная изба» откроется на обжитости {nextGateLevel}"));
            Assert.That(player.Domiks().Count(x => x.Type.Id == DomikIds.Barrack), Is.EqualTo(ownedCount));
        }
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
        var ex = Throws.Business(() => player.Buy(DomikIds.StoneMine));
        Assert.That(ex.Message, Is.EqualTo("Постройка «Каменоломня» откроется на обжитости 12"));

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
        var ex = Throws.Business(() => player.Buy(domikTypeId));
        Assert.That(ex.Message, Is.EqualTo($"Постройка «{name}» откроется на обжитости {gateLevel}"));
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

    /// <summary>
    /// Шестая, седьмая и восьмая Артельные избы открываются мидгеймными порогами обжитости – лестница коек ведёт игрока
    /// от первого дня до самого переезда.
    /// </summary>
    /// <param name="ownedCount">Сколько изб уже стоит у игрока.</param>
    /// <param name="gateLevel">Обжитость, открывающая следующую избу.</param>
    [TestCase(5, 60)]
    [TestCase(6, 110)]
    [TestCase(7, 175)]
    public void BarracksMidgameLadderGateTest(int ownedCount, int gateLevel)
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, ownedCount - 1);

        SetVillageLevel(player, gateLevel - 1);
        var ex = Throws.Business(() => player.Buy(DomikIds.Barrack));
        Assert.That(ex.Message, Is.EqualTo($"Постройка «Артельная изба» откроется на обжитости {gateLevel}"));

        SetVillageLevel(player, gateLevel);
        Assert.DoesNotThrow(() => player.Buy(DomikIds.Barrack));
    }

    /// <summary>
    /// Порог очередной избы виден в дорожной карте обжитости заранее, а построенные ступени из неё уходят.
    /// </summary>
    [Test]
    public void BarracksLadderIsVisibleInUnlocksTest()
    {
        const int sixthGateLevel = 60;

        var player = TestPlayer.Create();

        var sixth = player.GetVillageLevel().Unlocks.SingleOrDefault(x => x.Label == "Артельная изба ×6");
        Assert.That(sixth?.Level, Is.EqualTo(sixthGateLevel));

        player.WithDomiks(DomikIds.Barrack, 5);

        var afterBuild = player.GetVillageLevel().Unlocks;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterBuild.Any(x => x.Label == "Артельная изба ×6"), Is.False);
            Assert.That(afterBuild.Any(x => x.Label == "Артельная изба ×7"), Is.True);
        }
    }

    private static void SetVillageLevel(TestPlayer player, int target)
    {
        while (player.GetVillageLevel().Level < target - MaxDomikLevel)
        {
            player.WithDomik(DomikIds.Market, MaxDomikLevel);
        }

        while (player.GetVillageLevel().Level < target)
        {
            player.WithDomik(DomikIds.Market);
        }
    }
}
