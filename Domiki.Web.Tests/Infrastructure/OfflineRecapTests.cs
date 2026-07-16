using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Infrastructure.Models;

namespace Domiki.Web.Tests;

[NonParallelizable]
public sealed class OfflineRecapTests
{
    /// <summary>
    /// Принятие лота покупателем кладёт продавцу в офлайн-сводку событие продажи (LotSold).
    /// </summary>
    [Test]
    public void AcceptedLotIsDeliveredToSellerTest()
    {
        var seller = TestPlayer.Create()
            .WithDomik(DomikIds.MarketYard)
            .WithResource(ResourceIds.Clay, 20);

        var buyer = TestPlayer.Create()
            .WithDomik(DomikIds.MarketYard)
            .WithResource(ResourceIds.Gold, 3);

        seller.PostLot(ResourceIds.Clay, 20, ResourceIds.Gold, 3);
        var lotId = seller.LastLot().Id;
        buyer.AcceptLot(lotId);

        var recap = seller.TakeRecap(DateTimeHelper.GetNowDate());
        Assert.That(recap.Events.Select(x => x.Type), Does.Contain(PlayerEventType.LotSold));
    }

    /// <summary>
    /// Событие завершения производства попадает в офлайн-сводку ровно один раз: повторный TakeRecap его уже не возвращает, но
    /// оно остаётся в истории событий игрока.
    /// </summary>
    [Test]
    public void FinishedManufactureIsDeliveredOnceTest()
    {
        var player = TestPlayer.Create();
        player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig);

        var recap = player.TakeRecap(DateTimeHelper.GetNowDate());
        Assert.That(recap.Events.Select(x => x.Type), Does.Contain(PlayerEventType.ManufactureFinished));

        var secondRecap = player.TakeRecap(DateTimeHelper.GetNowDate());
        Assert.That(secondRecap.Events, Is.Empty);

        var recentEvents = player.RecentEvents();
        Assert.That(recentEvents.Select(x => x.Type), Does.Contain(PlayerEventType.ManufactureFinished));
    }
}

file static class OfflineRecapTestsActs
{
    public static RecapModel TakeRecap(this TestPlayer p, DateTime now)
    {
        return App.Act<PlayerEventManager, RecapModel>(m => m.TakeRecap(p.Id, now));
    }

    public static IReadOnlyList<RecapEventModel> RecentEvents(this TestPlayer p)
    {
        return App.Act<PlayerEventManager, IReadOnlyList<RecapEventModel>>(m => m.GetRecentEvents(p.Id).ToList());
    }
}
