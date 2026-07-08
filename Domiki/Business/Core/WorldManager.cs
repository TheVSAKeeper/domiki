using Domiki.Web.Business.Models;
using Domiki.Web.Data;

namespace Domiki.Web.Business.Core
{
    public class WorldManager
    {
        private static readonly Dictionary<string, NpcPresentation> NpcPresentations = new Dictionary<string, NpcPresentation>
        {
            { "zarechye", new NpcPresentation { Level = 45, CrestIcon = 2, CrestColor = 2 } },
            { "borovoe", new NpcPresentation { Level = 38, CrestIcon = 1, CrestColor = 0 } },
            { "kamenka", new NpcPresentation { Level = 30, CrestIcon = 3, CrestColor = 4 } },
            { "glinischi", new NpcPresentation { Level = 24, CrestIcon = 6, CrestColor = 1 } },
            { "dubrava", new NpcPresentation { Level = 18, CrestIcon = 4, CrestColor = 5 } },
        };

        private readonly ApplicationDbContext _context;
        private readonly VillageLevelCalculator _villageLevelCalculator;
        private readonly DomikManager _domikManager;
        private readonly ResourceManager _resourceManager;

        public WorldManager(ApplicationDbContext context, VillageLevelCalculator villageLevelCalculator, DomikManager domikManager, ResourceManager resourceManager)
        {
            _context = context;
            _villageLevelCalculator = villageLevelCalculator;
            _domikManager = domikManager;
            _resourceManager = resourceManager;
        }

        public World GetWorld(int currentPlayerId)
        {
            var villages = _context.Players
                .Where(x => x.VillageName != null)
                .ToArray()
                .Select(x => new WorldVillage
                {
                    PlayerId = x.Id,
                    VillageName = x.VillageName,
                    CrestIcon = x.CrestIcon,
                    CrestColor = x.CrestColor,
                    Level = _villageLevelCalculator.GetLevel(x.Id).Level,
                    IsNpc = false,
                    IsMe = x.Id == currentPlayerId,
                    NpcResourceTypeId = null,
                })
                .Concat(GetNpcVillages())
                .OrderByDescending(x => x.Level)
                .ThenBy(x => x.IsNpc ? 1 : 0)
                .ThenBy(x => x.VillageName)
                .ToArray();

            return new World { Villages = villages };
        }

        public VillageVisit VisitVillage(int targetPlayerId)
        {
            var player = _context.Players.SingleOrDefault(x => x.Id == targetPlayerId);
            if (player == null || player.VillageName == null)
            {
                throw new BusinessException("Деревня не найдена");
            }

            return new VillageVisit
            {
                VillageName = player.VillageName,
                CrestIcon = player.CrestIcon,
                CrestColor = player.CrestColor,
                Level = _villageLevelCalculator.GetLevel(targetPlayerId),
                Buildings = _domikManager.GetDomiks(targetPlayerId)
                    .Select(x => new VisitBuilding
                    {
                        TypeName = x.Type.Name,
                        Level = x.Level,
                    })
                    .ToArray(),
            };
        }

        private IEnumerable<WorldVillage> GetNpcVillages()
        {
            return _resourceManager.GetNeighbors()
                .Where(x => NpcPresentations.ContainsKey(x.LogicName))
                .Select(x =>
                {
                    var presentation = NpcPresentations[x.LogicName];
                    return new WorldVillage
                    {
                        PlayerId = null,
                        VillageName = x.Name,
                        CrestIcon = presentation.CrestIcon,
                        CrestColor = presentation.CrestColor,
                        Level = presentation.Level,
                        IsNpc = true,
                        IsMe = false,
                        NpcResourceTypeId = x.PrimaryResourceTypeId,
                    };
                });
        }

        private class NpcPresentation
        {
            public int Level { get; set; }
            public int CrestIcon { get; set; }
            public int CrestColor { get; set; }
        }
    }
}
