using Domiki.Web.Core;
using Domiki.Web.Economy;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Tests;

[NonParallelizable]
public class MarketTests
{
    private const int CoinResourceTypeId = 1;
    private const int StoneResourceTypeId = 2;
    private const int WoodResourceTypeId = 3;
    private const int ClayResourceTypeId = 4;
    private const int GoldResourceTypeId = 5;
    private const int FountainDecorTypeId = 4;
    private const int MarketYardDomikTypeId = 9;

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
        var seller = GetUnlockedPlayer().WithResource(ClayResourceTypeId, 100);
        var buyer = GetUnlockedPlayer().WithResource(GoldResourceTypeId, 1);
        var lockedBuyer = TestPlayer.Create().WithResource(GoldResourceTypeId, 10);
        seller.PostLot(ClayResourceTypeId, 20, GoldResourceTypeId, 3);
        var lotId = seller.LastLot().Id;

        Assert.Throws<BusinessException>(() => seller.AcceptLot(lotId));
        Assert.Throws<BusinessException>(() => lockedBuyer.AcceptLot(lotId));
        Assert.Throws<BusinessException>(() => buyer.AcceptLot(lotId));
        Assert.Throws<BusinessException>(() => buyer.AcceptLot(987654));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(seller.Market()!.MyLots.Single().Id, Is.EqualTo(lotId));
            Assert.That(seller.Resource(ClayResourceTypeId), Is.EqualTo(80));
            Assert.That(buyer.Resource(GoldResourceTypeId), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Принятие лота обменивает ресурсы между продавцом и покупателем и убирает лот с обеих сторон рынка.
    /// </summary>
    [Test]
    public void AcceptLotTransfersResourcesAndDeletesLotTest()
    {
        var seller = GetUnlockedPlayer().WithResource(ClayResourceTypeId, 100);
        var buyer = GetUnlockedPlayer().WithResource(GoldResourceTypeId, 10);
        var sellerCoins = seller.Resource(CoinResourceTypeId);
        seller.PostLot(ClayResourceTypeId, 20, GoldResourceTypeId, 3);
        var lotId = seller.LastLot().Id;

        buyer.AcceptLot(lotId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(buyer.Resource(GoldResourceTypeId), Is.EqualTo(7));
            Assert.That(buyer.Resource(ClayResourceTypeId), Is.EqualTo(20));
            Assert.That(seller.Resource(GoldResourceTypeId), Is.EqualTo(3));
            Assert.That(seller.Resource(ClayResourceTypeId), Is.EqualTo(80));
            Assert.That(seller.Resource(CoinResourceTypeId), Is.EqualTo(sellerCoins - Commission(ClayResourceTypeId, 20)));
            Assert.That(seller.Market()!.MyLots, Is.Empty);
            Assert.That(buyer.Market()!.Lots, Is.Empty);
        }
    }

    /// <summary>
    /// Отмена лота возвращает эскроу-ресурс, но не возвращает уплаченную комиссию; отменить лот может только его продавец.
    /// </summary>
    [Test]
    public void CancelLotReturnsEscrowButNotCommissionTest()
    {
        var seller = GetUnlockedPlayer().WithResource(ClayResourceTypeId, 100);
        var other = GetUnlockedPlayer();
        var sellerCoins = seller.Resource(CoinResourceTypeId);
        seller.PostLot(ClayResourceTypeId, 20, GoldResourceTypeId, 3);
        var lotId = seller.LastLot().Id;

        Assert.Throws<BusinessException>(() => other.CancelLot(lotId));
        seller.CancelLot(lotId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(seller.Resource(ClayResourceTypeId), Is.EqualTo(100));
            Assert.That(seller.Resource(CoinResourceTypeId), Is.EqualTo(sellerCoins - Commission(ClayResourceTypeId, 20)));
            Assert.That(seller.Market()!.MyLots, Is.Empty);
        }
    }

    /// <summary>
    /// Одновременные попытки принять один и тот же лот успешны только у одного из конкурирующих покупателей.
    /// </summary>
    [Test]
    public async Task ConcurrentAcceptOneLotAllowsOneSuccessTest()
    {
        var seller = GetUnlockedPlayer().WithResource(ClayResourceTypeId, 100);
        var firstBuyer = GetUnlockedPlayer().WithResource(GoldResourceTypeId, 10);
        var secondBuyer = GetUnlockedPlayer().WithResource(GoldResourceTypeId, 10);
        seller.PostLot(ClayResourceTypeId, 20, GoldResourceTypeId, 3);
        var lotId = seller.LastLot().Id;

        var results = await Task.WhenAll(Task.Run(() => TryAcceptLot(firstBuyer, lotId)),
            Task.Run(() => TryAcceptLot(secondBuyer, lotId)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.Count(x => x), Is.EqualTo(1));
            Assert.That(seller.Market()!.MyLots, Is.Empty);
            Assert.That(seller.Resource(GoldResourceTypeId), Is.EqualTo(3));
            Assert.That(firstBuyer.Resource(ClayResourceTypeId) + secondBuyer.Resource(ClayResourceTypeId), Is.EqualTo(20));
        }
    }

    /// <summary>
    /// Взаимное одновременное принятие лотов друг друга двумя игроками завершается успешно, без взаимной блокировки.
    /// </summary>
    [Test]
    public async Task CrossAcceptLotsDoesNotDeadlockTest()
    {
        var first = GetUnlockedPlayer().WithResource(ClayResourceTypeId, 100).WithResource(WoodResourceTypeId, 100);
        var second = GetUnlockedPlayer().WithResource(ClayResourceTypeId, 100).WithResource(WoodResourceTypeId, 100);
        first.PostLot(ClayResourceTypeId, 20, WoodResourceTypeId, 10);
        var firstLotId = first.LastLot().Id;
        second.PostLot(WoodResourceTypeId, 10, ClayResourceTypeId, 20);
        var secondLotId = second.LastLot().Id;

        var results = await Task.WhenAll(Task.Run(() => TryAcceptLot(first, secondLotId)),
            Task.Run(() => TryAcceptLot(second, firstLotId)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results, Is.All.True);
            Assert.That(first.Market()!.MyLots, Is.Empty);
            Assert.That(second.Market()!.MyLots, Is.Empty);
        }
    }

    /// <summary>
    /// Истёкший лот, снятый планировщиком, возвращает эскроу-ресурс, но не возвращает уплаченную комиссию.
    /// </summary>
    [Test]
    public void ExpiredLotReturnsEscrowButNotCommissionTest()
    {
        var seller = GetUnlockedPlayer().WithResource(ClayResourceTypeId, 100);
        var sellerCoins = seller.Resource(CoinResourceTypeId);
        seller.PostLot(ClayResourceTypeId, 20, GoldResourceTypeId, 3);
        var lotId = seller.LastLot().Id;
        var expireDate = DateTimeHelper.GetNowDate().AddSeconds(-1);
        SetTradeLotExpire(lotId, expireDate);

        seller.FinishTradeLot(lotId, expireDate);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(seller.Resource(ClayResourceTypeId), Is.EqualTo(100));
            Assert.That(seller.Resource(CoinResourceTypeId), Is.EqualTo(sellerCoins - Commission(ClayResourceTypeId, 20)));
            Assert.That(seller.Market()!.MyLots, Is.Empty);
        }
    }

