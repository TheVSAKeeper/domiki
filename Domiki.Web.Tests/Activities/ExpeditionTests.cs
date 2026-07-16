using Domiki.Web.Activities;
using Domiki.Web.Activities.Models;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;

namespace Domiki.Web.Tests;

public sealed class ExpeditionTests
{
    private const int ShortScoutId = 1;
    private const int LongJourneyId = 2;
    private const int FootScoutId = 3;
    private const int OrdinaryTraitId = 1;
    private const int NimbleTraitId = 2;

    /// <summary>
    /// Если у игрока уже изучены все чертежи, лут чертежа заменяется другой наградой вместо повторной выдачи.
    /// </summary>
    [Test]
    public void ApplyBlueprintLootFallsBackWhenAllOwnedTest()
    {
        var player = TestPlayer.Create();
        using var scope = App.Scope();
        var resourceManager = scope.Get<ResourceManager>();
        var blueprintManager = scope.Get<BlueprintManager>();
        foreach (var blueprint in resourceManager.GetBlueprints())
        {
            blueprintManager.GrantBlueprint(player.Id, blueprint.Id);
        }

        var manager = scope.Get<ExpeditionManager>();
        var type = resourceManager.GetExpeditionTypes().First(x => x.Id == ShortScoutId);
        var entry = type.Loot.First(x => x.Kind == ExpeditionLootKind.Blueprint);
        var traits = resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);

