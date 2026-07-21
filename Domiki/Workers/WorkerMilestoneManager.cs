using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Workers;

/// <summary>
/// Лениво выдаёт трудягам однократные вехи за пожизненные достижения.
/// </summary>
public class WorkerMilestoneManager
{
    /// <summary>
    /// Минимальная обжитость для выдачи вех трудяг.
    /// </summary>
    public const int WorkerMilestoneUnlockLevel = 8;

    /// <summary>
    /// Кулдаун между выдачами вех трудяг одному игроку.
    /// </summary>
    /// <value>Часы.</value>
    public const int WorkerMilestoneCooldownHours = 48;

    /// <summary>
    /// Число суток после найма для вехи службы в казармах.
    /// </summary>
    /// <value>Сутки.</value>
    public const int MonthInBarracksDays = 30;

    /// <summary>
    /// Число использований навыков для первой смены.
    /// </summary>
    public const int FirstShiftThreshold = 1;

    /// <summary>
    /// Число использований навыков для сотой смены.
    /// </summary>
    public const int HundredthShiftThreshold = 100;

    /// <summary>
    /// Число использований навыка одного типа домика для набитой руки.
    /// </summary>
    public const int SkilledHandThreshold = 50;

    /// <summary>
    /// Число использований навыка одного типа домика у каждого из напарников.
    /// </summary>
    public const int TwoAtBenchThreshold = 25;

    /// <summary>
    /// Число завершённых походов для десятой дороги.
    /// </summary>
    public const int TenthRoadThreshold = 10;

    /// <summary>
    /// Базовая рыночная ценность находки за веху трудяги.
    /// </summary>
    public const int WorkerMilestoneFindBaseValue = 4;

    private readonly ApplicationDbContext _context;
    private readonly PlayerResourceManager _playerResourceManager;
    private readonly ResourceManager _resourceManager;
    private readonly PlayerEventManager _playerEventManager;
    private readonly VillageLevelCalculator _villageLevelCalculator;

    /// <summary>
    /// Создаёт менеджер вех трудяг.
    /// </summary>
    /// <param name="context">Контекст игровой базы данных.</param>
    /// <param name="playerResourceManager">Менеджер ресурсов и блокировки игрока.</param>
    /// <param name="resourceManager">Справочник построек, рецептов, походов, ресурсов и черт.</param>
    /// <param name="playerEventManager">Журнал игровых событий игрока.</param>
    /// <param name="villageLevelCalculator">Калькулятор обжитости деревни игрока.</param>
    public WorkerMilestoneManager(ApplicationDbContext context, PlayerResourceManager playerResourceManager, ResourceManager resourceManager, PlayerEventManager playerEventManager, VillageLevelCalculator villageLevelCalculator)
    {
        _context = context;
        _playerResourceManager = playerResourceManager;
        _resourceManager = resourceManager;
        _playerEventManager = playerEventManager;
        _villageLevelCalculator = villageLevelCalculator;
    }