    /// <summary>
    /// У нового игрока без Торгового двора рынок отсутствует.
    /// </summary>
    [Test]
    public void GetMarketForNewPlayerReturnsNullTest()
    {
        var player = TestPlayer.Create();

        var market = player.Market();

        Assert.That(market, Is.Null);
    }

    /// <summary>
    /// Постройка Торгового двора первого уровня открывает пустой рынок с комиссией и лимитом первого уровня, а также
    /// показывает ставку следующего уровня.
    /// </summary>
    [Test]
    public void GetMarketWithBuildingReturnsEmptyTest()
    {
        var player = GetUnlockedPlayer();

        var market = player.Market()!;

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
        var player = GetUnlockedPlayer().WithResource(ClayResourceTypeId, 3);

        player.PostLot(ClayResourceTypeId, 1, GoldResourceTypeId, 1);
        player.PostLot(ClayResourceTypeId, 1, GoldResourceTypeId, 1);

        var ex = Assert.Throws<BusinessException>(() => player.PostLot(ClayResourceTypeId, 1, GoldResourceTypeId, 1));
        Assert.That(ex!.Message, Is.EqualTo("Все места на прилавке заняты – улучшите Торговый двор"));
    }

    /// <summary>
    /// Некорректный лот (нехватка ресурса, нулевое количество, совпадение отдаваемого и желаемого ресурса, нехватка монет на
    /// комиссию) не создаётся и ничего не списывает.
    /// </summary>
    [Test]
    public void PostLotInvalidOrInsufficientDoesNotCreateLotTest()
    {
        var player = GetUnlockedPlayer();

        Assert.Throws<BusinessException>(() => player.PostLot(ClayResourceTypeId, 1, GoldResourceTypeId, 1));
        Assert.Throws<BusinessException>(() => player.PostLot(ClayResourceTypeId, 0, GoldResourceTypeId, 1));
        player.WithResource(ClayResourceTypeId, 10);
        Assert.Throws<BusinessException>(() => player.PostLot(ClayResourceTypeId, 1, ClayResourceTypeId, 1));
        SetResource(player.Id, CoinResourceTypeId, 0);
        Assert.Throws<BusinessException>(() => player.PostLot(ClayResourceTypeId, 1, GoldResourceTypeId, 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Market()!.MyLots, Is.Empty);
            Assert.That(player.Resource(ClayResourceTypeId), Is.EqualTo(10));
        }
    }

