using Domiki.Web.Core;

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

    public static TestPlayer StartManufacture(this TestPlayer p, int domikId, int receiptId, bool useOptional = false)
    {
        App.Act<DomikManager>(m => m.StartManufacture(p.Id, domikId, receiptId, useOptional));
        return p;
    }
}
