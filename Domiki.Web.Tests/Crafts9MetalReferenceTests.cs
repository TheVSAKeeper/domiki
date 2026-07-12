using Domiki.Web.Business.Core;

namespace Domiki.Web.Tests
{
    public class Crafts9MetalReferenceTests : TestBase
    {
        private const int ForgeTypeId = 1;
        private const int MineTypeId = 4;
        private const int MarketTypeId = 7;
        private const int CoinResourceTypeId = 1;
        private const int OreResourceTypeId = 16;
        private const int IronResourceTypeId = 17;
        private const int ToolResourceTypeId = 8;
        private const int BoardResourceTypeId = 7;
        private const int BrickResourceTypeId = 6;
        private const int LanternDecorTypeId = 9;

        [TestCase(OreResourceTypeId, "ore", 10)]
        [TestCase(IronResourceTypeId, "iron", 35)]
        public void MetalResourceTypesAndMarketValuesTest(int resourceTypeId, string logicName, int marketValue)
        {
            using var uow = GetUow();
            var resourceType = GetResourceManager(uow).GetResourceTypes().Single(x => x.Id == resourceTypeId);

            Assert.That(resourceType.LogicName, Is.EqualTo(logicName));
            Assert.That(ResourceManager.GetMarketValue(resourceTypeId), Is.EqualTo(marketValue));
        }

        [TestCase(24, 1)]
        [TestCase(49, 8)]
        public void ToolReceiptsRequireIronAndBoardsTest(int receiptId, int value)
        {
            using var uow = GetUow();
            var receipt = GetResourceManager(uow).GetReceipts().Single(x => x.Id == receiptId);

            Assert.That(receipt.InputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo(new[] { (IronResourceTypeId, value), (BoardResourceTypeId, value) }));
            Assert.That(receipt.InputResources.Any(x => x.Type.Id == BrickResourceTypeId), Is.False);
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo(new[] { (ToolResourceTypeId, value) }));
        }

        [TestCase(1, 62, true)]
        [TestCase(2, 62, true)]
        [TestCase(3, 63, true)]
        [TestCase(2, 63, false)]
        public void IronReceiptsHaveExpectedForgeBindingsTest(int level, int receiptId, bool expected)
        {
            AssertBinding(ForgeTypeId, level, receiptId, expected);
        }

        [TestCase(1, 59)]
        [TestCase(5, 59)]
        [TestCase(1, 60)]
        [TestCase(5, 60)]
        [TestCase(1, 61)]
        [TestCase(5, 61)]
        public void OreReceiptsHaveExpectedMineBindingsTest(int level, int receiptId)
        {
            AssertBinding(MineTypeId, level, receiptId, true);
        }

        [TestCase(60, 8)]
        [TestCase(61, 24)]
        public void OreShiftsHaveOptionalToolTest(int receiptId, int outputValue)
        {
            using var uow = GetUow();
            var receipt = GetResourceManager(uow).GetReceipts().Single(x => x.Id == receiptId);

            Assert.That(receipt.OptionalInputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo(new[] { (ToolResourceTypeId, 1) }));
            Assert.That(receipt.InputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo(new[] { (CoinResourceTypeId, outputValue) }));
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo(new[] { (OreResourceTypeId, outputValue) }));
            Assert.That(receipt.OutputBonusPercent, Is.EqualTo(40));
        }

        [TestCase(5, 67, true)]
        [TestCase(4, 67, false)]
        public void OreBulkSellIsBoundToMarketLevelFiveTest(int level, int receiptId, bool expected)
        {
            AssertBinding(MarketTypeId, level, receiptId, expected);
        }

        [TestCase(62, OreResourceTypeId, 2, IronResourceTypeId, 1)]
        [TestCase(63, OreResourceTypeId, 16, IronResourceTypeId, 8)]
        [TestCase(64, OreResourceTypeId, 1, CoinResourceTypeId, 10)]
        [TestCase(65, IronResourceTypeId, 1, CoinResourceTypeId, 35)]
        [TestCase(66, IronResourceTypeId, 10, CoinResourceTypeId, 350)]
        [TestCase(67, OreResourceTypeId, 10, CoinResourceTypeId, 100)]
        public void MetalReceiptsHaveExpectedInputsAndOutputsTest(int receiptId, int inputResourceTypeId, int inputValue, int outputResourceTypeId, int outputValue)
        {
            using var uow = GetUow();
            var receipt = GetResourceManager(uow).GetReceipts().Single(x => x.Id == receiptId);

            Assert.That(receipt.InputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo(new[] { (inputResourceTypeId, inputValue) }));
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo(new[] { (outputResourceTypeId, outputValue) }));
        }

        [Test]
        public void OreDigHasNoInputsTest()
        {
            using var uow = GetUow();
            var receipt = GetResourceManager(uow).GetReceipts().Single(x => x.Id == 59);

            Assert.That(receipt.InputResources, Is.Empty);
            Assert.That(receipt.OptionalInputResources, Is.Empty);
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo(new[] { (OreResourceTypeId, 1) }));
        }

        [Test]
        public void MineIsRenamedTest()
        {
            using var uow = GetUow();
            var mine = GetResourceManager(uow).GetDomikTypes().Single(x => x.Id == MineTypeId);

            Assert.That(mine.Name, Is.EqualTo("Рудник"));
        }

        [Test]
        public void LanternHasExpectedComfortAndCostTest()
        {
            using var uow = GetUow();
            var lantern = GetResourceManager(uow).GetDecorTypes().Single(x => x.Id == LanternDecorTypeId);

            Assert.That(lantern.LogicName, Is.EqualTo("lantern"));
            Assert.That(lantern.ComfortPoints, Is.EqualTo(5));
            Assert.That(lantern.Cost.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo(new[] { (IronResourceTypeId, 10), (BoardResourceTypeId, 4) }));
        }

        [Test]
        public void ForgeUnlockLevelIsTwentyTest()
        {
            using var uow = GetUow();
            var forge = GetResourceManager(uow).GetDomikTypes().Single(x => x.Id == ForgeTypeId);

            Assert.That(forge.UnlockLevel, Is.EqualTo(20));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void OreIsInExpeditionResourceLootTest(int expeditionTypeId)
        {
            using var uow = GetUow();
            var expedition = GetResourceManager(uow).GetExpeditionTypes().Single(x => x.Id == expeditionTypeId);

            Assert.That(expedition.Loot.Any(x => x.Kind == Domiki.Web.Data.ExpeditionLootKind.Resource && x.ResourceTypeId == OreResourceTypeId), Is.True);
        }

        private void AssertBinding(int domikTypeId, int level, int receiptId, bool expected)
        {
            using var uow = GetUow();
            var domikType = GetResourceManager(uow).GetDomikTypes().Single(x => x.Id == domikTypeId);
            var receiptIds = domikType.Levels.Single(x => x.Value == level).Receipts.Select(x => x.Id);

            Assert.That(receiptIds.Contains(receiptId), Is.EqualTo(expected));
        }
    }
}
