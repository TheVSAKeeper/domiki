using Domiki.Web.Core;
using Domiki.Web.Infrastructure;
using Domiki.Web.Village;

namespace Domiki.Web.Tests;

[NonParallelizable]
public sealed class VillageProfileTests
{
    private const int OrdinaryTraitId = 1;
    private const int DiligentTraitId = 3;
    private const int MaxSkillUses = 1000;

    [TearDown]
    public void TearDown()
    {
        ClearWeatherSchedule();
    }

    /// <summary>
    /// Принять уклад нельзя ниже обжитости <see cref="VillageProfileManager.VillageLevelRequirement"/>.
    /// </summary>
    [Test]
    public void SetVillageProfileRejectsBelowVillageLevelTest()
    {
        var player = TestPlayer.Create();

        var ex = Throws.Business(() => player.SetVillageProfile(NeighborIds.Zarechye));

        Assert.That(ex.Message, Is.EqualTo($"Уклад деревни откроется на обжитости {VillageProfileManager.VillageLevelRequirement}"));
    }

    /// <summary>
    /// При обжитости не ниже порога, но репутации у соседа меньше <see cref="VillageProfileManager.ReputationRequirement"/>, уклад не принимается.
    /// </summary>
    [Test]
    public void SetVillageProfileRejectsBelowReputationTest()
    {
        var player = TestPlayer.Create()
            .RaiseVillageLevel(VillageProfileManager.VillageLevelRequirement);

        var ex = Throws.Business(() => player.SetVillageProfile(NeighborIds.Zarechye));

        Assert.That(ex.Message, Is.EqualTo($"Для уклада нужно доброе имя {VillageProfileManager.ReputationRequirement} у этого соседа"));
    }

    /// <summary>
    /// Уклад нельзя принять у соседа, для которого в справочнике нет ни одной строки уклада специализации, – это отдельная
    /// ошибка, а не молчаливый профиль без эффекта.
    /// </summary>
    [Test]
    public void SetVillageProfileRejectsNeighborWithoutProfileRowsTest()
    {
        const int neighborIdWithoutProfile = 999999;

        var player = TestPlayer.Create();

        var ex = Throws.Business(() => player.SetVillageProfile(neighborIdWithoutProfile));

        Assert.That(ex.Message, Is.EqualTo("У этого соседа нет своего уклада"));
    }

    /// <summary>
    /// Гейты уклада читаются из БД напрямую, а не из уже отслеживаемой в scope-е сущности игрока: кулдаун срабатывает,
    /// даже если Player был затрекан ДО того, как предыдущая смена уклада зафиксировала новую дату в другой транзакции
    /// (двойной сабмит, воспроизводимый через <see cref="DomikManager.GetPlayerId"/>-подобное предварительное отслеживание).
    /// </summary>
    [Test]
    public void SetVillageProfileReadsGatesFreshEvenWhenPlayerAlreadyTrackedTest()
    {
        var player = TestPlayer.Create()
            .RaiseVillageLevel(VillageProfileManager.VillageLevelRequirement)
            .WithReputation(NeighborIds.Zarechye, VillageProfileManager.ReputationRequirement)
            .WithReputation(NeighborIds.Borovoe, VillageProfileManager.ReputationRequirement);

        using var staleScope = App.Scope();
        staleScope.Context.Players.Single(x => x.Id == player.Id);

        player.SetVillageProfile(NeighborIds.Zarechye);

        var ex = Throws.Business(() => staleScope.Get<VillageProfileManager>().SetVillageProfile(player.Id, NeighborIds.Borovoe));

        Assert.That(ex.Message, Is.EqualTo($"Сменить уклад можно не чаще раза в {VillageProfileManager.ProfileChangeCooldownDays} суток"));
    }

