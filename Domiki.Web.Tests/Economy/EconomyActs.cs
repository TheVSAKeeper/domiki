using Domiki.Web.Core.Scheduling;
using Domiki.Web.Economy;
using Domiki.Web.Economy.Models;
using Domiki.Web.Infrastructure;
using TradeLot = Domiki.Web.Data.Entities.TradeLot;
using TradeLotKind = Domiki.Web.Data.Entities.TradeLotKind;

namespace Domiki.Web.Tests;

public static class EconomyActs
{
    public static TestPlayer PostLot(this TestPlayer p, int giveResourceTypeId, int giveValue, int wantResourceTypeId, int wantValue, TradeLotKind kind = TradeLotKind.Sell)
    {
        using (TestCalculator.Defer())
        {
            App.Act<MarketManager>(m => m.PostLot(p.Id, kind, giveResourceTypeId, giveValue, wantResourceTypeId, wantValue, DateTimeHelper.GetNowDate()));
        }

        return p;
    }

    public static TestPlayer PostBuyLot(this TestPlayer p, int giveGold, int wantResourceTypeId, int wantValue)
    {
        return p.PostLot(ResourceIds.Gold, giveGold, wantResourceTypeId, wantValue, TradeLotKind.Buy);
    }

    public static TestPlayer AcceptLot(this TestPlayer p, int lotId)
    {
        using (TestCalculator.Defer())
        {
            App.Act<MarketManager>(m => m.AcceptLot(p.Id, lotId, DateTimeHelper.GetNowDate()));
        }

        return p;
    }

    public static TestPlayer CancelLot(this TestPlayer p, int lotId)
    {
        using (TestCalculator.Defer())
        {
            App.Act<MarketManager>(m => m.CancelLot(p.Id, lotId, DateTimeHelper.GetNowDate()));
        }

        return p;
    }

    public static TestPlayer FinishTradeLot(this TestPlayer p, int lotId, DateTime date)
    {
        var result = App.Act<MarketManager, bool>(m => m.FinishTradeLot(date, new()
        {
            PlayerId = p.Id,
            ObjectId = lotId,
            Date = date,
            Type = CalculateTypes.TradeLotExpire,
        }));

        Assert.That(result, Is.True);
        return p;
    }

    public static TradeLot LastLot(this TestPlayer p)
    {
        return App.Read(context => context.TradeLots.Where(x => x.SellerId == p.Id).OrderByDescending(x => x.Id).First());
    }

    public static MarketState Market(this TestPlayer p)
    {
        var market = App.Act<MarketManager, MarketState?>(m => m.GetMarket(p.Id));
        Assert.That(market, Is.Not.Null);
        return market!;
    }

    public static MarketState? MarketOrNull(this TestPlayer p)
    {
        return App.Act<MarketManager, MarketState?>(m => m.GetMarket(p.Id));
    }

    public static IReadOnlyList<Order> Orders(this TestPlayer p)
    {
        return App.Act<OrderManager, IReadOnlyList<Order>>(m => m.GetOrders(p.Id).ToList());
    }

    public static TestPlayer CompleteOrder(this TestPlayer p, int orderId)
    {
        App.Act<OrderManager>(m => m.CompleteOrder(p.Id, orderId));
        return p;
    }

    public static TestPlayer CancelOrder(this TestPlayer p, int orderId)
    {
        App.Act<OrderManager>(m => m.CancelOrder(p.Id, orderId));
        return p;
    }

    public static TestPlayer FinishOrder(this TestPlayer p, int orderId, DateTime date)
    {
        var result = App.Act<OrderManager, bool>(m => m.FinishOrder(date, new()
        {
            PlayerId = p.Id,
            ObjectId = orderId,
            Date = date,
            Type = CalculateTypes.OrderExpire,
        }));

        Assert.That(result, Is.True);
        return p;
    }

    public static IReadOnlyList<NeighborReputation> Reputation(this TestPlayer p)
    {
        return App.Act<OrderManager, IReadOnlyList<NeighborReputation>>(m => m.GetReputation(p.Id).ToList());
    }

    public static int Capacity(this TestPlayer p, int resourceTypeId)
    {
        return App.Act<OrderManager, int>(m => m.GetCapacity(p.Id, resourceTypeId));
    }

    public static TestPlayer EnsureOrderBoard(this TestPlayer p)
    {
        App.Act<OrderManager>(m => m.EnsureOrderBoard(p.Id));
        return p;
    }

    public static TestPlayer BuyFromConvoy(this TestPlayer p, int neighborId, int resourceTypeId, int count = 1)
    {
        App.Act<ConvoyManager>(m => m.Buy(p.Id, neighborId, resourceTypeId, count, DateTimeHelper.GetNowDate()));
        return p;
    }

    public static IReadOnlyList<Convoy> Convoys(this TestPlayer p)
    {
        return App.Act<ConvoyManager, IReadOnlyList<Convoy>>(m => m.GetConvoys(p.Id).ToList());
    }

    public static TestPlayer SetFriendNeighbor(this TestPlayer p, int? neighborId)
    {
        App.Act<OrderManager>(m => m.SetFriendNeighbor(p.Id, neighborId));
        return p;
    }
}
