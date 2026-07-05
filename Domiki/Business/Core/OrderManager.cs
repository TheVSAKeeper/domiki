using Domiki.Web.Business.Models;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Business.Core
{
    public class OrderManager
    {
        private const int BoardSize = 3;
        private const int CoinResourceTypeId = 1;
        private const int GoldResourceTypeId = 5;

        private Data.ApplicationDbContext _context;
        private ICalculator _calculator;
        private Data.UnitOfWork _uow;
        private ResourceManager _resourceManager;
        private PlayerResourceManager _playerResourceManager;

        private readonly OrderTier[] _tiers =
        {
            new OrderTier(5, 4 * 60 * 60, 1.5, 0, 1),
            new OrderTier(15, 8 * 60 * 60, 2.0, 1, 2),
            new OrderTier(30, 24 * 60 * 60, 3.0, 2, 4),
        };

        public OrderManager(
            Data.UnitOfWork uow,
            Data.ApplicationDbContext context,
            ICalculator calculator,
            ResourceManager resourceManager,
            PlayerResourceManager playerResourceManager)
        {
            _context = context;
            _calculator = calculator;
            _uow = uow;
            _resourceManager = resourceManager;
            _playerResourceManager = playerResourceManager;
        }

        public void EnsureOrderBoard(int playerId)
        {
            var count = _context.Orders.Count(x => x.PlayerId == playerId);
            var created = new List<CalculateInfo>();

            while (count < BoardSize)
            {
                var calcInfo = CreateOrder(playerId);
                created.Add(calcInfo);
                count++;
            }

            if (created.Count == 0)
            {
                return;
            }

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
            _playerResourceManager.GrantResource(playerId, CoinResourceTypeId, dbOrder.RewardCoins);
            _playerResourceManager.GrantResource(playerId, GoldResourceTypeId, dbOrder.RewardGold);
            GrantReputation(playerId, dbOrder.NeighborId, dbOrder.RewardReputation);

            _context.Orders.Remove(dbOrder);
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

        private CalculateInfo CreateOrder(int playerId)
        {
            var neighbors = _resourceManager.GetNeighbors();
            var neighbor = neighbors[Random.Shared.Next(neighbors.Length)];
            var tier = _tiers[Random.Shared.Next(_tiers.Length)];
            var now = DateTimeHelper.GetNowDate();
            var expireDate = now.AddSeconds(tier.DurationSeconds);
            var rewardCoins = (int)Math.Round(tier.Quantity * GetMarketValue(neighbor.PrimaryResourceTypeId) * tier.DemandMultiplier, MidpointRounding.AwayFromZero);

            var order = new Data.Order
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

            _context.OrderResources.Add(new Data.OrderResource
            {
                OrderId = order.Id,
                ResourceTypeId = neighbor.PrimaryResourceTypeId,
                Value = tier.Quantity,
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

        private void GrantReputation(int playerId, int neighborId, int points)
        {
            var reputation = _context.NeighborReputations.FirstOrDefault(x => x.PlayerId == playerId && x.NeighborId == neighborId);
            if (reputation == null)
            {
                reputation = new Data.NeighborReputation { PlayerId = playerId, NeighborId = neighborId };
                _context.NeighborReputations.Add(reputation);
            }

            reputation.Points += points;
        }

        private int GetMarketValue(int resourceTypeId)
        {
            if (resourceTypeId == 6 || resourceTypeId == 7)
            {
                return 35;
            }

            return 10;
        }

        private class OrderTier
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
