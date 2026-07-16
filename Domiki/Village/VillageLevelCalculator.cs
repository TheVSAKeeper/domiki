using Domiki.Web.Core.Models;
using Domiki.Web.Economy.Models;
using Domiki.Web.Reference;
using Domiki.Web.Village.Models;
using Domiki.Web.Workers;

namespace Domiki.Web.Village
{
    public class VillageLevelCalculator
    {
        public const int BuildingWeight = 1;
        public const int ResidentWeight = 2;
        public const int ReputationWeight = 5;
        public const int ComfortWeight = 1;
        public const int ComfortHabitabilityCap = 50;
        public const int ReputationPointsPerMilestone = 10;
        public const int SmartAutoUnlockLevel = 8;

        private readonly Data.ApplicationDbContext _context;
        private readonly ResourceManager _resourceManager;
        private readonly WorkerManager _workerManager;

        public VillageLevelCalculator(Data.ApplicationDbContext context, ResourceManager resourceManager, WorkerManager workerManager)
        {
            _context = context;
            _resourceManager = resourceManager;
            _workerManager = workerManager;
        }

        public VillageLevel GetLevel(int playerId)
        {
            var buildings = _context.Domiks.Where(x => x.PlayerId == playerId).Sum(x => x.Level);
            var residents = _workerManager.GetCapacity(playerId);
            var reputation = _context.NeighborReputations
                .Where(x => x.PlayerId == playerId)
                .ToArray()
                .Sum(x => x.Points / ReputationPointsPerMilestone);
            var comfort = DecorCalculator.GetComfort(
                _context.PlayerDecors.Where(x => x.PlayerId == playerId).Select(x => new PlayerDecor { DecorTypeId = x.DecorTypeId, Count = x.Count }).ToArray(),
                _resourceManager.GetDecorTypes());
            var level = ComputeLevel(buildings, residents, reputation, comfort);

            return new VillageLevel
            {
                Level = level,
                Buildings = buildings,
                Residents = residents,
                Reputation = reputation,
                Comfort = comfort,
                UpcomingUnlocks = GetUpcomingUnlocks(playerId, level),
            };
        }

        public static int ComputeLevel(int buildings, int residents, int reputationMilestones, int comfort)
        {
            return buildings * BuildingWeight
                + residents * ResidentWeight
                + reputationMilestones * ReputationWeight
                + Math.Min(comfort, ComfortHabitabilityCap) * ComfortWeight;
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

        private VillageLevelUnlock[] GetUpcomingUnlocks(int playerId, int level)
        {
            var domikUnlocks = _resourceManager.GetDomikTypes()
                .Where(x => x.UnlockLevel > level)
                .Select(x => new VillageLevelUnlock { Level = x.UnlockLevel, Label = x.Name, Requirement = null });
            var neighborUnlocks = _resourceManager.GetNeighbors()
                .Where(x => x.UnlockLevel > level)
                .Select(x => new VillageLevelUnlock { Level = x.UnlockLevel, Label = $"Сосед {x.Name}", Requirement = null });
            var smartAutoUnlock = SmartAutoUnlockLevel > level
                ? new[] { new VillageLevelUnlock { Level = SmartAutoUnlockLevel, Label = "Умная артель", Requirement = null } }
                : Array.Empty<VillageLevelUnlock>();
            var owned = _context.PlayerBlueprints.Where(x => x.PlayerId == playerId).Select(x => x.BlueprintId).ToArray();
            var reputations = _context.NeighborReputations.Where(x => x.PlayerId == playerId).ToArray();
            var neighbors = _resourceManager.GetNeighbors();
            var domikTypes = _resourceManager.GetDomikTypes();
            var blueprintUnlocks = _resourceManager.GetBlueprints()
                .Where(b => !owned.Contains(b.Id))
                .Select(b => new { b, neighbor = neighbors.First(n => n.Id == b.NeighborId), points = reputations.FirstOrDefault(r => r.NeighborId == b.NeighborId)?.Points ?? 0 })
                .Where(x => x.points < x.b.ReputationThreshold)
                .Select(x => new VillageLevelUnlock
                {
                    Level = null,
                    Label = domikTypes.First(d => d.Id == x.b.DomikTypeId).Name,
                    Requirement = $"чертёж: {x.neighbor.Name}, репутация {x.points}/{x.b.ReputationThreshold}",
                });

            return domikUnlocks
                .Concat(neighborUnlocks)
                .Concat(smartAutoUnlock)
                .OrderBy(x => x.Level)
                .ThenBy(x => x.Label)
                .Concat(blueprintUnlocks.OrderBy(x => x.Requirement).ThenBy(x => x.Label))
                .ToArray();
        }
    }
}