    /// <summary>
    /// Без Торгового двора выставить лот нельзя: бросается ошибка «Нужен Торговый двор», ресурсы не списываются.
    /// </summary>
    [Test]
    public void PostLotWithoutBuildingThrowsAndDoesNotWriteOffTest()
    {
        var player = TestPlayer.Create().WithResource(ClayResourceTypeId, 100);
        var before = player.Resource(ClayResourceTypeId);

        var ex = Assert.Throws<BusinessException>(() => player.PostLot(ClayResourceTypeId, 20, GoldResourceTypeId, 3))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("Нужен Торговый двор"));
            Assert.That(player.Market(), Is.Null);
            Assert.That(player.Resource(ClayResourceTypeId), Is.EqualTo(before));
        }
    }

    /// <summary>
    /// Выставление лота списывает отдаваемый ресурс в эскроу и комиссию монетами, а лот со сведениями о деревне продавца
    /// становится виден другим игрокам.
    /// </summary>
    [Test]
    public void PostLotWritesOffEscrowAndCommissionAndVisibleToOtherPlayerTest()
    {
        var seller = GetUnlockedPlayer();
        var buyer = GetUnlockedPlayer();
        var villageName = "TestVillage-" + Guid.NewGuid().ToString("N")[..8];
        SetVillage(seller.Id, villageName, 2, 3);
        seller.WithResource(ClayResourceTypeId, 100);
        var coinBefore = seller.Resource(CoinResourceTypeId);

        seller.PostLot(ClayResourceTypeId, 20, GoldResourceTypeId, 3);
        var lotId = seller.LastLot().Id;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(seller.Resource(ClayResourceTypeId), Is.EqualTo(80));
            Assert.That(seller.Resource(CoinResourceTypeId), Is.EqualTo(coinBefore - Commission(ClayResourceTypeId, 20)));
        }

        var sellerMarket = seller.Market()!;
        Assert.That(sellerMarket.MyLots.Single().Id, Is.EqualTo(lotId));
        var buyerLot = buyer.Market()!.Lots.Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(buyerLot.Id, Is.EqualTo(lotId));
            Assert.That(buyerLot.SellerVillageName, Is.EqualTo(villageName));
            Assert.That(buyerLot.SellerCrestIcon, Is.EqualTo(2));
            Assert.That(buyerLot.SellerCrestColor, Is.EqualTo(3));
        }
    }

    /// <summary>
    /// Комиссия рынка считается по уровню Торгового двора, типу и количеству отдаваемого ресурса.
    /// </summary>
    /// <param name="level">Уровень Торгового двора.</param>
    /// <param name="giveResourceTypeId">Тип отдаваемого ресурса.</param>
    /// <param name="giveValue">Количество отдаваемого ресурса.</param>
    /// <param name="expected">Ожидаемая комиссия в монетах.</param>
    [TestCase(1, 4, 20, 16)]
    [TestCase(5, 4, 20, 6)]
    [TestCase(1, 6, 1, 3)]
    [TestCase(6, 6, 1, 2)]
    [TestCase(1, 4, 1, 2)]
    [TestCase(6, 4, 100, 30)]
    public void ComputeCommissionByLevelTest(int level, int giveResourceTypeId, int giveValue, int expected)
    {
        Assert.That(MarketManager.ComputeCommission(level, giveResourceTypeId, giveValue), Is.EqualTo(expected));
    }

    private static int Commission(int giveResourceTypeId, int giveValue)
    {
        return MarketManager.ComputeCommission(1, giveResourceTypeId, giveValue);
    }

    private static TestPlayer GetUnlockedPlayer()
    {
        return TestPlayer.Create().WithDecor(FountainDecorTypeId, 4).WithResource(CoinResourceTypeId, 800).Buy(MarketYardDomikTypeId);
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
