using Domiki.Web.Activities;
using Domiki.Web.Core.Scheduling;
using Domiki.Web.Data;
using Domiki.Web.Economy.Models;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Reference.Models;
using Domiki.Web.Village;
using Domiki.Web.Village.Models;
using Domiki.Web.Workers;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Economy;

public class OrderManager
{
    public const int BoardSize = 3;

    /// <summary>
    /// Во сколько раз чаще на доске появляется заказ соседа, с которым игрок водит дружбу (см. <see cref="Data.Entities.Player.FriendNeighborId"/>).
    /// </summary>
    public const int FriendWeight = 3;

    /// <summary>
    /// Сколько ячеек доски может занять сосед, с которым водят дружбу: хотя бы одна весточка всегда приходит от других выселок.
    /// </summary>
    public const int FriendBoardLimit = BoardSize - 1;

    /// <summary>
    /// Доля двора, на которую рассчитан один заказ: один заказ рассчитан на половину мощности двора за свой срок, чтобы
    /// доска из трёх заказов была посильна, а не занимала весь двор целиком.
    /// </summary>
    public const int BoardShare = 2;

    /// <summary>
    /// Расчётная длительность одного производственного цикла в часах.
    /// </summary>
    public const double ManufactureCycleHours = 1.5;

    public const int OrderRefillDelaySeconds = 30 * 60;
    private const int CoinResourceTypeId = 1;
    private const int GoldResourceTypeId = 5;

    public static readonly OrderTier[] Tiers =
    {
        new(5, 4 * 60 * 60, 1.5, 0, 1),
        new(15, 8 * 60 * 60, 2.0, 1, 2),
        new(30, 24 * 60 * 60, 3.0, 2, 4),
    };

    private readonly ApplicationDbContext _context;
    private readonly ICalculator _calculator;
    private readonly UnitOfWork _uow;
    private readonly ResourceManager _resourceManager;
    private readonly PlayerResourceManager _playerResourceManager;
    private readonly WorkerManager _workerManager;
    private readonly VillageLevelCalculator _villageLevelCalculator;
    private readonly SeasonManager _seasonManager;
    private readonly TolokaManager _tolokaManager;
    private readonly GoalManager _goalManager;
    private readonly ErrandManager _errandManager;

    public OrderManager(
        UnitOfWork uow,
        ApplicationDbContext context,
        ICalculator calculator,
        ResourceManager resourceManager,
        PlayerResourceManager playerResourceManager,
        WorkerManager workerManager,
        VillageLevelCalculator villageLevelCalculator,
        SeasonManager seasonManager,
        TolokaManager tolokaManager,
        GoalManager goalManager,
        ErrandManager errandManager)
    {
        _context = context;
        _calculator = calculator;
        _uow = uow;
        _resourceManager = resourceManager;
        _playerResourceManager = playerResourceManager;
        _workerManager = workerManager;
        _villageLevelCalculator = villageLevelCalculator;
        _seasonManager = seasonManager;
        _tolokaManager = tolokaManager;
        _goalManager = goalManager;
        _errandManager = errandManager;
    }

