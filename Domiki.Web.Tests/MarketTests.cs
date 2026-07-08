using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

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

        [Test]
        public void GetMarketForNewPlayerReturnsEmptyAndLockedTest()
        {
            var playerId = GetPlayerId();

            var market = GetMarket(playerId);

            Assert.That(market.Lots, Is.Empty);
            Assert.That(market.MyLots, Is.Empty);
            Assert.That(market.CanTrade, Is.False);
            Assert.That(market.UnlockLevel, Is.EqualTo(MarketManager.MarketUnlockLevel));
            Assert.That(market.Commission, Is.EqualTo(MarketManager.MarketCommissionCoins));
        }

        [Test]
        public void PostLotBelowUnlockThrowsAndDoesNotWriteOffTest()
        {
            var playerId = GetPlayerId();
            GrantResource(playerId, ClayResourceTypeId, 100);
            var before = GetResourceValue(playerId, ClayResourceTypeId);

            var ex = Assert.Throws<BusinessException>(() => PostLot(playerId, ClayResourceTypeId, 20, GoldResourceTypeId, 3));

            Assert.That(ex.Message, Is.EqualTo($"Ярмарка откроется при обжитости {MarketManager.MarketUnlockLevel}"));
            Assert.That(GetMarket(playerId).MyLots, Is.Empty);
            Assert.That(GetResourceValue(playerId, ClayResourceTypeId), Is.EqualTo(before));
        }

        [Test]
        public void PostLotWritesOffEscrowAndCommissionAndVisibleToOtherPlayerTest()
        {
            var sellerId = GetUnlockedPlayerId();
            var buyerId = GetUnlockedPlayerId();
            var villageName = "Слоб" + Guid.NewGuid().ToString("N")[..8];
            SetVillage(sellerId, villageName, 2, 3);
            GrantResource(sellerId, ClayResourceTypeId, 100);
            var coinBefore = GetResourceValue(sellerId, CoinResourceTypeId);

            var lotId = PostLot(sellerId, ClayResourceTypeId, 20, GoldResourceTypeId, 3);

            Assert.That(GetResourceValue(sellerId, ClayResourceTypeId), Is.EqualTo(80));
            Assert.That(GetResourceValue(sellerId, CoinResourceTypeId), Is.EqualTo(coinBefore - MarketManager.MarketCommissionCoins));
            var sellerMarket = GetMarket(sellerId);
            Assert.That(sellerMarket.MyLots.Single().Id, Is.EqualTo(lotId));
            var buyerLot = GetMarket(buyerId).Lots.Single();
            Assert.That(buyerLot.Id, Is.EqualTo(lotId));
            Assert.That(buyerLot.SellerVillageName, Is.EqualTo(villageName));
            Assert.That(buyerLot.SellerCrestIcon, Is.EqualTo(2));
            Assert.That(buyerLot.SellerCrestColor, Is.EqualTo(3));
        }

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

            Assert.That(GetMarket(playerId).MyLots, Is.Empty);
            Assert.That(GetResourceValue(playerId, ClayResourceTypeId), Is.EqualTo(10));
        }

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
            Assert.That(GetResourceValue(sellerId, CoinResourceTypeId), Is.EqualTo(sellerCoins - MarketManager.MarketCommissionCoins));
            Assert.That(GetMarket(sellerId).MyLots, Is.Empty);
            Assert.That(GetMarket(buyerId).Lots, Is.Empty);
        }

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

            Assert.That(GetMarket(sellerId).MyLots.Single().Id, Is.EqualTo(lotId));
            Assert.That(GetResourceValue(sellerId, ClayResourceTypeId), Is.EqualTo(80));
            Assert.That(GetResourceValue(buyerId, GoldResourceTypeId), Is.EqualTo(1));
        }

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
            Assert.That(GetResourceValue(sellerId, CoinResourceTypeId), Is.EqualTo(sellerCoins - MarketManager.MarketCommissionCoins));
            Assert.That(GetMarket(sellerId).MyLots, Is.Empty);
        }

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
            Assert.That(GetResourceValue(sellerId, CoinResourceTypeId), Is.EqualTo(sellerCoins - MarketManager.MarketCommissionCoins));
            Assert.That(GetMarket(sellerId).MyLots, Is.Empty);
        }

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
            Assert.That(GetMarket(sellerId).MyLots, Is.Empty);
            Assert.That(GetResourceValue(sellerId, GoldResourceTypeId), Is.EqualTo(3));
            Assert.That(GetResourceValue(firstBuyerId, ClayResourceTypeId) + GetResourceValue(secondBuyerId, ClayResourceTypeId), Is.EqualTo(20));
        }

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
            Assert.That(GetMarket(firstId).MyLots, Is.Empty);
            Assert.That(GetMarket(secondId).MyLots, Is.Empty);
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
            GrantDecor(playerId, FountainDecorTypeId, 3);
            return playerId;
        }

        private MarketState GetMarket(int playerId)
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
                    resource = new Domiki.Web.Data.Resource { PlayerId = playerId, TypeId = resourceTypeId };
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
                    resource = new Domiki.Web.Data.Resource { PlayerId = playerId, TypeId = resourceTypeId };
                    uow.Context.Resources.Add(resource);
                }

                resource.Value = value;
                uow.Context.SaveChanges();
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
                    decor = new Domiki.Web.Data.PlayerDecor { PlayerId = playerId, DecorTypeId = decorTypeId };
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
