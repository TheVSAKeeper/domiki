using Domiki.Web.Core;
using Domiki.Web.Core.Scheduling;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village;
using Domiki.Web.Workers;

namespace Domiki.Web.Tests;

public sealed class RelocationTests
{
    private const int UnlockLevel = VillageLevelCalculator.RelocationUnlockLevel;
    private const int LevelStep = VillageLevelCalculator.RelocationLevelStep;
    private const int CooldownDays = VillageLevelCalculator.RelocationCooldownDays;
    private const int FirstValleyId = 1;
    private const int MaxDomikLevel = 5;
    private const int FirstDomikId = 1;

    /// <summary>
    /// Ниже порога обжитости переезд отклоняется, а порог назван числом.
    /// </summary>
    [Test]
    public void RelocateRejectsBelowThresholdTest()
    {
        var player = TestPlayer.Create();

        var ex = Throws.Business(() => player.Relocate());

        Assert.That(ex.Message, Is.EqualTo($"Переезд откроется на обжитости {UnlockLevel}"));
    }

    /// <summary>
    /// Второй переезд подряд отклоняется кулдауном, а по истечении семи суток проходит.
    /// </summary>
    [Test]
    public void RelocateRespectsCooldownTest()
    {
        var player = TestPlayer.Create()
            .AtRelocationThreshold()
            .Relocate()
            .AtRelocationThreshold(relocationCount: 1);

        var tooEarly = Throws.Business(() => player.Relocate());
        Assert.That(tooEarly.Message, Is.EqualTo($"Собраться в новую долину можно не чаще раза в {CooldownDays} суток"));

        player.BackdateRelocation(TimeSpan.FromDays(CooldownDays) + TimeSpan.FromSeconds(1));
        player.Relocate();

        Assert.That(player.Relocation().RelocationCount, Is.EqualTo(2));
    }

    /// <summary>
    /// С активным лотом на Торговом дворе уехать нельзя.
    /// </summary>
    [Test]
    public void RelocateRejectsWithActiveLotTest()
    {
        var player = TestPlayer.Create()
            .AtRelocationThreshold()
            .WithTradeLot();

        var ex = Throws.Business(() => player.Relocate());

        Assert.That(ex.Message, Is.EqualTo("На Торговом дворе остались ваши лоты"));
    }

    /// <summary>
    /// Пока хоть один трудяга в походе, на поручении или в происшествии, артель не в сборе и переезд отклоняется.
    /// </summary>
    /// <param name="busyKind">Чем занят трудяга: походом, поручением или происшествием.</param>
    [TestCase(CalculateTypes.Expedition)]
    [TestCase(CalculateTypes.Errand)]
    [TestCase(CalculateTypes.Incident)]
    public void RelocateRejectsWhenArtelIsNotHomeTest(CalculateTypes busyKind)
    {
        var player = TestPlayer.Create()
            .AtRelocationThreshold();

        OccupyWorker(player, player.Workers().Single().Id, busyKind);

        var ex = Throws.Business(() => player.Relocate());

        Assert.That(ex.Message, Is.EqualTo("Артель не в сборе – кто-то в походе, на поручении или в происшествии"));
    }

