using Domiki.Web.Activities;
using Domiki.Web.Core.Scheduling;
using Domiki.Web.Economy.Models;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference.Models;
using Domiki.Web.Reference;
using Domiki.Web.Village.Models;
using Domiki.Web.Village;
using Domiki.Web.Workers;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Economy
{
    public class OrderManager
    {
        public const int BoardSize = 3;
        public const int OrderRefillDelaySeconds = 30 * 60;
        private const int CoinResourceTypeId = 1;
        private const int GoldResourceTypeId = 5;

        private Data.ApplicationDbContext _context;
        private ICalculator _calculator;
        private UnitOfWork _uow;
        private ResourceManager _resourceManager;
        private PlayerResourceManager _playerResourceManager;
        private WorkerManager _workerManager;
        private VillageLevelCalculator _villageLevelCalculator;
        private SeasonManager _seasonManager;
        private TolokaManager _tolokaManager;
        private GoalManager _goalManager;

        public static readonly OrderTier[] Tiers =
        {
            new OrderTier(5, 4 * 60 * 60, 1.5, 0, 1),
            new OrderTier(15, 8 * 60 * 60, 2.0, 1, 2),
            new OrderTier(30, 24 * 60 * 60, 3.0, 2, 4),
        };

        public static int GetOrderQuantity(OrderTier tier, int resourceTypeId)
        {
            return Math.Max(1, (int)Math.Round(tier.Quantity * (double)ResourceManager.BaseMarketValue / ResourceManager.GetMarketValue(resourceTypeId), MidpointRounding.AwayFromZero));
        }

        public static int GetEffectiveQuantity(OrderTier tier, int resourceTypeId, int capacity)
        {
            var quantity = GetOrderQuantity(tier, resourceTypeId);
            var capacityLimit = Math.Max(2, (int)Math.Floor(capacity * tier.DurationSeconds / 3600.0 / 1.5));
            return Math.Min(quantity, capacityLimit);
        }

        public OrderManager(
            UnitOfWork uow,
            Data.ApplicationDbContext context,
            ICalculator calculator,
            ResourceManager resourceManager,
            PlayerResourceManager playerResourceManager,
            WorkerManager workerManager,
            VillageLevelCalculator villageLevelCalculator,
            SeasonManager seasonManager,
            TolokaManager tolokaManager,
            GoalManager goalManager)
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

            while (count < BoardSize)
            {
                var calcInfo = CreateOrder(playerId, villageLevel);
                created.Add(calcInfo);
                count++;
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
            }).ToArray();

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

        public IEnumerable<NeighborReputation> GetReputation(int playerId)
        {
            var reputations = _context.NeighborReputations.Where(x => x.PlayerId == playerId).ToArray();
            return _resourceManager.GetNeighbors().Select(neighbor => new NeighborReputation
            {
                Neighbor = neighbor,
                Points = reputations.FirstOrDefault(x => x.NeighborId == neighbor.Id)?.Points ?? 0,
            }).ToArray();
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

        private CalculateInfo CreateOrder(int playerId, int villageLevel)
        {
            var neighbors = _villageLevelCalculator.GetOpenNeighbors(villageLevel);
            var producible = GetProducibleResourceTypeIds(playerId);
            var pairs = neighbors
                .SelectMany(n => new[] { (Neighbor: n, ResourceTypeId: n.PrimaryResourceTypeId), (Neighbor: n, ResourceTypeId: n.SecondaryResourceTypeId ?? 0) })
                .Where(x => x.ResourceTypeId != 0 && producible.Contains(x.ResourceTypeId))
                .ToArray();
            Neighbor neighbor;
            int resourceTypeId;
            if (pairs.Length > 0)
            {
                (neighbor, resourceTypeId) = pairs[Random.Shared.Next(pairs.Length)];
            }
            else
            {
                neighbor = neighbors[Random.Shared.Next(neighbors.Length)];
                resourceTypeId = neighbor.PrimaryResourceTypeId;
            }

            var tier = Tiers[Random.Shared.Next(Tiers.Length)];
            var now = DateTimeHelper.GetNowDate();
            var expireDate = now.AddSeconds(tier.DurationSeconds);
            var capacity = GetCapacity(playerId, resourceTypeId);
            var quantity = GetEffectiveQuantity(tier, resourceTypeId, capacity);
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

            _context.OrderResources.Add(new Data.Entities.OrderResource
            {
                OrderId = order.Id,
                ResourceTypeId = resourceTypeId,
                Value = quantity,
            });
            _context.SaveChanges();

            return new CalculateInfo
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
                    }).ToArray(),
                }).ToArray();
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
}
