using Domiki.Web.Core.Models;
using Domiki.Web.Core;
using Domiki.Web.Data;
using Domiki.Web.Economy.Models;
using Domiki.Web.Reference;
using Domiki.Web.Village.Models;
using Domiki.Web.Workers;

namespace Domiki.Web.Village;

public class VillageLevelCalculator
{
    public const int BuildingWeight = 1;
    public const int ResidentWeight = 2;
    public const int ReputationWeight = 5;
    public const int ComfortWeight = 1;
    public const int ComfortHabitabilityCap = 50;
    public const int ReputationPointsPerMilestone = 10;
    public const int SmartAutoUnlockLevel = 8;
    public const int RelocationUnlockLevel = 350;
    public const int RelocationLevelStep = 50;
    public const int RelocationMaxUnlockLevel = 500;
    public const int RelocationCooldownDays = 7;

    private readonly ApplicationDbContext _context;
    private readonly ResourceManager _resourceManager;
    private readonly WorkerManager _workerManager;

    public VillageLevelCalculator(ApplicationDbContext context, ResourceManager resourceManager, WorkerManager workerManager)
    {
        _context = context;
        _resourceManager = resourceManager;
        _workerManager = workerManager;
    }

    public static int ComputeLevel(int buildings, int residents, int reputationMilestones, int comfort)
    {
        return buildings * BuildingWeight
               + residents * ResidentWeight
               + reputationMilestones * ReputationWeight
               + Math.Min(comfort, ComfortHabitabilityCap) * ComfortWeight;
    }

    public VillageLevel GetLevel(int playerId)
    {
        var visitsSinceBigGift = _context.Players
            .Where(x => x.Id == playerId)
            .Select(x => x.VisitsSinceBigGift)
            .Single();
        var buildings = _context.Domiks.Where(x => x.PlayerId == playerId && x.TypeId != DomikManager.PostHouseTypeId).Sum(x => x.Level);
        var residents = _workerManager.GetCapacity(playerId);
        var reputation = _context.NeighborReputations
            .Where(x => x.PlayerId == playerId)
            .ToArray()
            .Sum(x => x.Points / ReputationPointsPerMilestone);

        var comfort = DecorCalculator.GetComfort(_context.PlayerDecors.Where(x => x.PlayerId == playerId).Select(x => new PlayerDecor { DecorTypeId = x.DecorTypeId, Count = x.Count }).ToArray(),
            _resourceManager.GetDecorTypes());

        var level = ComputeLevel(buildings, residents, reputation, comfort);

        return new()
        {
            Level = level,
            Buildings = buildings,
            Residents = residents,
            ResidentsCap = WorkerManager.MaxCapacity,
            Reputation = reputation,
            Comfort = comfort,
            VisitsSinceBigGift = visitsSinceBigGift,
            Unlocks = GetUnlocks(playerId, level),
        };
    }

    /// <summary>
    /// Возвращает обжитость, на которой открывается очередной переезд в новую долину.
    /// </summary>
    /// <param name="relocationCount">Число уже совершённых игроком переездов.</param>
    /// <returns>Порог обжитости, не выше <see cref="RelocationMaxUnlockLevel"/>.</returns>
    /// <remarks>
    /// Порог растёт шагом <see cref="RelocationLevelStep"/> от <see cref="RelocationUnlockLevel"/>: каждая следующая
    /// глава длиннее прежней, пока не упрётся в потолок (GAMEDESIGN.md §3 Слой 4).
    /// </remarks>
    public static int GetRelocationThreshold(int relocationCount)
    {
        return Math.Min(RelocationUnlockLevel + RelocationLevelStep * relocationCount, RelocationMaxUnlockLevel);
    }

    public bool CanBuyDomik(int playerId, DomikType domikType)
    {
        return GetLevel(playerId).Level >= domikType.UnlockLevel;
    }

    public Neighbor[] GetOpenNeighbors(int villageLevel)
    {
        return _resourceManager.GetNeighbors()
            .Where(x => x.UnlockLevel <= villageLevel)
            .ToArray();
    }

    public bool IsSmartAutoUnlocked(int playerId)
    {
        return GetLevel(playerId).Level >= SmartAutoUnlockLevel;
    }

