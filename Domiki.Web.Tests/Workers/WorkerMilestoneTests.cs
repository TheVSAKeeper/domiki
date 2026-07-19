using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Infrastructure.Models;
using Domiki.Web.Reference;
using Domiki.Web.Workers;
using System.Text.Json;
using Trait = Domiki.Web.Workers.Models.Trait;

namespace Domiki.Web.Tests;

[NonParallelizable]
public sealed class WorkerMilestoneTests
{
    /// <summary>
    /// Сумма использований открывает первую смену ровно на единице, а сотую смену – ровно на ста после уже выданной первой.
    /// </summary>
    /// <param name="uses">Сумма использований навыков трудяги.</param>
    /// <param name="milestoneType">Веха, для которой проверяется порог.</param>
    /// <param name="firstShiftGranted">Первая смена уже выдана для проверки сотой.</param>
    /// <param name="expectedGranted">Ожидается ли выдача вехи.</param>
    [TestCase(0, WorkerMilestoneType.FirstShift, false, false)]
    [TestCase(WorkerMilestoneManager.FirstShiftThreshold, WorkerMilestoneType.FirstShift, false, true)]
    [TestCase(WorkerMilestoneManager.HundredthShiftThreshold - 1, WorkerMilestoneType.HundredthShift, true, false)]
    [TestCase(WorkerMilestoneManager.HundredthShiftThreshold, WorkerMilestoneType.HundredthShift, true, true)]
    public void TotalUsesThresholdTest(int uses, WorkerMilestoneType milestoneType, bool firstShiftGranted, bool expectedGranted)
    {
        const int villageLevel = WorkerMilestoneManager.WorkerMilestoneUnlockLevel;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.ClayMine);
        var worker = player.Workers().Single();
        var now = DateTimeHelper.GetNowDate();
        if (milestoneType == WorkerMilestoneType.HundredthShift && expectedGranted == false)
        {
            const int skillUses = 33;
            player.SetWorkerSkill(worker.Id, DomikIds.ClayMine, skillUses)
                .SetWorkerSkill(worker.Id, DomikIds.StoneMine, skillUses)
                .SetWorkerSkill(worker.Id, DomikIds.LumberMill, skillUses);
        }
        else
        {
            player.SetWorkerSkill(worker.Id, DomikIds.ClayMine, uses);
        }
        if (firstShiftGranted)
        {
            player.GrantWorkerMilestone(worker.Id, WorkerMilestoneType.FirstShift, now);
        }

        player.TryGrantWorkerMilestone(villageLevel, now);

