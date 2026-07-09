using Domiki.Web.Business;
using Domiki.Web.Data;

namespace Domiki.Web.Tests
{
    [NonParallelizable]
    public class OfflineRecapTests : TestBase
    {
        [Test]
        public void FinishedManufactureIsDeliveredOnceTest()
        {
            var playerId = CreatePlayer();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 5);

            using (var uow = GetUow())
            {
                GetDomikManager(uow).StartManufacture(playerId, 2, 1);
                uow.Commit();
            }

            using (var uow = GetUow())
            {
                var recap = GetPlayerEventManager(uow).TakeRecap(playerId, DateTimeHelper.GetNowDate());
                Assert.That(recap.Events.Select(x => x.Type), Does.Contain(PlayerEventType.ManufactureFinished));
                uow.Commit();
            }

            using (var uow = GetUow())
            {
                var recap = GetPlayerEventManager(uow).TakeRecap(playerId, DateTimeHelper.GetNowDate());
                Assert.That(recap.Events, Is.Empty);
                uow.Commit();
            }
        }

        [Test]
        public void AcceptedLotIsDeliveredToSellerTest()
        {
            var sellerId = CreateUnlockedMarketPlayer();
            var buyerId = CreateUnlockedMarketPlayer();
            GrantResource(sellerId, 4, 20);
            GrantResource(buyerId, 5, 3);
            var lotId = PostLot(sellerId, 4, 20, 5, 3);

            using (var uow = GetUow())
            {
                GetMarketManager(uow, false).AcceptLot(buyerId, lotId, DateTimeHelper.GetNowDate());
                uow.Commit();
            }

            using (var uow = GetUow())
            {
                var recap = GetPlayerEventManager(uow).TakeRecap(sellerId, DateTimeHelper.GetNowDate());
                Assert.That(recap.Events.Select(x => x.Type), Does.Contain(PlayerEventType.LotSold));
                uow.Commit();
            }
        }

        private int CreatePlayer()
        {
            using (var uow = GetUow())
            {
                var playerId = GetDomikManager(uow).GetPlayerId("testUser_" + Guid.NewGuid());
                uow.Commit();
                return playerId;
            }
        }

        private int CreateUnlockedMarketPlayer()
        {
            var playerId = CreatePlayer();
            using (var uow = GetUow())
            {
                uow.Context.Domiks.Add(new Domik
                {
                    PlayerId = playerId,
                    Id = -9,
                    TypeId = 9,
                    Level = 1,
                });
                uow.Commit();
            }
            return playerId;
        }

        private void BuyDomik(int playerId, int domikTypeId)
        {
            using (var uow = GetUow())
            {
                GetDomikManager(uow).BuyDomik(playerId, domikTypeId);
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
                    resource = new Resource { PlayerId = playerId, TypeId = resourceTypeId };
                    uow.Context.Resources.Add(resource);
                }
                resource.Value += value;
                uow.Commit();
            }
        }

        private int PostLot(int playerId, int giveResourceTypeId, int giveValue, int wantResourceTypeId, int wantValue)
        {
            using (var uow = GetUow())
            {
                GetMarketManager(uow, false).PostLot(playerId, giveResourceTypeId, giveValue, wantResourceTypeId, wantValue, DateTimeHelper.GetNowDate());
                uow.Commit();
                return uow.Context.TradeLots.OrderByDescending(x => x.Id).First().Id;
            }
        }
    }
}
