using Domiki.Web.Core;
using Domiki.Web.Data.Entities;
using Domiki.Web.Economy;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Tests;

[NonParallelizable]
public sealed class MarketTests
{
    [SetUp]
    public void SetUp()
    {
        ClearTradeLots();
    }

    [TearDown]
    public void TearDown()
    {
        ClearTradeLots();
    }

    /// <summary>
    /// Лот нельзя принять самому продавцу, без Торгового двора, без нужного ресурса или по несуществующему id – во всех
    /// случаях обмен не происходит.
    /// </summary>
    [Test]
    public void AcceptLotInvalidCasesDoNotTransferTest()
    {
        const int startClay = 100;
        const int giveClay = 20;
        const int askGold = 3;
        const int buyerGold = 1;

        var seller = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Clay, startClay);

        var buyer = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Gold, buyerGold);

        var lockedBuyer = TestPlayer.Create()
            .WithResource(ResourceIds.Gold, 10);

        seller.PostLot(ResourceIds.Clay, giveClay, ResourceIds.Gold, askGold);
        var lotId = seller.LastLot().Id;

        Assert.Throws<BusinessException>(() => seller.AcceptLot(lotId));
        Assert.Throws<BusinessException>(() => lockedBuyer.AcceptLot(lotId));
        Assert.Throws<BusinessException>(() => buyer.AcceptLot(lotId));
        Assert.Throws<BusinessException>(() => buyer.AcceptLot(987654));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(seller.Market().MyLots.Single().Id, Is.EqualTo(lotId));
            Assert.That(seller.Resource(ResourceIds.Clay), Is.EqualTo(startClay - giveClay));
            Assert.That(buyer.Resource(ResourceIds.Gold), Is.EqualTo(buyerGold));
        }
    }

    /// <summary>
    /// Принятие лота обменивает ресурсы между продавцом и покупателем и убирает лот с обеих сторон рынка.
    /// </summary>
    [Test]
    public void AcceptLotTransfersResourcesAndDeletesLotTest()
    {
        const int startClay = 100;
        const int startGold = 10;
        const int giveClay = 20;
        const int askGold = 3;

        var seller = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Clay, startClay);

        var buyer = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Gold, startGold);

        var sellerCoins = seller.Resource(ResourceIds.Coin);
        seller.PostLot(ResourceIds.Clay, giveClay, ResourceIds.Gold, askGold);
        var lotId = seller.LastLot().Id;

        buyer.AcceptLot(lotId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(buyer.Resource(ResourceIds.Gold), Is.EqualTo(startGold - askGold));
            Assert.That(buyer.Resource(ResourceIds.Clay), Is.EqualTo(giveClay));
            Assert.That(seller.Resource(ResourceIds.Gold), Is.EqualTo(askGold));
            Assert.That(seller.Resource(ResourceIds.Clay), Is.EqualTo(startClay - giveClay));
            Assert.That(seller.Resource(ResourceIds.Coin), Is.EqualTo(sellerCoins - Commission(ResourceIds.Clay, giveClay)));
            Assert.That(seller.Market().MyLots, Is.Empty);
            Assert.That(buyer.Market().Lots, Is.Empty);
        }
    }

    /// <summary>
    /// Отмена лота возвращает эскроу-ресурс, но не возвращает уплаченную комиссию; отменить лот может только его продавец.
    /// </summary>
    [Test]
    public void CancelLotReturnsEscrowButNotCommissionTest()
    {
        const int startClay = 100;
        const int giveClay = 20;
        const int askGold = 3;

        var seller = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Clay, startClay);

        var other = TestPlayer.Create()
            .WithMarketUnlocked();

        var sellerCoins = seller.Resource(ResourceIds.Coin);
        seller.PostLot(ResourceIds.Clay, giveClay, ResourceIds.Gold, askGold);
        var lotId = seller.LastLot().Id;

        Assert.Throws<BusinessException>(() => other.CancelLot(lotId));
        seller.CancelLot(lotId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(seller.Resource(ResourceIds.Clay), Is.EqualTo(startClay));
            Assert.That(seller.Resource(ResourceIds.Coin), Is.EqualTo(sellerCoins - Commission(ResourceIds.Clay, giveClay)));
            Assert.That(seller.Market().MyLots, Is.Empty);
        }
    }

    /// <summary>
    /// Одновременные попытки принять один и тот же лот успешны только у одного из конкурирующих покупателей.
    /// </summary>
    [Test]
    public async Task ConcurrentAcceptOneLotAllowsOneSuccessTest()
    {
        const int startClay = 100;
        const int startGold = 10;
        const int giveClay = 20;
        const int askGold = 3;

        var seller = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Clay, startClay);

        var firstBuyer = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Gold, startGold);

        var secondBuyer = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Gold, startGold);

        seller.PostLot(ResourceIds.Clay, giveClay, ResourceIds.Gold, askGold);
        var lotId = seller.LastLot().Id;

        var results = await Task.WhenAll(Task.Run(() => TryAcceptLot(firstBuyer, lotId)),
            Task.Run(() => TryAcceptLot(secondBuyer, lotId)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.Count(x => x), Is.EqualTo(1));
            Assert.That(seller.Market().MyLots, Is.Empty);
            Assert.That(seller.Resource(ResourceIds.Gold), Is.EqualTo(askGold));
            Assert.That(firstBuyer.Resource(ResourceIds.Clay) + secondBuyer.Resource(ResourceIds.Clay), Is.EqualTo(giveClay));
        }
    }

    /// <summary>
    /// Взаимное одновременное принятие лотов друг друга двумя игроками завершается успешно, без взаимной блокировки.
    /// </summary>
    [Test]
    public async Task CrossAcceptLotsDoesNotDeadlockTest()
    {
        var first = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Clay, 100)
            .WithResource(ResourceIds.Wood, 100);

        var second = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Clay, 100)
            .WithResource(ResourceIds.Wood, 100);

        first.PostLot(ResourceIds.Clay, 20, ResourceIds.Wood, 10);
        var firstLotId = first.LastLot().Id;
        second.PostLot(ResourceIds.Wood, 10, ResourceIds.Clay, 20);
        var secondLotId = second.LastLot().Id;

        var results = await Task.WhenAll(Task.Run(() => TryAcceptLot(first, secondLotId)),
            Task.Run(() => TryAcceptLot(second, firstLotId)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results, Is.All.True);
            Assert.That(first.Market().MyLots, Is.Empty);
            Assert.That(second.Market().MyLots, Is.Empty);
        }
    }

    /// <summary>
    /// Истёкший лот, снятый планировщиком, возвращает эскроу-ресурс, но не возвращает уплаченную комиссию.
    /// </summary>
    [Test]
    public void ExpiredLotReturnsEscrowButNotCommissionTest()
    {
        const int startClay = 100;
        const int giveClay = 20;
        const int askGold = 3;

        var seller = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Clay, startClay);

        var sellerCoins = seller.Resource(ResourceIds.Coin);
        seller.PostLot(ResourceIds.Clay, giveClay, ResourceIds.Gold, askGold);
        var lotId = seller.LastLot().Id;
        var expireDate = DateTimeHelper.GetNowDate().AddSeconds(-1);
        SetTradeLotExpire(lotId, expireDate);

        seller.FinishTradeLot(lotId, expireDate);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(seller.Resource(ResourceIds.Clay), Is.EqualTo(startClay));
            Assert.That(seller.Resource(ResourceIds.Coin), Is.EqualTo(sellerCoins - Commission(ResourceIds.Clay, giveClay)));
            Assert.That(seller.Market().MyLots, Is.Empty);
        }
    }

    /// <summary>
    /// У нового игрока без Торгового двора рынок отсутствует.
    /// </summary>
    [Test]
    public void GetMarketForNewPlayerReturnsNullTest()
    {
        var player = TestPlayer.Create();

        var market = player.MarketOrNull();

        Assert.That(market, Is.Null);
    }

    /// <summary>
    /// Постройка Торгового двора первого уровня открывает пустой рынок с комиссией и лимитом первого уровня, а также
    /// показывает ставку следующего уровня.
    /// </summary>
    [Test]
    public void GetMarketWithBuildingReturnsEmptyTest()
    {
        var player = TestPlayer.Create()
            .WithMarketUnlocked();

        var market = player.Market();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(market.Lots, Is.Empty);
            Assert.That(market.MyLots, Is.Empty);
            Assert.That(market.BuildingLevel, Is.EqualTo(1));
            Assert.That(market.CommissionRate, Is.EqualTo(MarketManager.CommissionRateL1));
            Assert.That(market.CommissionMin, Is.EqualTo(MarketManager.MinCommissionCoins));
            Assert.That(market.NextCommissionRate, Is.EqualTo(MarketManager.GetCommissionRate(2)));
        }
    }

    /// <summary>
    /// Торговый двор первого уровня допускает не больше двух активных лотов одновременно, третий бросает ошибку с предложением
    /// улучшить постройку.
    /// </summary>
    [Test]
    public void MarketYardLevelOneAllowsTwoActiveLotsOnlyTest()
    {
        var player = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Clay, 3);

        player.PostLot(ResourceIds.Clay, 1, ResourceIds.Gold, 1);
        player.PostLot(ResourceIds.Clay, 1, ResourceIds.Gold, 1);

        var ex = Throws.Business(() => player.PostLot(ResourceIds.Clay, 1, ResourceIds.Gold, 1));
        Assert.That(ex.Message, Is.EqualTo("Все места на прилавке заняты – улучшите Торговый двор"));
    }

    /// <summary>
    /// Некорректный лот (нехватка ресурса, нулевое количество, совпадение отдаваемого и желаемого ресурса, нехватка монет на
    /// комиссию) не создаётся и ничего не списывает.
    /// </summary>
    [Test]
    public void PostLotInvalidOrInsufficientDoesNotCreateLotTest()
    {
        const int startClay = 10;

        var player = TestPlayer.Create()
            .WithMarketUnlocked();

        Assert.Throws<BusinessException>(() => player.PostLot(ResourceIds.Clay, 1, ResourceIds.Gold, 1));
        Assert.Throws<BusinessException>(() => player.PostLot(ResourceIds.Clay, 0, ResourceIds.Gold, 1));
        player.WithResource(ResourceIds.Clay, startClay);
        Assert.Throws<BusinessException>(() => player.PostLot(ResourceIds.Clay, 1, ResourceIds.Clay, 1));
        SetResource(player.Id, ResourceIds.Coin, 0);
        Assert.Throws<BusinessException>(() => player.PostLot(ResourceIds.Clay, 1, ResourceIds.Gold, 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Market().MyLots, Is.Empty);
            Assert.That(player.Resource(ResourceIds.Clay), Is.EqualTo(startClay));
        }
    }

    /// <summary>
    /// Без Торгового двора выставить лот нельзя: бросается ошибка «Нужен Торговый двор», ресурсы не списываются.
    /// </summary>
    [Test]
    public void PostLotWithoutBuildingThrowsAndDoesNotWriteOffTest()
    {
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Clay, 100);

        var before = player.Resource(ResourceIds.Clay);
        var ex = Throws.Business(() => player.PostLot(ResourceIds.Clay, 20, ResourceIds.Gold, 3));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("Нужен Торговый двор"));
            Assert.That(player.MarketOrNull(), Is.Null);
            Assert.That(player.Resource(ResourceIds.Clay), Is.EqualTo(before));
        }
    }

    /// <summary>
    /// Выставление лота списывает отдаваемый ресурс в эскроу и комиссию монетами, а лот со сведениями о деревне продавца
    /// становится виден другим игрокам.
    /// </summary>
    [Test]
    public void PostLotWritesOffEscrowAndCommissionAndVisibleToOtherPlayerTest()
    {
        const int startClay = 100;
        const int giveClay = 20;
        const int askGold = 3;
        const int crestIcon = 2;
        const int crestColor = 3;

        var seller = TestPlayer.Create()
            .WithMarketUnlocked();

        var buyer = TestPlayer.Create()
            .WithMarketUnlocked();

        var villageName = "TestVillage-" + Guid.NewGuid().ToString("N")[..8];
        SetVillage(seller.Id, villageName, crestIcon, crestColor);
        seller.WithResource(ResourceIds.Clay, startClay);
        var coinBefore = seller.Resource(ResourceIds.Coin);

        seller.PostLot(ResourceIds.Clay, giveClay, ResourceIds.Gold, askGold);
        var lotId = seller.LastLot().Id;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(seller.Resource(ResourceIds.Clay), Is.EqualTo(startClay - giveClay));
            Assert.That(seller.Resource(ResourceIds.Coin), Is.EqualTo(coinBefore - Commission(ResourceIds.Clay, giveClay)));
        }

        var sellerMarket = seller.Market();
        Assert.That(sellerMarket.MyLots.Single().Id, Is.EqualTo(lotId));
        var buyerLot = buyer.Market().Lots.Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(buyerLot.Id, Is.EqualTo(lotId));
            Assert.That(buyerLot.SellerVillageName, Is.EqualTo(villageName));
            Assert.That(buyerLot.SellerCrestIcon, Is.EqualTo(crestIcon));
            Assert.That(buyerLot.SellerCrestColor, Is.EqualTo(crestColor));
        }
    }

    /// <summary>
    /// Заявка на покупку эскроуит золото и комиссию монетами, а созданный лот помечен как Buy.
    /// </summary>
    [Test]
    public void PostBuyLotEscrowsGoldAndCommissionAndMarksKindTest()
    {
        const int startGold = 10;
        const int giveGold = 3;
        const int wantBrick = 20;

        var buyer = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Gold, startGold);

        var coinBefore = buyer.Resource(ResourceIds.Coin);
        buyer.PostBuyLot(giveGold, ResourceIds.Brick, wantBrick);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(buyer.Resource(ResourceIds.Gold), Is.EqualTo(startGold - giveGold));
            Assert.That(buyer.Resource(ResourceIds.Coin), Is.EqualTo(coinBefore - Commission(ResourceIds.Gold, giveGold)));
            Assert.That(buyer.LastLot().Kind, Is.EqualTo(TradeLotKind.Buy));
        }
    }

    /// <summary>
    /// Принятие заявки на покупку отдаёт акцептору золото в обмен на товар, а заявителю – товар.
    /// </summary>
    [Test]
    public void AcceptBuyLotTransfersResourcesTest()
    {
        const int startGold = 10;
        const int startBrick = 50;
        const int giveGold = 3;
        const int wantBrick = 20;

        var poster = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Gold, startGold);

        var acceptor = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Brick, startBrick);

        poster.PostBuyLot(giveGold, ResourceIds.Brick, wantBrick);
        var lotId = poster.LastLot().Id;

        acceptor.AcceptLot(lotId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(acceptor.Resource(ResourceIds.Brick), Is.EqualTo(startBrick - wantBrick));
            Assert.That(acceptor.Resource(ResourceIds.Gold), Is.EqualTo(giveGold));
            Assert.That(poster.Resource(ResourceIds.Brick), Is.EqualTo(wantBrick));
            Assert.That(poster.Resource(ResourceIds.Gold), Is.EqualTo(startGold - giveGold));
            Assert.That(poster.Market().MyLots, Is.Empty);
            Assert.That(acceptor.Market().Lots, Is.Empty);
        }
    }

    /// <summary>
    /// Истёкшая заявка на покупку, снятая планировщиком, возвращает эскроуированное золото, но не комиссию.
    /// </summary>
    [Test]
    public void ExpiredBuyLotReturnsGoldButNotCommissionTest()
    {
        const int startGold = 10;
        const int giveGold = 3;
        const int wantBrick = 20;

        var buyer = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Gold, startGold);

        var coinBefore = buyer.Resource(ResourceIds.Coin);
        buyer.PostBuyLot(giveGold, ResourceIds.Brick, wantBrick);
        var lotId = buyer.LastLot().Id;
        var expireDate = DateTimeHelper.GetNowDate().AddSeconds(-1);
        SetTradeLotExpire(lotId, expireDate);

        buyer.FinishTradeLot(lotId, expireDate);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(buyer.Resource(ResourceIds.Gold), Is.EqualTo(startGold));
            Assert.That(buyer.Resource(ResourceIds.Coin), Is.EqualTo(coinBefore - Commission(ResourceIds.Gold, giveGold)));
            Assert.That(buyer.Market().MyLots, Is.Empty);
        }
    }

    /// <summary>
    /// Отмена заявки на покупку возвращает эскроуированное золото, но не комиссию.
    /// </summary>
    [Test]
    public void CancelBuyLotReturnsGoldButNotCommissionTest()
    {
        const int startGold = 10;
        const int giveGold = 3;
        const int wantBrick = 20;

        var buyer = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Gold, startGold);

        var coinBefore = buyer.Resource(ResourceIds.Coin);
        buyer.PostBuyLot(giveGold, ResourceIds.Brick, wantBrick);
        var lotId = buyer.LastLot().Id;

        buyer.CancelLot(lotId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(buyer.Resource(ResourceIds.Gold), Is.EqualTo(startGold));
            Assert.That(buyer.Resource(ResourceIds.Coin), Is.EqualTo(coinBefore - Commission(ResourceIds.Gold, giveGold)));
            Assert.That(buyer.Market().MyLots, Is.Empty);
        }
    }

    /// <summary>
    /// Заявку на покупку нельзя оплатить не золотом и нельзя выставить на покупку золота или монет.
    /// </summary>
    /// <param name="giveResourceTypeId">Тип отдаваемого ресурса.</param>
    /// <param name="wantResourceTypeId">Тип желаемого ресурса.</param>
    /// <param name="expectedMessage">Текст ошибки.</param>
    [TestCase(ResourceIds.Clay, ResourceIds.Brick, "Заявка оплачивается только золотом")]
    [TestCase(ResourceIds.Gold, ResourceIds.Coin, "Нельзя купить за золото само золото или монеты")]
    public void PostBuyLotValidatesResourceKindsTest(int giveResourceTypeId, int wantResourceTypeId, string expectedMessage)
    {
        var player = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Clay, 10)
            .WithResource(ResourceIds.Gold, 10);

        var ex = Throws.Business(() => player.PostLot(giveResourceTypeId, 1, wantResourceTypeId, 1, TradeLotKind.Buy));
        Assert.That(ex.Message, Is.EqualTo(expectedMessage));

        Assert.That(player.Market().MyLots, Is.Empty);
    }

    /// <summary>
    /// Продажа за золото не ограничена – в отличие от заявки на покупку, лот создаётся с типом Sell.
    /// </summary>
    [Test]
    public void SellLotForGoldIsAllowedTest()
    {
        var player = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Gold, 10);

        player.PostLot(ResourceIds.Gold, 1, ResourceIds.Brick, 1);

        Assert.That(player.LastLot().Kind, Is.EqualTo(TradeLotKind.Sell));
    }

    /// <summary>
    /// Лоты продажи и заявки на покупку делят один и тот же лимит прилавка Торгового двора.
    /// </summary>
    [Test]
    public void SellAndBuyLotsShareTheSameShelfLimitTest()
    {
        var player = TestPlayer.Create()
            .WithMarketUnlocked()
            .WithResource(ResourceIds.Clay, 10)
            .WithResource(ResourceIds.Gold, 10);

        player.PostLot(ResourceIds.Clay, 1, ResourceIds.Gold, 1);
        player.PostBuyLot(1, ResourceIds.Brick, 1);

        var ex = Throws.Business(() => player.PostBuyLot(1, ResourceIds.Brick, 1));
        Assert.That(ex.Message, Is.EqualTo("Все места на прилавке заняты – улучшите Торговый двор"));
    }

    /// <summary>
    /// Комиссия рынка считается по уровню Торгового двора, типу и количеству отдаваемого ресурса.
    /// </summary>
    /// <param name="level">Уровень Торгового двора.</param>
    /// <param name="giveResourceTypeId">Тип отдаваемого ресурса.</param>
    /// <param name="giveValue">Количество отдаваемого ресурса.</param>
    /// <param name="expected">Ожидаемая комиссия в монетах.</param>
    [TestCase(1, ResourceIds.Clay, 20, 16)]
    [TestCase(5, ResourceIds.Clay, 20, 6)]
    [TestCase(1, ResourceIds.Brick, 1, 3)]
    [TestCase(6, ResourceIds.Brick, 1, 2)]
    [TestCase(1, ResourceIds.Clay, 1, 2)]
    [TestCase(6, ResourceIds.Clay, 100, 30)]
    public void ComputeCommissionByLevelTest(int level, int giveResourceTypeId, int giveValue, int expected)
    {
        Assert.That(MarketManager.ComputeCommission(level, giveResourceTypeId, giveValue), Is.EqualTo(expected));
    }

    private static int Commission(int giveResourceTypeId, int giveValue)
    {
        return MarketManager.ComputeCommission(1, giveResourceTypeId, giveValue);
    }

    private static bool TryAcceptLot(TestPlayer player, int lotId)
    {
        try
        {
            player.AcceptLot(lotId);
            return true;
        }
        catch (BusinessException)
        {
            return false;
        }
    }

    private static void SetVillage(int playerId, string name, int crestIcon, int crestColor)
    {
        App.Act<DomikManager>(m => m.SetVillageIdentity(playerId, name, crestIcon, crestColor));
    }

    private static void SetTradeLotExpire(int lotId, DateTime expireDate)
    {
        using var scope = App.Scope();
        var lot = scope.Context.TradeLots.Single(x => x.Id == lotId);
        lot.ExpireDate = expireDate;
        scope.Commit();
    }

    private static void SetResource(int playerId, int resourceTypeId, int value)
    {
        using var scope = App.Scope();
        var resource = scope.Context.Resources.SingleOrDefault(x => x.PlayerId == playerId && x.TypeId == resourceTypeId);
        if (resource == null)
        {
            resource = new()
            {
                PlayerId = playerId,
                TypeId = resourceTypeId,
            };

            scope.Context.Resources.Add(resource);
        }

        resource.Value = value;
        scope.Commit();
    }

    private static void ClearTradeLots()
    {
        using var scope = App.Scope();
        scope.Context.TradeLots.RemoveRange(scope.Context.TradeLots);
        scope.Commit();
    }
}

file static class MarketTestsActs
{
    public static TestPlayer WithMarketUnlocked(this TestPlayer player)
    {
        return player.WithDecor(DecorIds.Fountain, 4)
            .WithResource(ResourceIds.Coin, 800)
            .Buy(DomikIds.MarketYard);
    }
}
