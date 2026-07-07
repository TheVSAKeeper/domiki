using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    public class ExpeditionTests : TestBase
    {
        private const int ShortScoutId = 1;
        private const int LongJourneyId = 2;
        private const int BarracksTypeId = 2;
        private const int ProducerDomikTypeId = 5;
        private const int GoldResourceTypeId = 5;
        private const int StoneResourceTypeId = 2;
        private const int WoodResourceTypeId = 3;
        private const int ClayResourceTypeId = 4;
        private const int ToolResourceTypeId = 8;
        private const int FurnitureResourceTypeId = 9;

        [Test]
        public void GetExpeditionsNewPlayerListsTypesWithNoActiveTest()
        {
            var playerId = GetPlayerId();

            var state = GetExpeditions(playerId);

            Assert.That(state.Types.Select(x => x.Id), Is.EquivalentTo(new[] { ShortScoutId, LongJourneyId }));
            Assert.That(state.Active, Is.Empty);
            Assert.That(state.ExpeditionsSincePity, Is.EqualTo(0));
            Assert.That(state.PityThreshold, Is.EqualTo(ExpeditionManager.ExpeditionPityThreshold));
        }

        [TestCase(ShortScoutId, 2, 1, 14400)]
        [TestCase(LongJourneyId, 5, 2, 86400)]
        public void StartExpeditionAssignsWorkersAndWritesOffGoldTest(int expeditionTypeId, int workerCount, int goldCost, int durationSeconds)
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, workerCount);
            GrantResource(playerId, GoldResourceTypeId, goldCost);
            var start = DateTimeHelper.GetNowDate();

            StartExpedition(playerId, expeditionTypeId);

            var state = GetExpeditions(playerId);
            var expedition = state.Active.Single();
            Assert.That(expedition.ExpeditionType.Id, Is.EqualTo(expeditionTypeId));
            Assert.That((expedition.FinishDate - start).TotalSeconds, Is.EqualTo(durationSeconds).Within(2));
            Assert.That(GetWorkers(playerId).Count(x => x.ExpeditionId == expedition.Id), Is.EqualTo(workerCount));
            Assert.That(ResourceValue(GetResources(playerId), GoldResourceTypeId), Is.EqualTo(0));
        }

        private static IEnumerable<TestCaseData> InsufficientWorkerCases()
        {
            yield return new TestCaseData(new Action<ExpeditionTests, int, int[]>((test, playerId, workerIds) =>
            {
                test.StartManufacture(playerId, 4, 1, new[] { workerIds[0] });
                test.StartManufacture(playerId, 5, 1, new[] { workerIds[1] });
            })).SetName("BusyWithManufacture");
            yield return new TestCaseData(new Action<ExpeditionTests, int, int[]>((test, playerId, workerIds) =>
            {
                test.SetWorkerRest(workerIds[0], DateTimeHelper.GetNowDate().AddHours(1));
                test.SetWorkerRest(workerIds[1], DateTimeHelper.GetNowDate().AddHours(1));
            })).SetName("Resting");
            yield return new TestCaseData(new Action<ExpeditionTests, int, int[]>((test, playerId, workerIds) =>
                test.StartExpedition(playerId, ShortScoutId))).SetName("BusyWithOtherExpedition");
        }

        [TestCaseSource(nameof(InsufficientWorkerCases))]
        public void StartExpeditionWithoutEnoughFreeWorkersThrowsTest(Action<ExpeditionTests, int, int[]> occupy)
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 3);
            BuyDomik(playerId, ProducerDomikTypeId);
            BuyDomik(playerId, ProducerDomikTypeId);
            GrantResource(playerId, GoldResourceTypeId, 3);
            var workerIds = GetWorkers(playerId).Select(x => x.Id).ToArray();

            occupy(this, playerId, workerIds);

            var ex = Assert.Throws<BusinessException>(() => StartExpedition(playerId, ShortScoutId));
            Assert.That(ex.Message, Is.EqualTo("Недостаточно трудяг"));
        }

        [Test]
        public void StartExpeditionWithoutEnoughGoldThrowsAndKeepsStateTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 2);

            var ex = Assert.Throws<BusinessException>(() => StartExpedition(playerId, ShortScoutId));

            Assert.That(ex.Message, Is.EqualTo("Недостаточно Золото"));
            Assert.That(GetExpeditions(playerId).Active, Is.Empty);
            Assert.That(GetWorkers(playerId).All(x => x.ExpeditionId == null), Is.True);
        }

        [Test]
        public void StartExpeditionWithUnknownTypeThrowsTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 2);
            GrantResource(playerId, GoldResourceTypeId, 5);

            Assert.Throws<BusinessException>(() => StartExpedition(playerId, int.MaxValue));
        }

        [Test]
        public void WorkerOnExpeditionIsNotSelectedForManufactureTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 2);
            BuyDomik(playerId, ProducerDomikTypeId);
            GrantResource(playerId, GoldResourceTypeId, 1);

            StartExpedition(playerId, ShortScoutId);

            var ex = Assert.Throws<BusinessException>(() => StartManufacture(playerId, 3, 1));
            Assert.That(ex.Message, Is.EqualTo("Недостаточно трудяг"));
        }

        [Test]
        public void FinishExpeditionReleasesWorkersAndRemovesExpeditionTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 2);
            GrantResource(playerId, GoldResourceTypeId, 1);
            StartExpedition(playerId, ShortScoutId);
            var expedition = GetExpeditions(playerId).Active.Single();

            SetExpeditionFinish(expedition.Id, DateTimeHelper.GetNowDate().AddSeconds(-1));
            FinishExpedition(playerId, expedition.Id, DateTimeHelper.GetNowDate());

            Assert.That(GetExpeditions(playerId).Active, Is.Empty);
            Assert.That(GetWorkers(playerId).All(x => x.ExpeditionId == null), Is.True);
        }

        [Test]
        public void FinishExpeditionGrantsLootWithinTableRangesTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 5);
            GrantResource(playerId, GoldResourceTypeId, 2);
            StartExpedition(playerId, LongJourneyId);
            var expedition = GetExpeditions(playerId).Active.Single();
            var before = GetResources(playerId);

            SetExpeditionFinish(expedition.Id, DateTimeHelper.GetNowDate().AddSeconds(-1));
            FinishExpedition(playerId, expedition.Id, DateTimeHelper.GetNowDate());

            var after = GetResources(playerId);
            var woodDelta = ResourceValue(after, WoodResourceTypeId) - ResourceValue(before, WoodResourceTypeId);
            var stoneDelta = ResourceValue(after, StoneResourceTypeId) - ResourceValue(before, StoneResourceTypeId);
            var clayDelta = ResourceValue(after, ClayResourceTypeId) - ResourceValue(before, ClayResourceTypeId);
            var toolDelta = ResourceValue(after, ToolResourceTypeId) - ResourceValue(before, ToolResourceTypeId);
            var furnitureDelta = ResourceValue(after, FurnitureResourceTypeId) - ResourceValue(before, FurnitureResourceTypeId);

            Assert.That(woodDelta + stoneDelta + clayDelta + toolDelta + furnitureDelta, Is.GreaterThan(0));
            Assert.That(woodDelta, Is.InRange(0, 25 * 3));
            Assert.That(stoneDelta, Is.InRange(0, 25 * 3));
            Assert.That(clayDelta, Is.InRange(0, 25 * 3));
            Assert.That(toolDelta, Is.InRange(0, 4 * 3));
            Assert.That(furnitureDelta, Is.InRange(0, 3 * 3));
        }

        [Test]
        public void FinishExpeditionUpdatesPityCounterConsistentlyWithRareGrantTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 2);
            GrantResource(playerId, GoldResourceTypeId, 1);
            StartExpedition(playerId, ShortScoutId);
            var expedition = GetExpeditions(playerId).Active.Single();
            var before = GetResources(playerId);

            SetExpeditionFinish(expedition.Id, DateTimeHelper.GetNowDate().AddSeconds(-1));
            FinishExpedition(playerId, expedition.Id, DateTimeHelper.GetNowDate());

            var after = GetResources(playerId);
            var toolGranted = ResourceValue(after, ToolResourceTypeId) - ResourceValue(before, ToolResourceTypeId) > 0;
            var pity = GetPityCounter(playerId);
            Assert.That(pity, Is.EqualTo(toolGranted ? 0 : 1));
        }

        [TestCase(ShortScoutId, 2, 1, ToolResourceTypeId, 1, 2)]
        [TestCase(LongJourneyId, 5, 2, FurnitureResourceTypeId, 1, 9)]
        public void PityThresholdForcesRareLootAndResetsCounterTest(int expeditionTypeId, int workerCount, int goldCost, int rareResourceTypeId, int minGranted, int maxGranted)
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, workerCount);
            GrantResource(playerId, GoldResourceTypeId, goldCost);
            SetPityCounter(playerId, ExpeditionManager.ExpeditionPityThreshold);
            StartExpedition(playerId, expeditionTypeId);
            var expedition = GetExpeditions(playerId).Active.Single();
            var before = GetResources(playerId);

            SetExpeditionFinish(expedition.Id, DateTimeHelper.GetNowDate().AddSeconds(-1));
            FinishExpedition(playerId, expedition.Id, DateTimeHelper.GetNowDate());

            var after = GetResources(playerId);
            Assert.That(ResourceValue(after, rareResourceTypeId) - ResourceValue(before, rareResourceTypeId), Is.InRange(minGranted, maxGranted));
            Assert.That(GetPityCounter(playerId), Is.EqualTo(0));
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

        private void BuyBarracks(int playerId, int count)
        {
            for (var i = 0; i < count; i++)
            {
                BuyDomik(playerId, BarracksTypeId);
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

        private void StartManufacture(int playerId, int domikId, int receiptId, int[]? workerIds = null)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow, calculatorJustFinishMode: false);
                domikManager.StartManufacture(playerId, domikId, receiptId, workerIds: workerIds);
                uow.Commit();
            }
        }

        private Worker[] GetWorkers(int playerId)
        {
            using (var uow = GetUow())
            {
                var workerManager = GetWorkerManager(uow);
                var workers = workerManager.GetWorkers(playerId).ToArray();
                uow.Commit();
                return workers;
            }
        }

        private void SetWorkerRest(int workerId, DateTime restUntil)
        {
            using (var uow = GetUow())
            {
                var worker = uow.Context.Workers.Single(x => x.Id == workerId);
                worker.RestUntil = restUntil;
                uow.Commit();
            }
        }

        private Resource[] GetResources(int playerId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var resources = domikManager.GetResources(playerId).ToArray();
                uow.Commit();
                return resources;
            }
        }

        private void GrantResource(int playerId, int typeId, int value)
        {
            using (var uow = GetUow())
            {
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
        }

        private ExpeditionState GetExpeditions(int playerId)
        {
            using (var uow = GetUow())
            {
                var manager = GetExpeditionManager(uow);
                var state = manager.GetExpeditions(playerId);
                uow.Commit();
                return state;
            }
        }

        private void StartExpedition(int playerId, int expeditionTypeId)
        {
            using (var uow = GetUow())
            {
                var manager = GetExpeditionManager(uow, calculatorJustFinishMode: false);
                manager.StartExpedition(playerId, expeditionTypeId);
                uow.Commit();
            }
        }

        private void FinishExpedition(int playerId, int expeditionId, DateTime date)
        {
            using (var uow = GetUow())
            {
                var manager = GetExpeditionManager(uow, calculatorJustFinishMode: false);
                var result = manager.FinishExpedition(date, new CalculateInfo
                {
                    PlayerId = playerId,
                    ObjectId = expeditionId,
                    Date = date,
                    Type = CalculateTypes.Expedition,
                });
                Assert.That(result, Is.True);
                uow.Commit();
            }
        }

        private void SetExpeditionFinish(int expeditionId, DateTime finishDate)
        {
            using (var uow = GetUow())
            {
                var expedition = uow.Context.Expeditions.Single(x => x.Id == expeditionId);
                expedition.FinishDate = finishDate;
                uow.Commit();
            }
        }

        private void SetPityCounter(int playerId, int value)
        {
            using (var uow = GetUow())
            {
                var player = uow.Context.Players.Single(x => x.Id == playerId);
                player.ExpeditionsSincePity = value;
                uow.Commit();
            }
        }

        private int GetPityCounter(int playerId)
        {
            using (var uow = GetUow())
            {
                return uow.Context.Players.Single(x => x.Id == playerId).ExpeditionsSincePity;
            }
        }

        private int ResourceValue(Resource[] resources, int typeId)
        {
            return resources.FirstOrDefault(x => x.Type.Id == typeId)?.Value ?? 0;
        }
    }
}
