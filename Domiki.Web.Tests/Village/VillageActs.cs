using Domiki.Web.Core;
using Domiki.Web.Infrastructure;
using Domiki.Web.Village;
using Domiki.Web.Village.Dto;
using Domiki.Web.Village.Models;

namespace Domiki.Web.Tests;

public static class VillageActs
{
    public static TestPlayer SetVillageIdentity(this TestPlayer p, string name, int crestIcon, int crestColor)
    {
        App.Act<DomikManager>(m => m.SetVillageIdentity(p.Id, name, crestIcon, crestColor));
        return p;
    }

    public static VillageState Village(this TestPlayer p)
    {
        return App.Act<DomikManager, VillageState>(m => m.GetVillage(p.Id));
    }

    public static World World(this TestPlayer p)
    {
        return App.Act<WorldManager, World>(m => m.GetWorld(p.Id));
    }

    public static WorldDto WorldDto(this TestPlayer p)
    {
        return App.Act<WorldManager, WorldDto>(m => m.GetWorld(p.Id).ToDto());
    }

    public static VillageVisit Visit(this TestPlayer p)
    {
        return VisitPlayer(p.Id);
    }

    public static VillageVisitDto VisitDto(this TestPlayer p)
    {
        return App.Act<WorldManager, VillageVisitDto>(m => m.VisitVillage(p.Id).ToDto());
    }

    public static VillageVisit VisitPlayer(int playerId)
    {
        return App.Act<WorldManager, VillageVisit>(m => m.VisitVillage(playerId));
    }

    public static DecorState Decor(this TestPlayer p)
    {
        return App.Act<DecorManager, DecorState>(m => m.GetDecor(p.Id));
    }

    public static TestPlayer BuyDecor(this TestPlayer p, int decorTypeId)
    {
        App.Act<DecorManager>(m => m.BuyDecor(p.Id, decorTypeId));
        return p;
    }

    public static TestPlayer GrantDecorViaManager(this TestPlayer p, int decorTypeId, int count)
    {
        App.Act<DecorManager>(m => m.GrantDecor(p.Id, decorTypeId, count));
        return p;
    }
}