    /// <summary>
    /// Пытается выдать игроку одну следующую достигнутую веху трудяги.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="villageLevel">Текущая обжитость деревни игрока (передаётся, чтобы не считать её повторно на горячем пути).</param>
    /// <param name="now">Текущий момент в UTC.</param>
    public void TryGrantNext(int playerId, int villageLevel, DateTime now)
    {
        var player = _context.Players.FirstOrDefault(x => x.Id == playerId);
        if (player == null || villageLevel < WorkerMilestoneUnlockLevel || !CooldownElapsed(player, now))
        {
            return;
        }

        if (FindCandidates(playerId, now).Candidates.Length == 0)
        {
            return;
        }

        _playerResourceManager.LockDbPlayerRow(playerId);
        _context.Entry(player).Reload();
        if (!CooldownElapsed(player, now))
        {
            return;
        }

        var (candidates, grantedMilestones) = FindCandidates(playerId, now);
        if (candidates.Length == 0)
        {
            return;
        }

        // TODO: очередь по фактической дате достижения – сейчас дат достижения не храним, порядок стабильный по типу+id
        var candidate = candidates[0];
        var traitUpgraded = false;
        string? newTrait = null;
        string? newTraitLogicName = null;
        int? resourceTypeId = null;
        int? value = null;

        if (candidate.MilestoneType == WorkerMilestoneType.SkilledHand && IsOrdinary(candidate.Worker))
        {
            (traitUpgraded, newTrait, newTraitLogicName) = UpgradeTrait(candidate.Worker);
        }
        else
        {
            (resourceTypeId, value) = GrantFind(playerId, GetResourcePool(playerId, candidate), GetFindMultiplier(candidate.MilestoneType));
        }

        player.LastWorkerMilestoneDate = now;
        _context.WorkerMilestones.Add(new WorkerMilestone
        {
            WorkerId = candidate.Worker.Id,
            MilestoneType = candidate.MilestoneType,
            GrantDate = now,
        });

        if (candidate.MilestoneType == WorkerMilestoneType.TwoAtBench && candidate.Partner != null && !HasMilestone(grantedMilestones, candidate.Partner.Id, WorkerMilestoneType.TwoAtBench))
        {
            _context.WorkerMilestones.Add(new WorkerMilestone
            {
                WorkerId = candidate.Partner.Id,
                MilestoneType = WorkerMilestoneType.TwoAtBench,
                GrantDate = now,
            });
        }

        _playerEventManager.Record(playerId, PlayerEventType.WorkerMilestone, new
        {
            milestoneType = (int)candidate.MilestoneType,
            workerId = candidate.Worker.Id,
            workerName = candidate.Worker.Name,
            workerGender = (int)NameGrammar.GenderOf(candidate.Worker.Name),
            domikTypeId = candidate.DomikTypeId,
            workerId2 = candidate.Partner?.Id,
            workerName2 = candidate.Partner?.Name,
            workerGender2 = candidate.Partner == null ? (int?)null : (int)NameGrammar.GenderOf(candidate.Partner.Name),
            resourceTypeId,
            value,
            traitUpgraded,
            newTrait,
            newTraitLogicName,
        });
        _context.SaveChanges();
    }

    private static bool CooldownElapsed(Player player, DateTime now)
    {
        return player.LastWorkerMilestoneDate == null || player.LastWorkerMilestoneDate.Value.AddHours(WorkerMilestoneCooldownHours) <= now;
    }

    private (MilestoneCandidate[] Candidates, Dictionary<int, HashSet<WorkerMilestoneType>> GrantedMilestones) FindCandidates(int playerId, DateTime now)
    {
        var workers = _context.Workers
            .Where(x => x.PlayerId == playerId)
            .Include(x => x.Skills)
            .OrderBy(x => x.Id)
            .ToArray();
        var workerIds = workers.Select(x => x.Id).ToArray();
        var grantedMilestones = _context.WorkerMilestones
            .Where(x => workerIds.Contains(x.WorkerId))
            .ToArray()
            .GroupBy(x => x.WorkerId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.MilestoneType).ToHashSet());

