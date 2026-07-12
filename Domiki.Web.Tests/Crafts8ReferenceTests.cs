using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    public class Crafts8ReferenceTests : TestBase
    {
        [Test]
        public void BreadReceiptResourcesTest()
        {
            using var uow = GetUow();
            var receipt = GetResourceManager(uow).GetReceipts().Single(x => x.Id == 55);

            Assert.That(receipt.LogicName, Is.EqualTo("make_bread"));
            Assert.That(receipt.InputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo(new[] { (14, 2) }));
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo(new[] { (15, 4) }));
        }

        [Test]
        public void GrainReceiptHasNoCoinInputTest()
        {
            using var uow = GetUow();
            var receipt = GetResourceManager(uow).GetReceipts().Single(x => x.Id == 50);

            Assert.That(receipt.LogicName, Is.EqualTo("grain_dig"));
            Assert.That(receipt.InputResources.Any(x => x.Type.Id == 1), Is.False);
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo(new[] { (13, 1) }));
        }

        [TestCase(15, 1, 1, 150)]
        [TestCase(15, 1, 11, 1)]
        [TestCase(15, 3, 11, 1)]
        [TestCase(15, 5, 11, 2)]
        [TestCase(16, 1, 5, 10)]
        [TestCase(16, 4, 12, 10)]
        public void FoodBuildingUpgradeCostsTest(int domikTypeId, int level, int resourceTypeId, int expected)
        {
            using var uow = GetUow();
            var domikType = GetResourceManager(uow).GetDomikTypes().Single(x => x.Id == domikTypeId);
            var resources = domikType.Levels.Single(x => x.Value == level).Resources;

            Assert.That(resources.Single(x => x.Type.Id == resourceTypeId).Value, Is.EqualTo(expected));
        }

        [TestCase(13, 10)]
        [TestCase(14, 35)]
        [TestCase(15, 20)]
        public void MarketValueTest(int resourceTypeId, int expected)
        {
            Assert.That(ResourceManager.GetMarketValue(resourceTypeId), Is.EqualTo(expected));
        }

        [Test]
        public void DubravaFoodProfileAndBakeryBlueprintTest()
        {
            using var uow = GetUow();
            var resourceManager = GetResourceManager(uow);
            var dubrava = resourceManager.GetNeighbors().Single(x => x.Id == 5);
            var blueprint = resourceManager.GetBlueprints().Single(x => x.Id == 4);

            Assert.That(dubrava.LogicName, Is.EqualTo("dubrava"));
            Assert.That(dubrava.SecondaryResourceTypeId, Is.EqualTo(15));
            Assert.That(blueprint.DomikTypeId, Is.EqualTo(16));
            Assert.That(blueprint.ReputationThreshold, Is.EqualTo(25));
            Assert.That(blueprint.NeighborId, Is.EqualTo(dubrava.Id));
        }
    }
}
