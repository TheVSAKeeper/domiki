using Domiki.Web.Business.Models;

namespace Domiki.Web.Business.Core
{
    public class VillageLevelCalculator
    {
        public const int BuildingWeight = 1;
        public const int ResidentWeight = 2;
        public const int ReputationWeight = 5;
        public const int ComfortWeight = 0;
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
            var comfort = 0;
            var level = buildings * BuildingWeight
                + residents * ResidentWeight
                + reputation * ReputationWeight
                + comfort * ComfortWeight;

            return new VillageLevel
            {
                Level = level,
                Buildings = buildings,
                Residents = residents,
                Reputation = reputation,
                Comfort = comfort,
                UpcomingUnlocks = GetUpcomingUnlocks(level),
            };
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

        private VillageLevelUnlock[] GetUpcomingUnlocks(int level)
        {
            var domikUnlocks = _resourceManager.GetDomikTypes()
                .Where(x => x.UnlockLevel > level)
                .Select(x => new VillageLevelUnlock { Level = x.UnlockLevel, Label = x.Name });
            var neighborUnlocks = _resourceManager.GetNeighbors()
                .Where(x => x.UnlockLevel > level)
                .Select(x => new VillageLevelUnlock { Level = x.UnlockLevel, Label = $"Сосед {x.Name}" });
            var smartAutoUnlock = SmartAutoUnlockLevel > level
                ? new[] { new VillageLevelUnlock { Level = SmartAutoUnlockLevel, Label = "Умная артель" } }
                : Array.Empty<VillageLevelUnlock>();

            return domikUnlocks
                .Concat(neighborUnlocks)
                .Concat(smartAutoUnlock)
                .OrderBy(x => x.Level)
                .ThenBy(x => x.Label)
                .ToArray();
        }
    }
}
