using Domiki.Web.Business.Models;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Business.Core
{
    public class MarketManager
    {
        public const double CommissionRateL1 = 0.08;
        public const double CommissionRateStep = 0.0125;
        public const double CommissionRateMin = 0.03;
        public const int MinCommissionCoins = 2;
        public const int MarketLotDurationSeconds = 24 * 3600;
        public const int CoinResourceTypeId = 1;

        private readonly Data.UnitOfWork _uow;
        private readonly Data.ApplicationDbContext _context;
        private readonly ICalculator _calculator;
        private readonly ResourceManager _resourceManager;
        private readonly PlayerResourceManager _playerResourceManager;

        public MarketManager(Data.UnitOfWork uow, Data.ApplicationDbContext context, ICalculator calculator, ResourceManager resourceManager, PlayerResourceManager playerResourceManager)
        {
            _uow = uow;
            _context = context;
            _calculator = calculator;
            _resourceManager = resourceManager;
            _playerResourceManager = playerResourceManager;
        }

        public MarketState GetMarket(int playerId)
        {
            var level = GetMarketYardLevel(playerId);
            if (level < 1)
            {
                return null;
            }

            var marketType = _resourceManager.GetDomikTypes().First(x => x.LogicName == "market_yard");
            var nextLevel = level + 1;
            var lots = _context.TradeLots.Include(x => x.Seller)
                .OrderBy(x => x.ExpireDate)
                .ThenBy(x => x.Id)
                .ToArray();

            return new MarketState
            {
                Lots = lots.Where(x => x.SellerId != playerId).Select(ToModel).ToArray(),
                MyLots = lots.Where(x => x.SellerId == playerId).Select(ToModel).ToArray(),
                BuildingLevel = level,
                CommissionRate = GetCommissionRate(level),
                CommissionMin = MinCommissionCoins,
                NextCommissionRate = nextLevel <= marketType.MaxLevel ? GetCommissionRate(nextLevel) : (double?)null,
            };
        }

        public void PostLot(int playerId, int giveResourceTypeId, int giveValue, int wantResourceTypeId, int wantValue, DateTime date)
        {
            ValidateLot(giveResourceTypeId, giveValue, wantResourceTypeId, wantValue);
            _playerResourceManager.LockDbPlayerRow(playerId);

            var level = GetMarketYardLevel(playerId);
            if (level < 1)
            {
                throw new BusinessException("Нужен Торговый двор");
            }

            var commission = ComputeCommission(level, giveResourceTypeId, giveValue);
            _playerResourceManager.WriteOffResources(playerId, new[]
            {
                new Resource { Type = new ResourceType { Id = giveResourceTypeId }, Value = giveValue },
                new Resource { Type = new ResourceType { Id = CoinResourceTypeId }, Value = commission },
            });

            var lot = new Data.TradeLot
            {
                SellerId = playerId,
                GiveResourceTypeId = giveResourceTypeId,
                GiveValue = giveValue,
                WantResourceTypeId = wantResourceTypeId,
                WantValue = wantValue,
                CommissionCoins = commission,
                CreateDate = date,
                ExpireDate = date.AddSeconds(MarketLotDurationSeconds),
            };
            _context.TradeLots.Add(lot);
            _context.SaveChanges();

            var calcInfo = new CalculateInfo
            {
                PlayerId = playerId,
                ObjectId = lot.Id,
                Date = lot.ExpireDate,
                Type = CalculateTypes.TradeLotExpire,
            };
            var afterEventAction = _uow.AfterEventAction;
            _uow.AfterEventAction = () =>
            {
                afterEventAction?.Invoke();
                _calculator.Insert(calcInfo);
            };
        }

        public void AcceptLot(int buyerId, int lotId, DateTime date)
        {
            var sellerId = _context.TradeLots.AsNoTracking()
                .Where(x => x.Id == lotId)
                .Select(x => (int?)x.SellerId)
                .SingleOrDefault();
            if (sellerId == null)
            {
                throw new BusinessException("Лот не найден");
            }

            if (buyerId == sellerId.Value)
            {
                throw new BusinessException("Нельзя принять свой лот");
            }

            foreach (var playerId in new[] { buyerId, sellerId.Value }.OrderBy(x => x))
            {
                _playerResourceManager.LockDbPlayerRow(playerId);
            }

            var lot = LockTradeLot(lotId);
            if (lot == null)
            {
                throw new BusinessException("Лот уже принят или истёк");
            }

            if (!HasBuilding(buyerId, "market_yard"))
            {
                throw new BusinessException("Нужен Торговый двор");
            }

            if (date >= lot.ExpireDate)
            {
                throw new BusinessException("Лот истёк");
            }

            _playerResourceManager.WriteOffResources(buyerId, new[]
            {
                new Resource { Type = new ResourceType { Id = lot.WantResourceTypeId }, Value = lot.WantValue },
            });
            _playerResourceManager.GrantResource(buyerId, lot.GiveResourceTypeId, lot.GiveValue);
            _playerResourceManager.GrantResource(lot.SellerId, lot.WantResourceTypeId, lot.WantValue);

            _context.TradeLots.Remove(lot);
            _context.SaveChanges();

            var afterEventAction = _uow.AfterEventAction;
            _uow.AfterEventAction = () =>
            {
                afterEventAction?.Invoke();
                _calculator.Remove(sellerId.Value, lotId, CalculateTypes.TradeLotExpire);
            };
        }

        public void CancelLot(int playerId, int lotId, DateTime date)
        {
            _playerResourceManager.LockDbPlayerRow(playerId);
            var lot = LockTradeLot(lotId);
            if (lot == null || lot.SellerId != playerId)
            {
                throw new BusinessException("Лот не найден");
            }

            _playerResourceManager.GrantResource(playerId, lot.GiveResourceTypeId, lot.GiveValue);
            _context.TradeLots.Remove(lot);
            _context.SaveChanges();

            var afterEventAction = _uow.AfterEventAction;
            _uow.AfterEventAction = () =>
            {
                afterEventAction?.Invoke();
                _calculator.Remove(playerId, lotId, CalculateTypes.TradeLotExpire);
            };
        }

        public bool FinishTradeLot(DateTime date, CalculateInfo calcInfo)
        {
            _playerResourceManager.LockDbPlayerRow(calcInfo.PlayerId);

            var lot = _context.TradeLots.FirstOrDefault(x => x.Id == calcInfo.ObjectId && x.SellerId == calcInfo.PlayerId);
            if (lot == null)
            {
                return true;
            }

            if (date >= lot.ExpireDate)
            {
                _playerResourceManager.GrantResource(calcInfo.PlayerId, lot.GiveResourceTypeId, lot.GiveValue);
                _context.TradeLots.Remove(lot);
                _context.SaveChanges();
                return true;
            }

            return false;
        }

        private void ValidateLot(int giveResourceTypeId, int giveValue, int wantResourceTypeId, int wantValue)
        {
            if (giveValue <= 0 || wantValue <= 0)
            {
                throw new BusinessException("Неверное количество");
            }

            if (giveResourceTypeId == wantResourceTypeId)
            {
                throw new BusinessException("Нельзя обменять ресурс на себя");
            }

            var resourceTypes = _resourceManager.GetResourceTypes();
            if (!resourceTypes.Any(x => x.Id == giveResourceTypeId) || !resourceTypes.Any(x => x.Id == wantResourceTypeId))
            {
                throw new BusinessException("Ресурс не найден");
            }
        }

        private Data.TradeLot LockTradeLot(int lotId)
        {
            _context.Database.ExecuteSqlRaw(@"SELECT 1 FROM ""TradeLots"" WHERE ""Id"" = {0} FOR UPDATE", lotId);
            return _context.TradeLots.FirstOrDefault(x => x.Id == lotId);
        }

        private bool HasBuilding(int playerId, string logicName)
        {
            var typeId = _resourceManager.GetDomikTypes().First(x => x.LogicName == logicName).Id;
            return _context.Domiks.Any(x => x.PlayerId == playerId && x.TypeId == typeId && x.Level >= 1);
        }

        public static double GetCommissionRate(int marketLevel)
        {
            return Math.Max(CommissionRateMin, CommissionRateL1 - (marketLevel - 1) * CommissionRateStep);
        }

        public static int ComputeCommission(int marketLevel, int giveResourceTypeId, int giveValue)
        {
            var coinValue = ResourceManager.GetMarketValue(giveResourceTypeId) * giveValue;
            var rate = GetCommissionRate(marketLevel);
            return Math.Max(MinCommissionCoins, (int)Math.Round(coinValue * rate, MidpointRounding.AwayFromZero));
        }

        private int GetMarketYardLevel(int playerId)
        {
            var typeId = _resourceManager.GetDomikTypes().First(x => x.LogicName == "market_yard").Id;
            return _context.Domiks
                .Where(x => x.PlayerId == playerId && x.TypeId == typeId)
                .Select(x => (int?)x.Level)
                .Max() ?? 0;
        }

        private static TradeLot ToModel(Data.TradeLot lot)
        {
            return new TradeLot
            {
                Id = lot.Id,
                SellerId = lot.SellerId,
                SellerVillageName = lot.Seller.VillageName,
                SellerCrestIcon = lot.Seller.CrestIcon,
                SellerCrestColor = lot.Seller.CrestColor,
                GiveResourceTypeId = lot.GiveResourceTypeId,
                GiveValue = lot.GiveValue,
                WantResourceTypeId = lot.WantResourceTypeId,
                WantValue = lot.WantValue,
                CommissionCoins = lot.CommissionCoins,
                ExpireDate = lot.ExpireDate,
            };
        }
    }
}
