using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Village;
using Domiki.Web.Village.Models;

namespace Domiki.Web.Tests;

public static class RelocationActs
{
    public static Relocation Relocation(this TestPlayer p)
    {
        return App.Act<RelocationManager, Relocation>(m => m.GetState(p.Id));
    }

    public static TestPlayer Relocate(this TestPlayer p, int valleyId = 1, string? villageName = null)
    {
        App.Act<RelocationManager>(m => m.Relocate(p.Id, valleyId, villageName));
        return p.MovePostHouseAside();
    }

    public static RelocationPlan Plan(this TestPlayer p)
    {
        return App.Act<RelocationManager, RelocationPlan>(m => m.GetPlan(p.Id));
    }

    public static MemorialPost MemorialPost(this TestPlayer p)
    {
        return App.Act<RelocationManager, MemorialPost>(m => m.GetMemorialPost(p.Id));
    }

    public static int Knots(this TestPlayer p, int level)
    {
        return App.Act<RelocationManager, int>(m => m.ComputeKnots(p.Id, level));
    }

    public static TestPlayer BuyPerk(this TestPlayer p, RelocationPerkType perkType)
    {
        App.Act<PerkManager>(m => m.BuyPerk(p.Id, perkType));
        return p;
    }

    public static TestPlayer WithKnots(this TestPlayer p, int knots)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == p.Id).MemoryKnots = knots;
        scope.Commit();
        return p;
    }

    /// <summary>
    /// Поднимает обжитость до порога переезда одной строкой репутации, не заводя сотню построек.
    /// </summary>
    public static TestPlayer AtRelocationThreshold(this TestPlayer p, int relocationCount = 0)
    {
        var threshold = VillageLevelCalculator.GetRelocationThreshold(relocationCount);
        return p.WithReputation(NeighborIds.Zarechye, threshold * VillageLevelCalculator.ReputationPointsPerMilestone / VillageLevelCalculator.ReputationWeight);
    }

    public static TestPlayer SetReputationPoints(this TestPlayer p, int neighborId, int points)
    {
        using var scope = App.Scope();
        scope.Context.NeighborReputations.Single(x => x.PlayerId == p.Id && x.NeighborId == neighborId).Points = points;
        scope.Commit();
        return p;
    }

    public static TestPlayer WithTradeLot(this TestPlayer p)
    {
        using var scope = App.Scope();
        var date = DateTimeHelper.GetNowDate();
        scope.Context.TradeLots.Add(new()
        {
            SellerId = p.Id,
            Kind = TradeLotKind.Sell,
            GiveResourceTypeId = ResourceIds.Clay,
            GiveValue = 1,
            WantResourceTypeId = ResourceIds.Coin,
            WantValue = 1,
            CommissionCoins = 0,
            CreateDate = date,
            ExpireDate = date.AddHours(1),
        });

        scope.Commit();
        return p;
    }

    public static TestPlayer BackdateRelocation(this TestPlayer p, TimeSpan age)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == p.Id).LastRelocationDate = DateTimeHelper.GetNowDate() - age;
        scope.Commit();
        return p;
    }

    public static TestPlayer BackdateVillageStart(this TestPlayer p, TimeSpan age)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == p.Id).VillageStartedDate = DateTimeHelper.GetNowDate() - age;
        scope.Commit();
        return p;
    }
}
