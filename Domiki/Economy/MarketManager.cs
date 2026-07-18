using Domiki.Web.Core.Scheduling;
using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Economy.Models;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Microsoft.EntityFrameworkCore;
using Resource = Domiki.Web.Reference.Models.Resource;
using TradeLot = Domiki.Web.Economy.Models.TradeLot;

namespace Domiki.Web.Economy;

public class MarketManager
{
    public const double CommissionRateL1 = 0.08;
    public const double CommissionRateStep = 0.0125;
    public const double CommissionRateMin = 0.03;
    public const int MinCommissionCoins = 2;
    public const int MarketLotDurationSeconds = 24 * 3600;
    public const int CoinResourceTypeId = 1;

    /// <summary>
    /// Идентификатор типа ресурса «золото» – единственная разрешённая оплата в заявке на покупку (<see cref="TradeLotKind.Buy"/>).
    /// </summary>
    /// <remarks>
    /// Совпадает с сидом справочника ресурсов (<c>ResourceIds.Gold</c>); при выставлении обратного лота золото эскроуится в give-стороне.
    /// </remarks>
    public const int GoldResourceTypeId = 5;

    private readonly UnitOfWork _uow;
    private readonly ApplicationDbContext _context;
    private readonly ICalculator _calculator;
    private readonly ResourceManager _resourceManager;
    private readonly PlayerResourceManager _playerResourceManager;
    private readonly PlayerEventManager _playerEventManager;
    private readonly GameStateBroker _broker;
    private readonly PushSender _pushSender;

    public MarketManager(UnitOfWork uow, ApplicationDbContext context, ICalculator calculator, ResourceManager resourceManager, PlayerResourceManager playerResourceManager, PlayerEventManager playerEventManager, GameStateBroker broker, PushSender pushSender)
    {
        _uow = uow;
        _context = context;
        _calculator = calculator;
        _resourceManager = resourceManager;
        _playerResourceManager = playerResourceManager;
        _playerEventManager = playerEventManager;
        _broker = broker;
        _pushSender = pushSender;
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

    public MarketState? GetMarket(int playerId)
    {
        var level = GetMarketYardLevel(playerId);
        if (level < 1)
        {
            return null;
        }

        var marketType = _resourceManager.GetDomikTypes().First(x => x.LogicName == "market_yard");
        var nextLevel = level + 1;
        var lots = _context.TradeLots.Include(x => x.Seller)
            .Where(x => x.ExpireDate > DateTimeHelper.GetNowDate())
            .OrderBy(x => x.ExpireDate)
            .ThenBy(x => x.Id)
            .ToArray();

        return new()
        {
            Lots = lots.Where(x => x.SellerId != playerId).Select(ToModel).ToArray(),
            MyLots = lots.Where(x => x.SellerId == playerId).Select(ToModel).ToArray(),
            BuildingLevel = level,
            CommissionRate = GetCommissionRate(level),
            CommissionMin = MinCommissionCoins,
            NextCommissionRate = nextLevel <= marketType.MaxLevel ? GetCommissionRate(nextLevel) : null,
            MaxLots = level + 1,
        };
    }

    public void PostLot(int playerId, TradeLotKind kind, int giveResourceTypeId, int giveValue, int wantResourceTypeId, int wantValue, DateTime date)
    {
        ValidateLot(kind, giveResourceTypeId, giveValue, wantResourceTypeId, wantValue);
        _playerResourceManager.LockDbPlayerRow(playerId);

        var level = GetMarketYardLevel(playerId);
        if (level < 1)
        {
            throw new BusinessException("Нужен Торговый двор");
        }

        if (_context.TradeLots.Count(x => x.SellerId == playerId && x.ExpireDate > date) >= level + 1)
        {
            throw new BusinessException("Все места на прилавке заняты – улучшите Торговый двор");
        }

        var commission = ComputeCommission(level, giveResourceTypeId, giveValue);
        _playerResourceManager.WriteOffResources(playerId, new[]
        {
            new Resource
            {
                Type = new()
                    { Id = giveResourceTypeId },
                Value = giveValue,
            },
            new Resource
            {
                Type = new()
                    { Id = CoinResourceTypeId },
                Value = commission,
            },
        });

        var lot = new Data.Entities.TradeLot
        {
            SellerId = playerId,
            Kind = kind,
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
            _broker.Broadcast(GameStateScopes.Market);
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
            new Resource
            {
                Type = new()
                    { Id = lot.WantResourceTypeId },
                Value = lot.WantValue,
            },
        });

        _playerResourceManager.GrantResource(buyerId, lot.GiveResourceTypeId, lot.GiveValue);
        _playerResourceManager.GrantResource(lot.SellerId, lot.WantResourceTypeId, lot.WantValue);
        _playerEventManager.Record(lot.SellerId, PlayerEventType.LotSold, new { giveResourceTypeId = lot.GiveResourceTypeId, giveValue = lot.GiveValue, wantResourceTypeId = lot.WantResourceTypeId, wantValue = lot.WantValue });

        var resourceTypes = _resourceManager.GetResourceTypes();
        var giveName = resourceTypes.First(x => x.Id == lot.GiveResourceTypeId).Name;
        var giveValue = lot.GiveValue;
        var wantName = resourceTypes.First(x => x.Id == lot.WantResourceTypeId).Name;
        var wantValue = lot.WantValue;
        var isBuy = lot.Kind == TradeLotKind.Buy;

        _context.TradeLots.Remove(lot);
        _context.SaveChanges();

        var afterEventAction = _uow.AfterEventAction;
        _uow.AfterEventAction = () =>
        {
            afterEventAction?.Invoke();
            _calculator.Remove(sellerId.Value, lotId, CalculateTypes.TradeLotExpire);
            _broker.Broadcast(GameStateScopes.Market);
            _broker.Publish(sellerId.Value, GameStateScopes.State);
            _pushSender.Notify(sellerId.Value, "Домики", isBuy
                ? $"Заявку на ярмарке исполнили: {wantName} ×{wantValue} за {giveName} ×{giveValue}"
                : $"Ваш лот на ярмарке купили: {giveName} ×{giveValue} за {wantName} ×{wantValue}", "/domiki-page");
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
            _broker.Broadcast(GameStateScopes.Market);
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
            _playerEventManager.Record(calcInfo.PlayerId, PlayerEventType.LotExpired, new { giveResourceTypeId = lot.GiveResourceTypeId, giveValue = lot.GiveValue });
            _context.TradeLots.Remove(lot);
            _context.SaveChanges();
            return true;
        }

        return false;
    }