        var recap = player.TakeRecap(now);
        if (expectedGranted)
        {
            var milestone = recap.Events.Single(x => x.Type == PlayerEventType.WorkerMilestone).Data;
            var productionResourcePool = new[] { ResourceIds.Clay };
            using (Assert.EnterMultipleScope())
            {
                Assert.That(milestone.GetProperty("milestoneType").GetInt32(), Is.EqualTo((int)milestoneType));
                Assert.That(productionResourcePool, Does.Contain(milestone.GetProperty("resourceTypeId").GetInt32()));
                Assert.That(milestone.GetProperty("value").GetInt32(), Is.GreaterThan(0));
            }
        }
        else
        {
            Assert.That(recap.Events.Select(x => x.Type), Does.Not.Contain(PlayerEventType.WorkerMilestone));
        }
    }

    /// <summary>
    /// Набитая рука у трудяги с обычной чертой меняет черту вместо выдачи находки.
    /// </summary>
    [Test]
    public void OrdinaryTraitUpgradesOnSkilledHandTest()
    {
        const int villageLevel = WorkerMilestoneManager.WorkerMilestoneUnlockLevel;

        var ordinaryTrait = TraitByLogicName("ordinary");
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.ClayMine);
        var worker = player.Workers().Single();
        var now = DateTimeHelper.GetNowDate();
        player.SetWorkerTrait(worker.Id, ordinaryTrait.Id)
            .SetWorkerSkill(worker.Id, DomikIds.ClayMine, WorkerMilestoneManager.SkilledHandThreshold)
            .GrantWorkerMilestone(worker.Id, WorkerMilestoneType.FirstShift, now)
            .TryGrantWorkerMilestone(villageLevel, now);

        var milestone = player.TakeWorkerMilestone(now);
        var updatedWorker = player.Workers().Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(milestone.GetProperty("milestoneType").GetInt32(), Is.EqualTo((int)WorkerMilestoneType.SkilledHand));
            Assert.That(milestone.GetProperty("traitUpgraded").GetBoolean(), Is.True);
            Assert.That(milestone.GetProperty("resourceTypeId").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(milestone.GetProperty("value").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(milestone.GetProperty("newTrait").GetString(), Is.Not.Null.And.Not.Empty);
            Assert.That(milestone.GetProperty("newTraitLogicName").GetString(), Is.Not.EqualTo("ordinary").And.Not.Null);
            Assert.That(updatedWorker.Trait.Id, Is.Not.EqualTo(ordinaryTrait.Id));
        }
    }

    /// <summary>
    /// Набитая рука у трудяги с необычной чертой приносит находку тройной рыночной ценности.
    /// </summary>
    [Test]
    public void NonOrdinaryTraitFindsTripleRewardOnSkilledHandTest()
    {
        const int villageLevel = WorkerMilestoneManager.WorkerMilestoneUnlockLevel;
        const int findMultiplier = 3;

        var nonOrdinaryTrait = Traits().First(x => x.LogicName != "ordinary");
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.ClayMine);
        var worker = player.Workers().Single();
        var now = DateTimeHelper.GetNowDate();
        player.SetWorkerTrait(worker.Id, nonOrdinaryTrait.Id)
            .SetWorkerSkill(worker.Id, DomikIds.ClayMine, WorkerMilestoneManager.SkilledHandThreshold)
            .GrantWorkerMilestone(worker.Id, WorkerMilestoneType.FirstShift, now)
            .TryGrantWorkerMilestone(villageLevel, now);

        var milestone = player.TakeWorkerMilestone(now);
        var resourceTypeId = milestone.GetProperty("resourceTypeId").GetInt32();
        var value = milestone.GetProperty("value").GetInt32();
        // Ценность находки пересчитывается в количество ресурса по его рыночной стоимости.
        var expectedValue = Math.Max(1, (int)Math.Round(WorkerMilestoneManager.WorkerMilestoneFindBaseValue * findMultiplier * (double)ResourceManager.BaseMarketValue / ResourceManager.GetMarketValue(resourceTypeId), MidpointRounding.AwayFromZero));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(milestone.GetProperty("milestoneType").GetInt32(), Is.EqualTo((int)WorkerMilestoneType.SkilledHand));
            Assert.That(milestone.GetProperty("traitUpgraded").GetBoolean(), Is.False);
            Assert.That(value, Is.EqualTo(expectedValue));
            Assert.That(player.Resource(resourceTypeId), Is.EqualTo(value));
        }
    }

    /// <summary>
    /// Тридцать суток службы в бараке дают веху, а на двадцать девятые сутки она ещё не выдаётся.
    /// </summary>
    /// <param name="daysSinceHire">Полных суток после найма.</param>
    /// <param name="expectedGranted">Ожидается ли выдача вехи.</param>
    [TestCase(WorkerMilestoneManager.MonthInBarracksDays - 1, false)]
    [TestCase(WorkerMilestoneManager.MonthInBarracksDays, true)]
    public void MonthInBarracksThresholdTest(int daysSinceHire, bool expectedGranted)
    {
        const int villageLevel = WorkerMilestoneManager.WorkerMilestoneUnlockLevel;
        var baseResourcePool = new[] { ResourceIds.Stone, ResourceIds.Wood, ResourceIds.Clay, ResourceIds.Grain, ResourceIds.Ore };

        var player = TestPlayer.Create();
        var worker = player.Workers().Single();
        var now = DateTimeHelper.GetNowDate();
        player.SetWorkerHireDate(worker.Id, now.AddDays(-daysSinceHire))
            .TryGrantWorkerMilestone(villageLevel, now);

        var recap = player.TakeRecap(now);
        if (expectedGranted)
        {
            var milestone = recap.Events.Single(x => x.Type == PlayerEventType.WorkerMilestone).Data;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(milestone.GetProperty("milestoneType").GetInt32(), Is.EqualTo((int)WorkerMilestoneType.MonthInBarracks));
                Assert.That(baseResourcePool, Does.Contain(milestone.GetProperty("resourceTypeId").GetInt32()));
                Assert.That(milestone.GetProperty("value").GetInt32(), Is.GreaterThan(0));
            }
        }
        else
        {
            Assert.That(recap.Events.Select(x => x.Type), Does.Not.Contain(PlayerEventType.WorkerMilestone));
        }
    }

    /// <summary>
    /// Два трудяги с двадцатью пятью сменами в одном деле получают одну общую веху и отмечаются оба.
    /// </summary>
    [Test]
    public void TwoAtBenchMarksBothWorkersTest()
    {
        const int villageLevel = WorkerMilestoneManager.WorkerMilestoneUnlockLevel;

        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 1)
            .WithDomik(DomikIds.ClayMine);
        var workers = player.Workers();
        var firstWorker = workers[0];
        var secondWorker = workers[1];
        var now = DateTimeHelper.GetNowDate();
        player.SetWorkerSkill(firstWorker.Id, DomikIds.ClayMine, WorkerMilestoneManager.TwoAtBenchThreshold)
            .SetWorkerSkill(secondWorker.Id, DomikIds.ClayMine, WorkerMilestoneManager.TwoAtBenchThreshold)
            .GrantWorkerMilestone(firstWorker.Id, WorkerMilestoneType.FirstShift, now)
            .GrantWorkerMilestone(secondWorker.Id, WorkerMilestoneType.FirstShift, now)
            .TryGrantWorkerMilestone(villageLevel, now);

        var milestone = player.TakeWorkerMilestone(now);
        var secondGrantDate = now.AddHours(WorkerMilestoneManager.WorkerMilestoneCooldownHours);
        player.TryGrantWorkerMilestone(villageLevel, secondGrantDate);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(milestone.GetProperty("milestoneType").GetInt32(), Is.EqualTo((int)WorkerMilestoneType.TwoAtBench));
            Assert.That(milestone.GetProperty("workerId").GetInt32(), Is.EqualTo(firstWorker.Id));
            Assert.That(milestone.GetProperty("workerName").GetString(), Is.EqualTo(firstWorker.Name));
            Assert.That(milestone.GetProperty("workerId2").GetInt32(), Is.EqualTo(secondWorker.Id));
            Assert.That(milestone.GetProperty("workerName2").GetString(), Is.EqualTo(secondWorker.Name));
            Assert.That(PlayerMilestones(firstWorker.Id), Does.Contain(WorkerMilestoneType.TwoAtBench));
            Assert.That(PlayerMilestones(secondWorker.Id), Does.Contain(WorkerMilestoneType.TwoAtBench));
            Assert.That(WorkerMilestoneEventCount(player.Id), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Третьему трудяге достаётся новая общая веха с уже отмеченным напарником без повторного приза для него.
    /// </summary>
    [Test]
    public void ThirdWorkerFindsMarkedPartnerWithoutMirrorRewardTest()
    {
        const int villageLevel = WorkerMilestoneManager.WorkerMilestoneUnlockLevel;

        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2)
            .WithDomik(DomikIds.ClayMine);
        var workers = player.Workers();
        var firstWorker = workers[0];
        var secondWorker = workers[1];
        var thirdWorker = workers[2];
        var now = DateTimeHelper.GetNowDate();
        foreach (var worker in workers)
        {
            player.SetWorkerSkill(worker.Id, DomikIds.ClayMine, WorkerMilestoneManager.TwoAtBenchThreshold)
                .GrantWorkerMilestone(worker.Id, WorkerMilestoneType.FirstShift, now);
        }

        player.TryGrantWorkerMilestone(villageLevel, now);
        player.TakeWorkerMilestone(now);
        var secondGrantDate = now.AddHours(WorkerMilestoneManager.WorkerMilestoneCooldownHours);
        player.TryGrantWorkerMilestone(villageLevel, secondGrantDate);

        var milestone = player.TakeWorkerMilestone(secondGrantDate);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(milestone.GetProperty("workerId").GetInt32(), Is.EqualTo(thirdWorker.Id));
            Assert.That(milestone.GetProperty("workerId2").GetInt32(), Is.EqualTo(firstWorker.Id));
            Assert.That(PlayerMilestones(firstWorker.Id).Count(x => x == WorkerMilestoneType.TwoAtBench), Is.EqualTo(1));
            Assert.That(PlayerMilestones(secondWorker.Id).Count(x => x == WorkerMilestoneType.TwoAtBench), Is.EqualTo(1));
            Assert.That(PlayerMilestones(thirdWorker.Id).Count(x => x == WorkerMilestoneType.TwoAtBench), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Десятый завершённый поход открывает веху, а девятый ещё не открывает её.
    /// </summary>
    /// <param name="expeditionCount">Число завершённых походов.</param>
    /// <param name="expectedGranted">Ожидается ли выдача вехи.</param>
    [TestCase(WorkerMilestoneManager.TenthRoadThreshold - 1, false)]
    [TestCase(WorkerMilestoneManager.TenthRoadThreshold, true)]
    public void TenthRoadThresholdTest(int expeditionCount, bool expectedGranted)
    {
        const int villageLevel = WorkerMilestoneManager.WorkerMilestoneUnlockLevel;

        var player = TestPlayer.Create();
        var worker = player.Workers().Single();
        var now = DateTimeHelper.GetNowDate();
        player.SetWorkerExpeditionCount(worker.Id, expeditionCount)
            .TryGrantWorkerMilestone(villageLevel, now);

        var recap = player.TakeRecap(now);
        if (expectedGranted)
        {
            var milestone = recap.Events.Single(x => x.Type == PlayerEventType.WorkerMilestone).Data;
            Assert.That(milestone.GetProperty("milestoneType").GetInt32(), Is.EqualTo((int)WorkerMilestoneType.TenthRoad));
        }
        else
        {
            Assert.That(recap.Events.Select(x => x.Type), Does.Not.Contain(PlayerEventType.WorkerMilestone));
        }
    }

    /// <summary>
    /// Уже выданная веха не выдаётся второй раз после ручного окончания кулдауна.
    /// </summary>
    [Test]
    public void GrantedMilestoneIsOneShotTest()
    {
        const int villageLevel = WorkerMilestoneManager.WorkerMilestoneUnlockLevel;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.ClayMine);
        var worker = player.Workers().Single();
        var now = DateTimeHelper.GetNowDate();
        player.SetWorkerSkill(worker.Id, DomikIds.ClayMine, WorkerMilestoneManager.FirstShiftThreshold)
            .TryGrantWorkerMilestone(villageLevel, now);
        player.TakeWorkerMilestone(now);
        player.SetLastWorkerMilestoneDate(now.AddHours(-WorkerMilestoneManager.WorkerMilestoneCooldownHours))
            .TryGrantWorkerMilestone(villageLevel, now);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(WorkerMilestoneEventCount(player.Id), Is.EqualTo(1));
            Assert.That(PlayerMilestones(worker.Id).Count(x => x == WorkerMilestoneType.FirstShift), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Достигнутые вехи ждут окончания сорокавосьмичасового кулдауна и выдаются по одной в порядке типа.
    /// </summary>
    [Test]
    public void CooldownKeepsMilestoneQueueTest()
    {
        const int villageLevel = WorkerMilestoneManager.WorkerMilestoneUnlockLevel;
        const int cooldownHours = WorkerMilestoneManager.WorkerMilestoneCooldownHours;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.ClayMine);
        var worker = player.Workers().Single();
        var now = DateTimeHelper.GetNowDate();
        player.SetWorkerSkill(worker.Id, DomikIds.ClayMine, WorkerMilestoneManager.FirstShiftThreshold)
            .SetWorkerHireDate(worker.Id, now.AddDays(-WorkerMilestoneManager.MonthInBarracksDays))
            .TryGrantWorkerMilestone(villageLevel, now);

        var firstMilestone = player.TakeWorkerMilestone(now);
        player.TryGrantWorkerMilestone(villageLevel, now.AddHours(cooldownHours - 1));
        var blockedRecap = player.TakeRecap(now.AddHours(cooldownHours - 1));
        player.TryGrantWorkerMilestone(villageLevel, now.AddHours(cooldownHours));

        var secondMilestone = player.TakeWorkerMilestone(now.AddHours(cooldownHours));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstMilestone.GetProperty("milestoneType").GetInt32(), Is.EqualTo((int)WorkerMilestoneType.FirstShift));
            Assert.That(blockedRecap.Events.Select(x => x.Type), Does.Not.Contain(PlayerEventType.WorkerMilestone));
            Assert.That(secondMilestone.GetProperty("milestoneType").GetInt32(), Is.EqualTo((int)WorkerMilestoneType.MonthInBarracks));
        }
    }

    /// <summary>
    /// Веха не выдаётся до восьмой обжитости и выдаётся сразу после достижения восьмой.
    /// </summary>
    [Test]
    public void VillageLevelGateTest()
    {
        const int lockedVillageLevel = WorkerMilestoneManager.WorkerMilestoneUnlockLevel - 1;
        const int unlockedVillageLevel = WorkerMilestoneManager.WorkerMilestoneUnlockLevel;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.ClayMine);
        var worker = player.Workers().Single();
        var now = DateTimeHelper.GetNowDate();
        player.SetWorkerSkill(worker.Id, DomikIds.ClayMine, WorkerMilestoneManager.FirstShiftThreshold)
            .TryGrantWorkerMilestone(lockedVillageLevel, now);

        var lockedRecap = player.TakeRecap(now);
        player.TryGrantWorkerMilestone(unlockedVillageLevel, now);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(lockedRecap.Events.Select(x => x.Type), Does.Not.Contain(PlayerEventType.WorkerMilestone));
            Assert.That(player.TakeWorkerMilestone(now).GetProperty("milestoneType").GetInt32(), Is.EqualTo((int)WorkerMilestoneType.FirstShift));
        }
    }

    /// <summary>
    /// Выдача вехи не создаёт происшествие и не меняет занятость трудяги.
    /// </summary>
    [Test]
    public void MilestoneDoesNotChangeWorkerActivityTest()
    {
        const int villageLevel = WorkerMilestoneManager.WorkerMilestoneUnlockLevel;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.ClayMine);
        var worker = player.Workers().Single();
        var now = DateTimeHelper.GetNowDate();
        var activityBefore = ReadWorkerActivity(worker.Id);
        player.SetWorkerSkill(worker.Id, DomikIds.ClayMine, WorkerMilestoneManager.FirstShiftThreshold)
            .TryGrantWorkerMilestone(villageLevel, now);

        var activityAfter = ReadWorkerActivity(worker.Id);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(IncidentCount(player.Id), Is.Zero);
            Assert.That(activityAfter, Is.EqualTo(activityBefore));
        }
    }

    private static Trait[] Traits()
    {
        return App.Act<ResourceManager, Trait[]>(m => m.GetTraits());
    }

    private static Trait TraitByLogicName(string logicName)
    {
        return Traits().First(x => x.LogicName == logicName);
    }

    private static WorkerMilestoneType[] PlayerMilestones(int workerId)
    {
        return App.Read(context => context.WorkerMilestones.Where(x => x.WorkerId == workerId).Select(x => x.MilestoneType).ToArray());
    }

    private static int WorkerMilestoneEventCount(int playerId)
    {
        return App.Read(context => context.PlayerEvents.Count(x => x.PlayerId == playerId && x.Type == PlayerEventType.WorkerMilestone));
    }

    private static int IncidentCount(int playerId)
    {
        return App.Read(context => context.Incidents.Count(x => x.PlayerId == playerId));
    }

    private static WorkerActivity ReadWorkerActivity(int workerId)
    {
        return App.Read(context => context.Workers.Where(x => x.Id == workerId)
            .Select(x => new WorkerActivity(x.IncidentId, x.ErrandId, x.ManufactureId))
            .Single());
    }

    private sealed record WorkerActivity(int? IncidentId, int? ErrandId, int? ManufactureId);
}

