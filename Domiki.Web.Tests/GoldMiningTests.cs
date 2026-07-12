using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    public class GoldMiningTests : TestBase
    {
        private const int BarracksDomikTypeId = 2;
        private const int GoldMineDomikTypeId = 4;
        private const int GoldMineDomikId = 4;
        private const int GoldDigReceiptId = 3;
        private const int GoldResourceTypeId = 5;

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(5)]
        public void GoldMineGrantsAtMostMineLevelPerDayTest(int mineLevel)
        {
            var playerId = CreatePlayerWithGoldMine(mineLevel);
            var beforeGold = ResourceValue(GetResources(playerId), GoldResourceTypeId);

            for (var i = 0; i < mineLevel + 2; i++)
            {
                MineGold(playerId);
            }

            var afterGold = ResourceValue(GetResources(playerId), GoldResourceTypeId);
            Assert.That(afterGold - beforeGold, Is.EqualTo(mineLevel));
            Assert.That(GetGoldMinedToday(playerId), Is.EqualTo(mineLevel));
        }

        [Test]
        public void GoldMineResetsCapOnNextDayTest()
        {
            var playerId = CreatePlayerWithGoldMine(1);

            MineGold(playerId);
            SetGoldMinedDate(playerId, DateTimeHelper.GetNowDate().AddDays(-1).Date);
            MineGold(playerId);

            Assert.That(ResourceValue(GetResources(playerId), GoldResourceTypeId), Is.EqualTo(2));
            Assert.That(GetGoldMinedToday(playerId), Is.EqualTo(1));
        }

        [Test]
        public void OrderGoldIsNotLimitedByGoldMineCapTest()
        {
            var playerId = CreatePlayerWithGoldMine(1);
            MineGold(playerId);
            var orderId = CreateManualOrder(playerId, 1, 1, 1, 0, 7, 1);
            var beforeGold = ResourceValue(GetResources(playerId), GoldResourceTypeId);

            CompleteOrder(playerId, orderId);

            Assert.That(ResourceValue(GetResources(playerId), GoldResourceTypeId) - beforeGold, Is.EqualTo(7));
            Assert.That(GetGoldMinedToday(playerId), Is.EqualTo(1));
        }

        private int CreatePlayerWithGoldMine(int mineLevel)
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, BarracksDomikTypeId);
            AddGoldMine(playerId, mineLevel);
            return playerId;
        }

        private int GetPlayerId()
        {
            using (var uow = GetUow())
            {
                var playerId = GetDomikManager(uow).GetPlayerId("testUser_" + Guid.NewGuid());
                uow.Commit();
                return playerId;
            }
        }

        private void BuyDomik(int playerId, int domikTypeId)
        {
            using (var uow = GetUow())
            {
                GetDomikManager(uow).BuyDomik(playerId, domikTypeId);
                uow.Commit();
            }
        }

        private void AddGoldMine(int playerId, int mineLevel)
        {
            using (var uow = GetUow())
            {
                uow.Context.Domiks.Add(new Domiki.Web.Data.Domik
                {
                    PlayerId = playerId,
                    Id = GoldMineDomikId,
                    TypeId = GoldMineDomikTypeId,
                    Level = mineLevel,
                });
                uow.Commit();
            }
        }

        private void MineGold(int playerId)
        {
            using (var uow = GetUow())
            {
                GetDomikManager(uow).StartManufacture(playerId, GoldMineDomikId, GoldDigReceiptId);
                uow.Commit();
            }
        }

        private Resource[] GetResources(int playerId)
        {
            using (var uow = GetUow())
            {
                var resources = GetDomikManager(uow).GetResources(playerId).ToArray();
                uow.Commit();
                return resources;
            }
        }

        private int GetGoldMinedToday(int playerId)
        {
            using (var uow = GetUow())
            {
                var value = uow.Context.Players.Single(x => x.Id == playerId).GoldMinedToday;
                uow.Commit();
                return value;
            }
        }

        private void SetGoldMinedDate(int playerId, DateTime value)
        {
            using (var uow = GetUow())
            {
                var player = uow.Context.Players.Single(x => x.Id == playerId);
                player.GoldMinedDate = value;
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private int CreateManualOrder(int playerId, int neighborId, int resourceTypeId, int value, int rewardCoins, int rewardGold, int rewardReputation)
        {
            using (var uow = GetUow())
            {
                var now = DateTimeHelper.GetNowDate();
                var order = new Domiki.Web.Data.Order
                {
                    PlayerId = playerId,
                    NeighborId = neighborId,
                    CreateDate = now,
                    ExpireDate = now.AddHours(4),
                    RewardCoins = rewardCoins,
                    RewardGold = rewardGold,
                    RewardReputation = rewardReputation,
                };
                uow.Context.Orders.Add(order);
                uow.Context.SaveChanges();
                uow.Context.OrderResources.Add(new Domiki.Web.Data.OrderResource
                {
                    OrderId = order.Id,
                    ResourceTypeId = resourceTypeId,
                    Value = value,
                });
                uow.Commit();
                return order.Id;
            }
        }

        private void CompleteOrder(int playerId, int orderId)
        {
            using (var uow = GetUow())
            {
                GetOrderManager(uow).CompleteOrder(playerId, orderId);
                uow.Commit();
            }
        }

        private int ResourceValue(Resource[] resources, int resourceTypeId)
        {
            return resources.FirstOrDefault(x => x.Type.Id == resourceTypeId)?.Value ?? 0;
        }
    }
}
