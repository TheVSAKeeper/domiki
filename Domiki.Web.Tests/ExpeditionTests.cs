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
        private const int ScoutHutDomikTypeId = 11;
        private const int ProducerDomikTypeId = 5;
        private const int GoldResourceTypeId = 5;
        private const int PlankResourceTypeId = 7;
        private const int OrdinaryTraitId = 1;
        private const int NimbleTraitId = 2;
        private const int StoneResourceTypeId = 2;
        private const int WoodResourceTypeId = 3;
        private const int ClayResourceTypeId = 4;
        private const int ToolResourceTypeId = 8;
        private const int FurnitureResourceTypeId = 9;
        private const int TrailIdolDecorTypeId = 6;
        private const int WandererBannerDecorTypeId = 7;

        [Test]
        public void GetExpeditionsNewPlayerReturnsNullTest()
        {
            var playerId = GetPlayerId();

            var state = GetExpeditions(playerId);

            Assert.That(state, Is.Null);
        }

        [Test]
        public void GetExpeditionsWithBuildingListsTypesWithNoActiveTest()
        {
            var playerId = GetPlayerId();
            GrantResource(playerId, 1, 50);
            BuyDomik(playerId, ScoutHutDomikTypeId);

            var state = GetExpeditions(playerId);

            Assert.That(state.Types.Select(x => x.Id), Is.EquivalentTo(new[] { ShortScoutId, LongJourneyId }));
            Assert.That(state.Active, Is.Empty);
            Assert.That(state.ExpeditionsSincePity, Is.EqualTo(0));
            Assert.That(state.PityThreshold, Is.EqualTo(ExpeditionManager.ExpeditionPityThreshold));
        }

        [Test]
        public void ScoutHutLevelOneAllowsOneActiveExpeditionOnlyTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 2);
            GrantResource(playerId, GoldResourceTypeId, 2);
            GrantResource(playerId, PlankResourceTypeId, 4);

            StartExpedition(playerId, ShortScoutId);

            var ex = Assert.Throws<BusinessException>(() => StartExpedition(playerId, ShortScoutId));
            Assert.That(ex!.Message, Is.EqualTo("Все отряды в походе – улучшите Сторожку"));
        }

        [Test]
        public void StartExpeditionWithoutBuildingThrowsTest()
        {
            var playerId = GetPlayerId();

            var ex = Assert.Throws<BusinessException>(() => StartExpeditionWithoutBuilding(playerId, ShortScoutId));

            Assert.That(ex.Message, Is.EqualTo("Нужна Сторожка"));
        }

        [TestCase(ShortScoutId, 2, 1, 14400)]
        [TestCase(LongJourneyId, 5, 2, 86400)]
        public void StartExpeditionAssignsWorkersAndWritesOffGoldTest(int expeditionTypeId, int workerCount, int goldCost, int durationSeconds)
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, workerCount);
            GrantResource(playerId, GoldResourceTypeId, goldCost);
            GrantResource(playerId, PlankResourceTypeId, EquipmentCost(expeditionTypeId));
            var start = DateTimeHelper.GetNowDate();

            StartExpedition(playerId, expeditionTypeId);

            var state = GetExpeditions(playerId);
            var expedition = state.Active.Single();
            Assert.That(expedition.ExpeditionType.Id, Is.EqualTo(expeditionTypeId));
            Assert.That((expedition.FinishDate - start).TotalSeconds, Is.EqualTo(durationSeconds).Within(2));
            Assert.That(GetWorkers(playerId).Count(x => x.ExpeditionId == expedition.Id), Is.EqualTo(workerCount));
            var resources = GetResources(playerId);
            Assert.That(ResourceValue(resources, GoldResourceTypeId), Is.EqualTo(0));
            Assert.That(ResourceValue(resources, PlankResourceTypeId), Is.EqualTo(0));
        }

        [Test]
        public void StartExpeditionWithChosenWorkersAssignsExactlyThoseTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 3);
            GrantResource(playerId, GoldResourceTypeId, 1);
            GrantResource(playerId, PlankResourceTypeId, EquipmentCost(ShortScoutId));
            var workerIds = GetWorkers(playerId).OrderBy(x => x.Id).Select(x => x.Id).ToArray();
            var chosen = new[] { workerIds[0], workerIds[2] };

            StartExpedition(playerId, ShortScoutId, chosen);

            var expedition = GetExpeditions(playerId).Active.Single();
            var assigned = GetWorkers(playerId).Where(x => x.ExpeditionId == expedition.Id).Select(x => x.Id).ToArray();
            Assert.That(assigned, Is.EquivalentTo(chosen));
        }

        [Test]
        public void StartExpeditionWithWrongWorkerCountThrowsAndKeepsStateTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 3);
            GrantResource(playerId, GoldResourceTypeId, 1);
            GrantResource(playerId, PlankResourceTypeId, EquipmentCost(ShortScoutId));
            var oneWorker = GetWorkers(playerId).Select(x => x.Id).Take(1).ToArray();

            var ex = Assert.Throws<BusinessException>(() => StartExpedition(playerId, ShortScoutId, oneWorker));

            Assert.That(ex.Message, Is.EqualTo("Неверное число трудяг"));
            Assert.That(GetExpeditions(playerId).Active, Is.Empty);
            Assert.That(ResourceValue(GetResources(playerId), GoldResourceTypeId), Is.EqualTo(1));
        }

        [Test]
        public void StartExpeditionWithDuplicateWorkerThrowsTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 3);
            GrantResource(playerId, GoldResourceTypeId, 1);
            GrantResource(playerId, PlankResourceTypeId, EquipmentCost(ShortScoutId));
            var workerId = GetWorkers(playerId).First().Id;

            var ex = Assert.Throws<BusinessException>(() => StartExpedition(playerId, ShortScoutId, new[] { workerId, workerId }));

            Assert.That(ex.Message, Is.EqualTo("Дублирующиеся трудяги"));
        }

        [Test]
        public void StartExpeditionWithBusyChosenWorkerThrowsTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 3);
            BuyDomik(playerId, ProducerDomikTypeId);
            GrantResource(playerId, GoldResourceTypeId, 1);
            GrantResource(playerId, PlankResourceTypeId, EquipmentCost(ShortScoutId));
            var workerIds = GetWorkers(playerId).OrderBy(x => x.Id).Select(x => x.Id).ToArray();
            StartManufacture(playerId, 4, 1, new[] { workerIds[0] });

            var ex = Assert.Throws<BusinessException>(() => StartExpedition(playerId, ShortScoutId, new[] { workerIds[0], workerIds[1] }));

            Assert.That(ex.Message, Is.EqualTo("Трудяга недоступен"));
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
            {
                test.SetScoutHutLevel(playerId, 2);
                test.StartExpedition(playerId, ShortScoutId);
            })).SetName("BusyWithOtherExpedition");
        }

        [TestCaseSource(nameof(InsufficientWorkerCases))]
        public void StartExpeditionWithoutEnoughFreeWorkersThrowsTest(Action<ExpeditionTests, int, int[]> occupy)
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 3);
            BuyDomik(playerId, ProducerDomikTypeId);
            BuyDomik(playerId, ProducerDomikTypeId);
            GrantResource(playerId, GoldResourceTypeId, 3);
            GrantResource(playerId, PlankResourceTypeId, 2);
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
            GrantResource(playerId, PlankResourceTypeId, 2);

            var ex = Assert.Throws<BusinessException>(() => StartExpedition(playerId, ShortScoutId));

            Assert.That(ex.Message, Is.EqualTo("Недостаточно Золото"));
            Assert.That(GetExpeditions(playerId).Active, Is.Empty);
            Assert.That(GetWorkers(playerId).All(x => x.ExpeditionId == null), Is.True);
            Assert.That(ResourceValue(GetResources(playerId), PlankResourceTypeId), Is.EqualTo(2));
        }

        [Test]
        public void StartExpeditionWithoutEnoughPlanksThrowsAndKeepsStateTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 2);
            GrantResource(playerId, GoldResourceTypeId, 1);

            var ex = Assert.Throws<BusinessException>(() => StartExpedition(playerId, ShortScoutId));

            Assert.That(ex.Message, Is.EqualTo("Недостаточно Доска"));
            Assert.That(GetExpeditions(playerId).Active, Is.Empty);
            Assert.That(GetWorkers(playerId).All(x => x.ExpeditionId == null), Is.True);
            Assert.That(ResourceValue(GetResources(playerId), GoldResourceTypeId), Is.EqualTo(1));
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
            GrantResource(playerId, PlankResourceTypeId, 2);

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
            GrantResource(playerId, PlankResourceTypeId, 2);
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
            GrantResource(playerId, PlankResourceTypeId, 6);
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
            Assert.That(woodDelta, Is.InRange(0, 70 * 3));
            Assert.That(stoneDelta, Is.InRange(0, 70 * 3));
            Assert.That(clayDelta, Is.InRange(0, 70 * 3));
            Assert.That(toolDelta, Is.InRange(0, 5 * 3));
            Assert.That(furnitureDelta, Is.InRange(0, 4 * 3));
        }

        [Test]
        public void FinishExpeditionUpdatesPityCounterConsistentlyWithRareGrantTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 2);
            GrantResource(playerId, GoldResourceTypeId, 1);
            GrantResource(playerId, PlankResourceTypeId, 2);
            StartExpedition(playerId, ShortScoutId);
            var expedition = GetExpeditions(playerId).Active.Single();
            var before = GetResources(playerId);

            SetExpeditionFinish(expedition.Id, DateTimeHelper.GetNowDate().AddSeconds(-1));
            FinishExpedition(playerId, expedition.Id, DateTimeHelper.GetNowDate());

            var after = GetResources(playerId);
            var toolGranted = ResourceValue(after, ToolResourceTypeId) - ResourceValue(before, ToolResourceTypeId) > 0;
            var decorGranted = GetDecor(playerId).Owned.Any(x => x.DecorTypeId == TrailIdolDecorTypeId);
            var pity = GetPityCounter(playerId);
            Assert.That(pity, Is.EqualTo(toolGranted || decorGranted ? 0 : 1));
        }

        [Test]
        public void PityThresholdForcesRareLootOnShortScoutAndResetsCounterTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 2);
            GrantResource(playerId, GoldResourceTypeId, 1);
            GrantResource(playerId, PlankResourceTypeId, EquipmentCost(ShortScoutId));
            SetPityCounter(playerId, ExpeditionManager.ExpeditionPityThreshold);
            StartExpedition(playerId, ShortScoutId);
            var expedition = GetExpeditions(playerId).Active.Single();
            var before = GetResources(playerId);

            SetExpeditionFinish(expedition.Id, DateTimeHelper.GetNowDate().AddSeconds(-1));
            FinishExpedition(playerId, expedition.Id, DateTimeHelper.GetNowDate());

            var after = GetResources(playerId);
            var toolGranted = ResourceValue(after, ToolResourceTypeId) - ResourceValue(before, ToolResourceTypeId) is >= 2 and <= 3;
            var decorGranted = GetDecor(playerId).Owned.Any(x => x.DecorTypeId == TrailIdolDecorTypeId);
            Assert.That(toolGranted || decorGranted, Is.True);
            Assert.That(GetPityCounter(playerId), Is.EqualTo(0));
        }

        [Test]
        public void PityThresholdForcesRareLootOnLongJourneyAndResetsCounterTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 5);
            GrantResource(playerId, GoldResourceTypeId, 2);
            GrantResource(playerId, PlankResourceTypeId, EquipmentCost(LongJourneyId));
            SetPityCounter(playerId, ExpeditionManager.ExpeditionPityThreshold);
            StartExpedition(playerId, LongJourneyId);
            var expedition = GetExpeditions(playerId).Active.Single();
            var before = GetResources(playerId);
            var squadBeforeTraits = GetWorkers(playerId).Where(x => x.ExpeditionId == expedition.Id).ToDictionary(x => x.Id, x => x.Trait.LogicName);

            SetExpeditionFinish(expedition.Id, DateTimeHelper.GetNowDate().AddSeconds(-1));
            FinishExpedition(playerId, expedition.Id, DateTimeHelper.GetNowDate());

            var after = GetResources(playerId);
            var furnitureGranted = ResourceValue(after, FurnitureResourceTypeId) - ResourceValue(before, FurnitureResourceTypeId) > 0;
            var decorGranted = GetDecor(playerId).Owned.Any(x => x.DecorTypeId == WandererBannerDecorTypeId);
            var traitUpgraded = GetWorkers(playerId).Any(x => squadBeforeTraits.TryGetValue(x.Id, out var trait) && trait == "ordinary" && x.Trait.LogicName != "ordinary");
            Assert.That(furnitureGranted || decorGranted || traitUpgraded, Is.True);
            Assert.That(GetPityCounter(playerId), Is.EqualTo(0));
        }

        [Test]
        public void ApplyLootEntryDecorGrantsPlayerDecorTest()
        {
            var playerId = GetPlayerId();
            using (var uow = GetUow())
            {
                var resourceManager = GetResourceManager(uow);
                var manager = GetExpeditionManager(uow);
                var type = resourceManager.GetExpeditionTypes().Single(x => x.Id == ShortScoutId);
                var traits = resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);
                var entry = type.Loot.First(x => x.Kind == Domiki.Web.Data.ExpeditionLootKind.Decor);

                manager.ApplyLootEntry(playerId, type, entry, Array.Empty<Domiki.Web.Data.Worker>(), traits, 0);
                uow.Commit();
            }

            var decor = GetDecor(playerId);
            Assert.That(decor.Owned.Single(x => x.DecorTypeId == TrailIdolDecorTypeId).Count, Is.EqualTo(1));
        }

        [Test]
        public void ApplyLootEntryTraitUpgradeChangesOrdinaryWorkerTraitTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 2);
            GetWorkers(playerId);
            SetAllWorkerTraits(playerId, OrdinaryTraitId);
            var workerIds = GetWorkers(playerId).Select(x => x.Id).ToArray();

            using (var uow = GetUow())
            {
                var resourceManager = GetResourceManager(uow);
                var manager = GetExpeditionManager(uow);
                var type = resourceManager.GetExpeditionTypes().Single(x => x.Id == LongJourneyId);
                var traits = resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);
                var entry = type.Loot.First(x => x.Kind == Domiki.Web.Data.ExpeditionLootKind.TraitUpgrade);
                var squad = uow.Context.Workers.Where(x => workerIds.Contains(x.Id)).ToArray();

                manager.ApplyLootEntry(playerId, type, entry, squad, traits, 0);
                uow.Commit();
            }

            Assert.That(GetWorkers(playerId).Count(x => x.Trait.LogicName != "ordinary"), Is.EqualTo(1));
        }

        [Test]
        public void ApplyLootEntryTraitUpgradeFallsBackWithoutOrdinaryWorkerTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 2);
            GetWorkers(playerId);
            SetAllWorkerTraits(playerId, NimbleTraitId);
            var workerIds = GetWorkers(playerId).Select(x => x.Id).ToArray();
            var before = GetResources(playerId);

            using (var uow = GetUow())
            {
                var resourceManager = GetResourceManager(uow);
                var manager = GetExpeditionManager(uow);
                var type = resourceManager.GetExpeditionTypes().Single(x => x.Id == LongJourneyId);
                var traits = resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);
                var entry = type.Loot.First(x => x.Kind == Domiki.Web.Data.ExpeditionLootKind.TraitUpgrade);
                var squad = uow.Context.Workers.Where(x => workerIds.Contains(x.Id)).ToArray();

                manager.ApplyLootEntry(playerId, type, entry, squad, traits, 0);
                uow.Commit();
            }

            var after = GetResources(playerId);
            var furnitureGranted = ResourceValue(after, FurnitureResourceTypeId) - ResourceValue(before, FurnitureResourceTypeId) > 0;
            var decorGranted = GetDecor(playerId).Owned.Any(x => x.DecorTypeId == WandererBannerDecorTypeId);
            Assert.That(furnitureGranted || decorGranted, Is.True);
            Assert.That(GetWorkers(playerId).All(x => x.Trait.LogicName == "nimble"), Is.True);
        }

        [Test]
        public void FinishExpeditionSetsRestForNonNoFatigueWorkersTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 2);
            GetWorkers(playerId);
            SetAllWorkerTraits(playerId, OrdinaryTraitId);
            GrantResource(playerId, GoldResourceTypeId, 1);
            GrantResource(playerId, PlankResourceTypeId, 2);
            StartExpedition(playerId, ShortScoutId);
            var expedition = GetExpeditions(playerId).Active.Single();
            var assignedWorkerIds = GetWorkers(playerId).Where(x => x.ExpeditionId == expedition.Id).Select(x => x.Id).ToArray();
            var finishDate = DateTimeHelper.GetNowDate().AddSeconds(-1);

            SetExpeditionFinish(expedition.Id, finishDate);
            FinishExpedition(playerId, expedition.Id, DateTimeHelper.GetNowDate());

            var workers = GetWorkers(playerId).Where(x => assignedWorkerIds.Contains(x.Id)).ToArray();
            foreach (var worker in workers)
            {
                Assert.That(worker.RestUntil, Is.Not.Null);
                Assert.That((worker.RestUntil!.Value - finishDate.AddSeconds(ExpeditionManager.ExpeditionRestSeconds)).TotalSeconds, Is.EqualTo(0).Within(2));
            }
        }

        [TestCase(ShortScoutId)]
        [TestCase(LongJourneyId)]
        public void ExpeditionEquipmentDoesNotOverlapLootTest(int expeditionTypeId)
        {
            using (var uow = GetUow())
            {
                var type = GetResourceManager(uow).GetExpeditionTypes().Single(x => x.Id == expeditionTypeId);

                Assert.That(type.Equipment.Select(x => x.ResourceTypeId).Intersect(type.Loot.Where(x => x.ResourceTypeId.HasValue).Select(x => x.ResourceTypeId!.Value)), Is.Empty);
                uow.Commit();
            }
        }

        [TestCase(false, 10, 0, 10)]
        [TestCase(true, 10, 0, 10)]
        [TestCase(true, 10, 100, 20)]
        [TestCase(false, 10, 100, 10)]
        public void ScaleWeightAppliesLuckOnlyToRareEntriesTest(bool isRare, int weight, int luckPercent, int expected)
        {
            Assert.That(ExpeditionManager.ScaleWeight(isRare, weight, luckPercent), Is.EqualTo(expected));
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

        private void AddBuiltDomik(int playerId, int typeId)
        {
            using (var uow = GetUow())
            {
                if (!uow.Context.Domiks.Any(x => x.PlayerId == playerId && x.TypeId == typeId))
                {
                    uow.Context.Domiks.Add(new Domiki.Web.Data.Domik
                    {
                        PlayerId = playerId,
                        Id = -typeId,
                        TypeId = typeId,
                        Level = 1,
                    });
                    uow.Context.SaveChanges();
                }

                uow.Commit();
            }
        }

        private void SetScoutHutLevel(int playerId, int level)
        {
            AddBuiltDomik(playerId, ScoutHutDomikTypeId);
            using (var uow = GetUow())
            {
                uow.Context.Domiks.Single(x => x.PlayerId == playerId && x.TypeId == ScoutHutDomikTypeId).Level = level;
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

        private void SetAllWorkerTraits(int playerId, int traitId)
        {
            using (var uow = GetUow())
            {
                foreach (var worker in uow.Context.Workers.Where(x => x.PlayerId == playerId))
                {
                    worker.TraitId = traitId;
                }

                uow.Commit();
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

        private DecorState GetDecor(int playerId)
        {
            using (var uow = GetUow())
            {
                var manager = GetDecorManager(uow);
                var decor = manager.GetDecor(playerId);
                uow.Commit();
                return decor;
            }
        }

        private ExpeditionState? GetExpeditions(int playerId)
        {
            using (var uow = GetUow())
            {
                var manager = GetExpeditionManager(uow);
                var state = manager.GetExpeditions(playerId);
                uow.Commit();
                return state;
            }
        }

        private void StartExpedition(int playerId, int expeditionTypeId, int[]? workerIds = null)
        {
            AddBuiltDomik(playerId, ScoutHutDomikTypeId);
            using (var uow = GetUow())
            {
                var manager = GetExpeditionManager(uow, calculatorJustFinishMode: false);
                manager.StartExpedition(playerId, expeditionTypeId, workerIds);
                uow.Commit();
            }
        }

        private void StartExpeditionWithoutBuilding(int playerId, int expeditionTypeId)
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

        private int EquipmentCost(int expeditionTypeId)
        {
            return expeditionTypeId == ShortScoutId ? 2 : 6;
        }

        private int ResourceValue(Resource[] resources, int typeId)
        {
            return resources.FirstOrDefault(x => x.Type.Id == typeId)?.Value ?? 0;
        }
    }
}