    private static TradeLot ToModel(Data.Entities.TradeLot lot)
    {
        return new()
        {
            Id = lot.Id,
            SellerId = lot.SellerId,
            Kind = lot.Kind,
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

    private void ValidateLot(TradeLotKind kind, int giveResourceTypeId, int giveValue, int wantResourceTypeId, int wantValue)
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

        if (kind is not (TradeLotKind.Sell or TradeLotKind.Buy))
        {
            throw new BusinessException("Неверный тип лота");
        }

        if (kind == TradeLotKind.Buy)
        {
            if (giveResourceTypeId != GoldResourceTypeId)
            {
                throw new BusinessException("Заявка оплачивается только золотом");
            }

            if (wantResourceTypeId == GoldResourceTypeId || wantResourceTypeId == CoinResourceTypeId)
            {
                throw new BusinessException("Нельзя купить за золото само золото или монеты");
            }
        }
    }

    private Data.Entities.TradeLot? LockTradeLot(int lotId)
    {
        _context.Database.ExecuteSqlRaw(@"SELECT 1 FROM ""TradeLots"" WHERE ""Id"" = {0} FOR UPDATE", lotId);
        return _context.TradeLots.FirstOrDefault(x => x.Id == lotId);
    }

    private bool HasBuilding(int playerId, string logicName)
    {
        var typeId = _resourceManager.GetDomikTypes().First(x => x.LogicName == logicName).Id;
        return _context.Domiks.Any(x => x.PlayerId == playerId && x.TypeId == typeId && x.Level >= 1);
    }

    private int GetMarketYardLevel(int playerId)
    {
        var typeId = _resourceManager.GetDomikTypes().First(x => x.LogicName == "market_yard").Id;
        return _context.Domiks
                   .Where(x => x.PlayerId == playerId && x.TypeId == typeId)
                   .Select(x => (int?)x.Level)
                   .Max()
               ?? 0;
    }
}