    public static int GetOrderQuantity(OrderTier tier, int resourceTypeId)
    {
        return Math.Max(2, (int)Math.Round(tier.Quantity * (double)ResourceManager.BaseMarketValue / ResourceManager.GetMarketValue(resourceTypeId), MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// Считает посильный объём заказа: нормированное по ценности количество, урезанное мощностью двора за срок заказа.
    /// </summary>
    /// <remarks>
    /// Заказ рассчитан на долю двора <see cref="BoardShare"/>, а каждый следующий заказ на тот же ресурс делит эту долю
    /// дальше – иначе доска, где сосед-друг просит одно и то же в нескольких ячейках, просила бы больше суточной выработки.
    /// </remarks>
    /// <param name="tier">Тир спроса, задающий базовое количество и срок.</param>
    /// <param name="resourceTypeId">Ссылка на справочник типов ресурсов – что просит сосед.</param>
    /// <param name="capacity">Мощность двора по этому ресурсу: меньшее из числа трудяг и слотов подходящих построек.</param>
    /// <param name="sameResourceOrders">Сколько заказов на этот же ресурс уже висит на доске.</param>
    /// <returns>Количество единиц ресурса, которое сосед просит в заказе.</returns>
    public static int GetEffectiveQuantity(OrderTier tier, int resourceTypeId, int capacity, int sameResourceOrders = 0)
    {
        var quantity = GetOrderQuantity(tier, resourceTypeId);
        var share = ManufactureCycleHours * BoardShare * (sameResourceOrders + 1);
        var capacityLimit = Math.Max(2, (int)Math.Floor(capacity * tier.DurationSeconds / 3600.0 / share));
        return Math.Min(quantity, capacityLimit);
    }

    public void EnsureOrderBoard(int playerId)
    {
        var count = _context.Orders.Count(x => x.PlayerId == playerId);
        var player = _context.Players.First(x => x.Id == playerId);
        if (count >= BoardSize)
        {
            if (player.NextOrderRefillAt != null)
            {
                player.NextOrderRefillAt = null;
                _context.SaveChanges();
            }

            return;
        }

        if (player.NextOrderRefillAt != null && DateTimeHelper.GetNowDate() < player.NextOrderRefillAt)
        {
            return;
        }

        var villageLevel = _villageLevelCalculator.GetLevel(playerId).Level;
        var created = new List<CalculateInfo>();
        var boardOrders = _context.OrderResources
            .Where(x => x.Order.PlayerId == playerId)
            .Select(x => new { x.Order.NeighborId, x.ResourceTypeId })
            .ToArray()
            .Select(x => (x.NeighborId, x.ResourceTypeId))
            .ToList();

        while (count < BoardSize)
        {
            var calcInfo = CreateOrder(playerId, villageLevel, boardOrders, player.FriendNeighborId);
            created.Add(calcInfo);
            count++;
        }

        if (created.Count > 0)
        {
            var errandCalcInfo = _errandManager.TryRollOffer(playerId, villageLevel);
            if (errandCalcInfo != null)
            {
                created.Add(errandCalcInfo);
            }
        }

        player.NextOrderRefillAt = null;
        _context.SaveChanges();

        var afterEventAction = _uow.AfterEventAction;
        _uow.AfterEventAction = () =>
        {
            afterEventAction?.Invoke();
            foreach (var calcInfo in created)
            {
                _calculator.Insert(calcInfo);
            }
        };
    }

    public IEnumerable<Order> GetOrders(int playerId)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);
        EnsureOrderBoard(playerId);
        return LoadOrders(playerId);
    }

    public void CompleteOrder(int playerId, int orderId)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);

        var dbOrder = _context.Orders.Include(x => x.Resources).FirstOrDefault(x => x.Id == orderId);
        if (dbOrder == null || dbOrder.PlayerId != playerId)
        {
            throw new BusinessException("Заказ не найден");
        }

        if (DateTimeHelper.GetNowDate() >= dbOrder.ExpireDate)
        {
            throw new BusinessException("Заказ истёк");
        }

        var resourceTypes = _resourceManager.GetResourceTypes();
        var resources = dbOrder.Resources.Select(x => new Resource
            {
                Type = resourceTypes.First(y => y.Id == x.ResourceTypeId),
                Value = x.Value,
            })
            .ToArray();

        _playerResourceManager.WriteOffResources(playerId, resources);
        var orderBonus = _tolokaManager.GetOrderRewardBonusPercent(playerId, DateTimeHelper.GetNowDate());
        var rewardCoins = (int)Math.Round(dbOrder.RewardCoins * (100 + orderBonus) / 100.0, MidpointRounding.AwayFromZero);
        _playerResourceManager.GrantResource(playerId, CoinResourceTypeId, rewardCoins);
        _playerResourceManager.GrantResource(playerId, GoldResourceTypeId, dbOrder.RewardGold);
        _playerResourceManager.GrantReputation(playerId, dbOrder.NeighborId, dbOrder.RewardReputation);
        _seasonManager.IncrementCounter(playerId, SeasonMetric.Orders, rewardCoins, DateTimeHelper.GetNowDate());

        _context.Orders.Remove(dbOrder);
        _context.SaveChanges();