    private VillageLevelUnlock[] GetUnlocks(int playerId, int level)
    {
        var blueprintDomikTypeIds = _resourceManager.GetBlueprints().Where(b => b.DomikTypeId != null).Select(b => b.DomikTypeId!.Value).ToHashSet();

        var domikUnlocks = _resourceManager.GetDomikTypes()
            .Where(x => x.UnlockLevel > 0 && !blueprintDomikTypeIds.Contains(x.Id))
            .Select(x => new VillageLevelUnlock
            {
                Level = x.UnlockLevel,
                Label = x.Name,
                Requirement = null,
                Unlocked = level >= x.UnlockLevel,
                Kind = "building",
                LogicName = x.LogicName,
            });

        var builtCounts = _context.Domiks
            .Where(x => x.PlayerId == playerId)
            .GroupBy(x => x.TypeId)
            .Select(x => new { TypeId = x.Key, Count = x.Count() })
            .ToDictionary(x => x.TypeId, x => x.Count);

        var domikTypesById = _resourceManager.GetDomikTypes().ToDictionary(x => x.Id);
        var countGateUnlocks = _resourceManager.GetDomikTypeCountGates()
            .Where(x => domikTypesById.ContainsKey(x.DomikTypeId) && x.Ordinal > builtCounts.GetValueOrDefault(x.DomikTypeId))
            .Select(x => new VillageLevelUnlock
            {
                Level = x.UnlockLevel,
                Label = $"{domikTypesById[x.DomikTypeId].Name} ×{x.Ordinal}",
                Requirement = null,
                Unlocked = level >= x.UnlockLevel,
                Kind = "building",
                LogicName = domikTypesById[x.DomikTypeId].LogicName,
            });

        var neighborUnlocks = _resourceManager.GetNeighbors()
            .Where(x => x.UnlockLevel > 0)
            .Select(x => new VillageLevelUnlock
            {
                Level = x.UnlockLevel,
                Label = $"Сосед {x.Name}",
                Requirement = null,
                Unlocked = level >= x.UnlockLevel,
                Kind = "neighbor",
                LogicName = x.LogicName,
            });

        var smartArtel = new[]
        {
            new VillageLevelUnlock
            {
                Level = SmartAutoUnlockLevel,
                Label = "Умная артель",
                Requirement = null,
                Unlocked = level >= SmartAutoUnlockLevel,
                Kind = "feature",
                LogicName = "smart_artel",
            },
        };

        var villageProfile = new[]
        {
            new VillageLevelUnlock
            {
                Level = VillageProfileManager.VillageLevelRequirement,
                Label = "Уклад деревни",
                Requirement = null,
                Unlocked = level >= VillageProfileManager.VillageLevelRequirement,
                Kind = "feature",
                LogicName = "village_profile",
            },
        };

        var owned = _context.PlayerBlueprints.Where(x => x.PlayerId == playerId).Select(x => x.BlueprintId).ToArray();
        var reputations = _context.NeighborReputations.Where(x => x.PlayerId == playerId).ToArray();
        var neighbors = _resourceManager.GetNeighbors();
        var domikTypes = _resourceManager.GetDomikTypes();
        var receipts = _resourceManager.GetReceipts();
        var blueprintUnlocks = _resourceManager.GetBlueprints()
            .Where(b => !owned.Contains(b.Id))
            .Select(b => new
            {
                b,
                neighbor = neighbors.First(n => n.Id == b.NeighborId),
                domik = b.DomikTypeId == null ? null : domikTypes.First(d => d.Id == b.DomikTypeId),
                receipt = b.ReceiptId == null ? null : receipts.First(r => r.Id == b.ReceiptId),
                points = reputations.FirstOrDefault(r => r.NeighborId == b.NeighborId)?.Points ?? 0,
            })
            .Where(x => x.points < x.b.ReputationThreshold)
            .Select(x => new VillageLevelUnlock
            {
                Level = null,
                Label = x.domik == null ? x.receipt!.Name ?? string.Empty : x.domik.Name,
                Requirement = $"чертёж: {x.neighbor.Name}, доброе имя {x.points}/{x.b.ReputationThreshold}",
                Unlocked = false,
                Kind = x.domik == null ? "receipt" : "building",
                LogicName = x.domik == null ? x.receipt!.LogicName ?? string.Empty : x.domik.LogicName,
            });

        return domikUnlocks
            .Concat(countGateUnlocks)
            .Concat(neighborUnlocks)
            .Concat(smartArtel)
            .Concat(villageProfile)
            .OrderBy(x => x.Level)
            .ThenBy(x => x.Label)
            .Concat(blueprintUnlocks.OrderBy(x => x.Requirement).ThenBy(x => x.Label))
            .ToArray();
    }
}
