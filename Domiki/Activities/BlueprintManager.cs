using Domiki.Web.Activities.Models;
using Domiki.Web.Data;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;

namespace Domiki.Web.Activities;

public class BlueprintManager
{
    private readonly ApplicationDbContext _context;
    private readonly ResourceManager _resourceManager;
    private readonly PlayerResourceManager _playerResourceManager;

    public BlueprintManager(ApplicationDbContext context, ResourceManager resourceManager, PlayerResourceManager playerResourceManager)
    {
        _context = context;
        _resourceManager = resourceManager;
        _playerResourceManager = playerResourceManager;
    }

    public void EnsureBlueprints(int playerId)
    {
        var blueprints = _resourceManager.GetBlueprints();
        var owned = _context.PlayerBlueprints.Where(x => x.PlayerId == playerId).Select(x => x.BlueprintId).ToArray();
        var reputations = _context.NeighborReputations.Where(x => x.PlayerId == playerId).ToArray();

        foreach (var blueprint in blueprints)
        {
            if (owned.Contains(blueprint.Id))
            {
                continue;
            }

            var points = reputations.FirstOrDefault(x => x.NeighborId == blueprint.NeighborId)?.Points ?? 0;
            if (points >= blueprint.ReputationThreshold)
            {
                _context.PlayerBlueprints.Add(new()
                    { PlayerId = playerId, BlueprintId = blueprint.Id });
            }
        }

        _context.SaveChanges();
    }

    public IEnumerable<PlayerBlueprint> GetBlueprints(int playerId)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);
        EnsureBlueprints(playerId);

        var owned = _context.PlayerBlueprints.Where(x => x.PlayerId == playerId).Select(x => x.BlueprintId).ToArray();
        var reputations = _context.NeighborReputations.Where(x => x.PlayerId == playerId).ToArray();
        var neighbors = _resourceManager.GetNeighbors();

        return _resourceManager.GetBlueprints()
            .Select(blueprint => new PlayerBlueprint
            {
                Blueprint = blueprint,
                Neighbor = neighbors.First(x => x.Id == blueprint.NeighborId),
                CurrentReputation = reputations.FirstOrDefault(x => x.NeighborId == blueprint.NeighborId)?.Points ?? 0,
                Owned = owned.Contains(blueprint.Id),
            })
            .ToArray();
    }

    public bool IsOwned(int playerId, int blueprintId)
    {
        return _context.PlayerBlueprints.Any(x => x.PlayerId == playerId && x.BlueprintId == blueprintId);
    }

    public bool GrantBlueprint(int playerId, int blueprintId)
    {
        if (IsOwned(playerId, blueprintId))
        {
            return false;
        }

        _context.PlayerBlueprints.Add(new()
            { PlayerId = playerId, BlueprintId = blueprintId });

        _context.SaveChanges();
        return true;
    }
}
