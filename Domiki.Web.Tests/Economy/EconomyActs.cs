using Domiki.Web.Core.Scheduling;
using Domiki.Web.Economy;
using Domiki.Web.Economy.Models;
using Domiki.Web.Infrastructure;
using TradeLot = Domiki.Web.Data.Entities.TradeLot;

namespace Domiki.Web.Tests;

public static class EconomyActs
{
    public static TestPlayer PostLot(this TestPlayer p, int giveResourceTypeId, int giveValue, int wantResourceTypeId, int wantValue)
    {
        using (TestCalculator.Defer())
        {
            App.Act<MarketManager>(m => m.PostLot(p.Id, giveResourceTypeId, giveValue, wantResourceTypeId, wantValue, DateTimeHelper.GetNowDate()));
        }

        return p;
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
}
