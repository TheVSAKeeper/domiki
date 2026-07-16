using Domiki.Web.Core.Scheduling;
using Domiki.Web.Economy.Models;
using Domiki.Web.Economy;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference.Models;
using Domiki.Web.Village.Models;

namespace Domiki.Web.Tests
{
    [NonParallelizable]
    public class MarketTests : TestBase
    {
        private const int CoinResourceTypeId = 1;
        private const int StoneResourceTypeId = 2;
        private const int WoodResourceTypeId = 3;
        private const int ClayResourceTypeId = 4;
        private const int GoldResourceTypeId = 5;
        private const int FountainDecorTypeId = 4;
        private const int MarketYardDomikTypeId = 9;

        private static int Commission(int giveResourceTypeId, int giveValue) => MarketManager.ComputeCommission(1, giveResourceTypeId, giveValue);

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
        /// У нового игрока без Торгового двора рынок отсутствует.
        /// </summary>
        [Test]
        public void GetMarketForNewPlayerReturnsNullTest()
        {
            var playerId = GetPlayerId();

            var market = GetMarket(playerId);

            Assert.That(market, Is.Null);
        }

        /// <summary>
        /// Постройка Торгового двора первого уровня открывает пустой рынок с комиссией и лимитом первого уровня, а также показывает ставку следующего уровня.
        /// </summary>
        [Test]
        public void GetMarketWithBuildingReturnsEmptyTest()
        {
            var playerId = GetUnlockedPlayerId();

            var market = GetMarket(playerId)!;

            Assert.That(market.Lots, Is.Empty);
            Assert.That(market.MyLots, Is.Empty);
            Assert.That(market.BuildingLevel, Is.EqualTo(1));
            Assert.That(market.CommissionRate, Is.EqualTo(MarketManager.CommissionRateL1));
            Assert.That(market.CommissionMin, Is.EqualTo(MarketManager.MinCommissionCoins));
            Assert.That(market.NextCommissionRate, Is.EqualTo(MarketManager.GetCommissionRate(2)));
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

        /// <summary>
        /// Без Торгового двора выставить лот нельзя: бросается ошибка «Нужен Торговый двор», ресурсы не списываются.
        /// </summary>
        [Test]
        public void PostLotWithoutBuildingThrowsAndDoesNotWriteOffTest()
        {
            var playerId = GetPlayerId();
            GrantResource(playerId, ClayResourceTypeId, 100);
            var before = GetResourceValue(playerId, ClayResourceTypeId);

            var ex = Assert.Throws<BusinessException>(() => PostLot(playerId, ClayResourceTypeId, 20, GoldResourceTypeId, 3))!;

            Assert.That(ex.Message, Is.EqualTo("Нужен Торговый двор"));
            Assert.That(GetMarket(playerId), Is.Null);
            Assert.That(GetResourceValue(playerId, ClayResourceTypeId), Is.EqualTo(before));
        }

        /// <summary>
        /// Выставление лота списывает отдаваемый ресурс в эскроу и комиссию монетами, а лот со сведениями о деревне продавца становится виден другим игрокам.
        /// </summary>
        [Test]
        public void PostLotWritesOffEscrowAndCommissionAndVisibleToOtherPlayerTest()
        {
            var sellerId = GetUnlockedPlayerId();
            var buyerId = GetUnlockedPlayerId();
            var villageName = TestVillageName();
            SetVillage(sellerId, villageName, 2, 3);
            GrantResource(sellerId, ClayResourceTypeId, 100);
            var coinBefore = GetResourceValue(sellerId, CoinResourceTypeId);

            var lotId = PostLot(sellerId, ClayResourceTypeId, 20, GoldResourceTypeId, 3);

            Assert.That(GetResourceValue(sellerId, ClayResourceTypeId), Is.EqualTo(80));
            Assert.That(GetResourceValue(sellerId, CoinResourceTypeId), Is.EqualTo(coinBefore - Commission(ClayResourceTypeId, 20)));
            var sellerMarket = GetMarket(sellerId)!;
            Assert.That(sellerMarket.MyLots.Single().Id, Is.EqualTo(lotId));
            var buyerLot = GetMarket(buyerId)!.Lots.Single();
            Assert.That(buyerLot.Id, Is.EqualTo(lotId));
            Assert.That(buyerLot.SellerVillageName, Is.EqualTo(villageName));
            Assert.That(buyerLot.SellerCrestIcon, Is.EqualTo(2));
            Assert.That(buyerLot.SellerCrestColor, Is.EqualTo(3));
        }

        /// <summary>
        /// Торговый двор первого уровня допускает не больше двух активных лотов одновременно, третий бросает ошибку с предложением улучшить постройку.
        /// </summary>
        [Test]
        public void MarketYardLevelOneAllowsTwoActiveLotsOnlyTest()
        {
            var playerId = GetUnlockedPlayerId();
            GrantResource(playerId, ClayResourceTypeId, 3);

            PostLot(playerId, ClayResourceTypeId, 1, GoldResourceTypeId, 1);
            PostLot(playerId, ClayResourceTypeId, 1, GoldResourceTypeId, 1);

            var ex = Assert.Throws<BusinessException>(() => PostLot(playerId, ClayResourceTypeId, 1, GoldResourceTypeId, 1));
            Assert.That(ex!.Message, Is.EqualTo("Все места на прилавке заняты – улучшите Торговый двор"));
        }

        /// <summary>
        /// Некорректный лот (нехватка ресурса, нулевое количество, совпадение отдаваемого и желаемого ресурса, нехватка монет на комиссию) не создаётся и ничего не списывает.
        /// </summary>
        [Test]
        public void PostLotInvalidOrInsufficientDoesNotCreateLotTest()
        {
            var playerId = GetUnlockedPlayerId();

            Assert.Throws<BusinessException>(() => PostLot(playerId, ClayResourceTypeId, 1, GoldResourceTypeId, 1));
            Assert.Throws<BusinessException>(() => PostLot(playerId, ClayResourceTypeId, 0, GoldResourceTypeId, 1));
            GrantResource(playerId, ClayResourceTypeId, 10);
            Assert.Throws<BusinessException>(() => PostLot(playerId, ClayResourceTypeId, 1, ClayResourceTypeId, 1));
            SetResource(playerId, CoinResourceTypeId, 0);
            Assert.Throws<BusinessException>(() => PostLot(playerId, ClayResourceTypeId, 1, GoldResourceTypeId, 1));

            Assert.That(GetMarket(playerId)!.MyLots, Is.Empty);
            Assert.That(GetResourceValue(playerId, ClayResourceTypeId), Is.EqualTo(10));
        }

        /// <summary>
        /// Принятие лота обменивает ресурсы между продавцом и покупателем и убирает лот с обеих сторон рынка.
        /// </summary>
        [Test]
        public void AcceptLotTransfersResourcesAndDeletesLotTest()
        {
            var sellerId = GetUnlockedPlayerId();
            var buyerId = GetUnlockedPlayerId();
            GrantResource(sellerId, ClayResourceTypeId, 100);
            GrantResource(buyerId, GoldResourceTypeId, 10);
            var sellerCoins = GetResourceValue(sellerId, CoinResourceTypeId);
            var lotId = PostLot(sellerId, ClayResourceTypeId, 20, GoldResourceTypeId, 3);

            AcceptLot(buyerId, lotId);

            Assert.That(GetResourceValue(buyerId, GoldResourceTypeId), Is.EqualTo(7));
            Assert.That(GetResourceValue(buyerId, ClayResourceTypeId), Is.EqualTo(20));
            Assert.That(GetResourceValue(sellerId, GoldResourceTypeId), Is.EqualTo(3));
            Assert.That(GetResourceValue(sellerId, ClayResourceTypeId), Is.EqualTo(80));
            Assert.That(GetResourceValue(sellerId, CoinResourceTypeId), Is.EqualTo(sellerCoins - Commission(ClayResourceTypeId, 20)));
            Assert.That(GetMarket(sellerId)!.MyLots, Is.Empty);
            Assert.That(GetMarket(buyerId)!.Lots, Is.Empty);
        }

        /// <summary>
        /// Лот нельзя принять самому продавцу, без Торгового двора, без нужного ресурса или по несуществующему id – во всех случаях обмен не происходит.
        /// </summary>
        [Test]
        public void AcceptLotInvalidCasesDoNotTransferTest()
        {
            var sellerId = GetUnlockedPlayerId();
            var buyerId = GetUnlockedPlayerId();
            var lockedBuyerId = GetPlayerId();
            GrantResource(sellerId, ClayResourceTypeId, 100);
            GrantResource(buyerId, GoldResourceTypeId, 1);
            GrantResource(lockedBuyerId, GoldResourceTypeId, 10);
            var lotId = PostLot(sellerId, ClayResourceTypeId, 20, GoldResourceTypeId, 3);

            Assert.Throws<BusinessException>(() => AcceptLot(sellerId, lotId));
            Assert.Throws<BusinessException>(() => AcceptLot(lockedBuyerId, lotId));
            Assert.Throws<BusinessException>(() => AcceptLot(buyerId, lotId));
            Assert.Throws<BusinessException>(() => AcceptLot(buyerId, 987654));

            Assert.That(GetMarket(sellerId)!.MyLots.Single().Id, Is.EqualTo(lotId));
            Assert.That(GetResourceValue(sellerId, ClayResourceTypeId), Is.EqualTo(80));
            Assert.That(GetResourceValue(buyerId, GoldResourceTypeId), Is.EqualTo(1));
        }

        /// <summary>
        /// Отмена лота возвращает эскроу-ресурс, но не возвращает уплаченную комиссию; отменить лот может только его продавец.
        /// </summary>
        [Test]
        public void CancelLotReturnsEscrowButNotCommissionTest()
        {
            var sellerId = GetUnlockedPlayerId();
            var otherId = GetUnlockedPlayerId();
            GrantResource(sellerId, ClayResourceTypeId, 100);
            var sellerCoins = GetResourceValue(sellerId, CoinResourceTypeId);
            var lotId = PostLot(sellerId, ClayResourceTypeId, 20, GoldResourceTypeId, 3);

            Assert.Throws<BusinessException>(() => CancelLot(otherId, lotId));
            CancelLot(sellerId, lotId);

            Assert.That(GetResourceValue(sellerId, ClayResourceTypeId), Is.EqualTo(100));
            Assert.That(GetResourceValue(sellerId, CoinResourceTypeId), Is.EqualTo(sellerCoins - Commission(ClayResourceTypeId, 20)));
            Assert.That(GetMarket(sellerId)!.MyLots, Is.Empty);
        }

        /// <summary>
        /// Истёкший лот, снятый планировщиком, возвращает эскроу-ресурс, но не возвращает уплаченную комиссию.
        /// </summary>
        [Test]
        public void ExpiredLotReturnsEscrowButNotCommissionTest()
        {
            var sellerId = GetUnlockedPlayerId();
            GrantResource(sellerId, ClayResourceTypeId, 100);
            var sellerCoins = GetResourceValue(sellerId, CoinResourceTypeId);
            var lotId = PostLot(sellerId, ClayResourceTypeId, 20, GoldResourceTypeId, 3);
            var expireDate = DateTimeHelper.GetNowDate().AddSeconds(-1);
            SetTradeLotExpire(lotId, expireDate);

            FinishTradeLot(sellerId, lotId, expireDate);

            Assert.That(GetResourceValue(sellerId, ClayResourceTypeId), Is.EqualTo(100));
            Assert.That(GetResourceValue(sellerId, CoinResourceTypeId), Is.EqualTo(sellerCoins - Commission(ClayResourceTypeId, 20)));
            Assert.That(GetMarket(sellerId)!.MyLots, Is.Empty);
        }

        /// <summary>
        /// Одновременные попытки принять один и тот же лот успешны только у одного из конкурирующих покупателей.
        /// </summary>
        [Test]
        public async Task ConcurrentAcceptOneLotAllowsOneSuccessTest()
        {
            var sellerId = GetUnlockedPlayerId();
            var firstBuyerId = GetUnlockedPlayerId();
            var secondBuyerId = GetUnlockedPlayerId();
            GrantResource(sellerId, ClayResourceTypeId, 100);
            GrantResource(firstBuyerId, GoldResourceTypeId, 10);
            GrantResource(secondBuyerId, GoldResourceTypeId, 10);
            var lotId = PostLot(sellerId, ClayResourceTypeId, 20, GoldResourceTypeId, 3);

            var results = await Task.WhenAll(
                Task.Run(() => TryAcceptLot(firstBuyerId, lotId)),
                Task.Run(() => TryAcceptLot(secondBuyerId, lotId)));

            Assert.That(results.Count(x => x), Is.EqualTo(1));
            Assert.That(GetMarket(sellerId)!.MyLots, Is.Empty);
            Assert.That(GetResourceValue(sellerId, GoldResourceTypeId), Is.EqualTo(3));
            Assert.That(GetResourceValue(firstBuyerId, ClayResourceTypeId) + GetResourceValue(secondBuyerId, ClayResourceTypeId), Is.EqualTo(20));
        }

        /// <summary>
        /// Взаимное одновременное принятие лотов друг друга двумя игроками завершается успешно, без взаимной блокировки.
        /// </summary>
        [Test]
        public async Task CrossAcceptLotsDoesNotDeadlockTest()
        {
            var firstId = GetUnlockedPlayerId();
            var secondId = GetUnlockedPlayerId();
            GrantResource(firstId, ClayResourceTypeId, 100);
            GrantResource(firstId, WoodResourceTypeId, 100);
            GrantResource(secondId, ClayResourceTypeId, 100);
            GrantResource(secondId, WoodResourceTypeId, 100);
            var firstLotId = PostLot(firstId, ClayResourceTypeId, 20, WoodResourceTypeId, 10);
            var secondLotId = PostLot(secondId, WoodResourceTypeId, 10, ClayResourceTypeId, 20);

            var results = await Task.WhenAll(
                Task.Run(() => TryAcceptLot(firstId, secondLotId)),
                Task.Run(() => TryAcceptLot(secondId, firstLotId)));

            Assert.That(results, Is.All.True);
            Assert.That(GetMarket(firstId)!.MyLots, Is.Empty);
            Assert.That(GetMarket(secondId)!.MyLots, Is.Empty);
        }

        private int GetPlayerId()
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var playerId = domikManager.GetPlayerId("testUser_" + Guid.NewGuid());
                uow.Commit();
                return playerId;
            }
        }

