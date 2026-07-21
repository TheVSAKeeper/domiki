using Domiki.Web.Economy;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;

namespace Domiki.Web.Tests;

public sealed class ConvoyTests
{
    /// <summary>
    /// Покупка у обоза списывает монеты по цене впятеро выше рыночной и выдаёт купленный ресурс на склад.
    /// </summary>
    [Test]
    public void BuyChargesFivefoldPriceAndGrantsResourceTest()
    {
        const int count = 2;

        var player = TestPlayer.Create()
            .WithReputation(NeighborIds.Glinischi, ConvoyManager.AccessReputationThreshold)
            .WithResource(ResourceIds.Coin, 1000);

        var coinBefore = player.Resource(ResourceIds.Coin);
        var clayBefore = player.Resource(ResourceIds.Clay);
        var price = ResourceManager.GetMarketValue(ResourceIds.Clay) * ConvoyManager.PriceMultiplier;

        player.BuyFromConvoy(NeighborIds.Glinischi, ResourceIds.Clay, count);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Resource(ResourceIds.Coin), Is.EqualTo(coinBefore - price * count));
            Assert.That(player.Resource(ResourceIds.Clay), Is.EqualTo(clayBefore + count));
        }
    }

    /// <summary>
    /// Суточный лимит обоза в 3 штуки исчерпывается тремя покупками, четвёртая в том же окне падает с ошибкой.
    /// </summary>
    [Test]
    public void LimitExhaustsAfterThreeBuysThenFourthFailsTest()
    {
        var player = TestPlayer.Create()
            .WithReputation(NeighborIds.Glinischi, ConvoyManager.AccessReputationThreshold)
            .WithResource(ResourceIds.Coin, 1000);

        for (var i = 0; i < ConvoyManager.BaseLimit; i++)
        {
            player.BuyFromConvoy(NeighborIds.Glinischi, ResourceIds.Clay);
        }

        var ex = Throws.Business(() => player.BuyFromConvoy(NeighborIds.Glinischi, ResourceIds.Clay));
        Assert.That(ex.Message, Is.EqualTo("Обоз на сегодня распродан – приходи завтра"));
    }

    /// <summary>
    /// По истечении суточного окна лимит обоза снова доступен.
    /// </summary>
    [Test]
    public void LimitRefillsAfterWindowElapsesTest()
    {
        var player = TestPlayer.Create()
            .WithReputation(NeighborIds.Glinischi, ConvoyManager.AccessReputationThreshold)
            .WithResource(ResourceIds.Coin, 1000);

        for (var i = 0; i < ConvoyManager.BaseLimit; i++)
        {
            player.BuyFromConvoy(NeighborIds.Glinischi, ResourceIds.Clay);
        }

        Assert.Throws<BusinessException>(() => player.BuyFromConvoy(NeighborIds.Glinischi, ResourceIds.Clay));

        SetConvoyWindowStart(player.Id, NeighborIds.Glinischi, DateTimeHelper.GetNowDate().AddSeconds(-ConvoyManager.WindowDurationSeconds));

        Assert.DoesNotThrow(() => player.BuyFromConvoy(NeighborIds.Glinischi, ResourceIds.Clay));
    }

    /// <summary>
    /// Обоз соседа закрыт для покупок, пока репутация у него меньше 5 – ассортимент пуст, а покупка бросает ошибку.
    /// </summary>
    [Test]
    public void LowReputationLocksConvoyTest()
    {
        var player = TestPlayer.Create();

        var convoy = player.Convoys().Single(x => x.Neighbor.Id == NeighborIds.Glinischi);
        var ex = Throws.Business(() => player.BuyFromConvoy(NeighborIds.Glinischi, ResourceIds.Clay));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(convoy.IsLocked, Is.True);
            Assert.That(convoy.Items, Is.Empty);
            Assert.That(ex.Message, Is.EqualTo("Сосед пока не пригоняет к тебе обоз – заслужи доверие его заказами"));
        }
    }

    /// <summary>
    /// Дополнительный товар соседа недоступен для покупки при репутации ниже 20 и открывается начиная с 20.
    /// </summary>
    /// <param name="reputation">Репутация игрока у соседа.</param>
    /// <param name="expectSuccess">Ожидается ли успешная покупка дополнительного товара.</param>
    [TestCase(ConvoyManager.SecondaryReputationThreshold - 1, false)]
    [TestCase(ConvoyManager.SecondaryReputationThreshold, true)]
    public void SecondaryAssortmentUnlocksAtReputationTwentyTest(int reputation, bool expectSuccess)
    {
        var player = TestPlayer.Create()
            .WithReputation(NeighborIds.Glinischi, reputation)
            .WithResource(ResourceIds.Coin, 1000);

        if (expectSuccess)
        {
            Assert.DoesNotThrow(() => player.BuyFromConvoy(NeighborIds.Glinischi, ResourceIds.Dishes));
        }
        else
        {
            var ex = Throws.Business(() => player.BuyFromConvoy(NeighborIds.Glinischi, ResourceIds.Dishes));
            Assert.That(ex.Message, Is.EqualTo("Этого товара обоз не возит"));
        }
    }

    /// <summary>
    /// При репутации от 40 суточный лимит обоза повышается до 5 штук, шестая покупка в том же окне падает с ошибкой.
    /// </summary>
    [Test]
    public void HighReputationRaisesLimitToFiveTest()
    {
        var player = TestPlayer.Create()
            .WithReputation(NeighborIds.Glinischi, ConvoyManager.HighLimitReputationThreshold)
            .WithResource(ResourceIds.Coin, 1000);

        for (var i = 0; i < ConvoyManager.HighLimit; i++)
        {
            player.BuyFromConvoy(NeighborIds.Glinischi, ResourceIds.Clay);
        }

        var ex = Throws.Business(() => player.BuyFromConvoy(NeighborIds.Glinischi, ResourceIds.Clay));
        Assert.That(ex.Message, Is.EqualTo("Обоз на сегодня распродан – приходи завтра"));
    }

    /// <summary>
    /// Ресурс вне ассортимента соседа и золото обоз не продаёт даже при высокой репутации.
    /// </summary>
    /// <param name="resourceTypeId">Тип ресурса, который пытаются купить.</param>
    [TestCase(ResourceIds.Stone)]
    [TestCase(ResourceIds.Gold)]
    public void OutOfAssortmentAndGoldAreRejectedTest(int resourceTypeId)
    {
        var player = TestPlayer.Create()
            .WithReputation(NeighborIds.Glinischi, ConvoyManager.HighLimitReputationThreshold);

        var ex = Throws.Business(() => player.BuyFromConvoy(NeighborIds.Glinischi, resourceTypeId));
        Assert.That(ex.Message, Is.EqualTo("Этого товара обоз не возит"));
    }

    /// <summary>
    /// Обоз не продаёт больше штук, чем допускает суточный лимит: запрос на огромное количество отвергается, а ресурс не выдаётся.
    /// </summary>
    [Test]
    public void CountAboveLimitIsRejectedTest()
    {
        var player = TestPlayer.Create()
            .WithReputation(NeighborIds.Glinischi, ConvoyManager.AccessReputationThreshold)
            .WithResource(ResourceIds.Coin, 1000);

        var clayBefore = player.Resource(ResourceIds.Clay);
        var ex = Throws.Business(() => player.BuyFromConvoy(NeighborIds.Glinischi, ResourceIds.Clay, int.MaxValue));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("Неверное количество"));
            Assert.That(player.Resource(ResourceIds.Clay), Is.EqualTo(clayBefore));
        }
    }

    private static void SetConvoyWindowStart(int playerId, int neighborId, DateTime windowStartDate)
    {
        using var scope = App.Scope();
        var row = scope.Context.NeighborConvoys.Single(x => x.PlayerId == playerId && x.NeighborId == neighborId);
        row.WindowStartDate = windowStartDate;
        scope.Commit();
    }
}