        var candidates = EnumerateCandidates(workers, grantedMilestones, now)
            .OrderBy(x => x.MilestoneType)
            .ThenBy(x => x.Worker.Id)
            .ToArray();
        return (candidates, grantedMilestones);
    }

    private static IEnumerable<MilestoneCandidate> EnumerateCandidates(Worker[] workers, IReadOnlyDictionary<int, HashSet<WorkerMilestoneType>> grantedMilestones, DateTime now)
    {
        foreach (var worker in workers)
        {
            var granted = grantedMilestones.GetValueOrDefault(worker.Id) ?? [];
            var skills = worker.Skills.ToArray();
            var topSkill = skills.OrderByDescending(x => x.Uses).ThenBy(x => x.DomikTypeId).FirstOrDefault();

            if (!granted.Contains(WorkerMilestoneType.FirstShift) && skills.Sum(x => x.Uses) >= FirstShiftThreshold)
            {
                yield return new(WorkerMilestoneType.FirstShift, worker, topSkill?.DomikTypeId, null);
            }

            if (!granted.Contains(WorkerMilestoneType.HundredthShift) && skills.Sum(x => x.Uses) >= HundredthShiftThreshold)
            {
                yield return new(WorkerMilestoneType.HundredthShift, worker, topSkill?.DomikTypeId, null);
            }

            var skilledSkill = skills.Where(x => x.Uses >= SkilledHandThreshold)
                .OrderByDescending(x => x.Uses)
                .ThenBy(x => x.DomikTypeId)
                .FirstOrDefault();
            if (!granted.Contains(WorkerMilestoneType.SkilledHand) && skilledSkill != null)
            {
                yield return new(WorkerMilestoneType.SkilledHand, worker, skilledSkill.DomikTypeId, null);
            }

            if (!granted.Contains(WorkerMilestoneType.MonthInBarracks) && worker.HireDate.AddDays(MonthInBarracksDays) <= now)
            {
                yield return new(WorkerMilestoneType.MonthInBarracks, worker, topSkill?.DomikTypeId, null);
            }

            var partnership = FindTeamwork(worker, workers, grantedMilestones);
            if (!granted.Contains(WorkerMilestoneType.TwoAtBench) && partnership != null)
            {
                yield return new(WorkerMilestoneType.TwoAtBench, worker, partnership.Value.DomikTypeId, partnership.Value.Partner);
            }

            if (!granted.Contains(WorkerMilestoneType.TenthRoad) && worker.ExpeditionCount >= TenthRoadThreshold)
            {
                yield return new(WorkerMilestoneType.TenthRoad, worker, null, null);
            }
        }
    }

    private static (int DomikTypeId, Worker Partner)? FindTeamwork(Worker worker, Worker[] workers, IReadOnlyDictionary<int, HashSet<WorkerMilestoneType>> grantedMilestones)
    {
        foreach (var skill in worker.Skills.Where(x => x.Uses >= TwoAtBenchThreshold).OrderBy(x => x.DomikTypeId))
        {
            var partner = workers.Where(x => x.Id != worker.Id && x.Skills.Any(y => y.DomikTypeId == skill.DomikTypeId && y.Uses >= TwoAtBenchThreshold))
                .OrderBy(x => HasMilestone(grantedMilestones, x.Id, WorkerMilestoneType.TwoAtBench) ? 1 : 0)
                .ThenBy(x => x.Id)
                .FirstOrDefault();
            if (partner != null)
            {
                return (skill.DomikTypeId, partner);
            }
        }

        return null;
    }

    private static bool HasMilestone(IReadOnlyDictionary<int, HashSet<WorkerMilestoneType>> grantedMilestones, int workerId, WorkerMilestoneType milestoneType)
    {
        return grantedMilestones.GetValueOrDefault(workerId)?.Contains(milestoneType) ?? false;
    }

    private (int? ResourceTypeId, int? Value) GrantFind(int playerId, IReadOnlyList<int> resourcePool, int multiplier)
    {
        if (resourcePool.Count == 0)
        {
            return (null, null);
        }

        var resourceTypeId = resourcePool[Random.Shared.Next(resourcePool.Count)];
        var value = Math.Max(1, (int)Math.Round(WorkerMilestoneFindBaseValue * multiplier * (double)ResourceManager.BaseMarketValue / ResourceManager.GetMarketValue(resourceTypeId), MidpointRounding.AwayFromZero));
        _playerResourceManager.GrantResource(playerId, resourceTypeId, value);
        return (resourceTypeId, value);
    }

    private int[] GetResourcePool(int playerId, MilestoneCandidate candidate)
    {
        var pool = candidate.MilestoneType switch
        {
            WorkerMilestoneType.FirstShift or WorkerMilestoneType.HundredthShift or WorkerMilestoneType.SkilledHand or WorkerMilestoneType.TwoAtBench when candidate.DomikTypeId != null => GetDomikFindResourcePool(candidate.DomikTypeId.Value),
            WorkerMilestoneType.TenthRoad => GetExpeditionResourcePool(),
            _ => Array.Empty<int>(),
        };

        return pool.Length > 0 ? pool : GetBaseResourcePool(playerId);
    }

    private static int GetFindMultiplier(WorkerMilestoneType milestoneType)
    {
        return milestoneType switch
        {
            WorkerMilestoneType.FirstShift => 1,
            WorkerMilestoneType.HundredthShift or WorkerMilestoneType.SkilledHand => 3,
            WorkerMilestoneType.MonthInBarracks or WorkerMilestoneType.TwoAtBench or WorkerMilestoneType.TenthRoad => 2,
            _ => 1,
        };
    }

    private bool IsOrdinary(Worker worker)
    {
        return _resourceManager.GetTraits().First(x => x.LogicName == "ordinary").Id == worker.TraitId;
    }

    private (bool TraitUpgraded, string? NewTrait, string? NewTraitLogicName) UpgradeTrait(Worker worker)
    {
        var candidates = _resourceManager.GetTraits().Where(x => x.LogicName != "ordinary").ToArray();
        var trait = candidates[Random.Shared.Next(candidates.Length)];
        worker.TraitId = trait.Id;
        return (true, trait.Name, trait.LogicName);
    }

    private int[] GetDomikFindResourcePool(int domikTypeId)
    {
        var domikType = _resourceManager.GetDomikTypes().First(x => x.Id == domikTypeId);
        var receiptIds = domikType.Levels.SelectMany(x => x.Receipts).Select(x => x.Id).ToHashSet();
        var resourcePool = _resourceManager.GetReceipts()
            .Where(x => receiptIds.Contains(x.Id))
            .SelectMany(x => x.OutputResources)
            .Select(x => x.Type.Id)
            .Distinct()
            .ToArray();
        if (resourcePool.Length > 0)
        {
            return resourcePool;
        }

        return domikType.Levels.SelectMany(x => x.Resources)
            .Select(x => x.Type.Id)
            .Distinct()
            .ToArray();
    }

    private int[] GetExpeditionResourcePool()
    {
        return _resourceManager.GetExpeditionTypes()
            .SelectMany(x => x.Loot)
            .Where(x => x.Kind == ExpeditionLootKind.Resource && x.ResourceTypeId != null)
            .Select(x => x.ResourceTypeId!.Value)
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// Возвращает базовый пул находок игрока – дешёвое сырьё рецептов без входов из построек, уже открытых обжитостью деревни.
    /// </summary>
    /// <remarks>
    /// Отсечка по <see cref="Core.Models.DomikType.UnlockLevel"/> не даёт вехе подарить ресурс, который игроку негде производить и некуда деть.
    /// </remarks>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <returns>Идентификаторы типов ресурсов, из которых выбирается находка.</returns>
    public int[] GetBaseResourcePool(int playerId)
    {
        var villageLevel = _villageLevelCalculator.GetLevel(playerId).Level;
        var receiptIds = _resourceManager.GetDomikTypes()
            .Where(x => x.UnlockLevel <= villageLevel)
            .SelectMany(x => x.Levels)
            .SelectMany(x => x.Receipts)
            .Select(x => x.Id)
            .ToHashSet();

        return _resourceManager.GetReceipts()
            .Where(x => receiptIds.Contains(x.Id) && x.InputResources.Length == 0)
            .SelectMany(x => x.OutputResources)
            .Select(x => x.Type.Id)
            .Where(x => ResourceManager.GetMarketValue(x) <= ResourceManager.BaseMarketValue)
            .Distinct()
            .ToArray();
    }

    private sealed record MilestoneCandidate(WorkerMilestoneType MilestoneType, Worker Worker, int? DomikTypeId, Worker? Partner);
}