    /// <summary>
    /// Идущее производство переезду не мешает – оно просто гаснет вместе с деревней.
    /// </summary>
    [Test]
    public void RelocateIgnoresRunningManufactureTest()
    {
        var player = TestPlayer.Create()
            .AtRelocationThreshold()
            .WithResource(ResourceIds.Clay, 100);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig);
        }

        player.Relocate();

        Assert.That(App.Read(context => context.Manufactures.Count(x => x.DomikPlayerId == player.Id)), Is.Zero);
    }

    /// <summary>
    /// С артелью переезжают выучка, вехи, чертежи, имя деревни с гербом, журнал, книга гостей, остаток зарядов
    /// и окно обоза, а усталость, отдых и хворь обнуляются.
    /// </summary>
    [Test]
    public void RelocateCarriesArtelAndMemoryTest()
    {
        const int skillUses = 40;
        const int zealCharges = 7;
        const int workedSeconds = 3600;

        var player = TestPlayer.Create()
            .AtRelocationThreshold()
            .WithBlueprint(BlueprintIds.Workshop)
            .SetVillageIdentity("Кривые Ложки", 3, 4);

        var workerId = player.Workers().Single().Id;
        player.SetWorkerSkill(workerId, DomikIds.ClayMine, skillUses)
            .SetWorkerWorked(workerId, workedSeconds)
            .SetWorkerRest(workerId, DateTimeHelper.GetNowDate().AddHours(1));

        SetZealCharges(player.Id, zealCharges);
        SetWorkerSick(workerId, DateTimeHelper.GetNowDate().AddHours(1));
        AddMilestone(workerId);
        AddJournalEntry(player.Id);
        AddConvoyWindow(player.Id);

        player.Relocate();

        var worker = App.Read(context => context.Workers.Single(x => x.Id == workerId));
        var village = player.Village();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(App.Read(context => context.Workers.Count(x => x.PlayerId == player.Id)), Is.EqualTo(1));
            Assert.That(App.Read(context => context.WorkerSkills.Single(x => x.WorkerId == workerId).Uses), Is.EqualTo(skillUses));
            Assert.That(App.Read(context => context.WorkerMilestones.Count(x => x.WorkerId == workerId)), Is.EqualTo(1));
            Assert.That(worker.WorkedSeconds, Is.Zero);
            Assert.That(worker.RestUntil, Is.Null);
            Assert.That(worker.SickUntil, Is.Null);
            Assert.That(worker.SickTypeId, Is.Null);
            Assert.That(App.Read(context => context.PlayerBlueprints.Count(x => x.PlayerId == player.Id)), Is.EqualTo(1));
            Assert.That(village.VillageName, Is.EqualTo("Кривые Ложки"));
            Assert.That(village.CrestIcon, Is.EqualTo(3));
            Assert.That(village.CrestColor, Is.EqualTo(4));
            Assert.That(App.Read(context => context.GuestbookEntries.Count(x => x.HostPlayerId == player.Id)), Is.EqualTo(1));
            Assert.That(App.Read(context => context.Players.Single(x => x.Id == player.Id).ZealCharges), Is.EqualTo(zealCharges));
            Assert.That(App.Read(context => context.NeighborConvoys.Count(x => x.PlayerId == player.Id)), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Двор остаётся в прожитой деревне: постройки, склад, декор, кладовая, заповедный припас, счётная книга,
    /// износ плащей, дружба и уклад сгорают, а казна новой деревни равна стартовым монетам.
    /// </summary>
    [Test]
    public void RelocateBurnsVillageTest()
    {
        var player = TestPlayer.Create()
            .AtRelocationThreshold()
            .WithDomik(DomikIds.Market)
            .WithResource(ResourceIds.Coin, 5000)
            .WithResource(ResourceIds.Clay, 300)
            .WithDecor(DecorIds.Fence)
            .SetFoodRule(ResourceIds.Bread, 5, false)
            .WithReputation(NeighborIds.Borovoe, VillageProfileManager.ReputationRequirement)
            .SetVillageProfile(NeighborIds.Borovoe);

        SetFriendNeighbor(player.Id, NeighborIds.Borovoe);
        SetCloakWear(player.Id, 40);
        AddReserve(player.Id);
        AddLedgerRows(player.Id);

        player.Relocate();

        var dbPlayer = App.Read(context => context.Players.Single(x => x.Id == player.Id));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(App.Read(context => context.Domiks.Count(x => x.PlayerId == player.Id)), Is.EqualTo(1));
            Assert.That(App.Read(context => context.Domiks.Single(x => x.PlayerId == player.Id).TypeId), Is.EqualTo(DomikIds.PostHouse));
            Assert.That(player.Resource(ResourceIds.Clay), Is.Zero);
            Assert.That(player.Resource(ResourceIds.Coin), Is.EqualTo(DomikManager.StartingCoins));
            Assert.That(App.Read(context => context.PlayerDecors.Count(x => x.PlayerId == player.Id)), Is.Zero);
            Assert.That(App.Read(context => context.PlayerFoodRules.Count(x => x.PlayerId == player.Id)), Is.Zero);
            Assert.That(App.Read(context => context.PlayerResourceReserves.Count(x => x.PlayerId == player.Id)), Is.Zero);
            Assert.That(App.Read(context => context.PlayerResourceFlows.Count(x => x.PlayerId == player.Id)), Is.Zero);
            Assert.That(App.Read(context => context.PlayerLaborDays.Count(x => x.PlayerId == player.Id)), Is.Zero);
            Assert.That(dbPlayer.CloakWearPoints, Is.Zero);
            Assert.That(dbPlayer.FriendNeighborId, Is.Null);
            Assert.That(dbPlayer.ProfileNeighborId, Is.Null);
            Assert.That(dbPlayer.ProfileChangedDate, Is.Null);
            Assert.That(dbPlayer.ValleyId, Is.EqualTo(FirstValleyId));
        }
    }

    /// <summary>
    /// Выполненные наказы старосты остаются выполненными – переезд их заново не выдаёт.
    /// </summary>
    [Test]
    public void RelocateKeepsCompletedGoalsTest()
    {
        var player = TestPlayer.Create()
            .AtRelocationThreshold();

        var before = App.Read(context => context.PlayerGoals.Count(x => x.PlayerId == player.Id));

        player.Relocate();

        Assert.That(App.Read(context => context.PlayerGoals.Count(x => x.PlayerId == player.Id)), Is.EqualTo(before));
    }

    /// <summary>
    /// Золото переезжает с капом 25, излишек сгорает.
    /// </summary>
    /// <param name="gold">Золото на складе до переезда.</param>
    /// <param name="carried">Сколько золота останется после переезда.</param>
    [TestCase(100, RelocationManager.GoldCarryCap)]
    [TestCase(RelocationManager.GoldCarryCap, RelocationManager.GoldCarryCap)]
    [TestCase(4, 4)]
    public void RelocateCapsCarriedGoldTest(int gold, int carried)
    {
        var player = TestPlayer.Create()
            .AtRelocationThreshold()
            .WithResource(ResourceIds.Gold, gold);

        player.Relocate();

        Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(carried));
    }

    /// <summary>
    /// Репутация переезжает половиной очков у каждого соседа, а вехи пересчитываются из уполовиненных очков.
    /// </summary>
    [Test]
    public void RelocateHalvesReputationTest()
    {
        const int borovoePoints = 37;

        var player = TestPlayer.Create()
            .AtRelocationThreshold()
            .WithReputation(NeighborIds.Borovoe, borovoePoints);

        var zarechyePoints = App.Read(context => context.NeighborReputations.Single(x => x.PlayerId == player.Id && x.NeighborId == NeighborIds.Zarechye).Points);

        player.Relocate();

        var halvedZarechye = zarechyePoints / 2;
        var halvedBorovoe = borovoePoints / 2;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Reputation(player.Id, NeighborIds.Zarechye), Is.EqualTo(halvedZarechye));
            Assert.That(Reputation(player.Id, NeighborIds.Borovoe), Is.EqualTo(halvedBorovoe));
            Assert.That(player.GetVillageLevel().Reputation, Is.EqualTo(halvedZarechye / VillageLevelCalculator.ReputationPointsPerMilestone + halvedBorovoe / VillageLevelCalculator.ReputationPointsPerMilestone));
        }
    }

    /// <summary>
    /// Узелки памяти начисляются по обжитости шагом 50.
    /// </summary>
    /// <param name="level">Обжитость деревни на день отъезда.</param>
    /// <param name="knots">Сколько узелков даст такая обжитость.</param>
    [TestCase(UnlockLevel, 7)]
    [TestCase(UnlockLevel + LevelStep, 8)]
    [TestCase(VillageLevelCalculator.RelocationMaxUnlockLevel, 10)]
    [TestCase(LevelStep - 1, 0)]
    public void KnotsGrowWithVillageLevelTest(int level, int knots)
    {
        var player = TestPlayer.Create();

        Assert.That(player.Knots(level), Is.EqualTo(knots));
    }

    /// <summary>
    /// Полный набор артельных украс добавляет ровно один узелок, неполный – ни одного.
    /// </summary>
    [Test]
    public void KnotsCountFullArtisanSetOnceTest()
    {
        var partial = TestPlayer.Create()
            .WithDecor(DecorIds.CarvedGate)
            .WithDecor(DecorIds.CraneWell)
            .WithDecor(DecorIds.Gazebo);

        var full = TestPlayer.Create()
            .WithDecor(DecorIds.CarvedGate)
            .WithDecor(DecorIds.CraneWell)
            .WithDecor(DecorIds.Gazebo)
            .WithDecor(DecorIds.CarpPond);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(partial.Knots(UnlockLevel), Is.EqualTo(UnlockLevel / LevelStep));
            Assert.That(full.Knots(UnlockLevel), Is.EqualTo(UnlockLevel / LevelStep + 1));
        }
    }

    /// <summary>
    /// Каждая постройка на своём последнем уровне даёт узелок, но не больше трёх за все постройки.
    /// </summary>
    /// <param name="maxLevelBuildings">Сколько построек доведено до последнего уровня.</param>
    /// <param name="bonus">Сколько узелков они дадут.</param>
    [TestCase(0, 0)]
    [TestCase(2, 2)]
    [TestCase(5, RelocationManager.MaxLevelBuildingKnotsCap)]
    public void KnotsCapMaxLevelBuildingsTest(int maxLevelBuildings, int bonus)
    {
        var player = TestPlayer.Create();
        for (var i = 0; i < maxLevelBuildings; i++)
        {
            player.WithDomik(DomikIds.Market, MaxDomikLevel);
        }

        Assert.That(player.Knots(UnlockLevel), Is.EqualTo(UnlockLevel / LevelStep + bonus));
    }

    /// <summary>
    /// Порог следующего переезда растёт шагом 50 от 350 и упирается в потолок 500.
    /// </summary>
    /// <param name="relocationCount">Число уже совершённых переездов.</param>
    /// <param name="threshold">Порог обжитости для следующего переезда.</param>
    [TestCase(0, UnlockLevel)]
    [TestCase(1, UnlockLevel + LevelStep)]
    [TestCase(3, VillageLevelCalculator.RelocationMaxUnlockLevel)]
    [TestCase(9, VillageLevelCalculator.RelocationMaxUnlockLevel)]
    public void RelocationThresholdGrowsToCapTest(int relocationCount, int threshold)
    {
        Assert.That(VillageLevelCalculator.GetRelocationThreshold(relocationCount), Is.EqualTo(threshold));
    }

    /// <summary>
    /// После первого переезда порог вырастает: прежней обжитости уже не хватает, и отказ называет новое число.
    /// </summary>
    [Test]
    public void RelocateSecondTimeDemandsGrownThresholdTest()
    {
        var player = TestPlayer.Create()
            .AtRelocationThreshold()
            .Relocate()
            .BackdateRelocation(TimeSpan.FromDays(CooldownDays) + TimeSpan.FromSeconds(1))
            .SetReputationPoints(NeighborIds.Zarechye, UnlockLevel * VillageLevelCalculator.ReputationPointsPerMilestone / VillageLevelCalculator.ReputationWeight);

        var ex = Throws.Business(() => player.Relocate());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.GetVillageLevel().Level, Is.GreaterThanOrEqualTo(UnlockLevel));
            Assert.That(ex.Message, Is.EqualTo($"Переезд откроется на обжитости {UnlockLevel + LevelStep}"));
        }
    }

    /// <summary>
    /// Переезд оставляет строку на памятном столбе: имя, герб, долину, обжитость, узелки и срок жизни деревни.
    /// </summary>
    [Test]
    public void RelocateWritesMemorialRowTest()
    {
        const int livedDays = 30;

        var player = TestPlayer.Create()
            .AtRelocationThreshold()
            .SetVillageIdentity("Гнилые Броды", 2, 5)
            .BackdateVillageStart(TimeSpan.FromDays(livedDays));

        var level = player.GetVillageLevel().Level;
        var knots = player.Knots(level);
        player.Relocate(FirstValleyId, "Новые Броды");

        var post = player.MemorialPost();
        var left = post.Villages.Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(left.VillageName, Is.EqualTo("Гнилые Броды"));
            Assert.That(left.CrestIcon, Is.EqualTo(2));
            Assert.That(left.CrestColor, Is.EqualTo(5));
            Assert.That(left.ValleyId, Is.EqualTo(RelocationValleys.StartingValleyId));
            Assert.That(left.ValleyName, Is.EqualTo(RelocationValleys.Get(RelocationValleys.StartingValleyId).Name));
            Assert.That(player.Relocation().ValleyName, Is.EqualTo(RelocationValleys.Get(FirstValleyId).Name));
            Assert.That(left.Level, Is.EqualTo(level));
            Assert.That(left.Knots, Is.EqualTo(knots));
            Assert.That(left.LivedDays, Is.EqualTo(livedDays));
            Assert.That(post.LevelSum, Is.EqualTo(level));
            Assert.That(post.RelocationCount, Is.EqualTo(1));
            Assert.That(player.Village().VillageName, Is.EqualTo("Новые Броды"));
        }
    }

    /// <summary>
    /// Сборы обоза называют поимённо обе колонки: артель, чертежи, золото с капом и общим числом, монеты, припасы,
    /// постройки и казну новой деревни.
    /// </summary>
    [Test]
    public void RelocationPlanCountsBothColumnsTest()
    {
        const int coins = 700;
        const int clay = 30;
        const int stone = 12;
        const int gold = 90;
        const int startingBuildings = 2;

        var player = TestPlayer.Create()
            .AtRelocationThreshold()
            .WithBlueprint(BlueprintIds.Workshop)
            .WithDomik(DomikIds.Market)
            .WithResource(ResourceIds.Coin, coins)
            .WithResource(ResourceIds.Clay, clay)
            .WithResource(ResourceIds.Stone, stone)
            .WithResource(ResourceIds.Gold, gold);

        var workerCount = player.Workers().Count;
        var plan = player.Plan();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(plan.Summary.Workers, Is.EqualTo(workerCount));
            Assert.That(plan.Summary.Blueprints, Is.EqualTo(1));
            Assert.That(plan.Summary.Gold, Is.EqualTo(RelocationManager.GoldCarryCap));
            Assert.That(plan.Summary.GoldTotal, Is.EqualTo(gold));
            Assert.That(plan.Summary.Coins, Is.EqualTo(DomikManager.StartingCoins + coins));
            Assert.That(plan.Summary.Resources, Is.EqualTo(clay + stone));
            Assert.That(plan.Summary.Buildings, Is.EqualTo(startingBuildings + 1));
            Assert.That(plan.Summary.StartingCoins, Is.EqualTo(DomikManager.StartingCoins));
            Assert.That(plan.KnotsOnRelocate, Is.EqualTo(player.Knots(player.GetVillageLevel().Level)));
            Assert.That(plan.Valleys, Is.EqualTo(RelocationValleys.Choices));
        }
    }

    /// <summary>
    /// Узелки переезда ложатся в остаток и тратятся на ступени перков по цене справочника.
    /// </summary>
    [Test]
    public void BuyPerkSpendsKnotsTest()
    {
        var liftings = PerkManager.Perks.Single(x => x.Type == RelocationPerkType.Liftings);
        var player = TestPlayer.Create()
            .AtRelocationThreshold()
            .Relocate();

        var knots = player.Relocation().Knots;
        player.BuyPerk(RelocationPerkType.Liftings);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Relocation().Knots, Is.EqualTo(knots - liftings.Costs[0]));
            Assert.That(player.Relocation().Perks.Single(x => x.Type == RelocationPerkType.Liftings).Level, Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Ступень перка не купить без узелков, и лесенка каждого перка конечна.
    /// </summary>
    [Test]
    public void BuyPerkRejectsWithoutKnotsAndBeyondLastStepTest()
    {
        var longHabit = PerkManager.Perks.Single(x => x.Type == RelocationPerkType.LongHabit);
        var poor = TestPlayer.Create();

        var noKnots = Throws.Business(() => poor.BuyPerk(RelocationPerkType.LongHabit));
        Assert.That(noKnots.Message, Is.EqualTo($"Нужно {longHabit.Costs[0]} узелков памяти, есть 0"));

        var rich = TestPlayer.Create().WithKnots(longHabit.Costs.Sum() + longHabit.Costs[^1]);
        foreach (var _ in longHabit.Costs)
        {
            rich.BuyPerk(RelocationPerkType.LongHabit);
        }

        var beyond = Throws.Business(() => rich.BuyPerk(RelocationPerkType.LongHabit));
        Assert.That(beyond.Message, Is.EqualTo($"«{longHabit.Name}» – все ступени уже взяты"));
    }

    /// <summary>
    /// Подъёмные кладут в казну новой деревни по 500 монет за ступень сверх стартовых.
    /// </summary>
    [Test]
    public void LiftingsAddCoinsToNewVillageTest()
    {
        var liftings = PerkManager.Perks.Single(x => x.Type == RelocationPerkType.Liftings);
        var player = TestPlayer.Create()
            .WithKnots(liftings.Costs[0])
            .BuyPerk(RelocationPerkType.Liftings)
            .AtRelocationThreshold()
            .Relocate();

        Assert.That(player.Resource(ResourceIds.Coin), Is.EqualTo(DomikManager.StartingCoins + PerkManager.LiftingsCoinsPerStep));
    }

    /// <summary>
    /// Долгая привычка сокращает длительность смены на 5 процентов за ступень.
    /// </summary>
    [Test]
    public void LongHabitShortensManufactureTest()
    {
        const int ordinaryTraitId = 1;

        // ClayDig8h: 28800 -> Ceiling(28800 * 0.95) = 27360
        const int expectedDurationSeconds = 27360;

        var longHabit = PerkManager.Perks.Single(x => x.Type == RelocationPerkType.LongHabit);
        var player = TestPlayer.Create()
            .WithKnots(longHabit.Costs[0])
            .BuyPerk(RelocationPerkType.LongHabit);

        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, ordinaryTraitId);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h, [worker.Id]);
        }

        Assert.That(player.Manufacture(StartingDomikIds.ClayMine).DurationSeconds, Is.EqualTo(expectedDurationSeconds));
    }

    /// <summary>
    /// Запасная койка добавляет жителя деревне, но общий потолок артели <see cref="WorkerManager.MaxCapacity"/> не двигает:
    /// на восьми избах пятого уровня она упирается в него.
    /// </summary>
    [Test]
    public void SpareBunkAddsBedWithinArtelCapTest()
    {
        const int barracksForCap = 8;

        var spareBunk = PerkManager.Perks.Single(x => x.Type == RelocationPerkType.SpareBunk);
        var player = TestPlayer.Create();
        var baseCapacity = Capacity(player.Id);

        player.WithKnots(spareBunk.Costs[0]).BuyPerk(RelocationPerkType.SpareBunk);
        var withPerk = Capacity(player.Id);

        var capped = TestPlayer.Create();
        for (var i = 0; i < barracksForCap; i++)
        {
            capped.WithDomik(DomikIds.Barrack, MaxDomikLevel);
        }

        capped.WithKnots(spareBunk.Costs[0]).BuyPerk(RelocationPerkType.SpareBunk);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(withPerk, Is.EqualTo(baseCapacity + PerkManager.SpareBunkBedsPerStep));
            Assert.That(Capacity(capped.Id), Is.EqualTo(WorkerManager.MaxCapacity));
        }
    }

    /// <summary>
    /// После переезда трудяги остаются при игроке, но сверх коек пустого двора числятся в отходе и выходят
    /// на работу, как только койка появится.
    /// </summary>
    [Test]
    public void WorkersOverBedsGoAwayAfterRelocationTest()
    {
        var player = TestPlayer.Create()
            .AtRelocationThreshold();

        var workerCount = player.Workers().Count;
        player.Relocate();

        var afterRelocation = player.Workers();
        player.Buy(DomikIds.Barrack);
        var afterBarrack = player.Workers();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterRelocation, Has.Count.EqualTo(workerCount));
            Assert.That(afterRelocation.All(x => x.IsAway), Is.True);
            Assert.That(afterBarrack.Count(x => !x.IsAway), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Трудяга в отходе не встаёт на смену, и отказ называет причину – не хватает коек.
    /// </summary>
    [Test]
    public void AwayWorkerDoesNotTakeManufactureTest()
    {
        var player = TestPlayer.Create()
            .AtRelocationThreshold();

        HireArtel(player);
        player.Relocate()
            .WithDomik(DomikIds.ClayMine);

        var ex = Throws.Business(() => player.StartManufacture(FirstDomikId, ReceiptIds.ClayDig));

        Assert.That(ex.Message, Is.EqualTo(WorkerManager.AwayWorkersMessage));
    }

    /// <summary>
    /// Трудяга в отходе не уходит и в поход: Сторожка коек не даёт, а работы отходник не берёт.
    /// </summary>
    [Test]
    public void AwayWorkerDoesNotJoinExpeditionTest()
    {
        var player = TestPlayer.Create()
            .AtRelocationThreshold();

        HireArtel(player);
        player.Relocate()
            .WithDomik(DomikIds.ScoutHut);

        var ex = Throws.Business(() => player.StartExpedition(ExpeditionTypeIds.ShortScout));

        Assert.That(ex.Message, Is.EqualTo(WorkerManager.AwayWorkersMessage));
    }

    /// <summary>
    /// Переезд снимает все события игрока из планировщика, и снимает уже после коммита транзакции.
    /// </summary>
    [Test]
    public void RelocateClearsSchedulerEventsAfterCommitTest()
    {
        var player = TestPlayer.Create()
            .AtRelocationThreshold()
            .WithResource(ResourceIds.Clay, 100);

        int manufactureId;
        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig);
            manufactureId = player.Manufacture(StartingDomikIds.ClayMine).Id;
        }

        var recorder = new RecordingCalculator();
        using (var scope = App.Scope())
        {
            scope.Context.Players.Single(x => x.Id == player.Id).NextOrderRefillAt = DateTimeHelper.GetNowDate().AddMinutes(30);
            scope.Context.SaveChanges();
            var manager = new RelocationManager(scope.Get<UnitOfWork>(), scope.Context, recorder, scope.Get<ResourceManager>(),
                scope.Get<PlayerResourceManager>(), scope.Get<VillageLevelCalculator>(), scope.Get<PerkManager>(),
                scope.Get<PlayerEventManager>(), scope.Get<DomikManager>());

            manager.Relocate(player.Id, FirstValleyId, null);
            Assert.That(recorder.Removed, Is.Empty, "события снимаются только после коммита транзакции");
            scope.Commit();
        }

        Assert.That(recorder.Removed, Does.Contain((player.Id, manufactureId, CalculateTypes.Manufacture)));
        Assert.That(recorder.Removed, Does.Contain((player.Id, player.Id, CalculateTypes.OrderRefill)));
    }

    private static int Reputation(int playerId, int neighborId)
    {
        return App.Read(context => context.NeighborReputations
            .Where(x => x.PlayerId == playerId && x.NeighborId == neighborId)
            .Select(x => (int?)x.Points)
            .FirstOrDefault() ?? 0);
    }

    private static int Capacity(int playerId)
    {
        return App.Act<WorkerManager, int>(m => m.GetCapacity(playerId));
    }

    private static void HireArtel(TestPlayer player)
    {
        App.Act<WorkerManager>(m => m.EnsureWorkers(player.Id));
    }

    private static void OccupyWorker(TestPlayer player, int workerId, CalculateTypes busyKind)
    {
        using var scope = App.Scope();
        var date = DateTimeHelper.GetNowDate();
        var worker = scope.Context.Workers.Single(x => x.Id == workerId);
        switch (busyKind)
        {
            case CalculateTypes.Expedition:
                var expedition = new Expedition
                {
                    PlayerId = player.Id,
                    ExpeditionTypeId = ExpeditionTypeIds.ShortScout,
                    StartDate = date,
                    FinishDate = date.AddHours(1),
                };

                scope.Context.Expeditions.Add(expedition);
                scope.Context.SaveChanges();
                worker.ExpeditionId = expedition.Id;
                break;

            case CalculateTypes.Errand:
                var errand = new Errand
                {
                    PlayerId = player.Id,
                    NeighborId = NeighborIds.Zarechye,
                    TemplateId = 0,
                    ExpireDate = date.AddHours(1),
                };

                scope.Context.Errands.Add(errand);
                scope.Context.SaveChanges();
                worker.ErrandId = errand.Id;
                break;

            case CalculateTypes.Incident:
                var incident = new Incident
                {
                    PlayerId = player.Id,
                    SourceType = IncidentSourceType.Expedition,
                    MissingWorkerId = worker.Id,
                    ExpeditionTypeId = ExpeditionTypeIds.ShortScout,
                    TemplateId = 0,
                    CreateDate = date,
                };

                scope.Context.Incidents.Add(incident);
                scope.Context.SaveChanges();
                worker.IncidentId = incident.Id;
                break;
        }

        scope.Commit();
    }

    private static void SetZealCharges(int playerId, int charges)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == playerId).ZealCharges = charges;
        scope.Commit();
    }

    private static void SetWorkerSick(int workerId, DateTime sickUntil)
    {
        using var scope = App.Scope();
        var worker = scope.Context.Workers.Single(x => x.Id == workerId);
        worker.SickUntil = sickUntil;
        worker.SickTypeId = SickTypeIds.Cold;
        scope.Commit();
    }

    private static void AddMilestone(int workerId)
    {
        using var scope = App.Scope();
        scope.Context.WorkerMilestones.Add(new()
        {
            WorkerId = workerId,
            MilestoneType = WorkerMilestoneType.FirstShift,
            GrantDate = DateTimeHelper.GetNowDate(),
        });

        scope.Commit();
    }

    private static void AddJournalEntry(int playerId)
    {
        using var scope = App.Scope();
        scope.Context.GuestbookEntries.Add(new()
        {
            HostPlayerId = playerId,
            GuestPlayerId = playerId,
            PhraseId = 1,
            Date = DateTimeHelper.GetNowDate(),
        });

        scope.Commit();
    }

    private static void AddConvoyWindow(int playerId)
    {
        using var scope = App.Scope();
        scope.Context.NeighborConvoys.Add(new()
        {
            PlayerId = playerId,
            NeighborId = NeighborIds.Glinischi,
            WindowStartDate = DateTimeHelper.GetNowDate(),
            BoughtCount = 1,
        });

        scope.Commit();
    }

    private static void SetFriendNeighbor(int playerId, int neighborId)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == playerId).FriendNeighborId = neighborId;
        scope.Commit();
    }

    private static void SetCloakWear(int playerId, int wearPoints)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == playerId).CloakWearPoints = wearPoints;
        scope.Commit();
    }

    private static void AddReserve(int playerId)
    {
        using var scope = App.Scope();
        scope.Context.PlayerResourceReserves.Add(new()
        {
            PlayerId = playerId,
            ResourceTypeId = ResourceIds.Clay,
            Reserve = 10,
        });

        scope.Commit();
    }

    private static void AddLedgerRows(int playerId)
    {
        using var scope = App.Scope();
        var date = DateTimeHelper.GetNowDate().Date;
        scope.Context.PlayerResourceFlows.Add(new()
        {
            PlayerId = playerId,
            Date = date,
            ResourceTypeId = ResourceIds.Clay,
            Gained = 10,
            Spent = 2,
        });

        scope.Context.PlayerLaborDays.Add(new()
        {
            PlayerId = playerId,
            Date = date,
            WorkedSeconds = 100,
        });

        scope.Commit();
    }

    private sealed class RecordingCalculator : ICalculator
    {
        public List<(int PlayerId, long ObjectId, CalculateTypes Type)> Removed { get; } = [];

        public void CheckInit()
        {
        }

        public void Insert(CalculateInfo cData)
        {
        }

        public void Remove(int playerId, long objectId, CalculateTypes type)
        {
            Removed.Add((playerId, objectId, type));
        }

        public void Reschedule(int playerId, long objectId, CalculateTypes type, DateTime newDate)
        {
        }
    }
}
