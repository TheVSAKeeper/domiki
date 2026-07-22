using Domiki.Web.Data;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village.Models;

namespace Domiki.Web.Village;

public class DecorManager
{
    private readonly ApplicationDbContext _context;
    private readonly ResourceManager _resourceManager;
    private readonly PlayerResourceManager _playerResourceManager;

    public DecorManager(UnitOfWork uow, ApplicationDbContext context, ResourceManager resourceManager, PlayerResourceManager playerResourceManager)
    {
        _context = context;
        _resourceManager = resourceManager;
        _playerResourceManager = playerResourceManager;
    }

    public DecorState GetDecor(int playerId)
    {
        var types = _resourceManager.GetDecorTypes();
        var owned = _context.PlayerDecors
            .Where(x => x.PlayerId == playerId)
            .Select(x => new PlayerDecor { DecorTypeId = x.DecorTypeId, Count = x.Count })
            .ToArray();

        return new()
        {
            Types = types,
            Owned = owned,
            Comfort = DecorCalculator.GetComfort(owned, types),
        };
    }

    public void BuyDecor(int playerId, int decorTypeId)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);

        var type = _resourceManager.GetDecorTypes().FirstOrDefault(x => x.Id == decorTypeId);
        if (type == null)
        {
            throw new BusinessException("Декор не найден");
        }

        if (!type.IsPurchasable)
        {
            throw new BusinessException("Этот декор нельзя купить");
        }

        if (type.NeighborId != null)
        {
            var points = _context.NeighborReputations
                             .FirstOrDefault(x => x.PlayerId == playerId && x.NeighborId == type.NeighborId)
                             ?.Points
                         ?? 0;

            if (points < type.ReputationThreshold)
            {
                var neighbor = _resourceManager.GetNeighbors().First(x => x.Id == type.NeighborId);
                throw new BusinessException($"Откроется за репутацию: {neighbor.Name}, {points}/{type.ReputationThreshold}");
            }
        }

        if (type.RequiresDecorTypeId != null)
        {
            var required = _resourceManager.GetDecorTypes().First(x => x.Id == type.RequiresDecorTypeId);
            var requiredCount = _context.PlayerDecors
                .Where(x => x.PlayerId == playerId && x.DecorTypeId == type.RequiresDecorTypeId)
                .Select(x => x.Count)
                .FirstOrDefault();

            if (requiredCount < 1)
            {
                throw new BusinessException($"Сначала поставьте: {required.Name}");
            }
        }

        if (type.MaxCount != null)
        {
            var ownedCount = _context.PlayerDecors
                .Where(x => x.PlayerId == playerId && x.DecorTypeId == decorTypeId)
                .Select(x => x.Count)
                .FirstOrDefault();

            if (ownedCount >= type.MaxCount)
            {
                throw new BusinessException("Такое украшение уже поставлено");
            }
        }

        _playerResourceManager.WriteOffResources(playerId, type.Cost);
        GrantDecor(playerId, decorTypeId, 1);
    }

    public void GrantDecor(int playerId, int decorTypeId, int count)
    {
        var decor = _context.PlayerDecors.Local.FirstOrDefault(x => x.PlayerId == playerId && x.DecorTypeId == decorTypeId)
                    ?? _context.PlayerDecors.FirstOrDefault(x => x.PlayerId == playerId && x.DecorTypeId == decorTypeId);

        if (decor == null)
        {
            decor = new()
                { PlayerId = playerId, DecorTypeId = decorTypeId };

            _context.PlayerDecors.Add(decor);
        }

        decor.Count += count;
    }
}
