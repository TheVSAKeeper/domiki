using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    public class CountGateTests : TestBase
    {
        private const int BarakTypeId = 2;
        private const int StoneMineTypeId = 3;
        private const int ClayMineTypeId = 5;
        private const int LumberMillTypeId = 6;
        private const int MarketTypeId = 7;
        private const int CoinResourceTypeId = 1;

        [TestCase(BarakTypeId, 5, true)]
        [TestCase(ClayMineTypeId, 8, true)]
        [TestCase(LumberMillTypeId, 8, false)]
        public void BuyNextGatedInstanceThrowsBelowThresholdTest(int domikTypeId, int gateLevel, bool ownsFirstInstance)
        {
            var playerId = GetPlayerId();
            if (!ownsFirstInstance)
            {
                GrantDomik(playerId, GetNextDomikId(playerId), domikTypeId);
            }
            SetVillageLevel(playerId, gateLevel - 1);

            var name = GetDomikTypes().First(x => x.Id == domikTypeId).Name;
            var ex = Assert.Throws<BusinessException>(() => BuyDomik(playerId, domikTypeId));
            Assert.That(ex.Message, Is.EqualTo($"Постройка «{name}» откроется при обжитости {gateLevel}"));
        }

        [TestCase(BarakTypeId, 5, true)]
        [TestCase(ClayMineTypeId, 8, true)]
        [TestCase(LumberMillTypeId, 8, false)]
        public void BuyNextGatedInstanceSucceedsAtThresholdTest(int domikTypeId, int gateLevel, bool ownsFirstInstance)
        {
            var playerId = GetPlayerId();
            if (!ownsFirstInstance)
            {
                GrantDomik(playerId, GetNextDomikId(playerId), domikTypeId);
            }
            SetVillageLevel(playerId, gateLevel);

            Assert.DoesNotThrow(() => BuyDomik(playerId, domikTypeId));
        }

        [Test]
        public void StoneMineSecondInstanceGateTest()
        {
            var playerId = GetPlayerId();
            GrantResource(playerId, CoinResourceTypeId, 500);
            SetVillageLevel(playerId, 6);
            BuyDomik(playerId, StoneMineTypeId);

            SetVillageLevel(playerId, 11);
            var ex = Assert.Throws<BusinessException>(() => BuyDomik(playerId, StoneMineTypeId));
            Assert.That(ex.Message, Is.EqualTo("Постройка «Каменоломня» откроется при обжитости 12"));

            SetVillageLevel(playerId, 12);
            Assert.DoesNotThrow(() => BuyDomik(playerId, StoneMineTypeId));
        }

        [Test]
        public void GrandfatheredOwnershipIsNotClippedAndAvailableCountNeverNegativeTest()
        {
            var playerId = GetPlayerId();
            var nextId = GetNextDomikId(playerId);
            GrantDomik(playerId, nextId, BarakTypeId);
            GrantDomik(playerId, nextId + 1, BarakTypeId);
            GrantDomik(playerId, nextId + 2, BarakTypeId);

            var ownedCount = GetDomiks(playerId).Count(x => x.Type.Id == BarakTypeId);
            Assert.That(ownedCount, Is.EqualTo(4));

            var available = GetPurchaseAvailableDomiks(playerId);
            var barak = available.First(x => x.Type.Id == BarakTypeId);
            Assert.That(barak.AvailableCount, Is.EqualTo(0));
            Assert.That(barak.NextCountGateLevel, Is.EqualTo(24));

            var ex = Assert.Throws<BusinessException>(() => BuyDomik(playerId, BarakTypeId));
            Assert.That(ex.Message, Is.EqualTo("Постройка «Артельная изба» откроется при обжитости 24"));

            ownedCount = GetDomiks(playerId).Count(x => x.Type.Id == BarakTypeId);
            Assert.That(ownedCount, Is.EqualTo(4));
        }

        [Test]
        public void DomikTypeWithoutGatesIsBoundedOnlyByMaxCountTest()
        {
            var playerId = GetPlayerId();

            var available = GetPurchaseAvailableDomiks(playerId);
            var market = available.First(x => x.Type.Id == MarketTypeId);
            Assert.That(market.AvailableCount, Is.EqualTo(1));
            Assert.That(market.NextCountGateLevel, Is.Null);

            BuyDomik(playerId, MarketTypeId);

            available = GetPurchaseAvailableDomiks(playerId);
            Assert.That(available.Any(x => x.Type.Id == MarketTypeId), Is.False);
        }

        private int GetPlayerId()
        {
            using var uow = GetUow();
            var domikManager = GetDomikManager(uow);
            var playerId = domikManager.GetPlayerId("testUser_" + Guid.NewGuid());
            uow.Commit();
            return playerId;
        }

        private void BuyDomik(int playerId, int domikTypeId)
        {
            using var uow = GetUow();
            var domikManager = GetDomikManager(uow);
            domikManager.BuyDomik(playerId, domikTypeId);
            uow.Commit();
        }

        private IEnumerable<Domik> GetDomiks(int playerId)
        {
            using var uow = GetUow();
            var domikManager = GetDomikManager(uow);
            return domikManager.GetDomiks(playerId);
        }

        private DomikType[] GetDomikTypes()
        {
            using var uow = GetUow();
            return GetResourceManager(uow).GetDomikTypes();
        }

        private (DomikType Type, int AvailableCount, int? NextCountGateLevel)[] GetPurchaseAvailableDomiks(int playerId)
        {
            using var uow = GetUow();
            var domikManager = GetDomikManager(uow);
            return domikManager.GetPurchaseAvailableDomiks(playerId).ToArray();
        }

        private int GetVillageLevel(int playerId)
        {
            using var uow = GetUow();
            return GetVillageLevelCalculator(uow).GetLevel(playerId).Level;
        }

        private int GetNextDomikId(int playerId)
        {
            using var uow = GetUow();
            return (uow.Context.Domiks.Where(x => x.PlayerId == playerId).Max(x => (int?)x.Id) ?? 0) + 1;
        }

        private void GrantResource(int playerId, int typeId, int value)
        {
            using var uow = GetUow();
            var resource = uow.Context.Resources.FirstOrDefault(x => x.PlayerId == playerId && x.TypeId == typeId);
            if (resource == null)
            {
                resource = new Domiki.Web.Data.Resource { PlayerId = playerId, TypeId = typeId };
                uow.Context.Resources.Add(resource);
            }

            resource.Value += value;
            uow.Context.SaveChanges();
            uow.Commit();
        }

        private void SetVillageLevel(int playerId, int target)
        {
            while (GetVillageLevel(playerId) < target)
            {
                GrantDomik(playerId, GetNextDomikId(playerId), MarketTypeId);
            }
        }
    }
}
