using Domiki.Web.Core;
using Domiki.Web.Data;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village.Models;

namespace Domiki.Web.Village;

public class WorldManager
{
    private static readonly Dictionary<string, NpcPresentation> NpcPresentations = new()
    {
        {
            "zarechye", new()
                { Level = 45, CrestIcon = 2, CrestColor = 2 }
        },
        {
            "borovoe", new()
                { Level = 38, CrestIcon = 1, CrestColor = 0 }
        },
        {
            "kamenka", new()
                { Level = 30, CrestIcon = 3, CrestColor = 4 }
        },
        {
            "glinischi", new()
                { Level = 24, CrestIcon = 6, CrestColor = 1 }
        },
        {
            "dubrava", new()
                { Level = 18, CrestIcon = 4, CrestColor = 5 }
        },
    };

    private readonly ApplicationDbContext _context;
    private readonly VillageLevelCalculator _villageLevelCalculator;
    private readonly DomikManager _domikManager;
    private readonly ResourceManager _resourceManager;
    private readonly SeasonManager _seasonManager;

    public WorldManager(ApplicationDbContext context, VillageLevelCalculator villageLevelCalculator, DomikManager domikManager, ResourceManager resourceManager, SeasonManager seasonManager)
    {
        _context = context;
        _villageLevelCalculator = villageLevelCalculator;
        _domikManager = domikManager;
        _resourceManager = resourceManager;
        _seasonManager = seasonManager;
    }

    public World GetWorld(int currentPlayerId)
    {
        var season = _seasonManager.GetCurrentSeason(DateTimeHelper.GetNowDate());
        var counters = _seasonManager.GetCounters(season.Number);

        var villages = _context.Players
            .Where(x => x.VillageName != null)
            .ToArray()
            .Select(x =>
            {
                var level = _villageLevelCalculator.GetLevel(x.Id);
                return new WorldVillage
                {
                    PlayerId = x.Id,
                    VillageName = x.VillageName,
                    CrestIcon = x.CrestIcon,
                    CrestColor = x.CrestColor,
                    Level = level.Level,
                    IsNpc = false,
                    IsMe = x.Id == currentPlayerId,
                    NpcResourceTypeId = null,
                    SeasonOrders = counters.GetValueOrDefault((x.Id, SeasonMetric.Orders)),
                    SeasonToloka = counters.GetValueOrDefault((x.Id, SeasonMetric.Toloka)),
                    SeasonExpeditions = counters.GetValueOrDefault((x.Id, SeasonMetric.Expeditions)),
                    Comfort = level.Comfort,
                };
            })
            .Concat(GetNpcVillages())
            .OrderByDescending(x => x.Level)
            .ThenBy(x => x.IsNpc ? 1 : 0)
            .ThenBy(x => x.VillageName)
            .ToArray();

        return new()
            { Villages = villages, Season = season };
    }

    public VillageVisit VisitVillage(int targetPlayerId)
    {
        var player = _context.Players.SingleOrDefault(x => x.Id == targetPlayerId);
        if (player == null || player.VillageName == null)
        {
            throw new BusinessException("Деревня не найдена");
        }

        var level = _villageLevelCalculator.GetLevel(targetPlayerId);
        level.VisitsSinceBigGift = 0;

        return new()
        {
            VillageName = player.VillageName,
            CrestIcon = player.CrestIcon,
            CrestColor = player.CrestColor,
            Level = level,
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
                    NpcLogicName = x.LogicName,
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
