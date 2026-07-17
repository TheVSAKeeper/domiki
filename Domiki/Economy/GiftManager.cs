using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village;

namespace Domiki.Web.Economy;

public class GiftManager
{
    private const int GiftAwayThresholdSeconds = 6 * 3600;
    private const int BigGiftEvery = 7;
    private const int BaseGiftValue = 40;
    private const int RepBonusThreshold = 25;
    private const int GiftCountCap = 10;
    private static readonly int[] BigGiftDecorPool = [2, 5, 9];

    private readonly ApplicationDbContext _context;
    private readonly PlayerResourceManager _playerResourceManager;
    private readonly DecorManager _decorManager;
    private readonly PlayerEventManager _playerEventManager;
    private readonly VillageLevelCalculator _villageLevelCalculator;

    public GiftManager(ApplicationDbContext context, PlayerResourceManager playerResourceManager, DecorManager decorManager, PlayerEventManager playerEventManager, VillageLevelCalculator villageLevelCalculator)
    {
        _context = context;
        _playerResourceManager = playerResourceManager;
        _decorManager = decorManager;
        _playerEventManager = playerEventManager;
        _villageLevelCalculator = villageLevelCalculator;
    }

    public void TryGrantGift(int playerId, DateTime now)
    {
        var player = _context.Players.FirstOrDefault(x => x.Id == playerId);
        if (player?.LastSeen == null || (now - player.LastSeen.Value).TotalSeconds < GiftAwayThresholdSeconds)
        {
            return;
        }

        _playerResourceManager.LockDbPlayerRow(playerId);
        _context.Entry(player).Reload();
        if (player.LastSeen == null || (now - player.LastSeen.Value).TotalSeconds < GiftAwayThresholdSeconds)
        {
            return;
        }

        var villageLevel = _villageLevelCalculator.GetLevel(playerId).Level;
        var neighbors = _villageLevelCalculator.GetOpenNeighbors(villageLevel);
        if (neighbors.Length == 0)
        {
            return;
        }

        var neighbor = neighbors[Random.Shared.Next(neighbors.Length)];
        var visitIndex = player.VisitsSinceBigGift + 1;
        if (visitIndex >= BigGiftEvery)
        {
            var decorTypeId = BigGiftDecorPool[Random.Shared.Next(BigGiftDecorPool.Length)];
            _decorManager.GrantDecor(playerId, decorTypeId, 1);
            player.VisitsSinceBigGift = 0;
            _playerEventManager.Record(playerId, PlayerEventType.NeighborGift, new
            {
                neighborId = neighbor.Id,
                resources = Array.Empty<object>(),
                decorTypeId,
                visitIndex = BigGiftEvery,
                big = true,
            });
        }
        else
        {
            var resourceTypeId = neighbor.SecondaryResourceTypeId != null && Random.Shared.Next(2) == 1
                ? neighbor.SecondaryResourceTypeId.Value
                : neighbor.PrimaryResourceTypeId;

            var reputation = _context.NeighborReputations
                                 .Where(x => x.PlayerId == playerId && x.NeighborId == neighbor.Id)
                                 .Select(x => (int?)x.Points)
                                 .FirstOrDefault()
                             ?? 0;

            var target = reputation >= RepBonusThreshold ? BaseGiftValue * 3 / 2 : BaseGiftValue;
            var count = Math.Clamp((int)Math.Ceiling((double)target / ResourceManager.GetMarketValue(resourceTypeId)), 1, GiftCountCap);
            _playerResourceManager.GrantResource(playerId, resourceTypeId, count);
            player.VisitsSinceBigGift = visitIndex;
            _playerEventManager.Record(playerId, PlayerEventType.NeighborGift, new
            {
                neighborId = neighbor.Id,
                resources = new[] { new { resourceTypeId, value = count } },
                decorTypeId = (int?)null,
                visitIndex,
                big = false,
            });
        }

        _context.SaveChanges();
    }
}