        var result = manager.ApplyLootEntry(player.Id, type, entry, [], traits, 0);
        scope.Commit();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ToString(), Does.Not.Contain("blueprintId"));
    }

    /// <summary>
    /// Запись лута чертежа выдаёт игроку ещё не изученный чертёж.
    /// </summary>
    [Test]
    public void ApplyBlueprintLootGrantsUnownedBlueprintTest()
    {
        var player = TestPlayer.Create();
        using var scope = App.Scope();
        var resourceManager = scope.Get<ResourceManager>();
        var manager = scope.Get<ExpeditionManager>();
        var blueprintManager = scope.Get<BlueprintManager>();
        var type = resourceManager.GetExpeditionTypes().First(x => x.Id == ShortScoutId);
        var entry = type.Loot.First(x => x.Kind == ExpeditionLootKind.Blueprint);
        var traits = resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);

        var result = manager.ApplyLootEntry(player.Id, type, entry, [], traits, 0);
        scope.Commit();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(blueprintManager.GetBlueprints(player.Id).Count(x => x.Owned), Is.EqualTo(1));
            Assert.That(result.ToString(), Does.Contain("blueprintId"));
        }
    }

    /// <summary>
    /// Запись лута типа декор выдаёт игроку соответствующий декор.
    /// </summary>
    [Test]
    public void ApplyLootEntryDecorGrantsPlayerDecorTest()
    {
        var player = TestPlayer.Create();
        using (var scope = App.Scope())
        {
            var resourceManager = scope.Get<ResourceManager>();
            var manager = scope.Get<ExpeditionManager>();
            var type = resourceManager.GetExpeditionTypes().Single(x => x.Id == ShortScoutId);
            var traits = resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);
            var entry = type.Loot.First(x => x.Kind == ExpeditionLootKind.Decor);

            manager.ApplyLootEntry(player.Id, type, entry, [], traits, 0);
            scope.Commit();
        }

        Assert.That(player.Decor().Owned.Single(x => x.DecorTypeId == DecorIds.TrailIdol).Count, Is.EqualTo(1));
    }

    /// <summary>
    /// Запись лута прокачки черты меняет черту одного обычного трудяги из отряда на особую.
    /// </summary>
    [Test]
    public void ApplyLootEntryTraitUpgradeChangesOrdinaryWorkerTraitTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2)
            .WithWorkerTraits(OrdinaryTraitId);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();

        using (var scope = App.Scope())
        {
            var resourceManager = scope.Get<ResourceManager>();
            var manager = scope.Get<ExpeditionManager>();
            var type = resourceManager.GetExpeditionTypes().Single(x => x.Id == LongJourneyId);
            var traits = resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);
            var entry = type.Loot.First(x => x.Kind == ExpeditionLootKind.TraitUpgrade);
            var squad = scope.Context.Workers.Where(x => workerIds.Contains(x.Id)).ToArray();

            manager.ApplyLootEntry(player.Id, type, entry, squad, traits, 0);
            scope.Commit();
        }

        Assert.That(player.Workers().Count(x => x.Trait.LogicName != "ordinary"), Is.EqualTo(1));
    }

    /// <summary>
    /// Если в отряде нет обычных трудяг для прокачки черты, лут заменяется другой наградой (мебель, декор или чертёж), а черты
    /// отряда не меняются.
    /// </summary>
    [Test]
    public void ApplyLootEntryTraitUpgradeFallsBackWithoutOrdinaryWorkerTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2)
            .WithWorkerTraits(NimbleTraitId);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var beforeFurniture = player.Resource(ResourceIds.Furniture);

        using (var scope = App.Scope())
        {
            var resourceManager = scope.Get<ResourceManager>();
            var manager = scope.Get<ExpeditionManager>();
            var type = resourceManager.GetExpeditionTypes().Single(x => x.Id == LongJourneyId);
            var traits = resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);
            var entry = type.Loot.First(x => x.Kind == ExpeditionLootKind.TraitUpgrade);
            var squad = scope.Context.Workers.Where(x => workerIds.Contains(x.Id)).ToArray();

            manager.ApplyLootEntry(player.Id, type, entry, squad, traits, 0);
            scope.Commit();
        }

        var furnitureGranted = player.Resource(ResourceIds.Furniture) - beforeFurniture > 0;
        var decorGranted = player.Decor().Owned.Any(x => x.DecorTypeId == DecorIds.WandererBanner);
        var blueprintGranted = player.HasAnyBlueprint();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(furnitureGranted || decorGranted || blueprintGranted, Is.True);
            Assert.That(player.Workers().All(x => x.Trait.LogicName == "nimble"), Is.True);
        }
    }

    /// <summary>
    /// Завершение экспедиции выдаёт хотя бы часть лута, и каждый ресурс укладывается в диапазон таблицы добычи
    /// (дерево/камень/глина до 210, инструменты до 15, мебель до 12 – трёхкратный запас на длительный поход).
    /// </summary>
    [Test]
    public void FinishExpeditionGrantsLootWithinTableRangesTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 4)
            .WithResource(ResourceIds.Gold, 2)
            .WithResource(ResourceIds.Board, 6);

        StartExpedition(player, LongJourneyId);
        var expedition = player.Expeditions().Active.Single();
        var beforeWood = player.Resource(ResourceIds.Wood);
        var beforeStone = player.Resource(ResourceIds.Stone);
        var beforeClay = player.Resource(ResourceIds.Clay);
        var beforeTool = player.Resource(ResourceIds.Tool);
        var beforeFurniture = player.Resource(ResourceIds.Furniture);

        SetExpeditionFinish(expedition.Id, DateTimeHelper.GetNowDate().AddSeconds(-1));
        player.FinishExpedition(expedition.Id, DateTimeHelper.GetNowDate());

        var woodDelta = player.Resource(ResourceIds.Wood) - beforeWood;
        var stoneDelta = player.Resource(ResourceIds.Stone) - beforeStone;
        var clayDelta = player.Resource(ResourceIds.Clay) - beforeClay;
        var toolDelta = player.Resource(ResourceIds.Tool) - beforeTool;
        var furnitureDelta = player.Resource(ResourceIds.Furniture) - beforeFurniture;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(woodDelta + stoneDelta + clayDelta + toolDelta + furnitureDelta, Is.GreaterThan(0));
            Assert.That(woodDelta, Is.InRange(0, 70 * 3));
            Assert.That(stoneDelta, Is.InRange(0, 70 * 3));
            Assert.That(clayDelta, Is.InRange(0, 70 * 3));
            Assert.That(toolDelta, Is.InRange(0, 5 * 3));
            Assert.That(furnitureDelta, Is.InRange(0, 4 * 3));
        }
    }

    /// <summary>
    /// Завершение экспедиции убирает её из активных и освобождает всех задействованных трудяг.
    /// </summary>
    [Test]
    public void FinishExpeditionReleasesWorkersAndRemovesExpeditionTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2)
            .WithResource(ResourceIds.Gold, 1)
            .WithResource(ResourceIds.Board, 2);

        StartExpedition(player, ShortScoutId);
        var expedition = player.Expeditions().Active.Single();

        SetExpeditionFinish(expedition.Id, DateTimeHelper.GetNowDate().AddSeconds(-1));
        player.FinishExpedition(expedition.Id, DateTimeHelper.GetNowDate());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Expeditions().Active, Is.Empty);
            Assert.That(player.Workers().All(x => x.ExpeditionId == null), Is.True);
        }
    }

    /// <summary>
    /// После завершения экспедиции трудяги без иммунитета к усталости уходят отдыхать на фиксированное время
    /// (ExpeditionRestSeconds).
    /// </summary>
    [Test]
    public void FinishExpeditionSetsRestForNonNoFatigueWorkersTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2)
            .WithWorkerTraits(OrdinaryTraitId)
            .WithResource(ResourceIds.Gold, 1)
            .WithResource(ResourceIds.Board, 2);

        StartExpedition(player, ShortScoutId);
        var expedition = player.Expeditions().Active.Single();
        var assignedWorkerIds = player.Workers().Where(x => x.ExpeditionId == expedition.Id).Select(x => x.Id).ToArray();
        var finishDate = DateTimeHelper.GetNowDate().AddSeconds(-1);

        SetExpeditionFinish(expedition.Id, finishDate);
        player.FinishExpedition(expedition.Id, DateTimeHelper.GetNowDate());

        var workers = player.Workers().Where(x => assignedWorkerIds.Contains(x.Id)).ToArray();
        foreach (var worker in workers)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(worker.RestUntil, Is.Not.Null);
                Assert.That((worker.RestUntilValue() - finishDate.AddSeconds(ExpeditionManager.ExpeditionRestSeconds)).TotalSeconds, Is.Zero.Within(2));
            }
        }
    }

    /// <summary>
    /// Счётчик жалости обнуляется, если выпал редкий лут (инструменты, декор, чертёж), и увеличивается на 1, если редкой
    /// награды не было.
    /// </summary>
    [Test]
    public void FinishExpeditionUpdatesPityCounterConsistentlyWithRareGrantTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2)
            .WithResource(ResourceIds.Gold, 1)
            .WithResource(ResourceIds.Board, 2);

        StartExpedition(player, ShortScoutId);
        var expedition = player.Expeditions().Active.Single();
        var beforeTool = player.Resource(ResourceIds.Tool);

        SetExpeditionFinish(expedition.Id, DateTimeHelper.GetNowDate().AddSeconds(-1));
        player.FinishExpedition(expedition.Id, DateTimeHelper.GetNowDate());

        var toolGranted = player.Resource(ResourceIds.Tool) - beforeTool > 0;
        var decorGranted = player.Decor().Owned.Any(x => x.DecorTypeId == DecorIds.TrailIdol);
        var blueprintGranted = player.HasAnyBlueprint();
        var pity = player.PityCounter();
        Assert.That(pity, Is.EqualTo(toolGranted || decorGranted || blueprintGranted ? 0 : 1));
    }

    /// <summary>
    /// Пеший разведчик уходит без золота, занимает одного трудягу и возвращается через 2 часа.
    /// </summary>
    [Test]
    public void FootScoutStartsWithoutGoldRowTest()
    {
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 500)
            .WithDomik(DomikIds.Barrack);

        player.Buy(DomikIds.ScoutHut);
        var start = DateTimeHelper.GetNowDate();

        StartExpedition(player, FootScoutId);

        var state = player.Expeditions();
        var expedition = state.Active.Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(expedition.ExpeditionType.Id, Is.EqualTo(FootScoutId));
            Assert.That((expedition.FinishDate - start).TotalSeconds, Is.EqualTo(7200).Within(2));
            Assert.That(player.Workers().Count(x => x.ExpeditionId == expedition.Id), Is.EqualTo(1));
            Assert.That(player.Resource(ResourceIds.Gold), Is.Zero);
        }
    }

    /// <summary>
    /// У нового игрока без Сторожки состояние экспедиций отсутствует – возвращается null.
    /// </summary>
    [Test]
    public void GetExpeditionsNewPlayerReturnsNullTest()
    {
        var player = TestPlayer.Create();

        Assert.That(player.ExpeditionsOrNull(), Is.Null);
    }

    /// <summary>
    /// С построенной Сторожкой доступны все типы экспедиций, активных походов ещё нет, счётчик жалости на нуле, а его порог
    /// равен глобальной константе.
    /// </summary>
    [Test]
    public void GetExpeditionsWithBuildingListsTypesWithNoActiveTest()
    {
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 50)
            .Buy(DomikIds.ScoutHut);

        var state = player.Expeditions();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.Types.Select(x => x.Id), Is.EquivalentTo([ShortScoutId, LongJourneyId, FootScoutId]));
            Assert.That(state.Active, Is.Empty);
            Assert.That(state.ExpeditionsSincePity, Is.Zero);
            Assert.That(state.PityThreshold, Is.EqualTo(ExpeditionManager.ExpeditionPityThreshold));
        }
    }

    /// <summary>
    /// По достижении порога жалости дальний поход гарантированно выдаёт редкий лут (мебель, декор, прокачку черты трудяги или
    /// чертёж) и обнуляет счётчик.
    /// </summary>
    [Test]
    public void PityThresholdForcesRareLootOnLongJourneyAndResetsCounterTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 4)
            .WithResource(ResourceIds.Gold, 2)
            .WithResource(ResourceIds.Board, EquipmentCost(LongJourneyId));

        SetPityCounter(player.Id, ExpeditionManager.ExpeditionPityThreshold);
        StartExpedition(player, LongJourneyId);
        var expedition = player.Expeditions().Active.Single();
        var beforeFurniture = player.Resource(ResourceIds.Furniture);
        var squadBeforeTraits = player.Workers().Where(x => x.ExpeditionId == expedition.Id).ToDictionary(x => x.Id, x => x.Trait.LogicName);

        SetExpeditionFinish(expedition.Id, DateTimeHelper.GetNowDate().AddSeconds(-1));
        player.FinishExpedition(expedition.Id, DateTimeHelper.GetNowDate());

        var furnitureGranted = player.Resource(ResourceIds.Furniture) - beforeFurniture > 0;
        var decorGranted = player.Decor().Owned.Any(x => x.DecorTypeId == DecorIds.WandererBanner);
        var traitUpgraded = player.Workers().Any(x => squadBeforeTraits.TryGetValue(x.Id, out var trait) && trait == "ordinary" && x.Trait.LogicName != "ordinary");
        var blueprintGranted = player.HasAnyBlueprint();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(furnitureGranted || decorGranted || traitUpgraded || blueprintGranted, Is.True);
            Assert.That(player.PityCounter(), Is.Zero);
        }
    }

    /// <summary>
    /// По достижении порога жалости короткая разведка гарантированно выдаёт редкий лут (инструменты, декор или чертёж) и
    /// обнуляет счётчик.
    /// </summary>
    [Test]
    public void PityThresholdForcesRareLootOnShortScoutAndResetsCounterTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2)
            .WithResource(ResourceIds.Gold, 1)
            .WithResource(ResourceIds.Board, EquipmentCost(ShortScoutId));

        SetPityCounter(player.Id, ExpeditionManager.ExpeditionPityThreshold);
        StartExpedition(player, ShortScoutId);
        var expedition = player.Expeditions().Active.Single();
        var beforeTool = player.Resource(ResourceIds.Tool);

        SetExpeditionFinish(expedition.Id, DateTimeHelper.GetNowDate().AddSeconds(-1));
        player.FinishExpedition(expedition.Id, DateTimeHelper.GetNowDate());

        var toolGranted = player.Resource(ResourceIds.Tool) - beforeTool is >= 2 and <= 3;
        var decorGranted = player.Decor().Owned.Any(x => x.DecorTypeId == DecorIds.TrailIdol);
        var blueprintGranted = player.HasAnyBlueprint();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(toolGranted || decorGranted || blueprintGranted, Is.True);
            Assert.That(player.PityCounter(), Is.Zero);
        }
    }

    /// <summary>
    /// Сторожка первого уровня держит только один активный отряд, вторая экспедиция отклоняется с подсказкой прокачать
    /// Сторожку.
    /// </summary>
    [Test]
    public void ScoutHutLevelOneAllowsOneActiveExpeditionOnlyTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2)
            .WithResource(ResourceIds.Gold, 2)
            .WithResource(ResourceIds.Board, 4);

        StartExpedition(player, ShortScoutId);

        var ex = Throws.Business(() => StartExpedition(player, ShortScoutId));
        Assert.That(ex.Message, Is.EqualTo("Все отряды в походе – улучшите Сторожку"));
    }

    /// <summary>
    /// Трудяга, занятый в производстве, нельзя выбрать в отряд экспедиции.
    /// </summary>
    [Test]
    public void StartExpeditionWithBusyChosenWorkerThrowsTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3)
            .WithDomik(DomikIds.ClayMine)
            .WithResource(ResourceIds.Gold, 1)
            .WithResource(ResourceIds.Board, EquipmentCost(ShortScoutId));

        var workerIds = player.Workers().OrderBy(x => x.Id).Select(x => x.Id).ToArray();
        using (App.PendingEvents())
        {
            player.StartManufacture(6, ReceiptIds.ClayDig, [workerIds[0]]);
        }

        var ex = Throws.Business(() => StartExpedition(player, ShortScoutId, [workerIds[0], workerIds[1]]));

        Assert.That(ex.Message, Is.EqualTo("Трудяга недоступен"));
    }

    /// <summary>
    /// При явном выборе трудяг в отряд попадают ровно указанные, а не произвольные свободные.
    /// </summary>
    [Test]
    public void StartExpeditionWithChosenWorkersAssignsExactlyThoseTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3)
            .WithResource(ResourceIds.Gold, 1)
            .WithResource(ResourceIds.Board, EquipmentCost(ShortScoutId));

        var workerIds = player.Workers().OrderBy(x => x.Id).Select(x => x.Id).ToArray();
        var chosen = new[] { workerIds[0], workerIds[2] };

        StartExpedition(player, ShortScoutId, chosen);

        var expedition = player.Expeditions().Active.Single();
        var assigned = player.Workers().Where(x => x.ExpeditionId == expedition.Id).Select(x => x.Id).ToArray();
        Assert.That(assigned, Is.EquivalentTo(chosen));
    }

    /// <summary>
    /// Один и тот же трудяга не может быть выбран в отряд дважды.
    /// </summary>
    [Test]
    public void StartExpeditionWithDuplicateWorkerThrowsTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3)
            .WithResource(ResourceIds.Gold, 1)
            .WithResource(ResourceIds.Board, EquipmentCost(ShortScoutId));

        var workerId = player.Workers().First().Id;

        var ex = Throws.Business(() => StartExpedition(player, ShortScoutId, [workerId, workerId]));

        Assert.That(ex.Message, Is.EqualTo("Дублирующиеся трудяги"));
    }

    /// <summary>
    /// Без Сторожки отправить экспедицию нельзя.
    /// </summary>
    [Test]
    public void StartExpeditionWithoutBuildingThrowsTest()
    {
        var player = TestPlayer.Create();

        var ex = Throws.Business(() => player.StartExpedition(ShortScoutId));

        Assert.That(ex.Message, Is.EqualTo("Нужна Сторожка"));
    }

    /// <summary>
    /// Без достаточного золота экспедиция не стартует, трудяги остаются свободны, а прочие ресурсы не тратятся.
    /// </summary>
    [Test]
    public void StartExpeditionWithoutEnoughGoldThrowsAndKeepsStateTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2)
            .WithResource(ResourceIds.Board, 2);

        var ex = Throws.Business(() => StartExpedition(player, ShortScoutId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("Недостаточно Золото"));
            Assert.That(player.Expeditions().Active, Is.Empty);
            Assert.That(player.Workers().All(x => x.ExpeditionId == null), Is.True);
            Assert.That(player.Resource(ResourceIds.Board), Is.EqualTo(2));
        }
    }

    /// <summary>
    /// Без достаточного количества досок на снаряжение экспедиция не стартует, трудяги остаются свободны, а золото не
    /// тратится.
    /// </summary>
    [Test]
    public void StartExpeditionWithoutEnoughPlanksThrowsAndKeepsStateTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2)
            .WithResource(ResourceIds.Gold, 1);

        var ex = Throws.Business(() => StartExpedition(player, ShortScoutId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("Недостаточно Доска"));
            Assert.That(player.Expeditions().Active, Is.Empty);
            Assert.That(player.Workers().All(x => x.ExpeditionId == null), Is.True);
            Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Отправка экспедиции с несуществующим типом похода отклоняется.
    /// </summary>
    [Test]
    public void StartExpeditionWithUnknownTypeThrowsTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2)
            .WithResource(ResourceIds.Gold, 5);

        Assert.Throws<BusinessException>(() => StartExpedition(player, int.MaxValue));
    }

    /// <summary>
    /// Число выбранных трудяг обязано совпадать с требуемым составом отряда, иначе экспедиция не стартует и ресурсы не
    /// списываются.
    /// </summary>
    [Test]
    public void StartExpeditionWithWrongWorkerCountThrowsAndKeepsStateTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3)
            .WithResource(ResourceIds.Gold, 1)
            .WithResource(ResourceIds.Board, EquipmentCost(ShortScoutId));

        var oneWorker = player.Workers().Select(x => x.Id).Take(1).ToArray();

        var ex = Throws.Business(() => StartExpedition(player, ShortScoutId, oneWorker));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("Неверное число трудяг"));
            Assert.That(player.Expeditions().Active, Is.Empty);
            Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Трудяга, ушедший в экспедицию, недоступен для автоподбора в производство.
    /// </summary>
    [Test]
    public void WorkerOnExpeditionIsNotSelectedForManufactureTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.ClayMine)
            .WithResource(ResourceIds.Gold, 1)
            .WithResource(ResourceIds.Board, 2);

        StartExpedition(player, ShortScoutId);

        var ex = Throws.Business(() => player.StartManufacture(4, ReceiptIds.ClayDig));
        Assert.That(ex.Message, Is.EqualTo("Недостаточно трудяг"));
    }

    /// <summary>
    /// Отправка экспедиции занимает ровно положенное число трудяг, списывает золото и снаряжение и назначает длительность
    /// похода по типу.
    /// </summary>
    /// <param name="expeditionTypeId">Тип экспедиции.</param>
    /// <param name="workerCount">Число трудяг в отряде.</param>
    /// <param name="goldCost">Стоимость похода в золоте.</param>
    /// <param name="durationSeconds">Длительность похода в секундах.</param>
    [TestCase(ShortScoutId, 2, 1, 14400)]
    [TestCase(LongJourneyId, 5, 2, 86400)]
    public void StartExpeditionAssignsWorkersAndWritesOffGoldTest(int expeditionTypeId, int workerCount, int goldCost, int durationSeconds)
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, workerCount - 1)
            .WithResource(ResourceIds.Gold, goldCost)
            .WithResource(ResourceIds.Board, EquipmentCost(expeditionTypeId));

        var start = DateTimeHelper.GetNowDate();

        StartExpedition(player, expeditionTypeId);

        var state = player.Expeditions();
        var expedition = state.Active.Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(expedition.ExpeditionType.Id, Is.EqualTo(expeditionTypeId));
            Assert.That((expedition.FinishDate - start).TotalSeconds, Is.EqualTo(durationSeconds).Within(2));
            Assert.That(player.Workers().Count(x => x.ExpeditionId == expedition.Id), Is.EqualTo(workerCount));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Resource(ResourceIds.Gold), Is.Zero);
            Assert.That(player.Resource(ResourceIds.Board), Is.Zero);
        }
    }

    /// <summary>
    /// Экспедиция не стартует без достаточного числа свободных трудяг, будь они заняты в производстве, на отдыхе или уже в
    /// другом походе.
    /// </summary>
    /// <param name="occupy">Сценарий, занимающий трудяг перед попыткой отправить экспедицию.</param>
    [TestCaseSource(nameof(InsufficientWorkerCases))]
    public void StartExpeditionWithoutEnoughFreeWorkersThrowsTest(Action<TestPlayer, int[]> occupy)
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2)
            .WithDomik(DomikIds.ClayMine)
            .WithResource(ResourceIds.Gold, 3)
            .WithResource(ResourceIds.Board, 2);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();

        occupy(player, workerIds);

        var ex = Throws.Business(() => StartExpedition(player, ShortScoutId));
        Assert.That(ex.Message, Is.EqualTo("Недостаточно трудяг"));
    }

    /// <summary>
    /// Ресурсы, которые экспедиция тратит на снаряжение, не пересекаются с ресурсами, которые она выдаёт как лут.
    /// </summary>
    /// <param name="expeditionTypeId">Тип экспедиции.</param>
    [TestCase(ShortScoutId)]
    [TestCase(LongJourneyId)]
    public void ExpeditionEquipmentDoesNotOverlapLootTest(int expeditionTypeId)
    {
        using var scope = App.Scope();
        var type = scope.Get<ResourceManager>().GetExpeditionTypes().Single(x => x.Id == expeditionTypeId);

        Assert.That(type.Equipment.Select(x => x.ResourceTypeId).Intersect(type.Loot.Where(x => x.ResourceTypeId.HasValue).Select(x => x.ResourceTypeId ?? 0)), Is.Empty);
        scope.Commit();
    }

    /// <summary>
    /// Бонус удачи увеличивает вес только редких записей лута, обычные записи он не затрагивает.
    /// </summary>
    /// <param name="isRare">Является ли запись лута редкой.</param>
    /// <param name="weight">Базовый вес записи.</param>
    /// <param name="luckPercent">Процент бонуса удачи.</param>
    /// <param name="expected">Ожидаемый итоговый вес.</param>
    [TestCase(false, 10, 0, 10)]
    [TestCase(true, 10, 0, 10)]
    [TestCase(true, 10, 100, 20)]
    [TestCase(false, 10, 100, 10)]
    public void ScaleWeightAppliesLuckOnlyToRareEntriesTest(bool isRare, int weight, int luckPercent, int expected)
    {
        Assert.That(ExpeditionManager.ScaleWeight(isRare, weight, luckPercent), Is.EqualTo(expected));
    }

    private static IEnumerable<TestCaseData> InsufficientWorkerCases()
    {
        yield return new TestCaseData(new Action<TestPlayer, int[]>((player, workerIds) =>
        {
            using (App.PendingEvents())
            {
                player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig, [workerIds[0]]);
                player.StartManufacture(5, ReceiptIds.ClayDig, [workerIds[1]]);
            }
        })).SetName("BusyWithManufacture");

        yield return new TestCaseData(new Action<TestPlayer, int[]>((player, workerIds) =>
        {
            player.SetWorkerRest(workerIds[0], DateTimeHelper.GetNowDate().AddHours(1));
            player.SetWorkerRest(workerIds[1], DateTimeHelper.GetNowDate().AddHours(1));
        })).SetName("Resting");

        yield return new TestCaseData(new Action<TestPlayer, int[]>((player, _) =>
        {
            SetScoutHutLevel(player.Id, 2);
            StartExpedition(player, ShortScoutId);
        })).SetName("BusyWithOtherExpedition");
    }

    private static TestPlayer StartExpedition(TestPlayer player, int expeditionTypeId, int[]? workerIds = null)
    {
        EnsureScoutHut(player.Id);
        return player.StartExpedition(expeditionTypeId, workerIds);
    }

    private static void EnsureScoutHut(int playerId)
    {
        using var scope = App.Scope();
        if (!scope.Context.Domiks.Any(x => x.PlayerId == playerId && x.TypeId == DomikIds.ScoutHut))
        {
            scope.Context.Domiks.Add(new()
            {
                PlayerId = playerId,
                Id = -DomikIds.ScoutHut,
                TypeId = DomikIds.ScoutHut,
                Level = 1,
            });
        }

        scope.Commit();
    }

    private static void SetScoutHutLevel(int playerId, int level)
    {
        EnsureScoutHut(playerId);
        using var scope = App.Scope();
        scope.Context.Domiks.Single(x => x.PlayerId == playerId && x.TypeId == DomikIds.ScoutHut).Level = level;
        scope.Commit();
    }

    private static void SetExpeditionFinish(int expeditionId, DateTime finishDate)
    {
        using var scope = App.Scope();
        scope.Context.Expeditions.Single(x => x.Id == expeditionId).FinishDate = finishDate;
        scope.Commit();
    }

    private static void SetPityCounter(int playerId, int value)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == playerId).ExpeditionsSincePity = value;
        scope.Commit();
    }

    private static int EquipmentCost(int expeditionTypeId)
    {
        return expeditionTypeId == ShortScoutId ? 2 : 6;
    }
}

file static class ExpeditionTestsActs
{
    public static bool HasAnyBlueprint(this TestPlayer p)
    {
        return App.Read(context => context.PlayerBlueprints.Any(x => x.PlayerId == p.Id));
    }

    public static int PityCounter(this TestPlayer p)
    {
        return App.Read(context => context.Players.Single(x => x.Id == p.Id).ExpeditionsSincePity);
    }
}