        private int GetUnlockedPlayerId()
        {
            var playerId = GetPlayerId();
            GrantDecor(playerId, FountainDecorTypeId, 4);
            GrantResource(playerId, CoinResourceTypeId, 800);
            BuyDomik(playerId, MarketYardDomikTypeId);
            return playerId;
        }

        private MarketState? GetMarket(int playerId)
        {
            using (var uow = GetUow())
            {
                var manager = GetMarketManager(uow);
                var market = manager.GetMarket(playerId);
                uow.Commit();
                return market;
            }
        }

        private int PostLot(int playerId, int giveResourceTypeId, int giveValue, int wantResourceTypeId, int wantValue)
        {
            using (var uow = GetUow())
            {
                var manager = GetMarketManager(uow);
                manager.PostLot(playerId, giveResourceTypeId, giveValue, wantResourceTypeId, wantValue, DateTimeHelper.GetNowDate());
                uow.Commit();
                return uow.Context.TradeLots.OrderByDescending(x => x.Id).First().Id;
            }
        }

        private void AcceptLot(int playerId, int lotId)
        {
            using (var uow = GetUow())
            {
                var manager = GetMarketManager(uow);
                manager.AcceptLot(playerId, lotId, DateTimeHelper.GetNowDate());
                uow.Commit();
            }
        }