        var player = _context.Players.First(x => x.Id == playerId);
        if (player.NextOrderRefillAt == null)
        {
            player.NextOrderRefillAt = DateTimeHelper.GetNowDate().AddSeconds(OrderRefillDelaySeconds);
            _context.SaveChanges();
        }

        EnsureOrderBoard(playerId);
        _goalManager.OnOrderCompleted(playerId);
    }

    /// <summary>
    /// Уступает заказ соседу без выполнения: ресурсы не списываются, награда не начисляется, слот освобождается.
    /// </summary>
    /// <remarks>
    /// Задержка на пополнение доски накапливается: уступка отодвигает <see cref="Data.Entities.Player.NextOrderRefillAt"/>
    /// на <see cref="OrderRefillDelaySeconds"/> вперёд от уже выставленного момента (если он ещё в будущем), а не выставляет
    /// его заново от текущего времени – поэтому сдать всю доску разом и тут же получить новую нельзя.
    /// </remarks>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="orderId">Идентификатор заказа, от которого игрок отказывается.</param>
    public void CancelOrder(int playerId, int orderId)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);

        var dbOrder = _context.Orders.FirstOrDefault(x => x.Id == orderId);
        if (dbOrder == null || dbOrder.PlayerId != playerId)
        {
            throw new BusinessException("Этого заказа уже нет на доске – видно, сосед не дождался.");
        }

        _context.Orders.Remove(dbOrder);
        _context.SaveChanges();

        var player = _context.Players.First(x => x.Id == playerId);
        var now = DateTimeHelper.GetNowDate();
        var baseline = player.NextOrderRefillAt is { } existingRefillAt && existingRefillAt > now ? existingRefillAt : now;
        player.NextOrderRefillAt = baseline.AddSeconds(OrderRefillDelaySeconds);
        _context.SaveChanges();

        EnsureOrderBoard(playerId);
    }

    public bool FinishOrder(DateTime date, CalculateInfo calcInfo)
    {
        _playerResourceManager.LockDbPlayerRow(calcInfo.PlayerId);

        var dbOrder = _context.Orders.FirstOrDefault(x => x.Id == calcInfo.ObjectId && x.PlayerId == calcInfo.PlayerId);
        if (dbOrder == null)
        {
            return true;
        }

        if (date >= dbOrder.ExpireDate)
        {
            _context.Orders.Remove(dbOrder);
            _context.SaveChanges();

            var player = _context.Players.First(x => x.Id == calcInfo.PlayerId);
            if (player.NextOrderRefillAt == null)
            {
                player.NextOrderRefillAt = DateTimeHelper.GetNowDate().AddSeconds(OrderRefillDelaySeconds);
                _context.SaveChanges();
            }

            EnsureOrderBoard(calcInfo.PlayerId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Назначает (или снимает) дружбу игрока с соседом: заказы дружественного соседа впредь чаще появляются на доске.
    /// </summary>
    /// <remarks>
    /// Не трогает уже выложенные на доску заказы – эффект проявляется только в последующей генерации (см. <see cref="CreateOrder"/>).
    /// </remarks>
    /// <param name="playerId">Игрок.</param>
    /// <param name="neighborId">Сосед, с которым назначается дружба – ссылка на справочник <see cref="Neighbor.Id"/>; <see langword="null"/> снимает дружбу.</param>
    public void SetFriendNeighbor(int playerId, int? neighborId)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);

        var player = _context.Players.First(x => x.Id == playerId);

        if (neighborId != null)
        {
            var villageLevel = _villageLevelCalculator.GetLevel(playerId).Level;
            var isOpen = _resourceManager.GetNeighbors().Any(x => x.Id == neighborId)
                && _villageLevelCalculator.GetOpenNeighbors(villageLevel).Any(x => x.Id == neighborId);

            if (!isOpen)
            {
                throw new BusinessException("С этой деревней вы пока не знакомы – дорога к ней откроется с ростом обжитости.");
            }
        }

        player.FriendNeighborId = neighborId;
        _context.SaveChanges();
    }

    public IEnumerable<NeighborReputation> GetReputation(int playerId)
    {
        var player = _context.Players.First(x => x.Id == playerId);
        var reputations = _context.NeighborReputations.Where(x => x.PlayerId == playerId).ToArray();
        var villageLevel = _villageLevelCalculator.GetLevel(playerId).Level;
        var openNeighborIds = _villageLevelCalculator.GetOpenNeighbors(villageLevel).Select(x => x.Id).ToHashSet();
        return _resourceManager.GetNeighbors()
            .Select(neighbor =>
            {
                var points = reputations.FirstOrDefault(x => x.NeighborId == neighbor.Id)?.Points ?? 0;
                var (nextThreshold, nextRewardName) = GetNextReputationMilestone(neighbor, points);
                return new NeighborReputation
                {
                    Neighbor = neighbor,
                    Points = points,
                    NextThreshold = nextThreshold,
                    NextRewardName = nextRewardName,
                    IsFriend = player.FriendNeighborId == neighbor.Id,
                    IsOpen = openNeighborIds.Contains(neighbor.Id),
                };
            })
            .ToArray();
    }

    private (int? Threshold, string? Name) GetNextReputationMilestone(Neighbor neighbor, int points)
    {
        var candidates = new List<(int Threshold, string Name)>();

        candidates.AddRange(_resourceManager.GetBlueprints()
            .Where(x => x.NeighborId == neighbor.Id)
            .Select(x => (x.ReputationThreshold, $"«{x.Name}»")));

        candidates.AddRange(_resourceManager.GetDecorTypes()
            .Where(x => x.NeighborId == neighbor.Id && x.ReputationThreshold > 0)
            .Select(x => (x.ReputationThreshold, $"украшение «{x.Name}»")));

        candidates.Add((ConvoyManager.AccessReputationThreshold, "обоз соседа"));
        candidates.Add((ConvoyManager.SecondaryReputationThreshold, "второй товар в обозе"));
        candidates.Add((ConvoyManager.HighLimitReputationThreshold, "щедрый обоз"));

        var next = candidates.Where(x => x.Threshold > points).OrderBy(x => x.Threshold).FirstOrDefault();
        return next.Name == null ? (null, null) : (next.Threshold, next.Name);
    }

    public int GetCapacity(int playerId, int resourceTypeId)
    {
        var domikTypes = _resourceManager.GetDomikTypes().ToDictionary(x => x.Id);
        var receipts = _resourceManager.GetReceipts().ToDictionary(x => x.Id);
        var slots = _context.Domiks
            .Where(x => x.PlayerId == playerId && x.Level >= 1)
            .Select(x => new { x.TypeId, x.Level })
            .ToArray()
            .Select(d => domikTypes[d.TypeId].Levels.First(l => l.Value == d.Level))
            .Where(level => level.Receipts.Any(r => receipts[r.Id].OutputResources.Any(o => o.Type.Id == resourceTypeId)))
            .Sum(level => level.MaxManufactureCount);

        return Math.Min(_workerManager.GetCapacity(playerId), slots);
    }

    private int[] GetProducibleResourceTypeIds(int playerId)
    {
        var domikTypes = _resourceManager.GetDomikTypes().ToDictionary(x => x.Id);
        var receipts = _resourceManager.GetReceipts().ToDictionary(x => x.Id);
        return _context.Domiks
            .Where(x => x.PlayerId == playerId && x.Level >= 1)
            .Select(x => new { x.TypeId, x.Level })
            .ToArray()
            .SelectMany(d => domikTypes[d.TypeId].Levels.First(l => l.Value == d.Level).Receipts)
            .SelectMany(r => receipts[r.Id].OutputResources)
            .Select(r => r.Type.Id)
            .Distinct()
            .ToArray();
    }

    private CalculateInfo CreateOrder(int playerId, int villageLevel, List<(int NeighborId, int ResourceTypeId)> boardOrders, int? friendNeighborId)
    {
        var neighbors = _villageLevelCalculator.GetOpenNeighbors(villageLevel);
        var producible = GetProducibleResourceTypeIds(playerId);
        var pairs = neighbors
            .SelectMany(n => new[] { (Neighbor: n, ResourceTypeId: n.PrimaryResourceTypeId), (Neighbor: n, ResourceTypeId: n.SecondaryResourceTypeId ?? 0) })
            .Where(x => x.ResourceTypeId != 0 && producible.Contains(x.ResourceTypeId))
            .ToArray();

        var friendHasFreeSlot = boardOrders.Count(x => x.NeighborId == friendNeighborId) < FriendBoardLimit;
        var distinctPairs = pairs
            .Where(x => (friendHasFreeSlot && x.Neighbor.Id == friendNeighborId) || boardOrders.All(y => y.ResourceTypeId != x.ResourceTypeId))
            .ToArray();

        if (distinctPairs.Length > 0)
        {
            pairs = distinctPairs;
        }

        Neighbor neighbor;
        int resourceTypeId;
        if (pairs.Length > 0)
        {
            var weights = pairs.Select(x => x.Neighbor.Id == friendNeighborId ? FriendWeight : 1).ToArray();
            var roll = Random.Shared.Next(weights.Sum());
            var cumulative = 0;
            var index = 0;
            for (var i = 0; i < pairs.Length; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                {
                    index = i;
                    break;
                }
            }

            (neighbor, resourceTypeId) = pairs[index];
        }
        else
        {
            neighbor = neighbors[Random.Shared.Next(neighbors.Length)];
            resourceTypeId = neighbor.PrimaryResourceTypeId;
        }

        var sameResourceOrders = boardOrders.Count(x => x.ResourceTypeId == resourceTypeId);
        boardOrders.Add((neighbor.Id, resourceTypeId));
        var tier = Tiers[Random.Shared.Next(Tiers.Length)];
        var now = DateTimeHelper.GetNowDate();
        var expireDate = now.AddSeconds(tier.DurationSeconds);
        var capacity = GetCapacity(playerId, resourceTypeId);
        var quantity = GetEffectiveQuantity(tier, resourceTypeId, capacity, sameResourceOrders);
        var rewardCoins = (int)Math.Round(quantity * ResourceManager.GetMarketValue(resourceTypeId) * tier.DemandMultiplier, MidpointRounding.AwayFromZero);

        var order = new Data.Entities.Order
        {
            PlayerId = playerId,
            NeighborId = neighbor.Id,
            CreateDate = now,
            ExpireDate = expireDate,
            RewardCoins = rewardCoins,
            RewardGold = tier.RewardGold,
            RewardReputation = tier.RewardReputation,
        };

        _context.Orders.Add(order);
        _context.SaveChanges();

        _context.OrderResources.Add(new()
        {
            OrderId = order.Id,
            ResourceTypeId = resourceTypeId,
            Value = quantity,
        });

        _context.SaveChanges();

        return new()
        {
            PlayerId = playerId,
            ObjectId = order.Id,
            Date = expireDate,
            Type = CalculateTypes.OrderExpire,
        };
    }

    private IEnumerable<Order> LoadOrders(int playerId)
    {
        var neighbors = _resourceManager.GetNeighbors();
        var resourceTypes = _resourceManager.GetResourceTypes();
        return _context.Orders.Include(x => x.Resources)
            .Where(x => x.PlayerId == playerId)
            .OrderBy(x => x.ExpireDate)
            .ThenBy(x => x.Id)
            .ToArray()
            .Select(x => new Order
            {
                Id = x.Id,
                Neighbor = neighbors.First(y => y.Id == x.NeighborId),
                CreateDate = x.CreateDate,
                ExpireDate = x.ExpireDate,
                RewardCoins = x.RewardCoins,
                RewardGold = x.RewardGold,
                RewardReputation = x.RewardReputation,
                Resources = x.Resources.Select(r => new Resource
                    {
                        Type = resourceTypes.First(y => y.Id == r.ResourceTypeId),
                        Value = r.Value,
                    })
                    .ToArray(),
            })
            .ToArray();
    }

    public class OrderTier
    {
        public OrderTier(int quantity, int durationSeconds, double demandMultiplier, int rewardGold, int rewardReputation)
        {
            Quantity = quantity;
            DurationSeconds = durationSeconds;
            DemandMultiplier = demandMultiplier;
            RewardGold = rewardGold;
            RewardReputation = rewardReputation;
        }

        public int Quantity { get; }
        public int DurationSeconds { get; }
        public double DemandMultiplier { get; }
        public int RewardGold { get; }
        public int RewardReputation { get; }
    }
}
