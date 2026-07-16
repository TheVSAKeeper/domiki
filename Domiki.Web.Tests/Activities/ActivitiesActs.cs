using Domiki.Web.Activities;
using Domiki.Web.Activities.Models;
using Domiki.Web.Core.Scheduling;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Tests;

public static class ActivitiesActs
{
    public static TestPlayer WithReputation(this TestPlayer p, int neighborId, int points)
    {
        App.Act<PlayerResourceManager>(m => m.GrantReputation(p.Id, neighborId, points));
        return p;
    }

    public static TestPlayer StartExpedition(this TestPlayer p, int expeditionTypeId, int[]? workerIds = null, bool provisions = false)
    {
        using (TestCalculator.Defer())
        {
            App.Act<ExpeditionManager>(m => m.StartExpedition(p.Id, expeditionTypeId, workerIds, provisions));
        }

        return p;
    }

    public static TestPlayer FinishExpedition(this TestPlayer p, int expeditionId, DateTime date)
    {
        var result = App.Act<ExpeditionManager, bool>(m => m.FinishExpedition(date, new()
        {
            PlayerId = p.Id,
            ObjectId = expeditionId,
            Date = date,
            Type = CalculateTypes.Expedition,
        }));

        Assert.That(result, Is.True);
        return p;
    }

    public static ExpeditionState Expeditions(this TestPlayer p)
    {
        var state = p.ExpeditionsOrNull();
        Assert.That(state, Is.Not.Null);
        return state!;
    }

    public static ExpeditionState? ExpeditionsOrNull(this TestPlayer p)
    {
        return App.Act<ExpeditionManager, ExpeditionState?>(m => m.GetExpeditions(p.Id));
    }
}