file static class WorkerMilestoneTestsActs
{
    public static RecapModel TakeRecap(this TestPlayer player, DateTime now)
    {
        return App.Act<PlayerEventManager, RecapModel>(m => m.TakeRecap(player.Id, now));
    }

    public static TestPlayer TryGrantWorkerMilestone(this TestPlayer player, int villageLevel, DateTime now)
    {
        App.Act<WorkerMilestoneManager>(m => m.TryGrantNext(player.Id, villageLevel, now));
        return player;
    }

    public static JsonElement TakeWorkerMilestone(this TestPlayer player, DateTime now)
    {
        return player.TakeRecap(now).Events.Single(x => x.Type == PlayerEventType.WorkerMilestone).Data;
    }

    public static TestPlayer GrantWorkerMilestone(this TestPlayer player, int workerId, WorkerMilestoneType milestoneType, DateTime grantDate)
    {
        using var scope = App.Scope();
        scope.Context.WorkerMilestones.Add(new()
        {
            WorkerId = workerId,
            MilestoneType = milestoneType,
            GrantDate = grantDate,
        });
        scope.Commit();
        return player;
    }

    public static TestPlayer SetLastWorkerMilestoneDate(this TestPlayer player, DateTime? date)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == player.Id).LastWorkerMilestoneDate = date;
        scope.Commit();
        return player;
    }
}
