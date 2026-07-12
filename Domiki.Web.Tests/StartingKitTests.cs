using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    public class StartingKitTests : TestBase
    {
        private const int BarracksTypeId = 2;
        private const int ClayMineTypeId = 5;
        private const int CoinResourceTypeId = 1;
        private const int ClayDigReceiptId = 1;

        [Test]
        public void NewPlayerStartsWithBarrackAndClayMineAndCoinsTest()
        {
            var playerId = GetPlayerId();

            var domiks = GetDomiks(playerId).OrderBy(x => x.Id).ToArray();
            Assert.That(domiks.Length, Is.EqualTo(2));
            Assert.That(domiks[0].Type.Id, Is.EqualTo(BarracksTypeId));
            Assert.That(domiks[0].Level, Is.EqualTo(1));
            Assert.That(domiks[1].Type.Id, Is.EqualTo(ClayMineTypeId));
            Assert.That(domiks[1].Level, Is.EqualTo(1));

            var resources = GetResources(playerId);
            Assert.That(resources.Single(x => x.Type.Id == CoinResourceTypeId).Value, Is.EqualTo(DomikManager.StartingCoins));
        }

        [Test]
        public void NewPlayerStartingVillageLevelIsFourTest()
        {
            var playerId = GetPlayerId();

            var level = GetVillageLevel(playerId);
            Assert.That(level.Buildings, Is.EqualTo(2));
            Assert.That(level.Residents, Is.EqualTo(1));
            Assert.That(level.Level, Is.EqualTo(4));
        }

        [Test]
        public void GetDomiksInSameScopeAsGetPlayerIdSeesStartingKitTest()
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var playerId = domikManager.GetPlayerId("testUser_" + Guid.NewGuid());

                var domiks = domikManager.GetDomiks(playerId);

                Assert.That(domiks.Count(), Is.EqualTo(2));
                uow.Commit();
            }
        }

        [Test]
        public void NewPlayerWithZeroCoinsCanStartClayDigWithoutBuyingAnythingTest()
        {
            var playerId = GetPlayerId();
            var coins = GetResources(playerId).First(x => x.Type.Id == CoinResourceTypeId).Value;
            GrantResource(playerId, CoinResourceTypeId, -coins);
            Assert.That(GetResources(playerId).First(x => x.Type.Id == CoinResourceTypeId).Value, Is.EqualTo(0));

            Assert.DoesNotThrow(() => StartManufacture(playerId, 2, ClayDigReceiptId));
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

        private IEnumerable<Domik> GetDomiks(int playerId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                return domikManager.GetDomiks(playerId);
            }
        }

        private IEnumerable<Resource> GetResources(int playerId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var resources = domikManager.GetResources(playerId);
                uow.Commit();
                return resources;
            }
        }

        private VillageLevel GetVillageLevel(int playerId)
        {
            using (var uow = GetUow())
            {
                var calculator = GetVillageLevelCalculator(uow);
                var level = calculator.GetLevel(playerId);
                uow.Commit();
                return level;
            }
        }

        private void GrantResource(int playerId, int typeId, int value)
        {
            using (var uow = GetUow())
            {
                var resource = uow.Context.Resources.First(x => x.PlayerId == playerId && x.TypeId == typeId);
                resource.Value += value;
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private void StartManufacture(int playerId, int domikId, int receiptId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                domikManager.StartManufacture(playerId, domikId, receiptId);
                uow.Commit();
            }
        }
    }
}
