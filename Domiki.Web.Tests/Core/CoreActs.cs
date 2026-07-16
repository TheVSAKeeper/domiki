using Domiki.Web.Core;
using Domiki.Web.Core.Models;
using Domiki.Web.Village;
using Domiki.Web.Village.Models;

namespace Domiki.Web.Tests;

public static class CoreActs
{
    public static TestPlayer Buy(this TestPlayer p, int domikTypeId)
    {
        App.Act<DomikManager>(m => m.BuyDomik(p.Id, domikTypeId));
        return p;
    }

    public static TestPlayer Upgrade(this TestPlayer p, int domikId)
    {
        App.Act<DomikManager>(m => m.UpgradeDomik(p.Id, domikId));
        return p;
    }

    public static TestPlayer StartManufacture(this TestPlayer p, int domikId, int receiptId, bool useOptional = false, bool autoRepeat = false)
    {
        App.Act<DomikManager>(m => m.StartManufacture(p.Id, domikId, receiptId, useOptional, autoRepeat: autoRepeat));
        return p;
    }

    public static TestPlayer HurryDomik(this TestPlayer p, int domikId)
    {
        App.Act<DomikManager>(m => m.HurryDomik(p.Id, domikId));
        return p;
    }

    public static TestPlayer HurryManufacture(this TestPlayer p, int manufactureId)
    {
        App.Act<DomikManager>(m => m.HurryManufacture(p.Id, manufactureId));
        return p;
    }

    public static IReadOnlyList<(DomikType Type, int AvailableCount, int? NextCountGateLevel)> PurchaseAvailableDomiks(this TestPlayer p)
    {
        return App.Act<DomikManager, IReadOnlyList<(DomikType Type, int AvailableCount, int? NextCountGateLevel)>>(m => m.GetPurchaseAvailableDomiks(p.Id).ToList());
    }

    public static VillageLevel GetVillageLevel(this TestPlayer p)
    {
        return App.Act<VillageLevelCalculator, VillageLevel>(m => m.GetLevel(p.Id));
    }
}