    /// <summary>
    /// При достаточной обжитости и репутации уклад принимается: записываются сосед и момент принятия.
    /// </summary>
    [Test]
    public void SetVillageProfileAcceptsWithLevelAndReputationTest()
    {
        var player = TestPlayer.Create()
            .RaiseVillageLevel(VillageProfileManager.VillageLevelRequirement)
            .WithReputation(NeighborIds.Zarechye, VillageProfileManager.ReputationRequirement);

        var date = DateTimeHelper.GetNowDate();
        player.SetVillageProfile(NeighborIds.Zarechye);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Village().ProfileNeighborId, Is.EqualTo(NeighborIds.Zarechye));
            Assert.That(ProfileChangedDate(player.Id), Is.EqualTo(date).Within(TimeSpan.FromSeconds(2)));
        }
    }

    /// <summary>
    /// Повторное принятие уже принятого уклада отклоняется как самостоятельная ошибка, а не тратит кулдаун смены.
    /// </summary>
    [Test]
    public void SetVillageProfileSameProfileAgainThrowsTest()
    {
        var player = TestPlayer.Create()
            .RaiseVillageLevel(VillageProfileManager.VillageLevelRequirement)
            .WithReputation(NeighborIds.Zarechye, VillageProfileManager.ReputationRequirement)
            .SetVillageProfile(NeighborIds.Zarechye);

        var ex = Throws.Business(() => player.SetVillageProfile(NeighborIds.Zarechye));

        Assert.That(ex.Message, Is.EqualTo("Этот уклад уже принят"));
    }

    /// <summary>
    /// Сменить уклад на другого соседа нельзя раньше <see cref="VillageProfileManager.ProfileChangeCooldownDays"/> суток
    /// после предыдущей смены, а по истечении кулдауна смена проходит.
    /// </summary>
    [Test]
    public void SetVillageProfileChangeRespectsCooldownTest()
    {
        var player = TestPlayer.Create()
            .RaiseVillageLevel(VillageProfileManager.VillageLevelRequirement)
            .WithReputation(NeighborIds.Zarechye, VillageProfileManager.ReputationRequirement)
            .WithReputation(NeighborIds.Borovoe, VillageProfileManager.ReputationRequirement)
            .SetVillageProfile(NeighborIds.Zarechye);

        var tooEarly = Throws.Business(() => player.SetVillageProfile(NeighborIds.Borovoe));
        Assert.That(tooEarly.Message, Is.EqualTo($"Сменить уклад можно не чаще раза в {VillageProfileManager.ProfileChangeCooldownDays} суток"));

        BackdateProfileChange(player.Id, TimeSpan.FromDays(VillageProfileManager.ProfileChangeCooldownDays) + TimeSpan.FromSeconds(1));
        player.SetVillageProfile(NeighborIds.Borovoe);

        Assert.That(player.Village().ProfileNeighborId, Is.EqualTo(NeighborIds.Borovoe));
    }

    /// <summary>
    /// После принятия уклада деревня сможет сменить его снова ровно через <see cref="VillageProfileManager.ProfileChangeCooldownDays"/> суток.
    /// </summary>
    [Test]
    public void SetVillageProfileSetsProfileChangeAvailableDateAfterCooldownTest()
    {
        var player = TestPlayer.Create()
            .RaiseVillageLevel(VillageProfileManager.VillageLevelRequirement)
            .WithReputation(NeighborIds.Zarechye, VillageProfileManager.ReputationRequirement);

        var date = DateTimeHelper.GetNowDate();
        player.SetVillageProfile(NeighborIds.Zarechye);

        var expectedAvailableDate = date.AddDays(VillageProfileManager.ProfileChangeCooldownDays);
        Assert.That(player.Village().ProfileChangeAvailableDate, Is.EqualTo(expectedAvailableDate).Within(TimeSpan.FromSeconds(2)));
    }

    /// <summary>
    /// Уклад деревни появляется в дорожной карте обжитости как открытие механики на пороге <see cref="VillageProfileManager.VillageLevelRequirement"/>.
    /// </summary>
    [Test]
    public void UnlockRoadmapContainsVillageProfileAtRequiredLevelTest()
    {
        var player = TestPlayer.Create();

        var unlock = player.GetVillageLevel().Unlocks.Single(x => x.LogicName == "village_profile");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unlock.Label, Is.EqualTo("Уклад деревни"));
            Assert.That(unlock.Level, Is.EqualTo(VillageProfileManager.VillageLevelRequirement));
            Assert.That(unlock.Kind, Is.EqualTo("feature"));
        }
    }

    /// <summary>
    /// Уклад сокращает длительность производства в постройке своей специализации на 15 процентов.
    /// </summary>
    [Test]
    public void StartManufactureAppliesProfileDiscountInSpecializationBuildingTest()
    {
        // Ceiling(MakeIron.DurationSeconds(1800) * 85 / 100.0)
        const int expectedDurationSeconds = 1530;

        var player = TestPlayer.Create()
            .RaiseVillageLevel(VillageProfileManager.VillageLevelRequirement)
            .WithReputation(NeighborIds.Zarechye, VillageProfileManager.ReputationRequirement)
            .SetVillageProfile(NeighborIds.Zarechye)
            .WithDomik(DomikIds.Forge)
            .WithResource(ResourceIds.Ore, 10);

        var domikId = player.DomikId(DomikIds.Forge);
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, OrdinaryTraitId);

        using (App.PendingEvents())
        {
            player.StartManufacture(domikId, ReceiptIds.MakeIron, [worker.Id]);
        }

        Assert.That(player.Manufacture(domikId).DurationSeconds, Is.EqualTo(expectedDurationSeconds));
    }

    /// <summary>
    /// Уклад не трогает длительность производства в постройке, не входящей в специализацию выбранного соседа.
    /// </summary>
    [Test]
    public void StartManufactureIgnoresProfileOutsideSpecializationBuildingTest()
    {
        const int expectedDurationSeconds = 3600;

        var player = TestPlayer.Create()
            .RaiseVillageLevel(VillageProfileManager.VillageLevelRequirement)
            .WithReputation(NeighborIds.Zarechye, VillageProfileManager.ReputationRequirement)
            .SetVillageProfile(NeighborIds.Zarechye)
            .WithDomik(DomikIds.LumberMill);

        var domikId = player.DomikId(DomikIds.LumberMill);
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, OrdinaryTraitId);

        using (App.PendingEvents())
        {
            player.StartManufacture(domikId, ReceiptIds.WoodDig, [worker.Id]);
        }

        Assert.That(player.Manufacture(domikId).DurationSeconds, Is.EqualTo(expectedDurationSeconds));
    }

    /// <summary>
    /// Даже худший стек черты, навыка и уклада не опускает длительность ниже общего клампа 0.6 базовой длительности.
    /// </summary>
    [Test]
    public void StartManufactureClampHoldsWithTraitSkillAndProfileStackedTest()
    {
        // stone_dig: 3600 -> Работящий (-20%) 2880 -> навык-кап (-15%) 2448 -> уклад (85%) 2081, что ниже клампа
        // Ceiling(3600 * 0.6) = 2160 - итог держится клампом, а не падает до 2081
        const int expectedFlooredDurationSeconds = 2160;

        var player = TestPlayer.Create()
            .RaiseVillageLevel(VillageProfileManager.VillageLevelRequirement)
            .WithReputation(NeighborIds.Zarechye, VillageProfileManager.ReputationRequirement)
            .SetVillageProfile(NeighborIds.Zarechye)
            .WithDomik(DomikIds.StoneMine);

        var domikId = player.DomikId(DomikIds.StoneMine);
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, DiligentTraitId);
        player.SetWorkerSkill(worker.Id, DomikIds.StoneMine, MaxSkillUses);

        using (App.PendingEvents())
        {
            player.StartManufacture(domikId, ReceiptIds.StoneDig, [worker.Id]);
        }

        Assert.That(player.Manufacture(domikId).DurationSeconds, Is.EqualTo(expectedFlooredDurationSeconds));
    }

    /// <summary>
    /// Уклад ускоряет только длительность: зафиксированные при старте шанс хвори и процент выхода не меняются от его наличия.
    /// </summary>
    [Test]
    public void StartManufactureProfileDoesNotAffectOutputPercentOrSickChanceTest()
    {
        const int expectedSickChance = 15;

        var baseline = TestPlayer.Create()
            .RaiseVillageLevel(DomikManager.SickMinVillageLevel);

        var profiled = TestPlayer.Create()
            .RaiseVillageLevel(VillageProfileManager.VillageLevelRequirement)
            .WithReputation(NeighborIds.Glinischi, VillageProfileManager.ReputationRequirement)
            .SetVillageProfile(NeighborIds.Glinischi);

        var baselineWorker = baseline.Workers().Single();
        baseline.SetWorkerTrait(baselineWorker.Id, OrdinaryTraitId);

        var profiledWorker = profiled.Workers().Single();
        profiled.SetWorkerTrait(profiledWorker.Id, OrdinaryTraitId);

        SetWeather(WeatherIds.Rain);

        using (App.PendingEvents())
        {
            baseline.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
            profiled.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        var baselineManufacture = baseline.Manufacture(StartingDomikIds.ClayMine);
        var profiledManufacture = profiled.Manufacture(StartingDomikIds.ClayMine);
        var baselineSickChance = GetManufactureSickChance(baselineManufacture.Id);
        var profiledSickChance = GetManufactureSickChance(profiledManufacture.Id);
        var baselineOutputPercent = GetManufactureOutputPercent(baselineManufacture.Id);
        var profiledOutputPercent = GetManufactureOutputPercent(profiledManufacture.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(baselineSickChance, Is.EqualTo(expectedSickChance));
            Assert.That(profiledSickChance, Is.EqualTo(baselineSickChance));
            Assert.That(profiledOutputPercent, Is.EqualTo(baselineOutputPercent));
            Assert.That(profiledManufacture.DurationSeconds, Is.LessThan(baselineManufacture.DurationSeconds));
        }
    }

    private static int GetManufactureSickChance(int manufactureId)
    {
        return App.Read(context => context.Manufactures.Single(x => x.Id == manufactureId).SickChance);
    }

    private static int GetManufactureOutputPercent(int manufactureId)
    {
        return App.Read(context => context.Manufactures.Single(x => x.Id == manufactureId).OutputPercent);
    }

    private static DateTime? ProfileChangedDate(int playerId)
    {
        return App.Read(context => context.Players.Where(x => x.Id == playerId).Select(x => x.ProfileChangedDate).Single());
    }

    private static void BackdateProfileChange(int playerId, TimeSpan age)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == playerId).ProfileChangedDate = DateTimeHelper.GetNowDate() - age;
        scope.Commit();
    }

    private static void SetWeather(int weatherTypeId)
    {
        ClearWeatherSchedule();
        var now = DateTimeHelper.GetNowDate();
        using var scope = App.Scope();
        scope.Context.WeatherPeriods.Add(new()
        {
            WeatherTypeId = weatherTypeId,
            StartDate = now,
            EndDate = now.AddSeconds(WeatherManager.WeatherPeriodSeconds),
        });

        scope.Commit();
    }

    private static void ClearWeatherSchedule()
    {
        using var scope = App.Scope();
        scope.Context.WeatherPeriods.RemoveRange(scope.Context.WeatherPeriods);
        scope.Commit();
    }
}
