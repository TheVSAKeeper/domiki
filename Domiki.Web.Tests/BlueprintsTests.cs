using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    public class BlueprintsTests : TestBase
    {
        private const int BlueprintId = 1;
        private const int BorovoeId = 2;
        private const int WorkshopDomikTypeId = 8;
        private const int BarracksDomikTypeId = 2;
        private const int MarketDomikTypeId = 7;
        private const int CoinResourceTypeId = 1;
        private const int BoardResourceTypeId = 7;
        private const int FurnitureResourceTypeId = 9;
        private const int MakeFurnitureReceiptId = 29;
        private const int SellFurnitureReceiptId = 31;

        [Test]
        public void GetBlueprintsNewPlayerReturnsWorkshopBlueprintLockedTest()
        {
            var playerId = GetPlayerId();

            var blueprint = GetBlueprints(playerId).Single(x => x.Blueprint.Id == BlueprintId);

            Assert.That(blueprint.Blueprint.DomikTypeId, Is.EqualTo(WorkshopDomikTypeId));
            Assert.That(blueprint.Neighbor.Id, Is.EqualTo(BorovoeId));
            Assert.That(blueprint.CurrentReputation, Is.EqualTo(0));
            Assert.That(blueprint.Owned, Is.False);
        }

        [TestCase(29, false)]
        [TestCase(30, true)]
        [TestCase(31, true)]
        public void ReputationThresholdGrantsBlueprintOnceTest(int reputation, bool expectedOwned)
        {
            var playerId = GetPlayerId();
            GrantReputation(playerId, BorovoeId, reputation);

            var first = GetBlueprints(playerId).Single(x => x.Blueprint.Id == BlueprintId);
            var second = GetBlueprints(playerId).Single(x => x.Blueprint.Id == BlueprintId);

            Assert.That(first.Owned, Is.EqualTo(expectedOwned));
            Assert.That(second.Owned, Is.EqualTo(expectedOwned));
            Assert.That(PlayerBlueprintCount(playerId), Is.EqualTo(expectedOwned ? 1 : 0));
        }

        [Test]
        public void BuyWorkshopWithoutBlueprintThrowsAndKeepsResourcesTest()
        {
            var playerId = GetPlayerId();
            GrantReputation(playerId, BorovoeId, 29);
            var before = GetResources(playerId);

            var ex = Assert.Throws<BusinessException>(() => BuyDomik(playerId, WorkshopDomikTypeId));

            Assert.That(ex.Message, Is.EqualTo("Нужен чертёж (репутация Боровое 30)"));
            Assert.That(GetResources(playerId).Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo(before.Select(x => (x.Type.Id, x.Value))));
            Assert.That(GetDomikCount(playerId, WorkshopDomikTypeId), Is.EqualTo(0));
        }

        [Test]
        public void BuyWorkshopWithBlueprintSucceedsTest()
        {
            var playerId = GetPlayerId();
            GrantReputation(playerId, BorovoeId, 30);

            BuyDomik(playerId, WorkshopDomikTypeId);

            Assert.That(GetDomikCount(playerId, WorkshopDomikTypeId), Is.EqualTo(1));
            Assert.That(PlayerBlueprintCount(playerId), Is.EqualTo(1));
        }

        [Test]
        public void BuyWorkshopLazilyGrantsBlueprintTest()
        {
            var playerId = GetPlayerId();
            GrantReputation(playerId, BorovoeId, 30);
            Assert.That(PlayerBlueprintCount(playerId), Is.EqualTo(0));

            BuyDomik(playerId, WorkshopDomikTypeId);

            Assert.That(PlayerBlueprintCount(playerId), Is.EqualTo(1));
            Assert.That(GetDomikCount(playerId, WorkshopDomikTypeId), Is.EqualTo(1));
        }

        [Test]
        public void FurnitureRecipeAndSaleTest()
        {
            var playerId = GetPlayerId();
            GrantReputation(playerId, BorovoeId, 30);
            GrantResource(playerId, BoardResourceTypeId, 2);
            BuyDomik(playerId, BarracksDomikTypeId);
            BuyDomik(playerId, MarketDomikTypeId);
            BuyDomik(playerId, WorkshopDomikTypeId);
            var workshop = GetDomiks(playerId).Single(x => x.Type.Id == WorkshopDomikTypeId);

            StartManufacture(playerId, workshop.Id, MakeFurnitureReceiptId);
            var afterMake = GetResources(playerId);
            Assert.That(ResourceValue(afterMake, BoardResourceTypeId), Is.EqualTo(0));
            Assert.That(ResourceValue(afterMake, FurnitureResourceTypeId), Is.EqualTo(1));

            var market = GetDomiks(playerId).First(x => x.Type.Id == MarketDomikTypeId && x.Level > 0);
            StartManufacture(playerId, market.Id, SellFurnitureReceiptId);
            var afterSell = GetResources(playerId);
            Assert.That(ResourceValue(afterSell, FurnitureResourceTypeId), Is.EqualTo(0));
            Assert.That(ResourceValue(afterSell, CoinResourceTypeId) - ResourceValue(afterMake, CoinResourceTypeId), Is.EqualTo(70));
        }

        private int GetPlayerId()
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var playerId = domikManager.GetPlayerId("testUser_" + Guid.NewGuid());
                new PlayerResourceManager(uow.Context, GetResourceManager(uow)).GrantResource(playerId, CoinResourceTypeId, 1000);
                uow.Commit();
                return playerId;
            }
        }

        private PlayerBlueprint[] GetBlueprints(int playerId)
        {
            using (var uow = GetUow())
            {
                var manager = GetBlueprintManager(uow);
                var blueprints = manager.GetBlueprints(playerId).ToArray();
                uow.Commit();
                return blueprints;
            }
        }

        private void BuyDomik(int playerId, int typeId)
        {
            using (var uow = GetUow())
            {
                var manager = GetDomikManager(uow);
                manager.BuyDomik(playerId, typeId);
                uow.Commit();
            }
        }

        private void StartManufacture(int playerId, int domikId, int receiptId)
        {
            using (var uow = GetUow())
            {
                var manager = GetDomikManager(uow);
                manager.StartManufacture(playerId, domikId, receiptId);
                uow.Commit();
            }
        }

        private Domik[] GetDomiks(int playerId)
        {
            using (var uow = GetUow())
            {
                var manager = GetDomikManager(uow);
                var domiks = manager.GetDomiks(playerId).ToArray();
                uow.Commit();
                return domiks;
            }
        }

        private Resource[] GetResources(int playerId)
        {
            using (var uow = GetUow())
            {
                var manager = GetDomikManager(uow);
                var resources = manager.GetResources(playerId).ToArray();
                uow.Commit();
                return resources;
            }
        }

        private void GrantReputation(int playerId, int neighborId, int points)
        {
            using (var uow = GetUow())
            {
                var resourceManager = GetResourceManager(uow);
                var playerResourceManager = new PlayerResourceManager(uow.Context, resourceManager);
                playerResourceManager.GrantReputation(playerId, neighborId, points);
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private void GrantResource(int playerId, int typeId, int value)
        {
            using (var uow = GetUow())
            {
                var resourceManager = GetResourceManager(uow);
                var playerResourceManager = new PlayerResourceManager(uow.Context, resourceManager);
                playerResourceManager.GrantResource(playerId, typeId, value);
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private int PlayerBlueprintCount(int playerId)
        {
            using (var uow = GetUow())
            {
                var count = uow.Context.PlayerBlueprints.Count(x => x.PlayerId == playerId && x.BlueprintId == BlueprintId);
                uow.Commit();
                return count;
            }
        }

        private int GetDomikCount(int playerId, int typeId)
        {
            return GetDomiks(playerId).Count(x => x.Type.Id == typeId);
        }

        private int ResourceValue(Resource[] resources, int typeId)
        {
            return resources.FirstOrDefault(x => x.Type.Id == typeId)?.Value ?? 0;
        }
    }
}
