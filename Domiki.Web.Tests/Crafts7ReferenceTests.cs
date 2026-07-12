using Domiki.Web.Business.Core;

namespace Domiki.Web.Tests
{
    public class Crafts7ReferenceTests : TestBase
    {
        private const int ForgeTypeId = 1;
        private const int MarketTypeId = 7;
        private const int StonecutterTypeId = 12;
        private const int PotteryTypeId = 13;
        private const int MakeBrickReceiptId = 22;
        private const int MakeBrick8hReceiptId = 27;
        private const int MakeToolReceiptId = 24;
        private const int MakeTool8hReceiptId = 49;

        [TestCase(PotteryTypeId, 1, MakeBrickReceiptId, true)]
        [TestCase(PotteryTypeId, 2, MakeBrick8hReceiptId, true)]
        [TestCase(PotteryTypeId, 1, MakeBrick8hReceiptId, false)]
        [TestCase(ForgeTypeId, 1, MakeBrickReceiptId, false)]
        [TestCase(ForgeTypeId, 5, MakeBrick8hReceiptId, false)]
        [TestCase(ForgeTypeId, 1, MakeToolReceiptId, true)]
        [TestCase(ForgeTypeId, 3, MakeTool8hReceiptId, true)]
        [TestCase(ForgeTypeId, 2, MakeTool8hReceiptId, false)]
        [TestCase(StonecutterTypeId, 1, 40, true)]
        [TestCase(StonecutterTypeId, 1, 41, false)]
        [TestCase(StonecutterTypeId, 2, 41, true)]
        [TestCase(StonecutterTypeId, 3, 42, true)]
        [TestCase(MarketTypeId, 1, 44, true)]
        [TestCase(MarketTypeId, 1, 45, true)]
        [TestCase(MarketTypeId, 4, 47, false)]
        [TestCase(MarketTypeId, 5, 47, true)]
        [TestCase(MarketTypeId, 5, 48, true)]
        public void ReceiptBindingTest(int domikTypeId, int level, int receiptId, bool expected)
        {
            using var uow = GetUow();
            var resourceManager = GetResourceManager(uow);
            var domikType = resourceManager.GetDomikTypes().First(x => x.Id == domikTypeId);
            var receiptIds = domikType.Levels.First(x => x.Value == level).Receipts.Select(x => x.Id);

            Assert.That(receiptIds.Contains(receiptId), Is.EqualTo(expected));
        }

        [Test]
        public void BlueprintLadderTest()
        {
            using var uow = GetUow();
            var blueprints = GetResourceManager(uow).GetBlueprints();

            Assert.That(blueprints.Single(x => x.LogicName == "pottery").ReputationThreshold, Is.EqualTo(15));
            Assert.That(blueprints.Single(x => x.LogicName == "stonecutter").ReputationThreshold, Is.EqualTo(20));
            Assert.That(blueprints.Single(x => x.LogicName == "workshop").ReputationThreshold, Is.EqualTo(30));
        }

        [Test]
        public void SecondaryProfilesTest()
        {
            using var uow = GetUow();
            var neighbors = GetResourceManager(uow).GetNeighbors();

            Assert.That(neighbors.Single(x => x.LogicName == "glinischi").SecondaryResourceTypeId, Is.EqualTo(12));
            Assert.That(neighbors.Single(x => x.LogicName == "kamenka").SecondaryResourceTypeId, Is.EqualTo(10));
            Assert.That(neighbors.Single(x => x.LogicName == "zarechye").SecondaryResourceTypeId, Is.EqualTo(2));
            Assert.That(neighbors.Single(x => x.LogicName == "borovoe").SecondaryResourceTypeId, Is.EqualTo(9));
            Assert.That(neighbors.Single(x => x.LogicName == "dubrava").SecondaryResourceTypeId, Is.EqualTo(15));
        }

        [TestCase(10, 35)]
        [TestCase(11, 150)]
        [TestCase(12, 45)]
        public void MarketValueTest(int resourceTypeId, int expected)
        {
            Assert.That(ResourceManager.GetMarketValue(resourceTypeId), Is.EqualTo(expected));
        }
    }
}