        private bool TryAcceptLot(int playerId, int lotId)
        {
            try
            {
                AcceptLot(playerId, lotId);
                return true;
            }
            catch (BusinessException)
            {
                return false;
            }
        }

        private void CancelLot(int playerId, int lotId)
        {
            using (var uow = GetUow())
            {
                var manager = GetMarketManager(uow);
                manager.CancelLot(playerId, lotId, DateTimeHelper.GetNowDate());
                uow.Commit();
            }
        }

        private void FinishTradeLot(int playerId, int lotId, DateTime date)
        {
            using (var uow = GetUow())
            {
                var manager = GetMarketManager(uow);
                var result = manager.FinishTradeLot(date, new CalculateInfo
                {
                    PlayerId = playerId,
                    ObjectId = lotId,
                    Date = date,
                    Type = CalculateTypes.TradeLotExpire,
                });
                Assert.That(result, Is.True);
                uow.Commit();
            }
        }

        private void SetVillage(int playerId, string name, int crestIcon, int crestColor)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                domikManager.SetVillageIdentity(playerId, name, crestIcon, crestColor);
                uow.Commit();
            }
        }

        private void SetTradeLotExpire(int lotId, DateTime expireDate)
        {
            using (var uow = GetUow())
            {
                var lot = uow.Context.TradeLots.Single(x => x.Id == lotId);
                lot.ExpireDate = expireDate;
                uow.Commit();
            }
        }

        private void GrantResource(int playerId, int resourceTypeId, int value)
        {
            using (var uow = GetUow())
            {
                var resource = uow.Context.Resources.SingleOrDefault(x => x.PlayerId == playerId && x.TypeId == resourceTypeId);
                if (resource == null)
                {
                    resource = new Data.Entities.Resource { PlayerId = playerId, TypeId = resourceTypeId };
                    uow.Context.Resources.Add(resource);
                }

                resource.Value += value;
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private void SetResource(int playerId, int resourceTypeId, int value)
        {
            using (var uow = GetUow())
            {
                var resource = uow.Context.Resources.SingleOrDefault(x => x.PlayerId == playerId && x.TypeId == resourceTypeId);
                if (resource == null)
                {
                    resource = new Data.Entities.Resource { PlayerId = playerId, TypeId = resourceTypeId };
                    uow.Context.Resources.Add(resource);
                }

                resource.Value = value;
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private void BuyDomik(int playerId, int typeId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                domikManager.BuyDomik(playerId, typeId);
                uow.Commit();
            }
        }

        private void GrantDecor(int playerId, int decorTypeId, int count)
        {
            using (var uow = GetUow())
            {
                var decor = uow.Context.PlayerDecors.SingleOrDefault(x => x.PlayerId == playerId && x.DecorTypeId == decorTypeId);
                if (decor == null)
                {
                    decor = new Data.Entities.PlayerDecor { PlayerId = playerId, DecorTypeId = decorTypeId };
                    uow.Context.PlayerDecors.Add(decor);
                }

                decor.Count += count;
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private int GetResourceValue(int playerId, int resourceTypeId)
        {
            using (var uow = GetUow())
            {
                return uow.Context.Resources.SingleOrDefault(x => x.PlayerId == playerId && x.TypeId == resourceTypeId)?.Value ?? 0;
            }
        }

        private void ClearTradeLots()
        {
            using (var uow = GetUow())
            {
                uow.Context.TradeLots.RemoveRange(uow.Context.TradeLots);
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }
    }
}
