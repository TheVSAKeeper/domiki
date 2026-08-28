using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Infrastructure.Models;
using System.Text.Json;

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

    /// <summary>
    /// Событие, дополненное слиянием уже после доставки, не выдаётся витриной повторно: курсор считает по идентификатору.
    /// </summary>
    [Test]
    public void MergeIntoDeliveredEventDoesNotRedeliverItTest()
    {
        var player = TestPlayer.Create();
        player.RecordManufactureFinished(DomikIds.Forge);

        var recap = player.TakeRecap(DateTimeHelper.GetNowDate());
        Assert.That(recap.Events.Count(x => x.Type == PlayerEventType.ManufactureFinished), Is.EqualTo(1));

        player.RecordManufactureFinished(DomikIds.Forge);

        var secondRecap = player.TakeRecap(DateTimeHelper.GetNowDate());
        Assert.That(secondRecap.Events, Is.Empty);

        var events = App.Read(context => context.PlayerEvents.Where(x => x.PlayerId == player.Id && x.Type == PlayerEventType.ManufactureFinished).ToList());
        Assert.That(events, Has.Count.EqualTo(1));
        using var data = JsonDocument.Parse(events[0].Data);
        Assert.That(data.RootElement.GetProperty("cycles").GetInt32(), Is.EqualTo(2));
    }

    /// <summary>
    /// Курсор доставки сдвигается по идентификатору, поэтому событие, записанное после витрины, приезжает следующей витриной.
    /// </summary>
    [Test]
    public void EventRecordedAfterRecapIsDeliveredNextTimeTest()
    {
        var player = TestPlayer.Create();
        player.TakeRecap(DateTimeHelper.GetNowDate());

        player.RecordManufactureFinished(DomikIds.Barrack);

        var recap = player.TakeRecap(DateTimeHelper.GetNowDate());
        Assert.That(recap.Events.Count(x => x.Type == PlayerEventType.ManufactureFinished), Is.EqualTo(1));
    }

    /// <summary>
    /// Повторные срывы автоповтора одной постройки по одной причине схлопываются в одну запись со счётчиком.
    /// </summary>
    [Test]
    public void RepeatFailuresOfSameDomikAreMergedTest()
    {
        const int domikId = 7;

        var player = TestPlayer.Create();
        App.Act<PlayerEventManager>(m => m.RecordManufactureRepeatFailed(player.Id, domikId, DomikIds.Forge, ReceiptIds.ClayDig, "Не хватает сырья"));
        App.Act<PlayerEventManager>(m => m.RecordManufactureRepeatFailed(player.Id, domikId, DomikIds.Forge, ReceiptIds.ClayDig, "Не хватает сырья"));
        App.Act<PlayerEventManager>(m => m.RecordManufactureRepeatFailed(player.Id, domikId + 1, DomikIds.Forge, ReceiptIds.ClayDig, "Не хватает сырья"));

        var events = App.Read(context => context.PlayerEvents
            .Where(x => x.PlayerId == player.Id && x.Type == PlayerEventType.ManufactureRepeatFailed)
            .OrderBy(x => x.Id)
            .ToList());
        Assert.That(events, Has.Count.EqualTo(2));

        var counts = events.Select(x =>
        {
            using var data = JsonDocument.Parse(x.Data);
            return data.RootElement.GetProperty("count").GetInt32();
        }).ToList();
        Assert.That(counts, Is.EqualTo(new[] { 2, 1 }));
    }
}

file static class OfflineRecapTestsActs
{
    public static RecapModel TakeRecap(this TestPlayer p, DateTime now)
    {
        return App.Act<PlayerEventManager, RecapModel>(m => m.TakeRecap(p.Id, now));
    }

    public static TestPlayer RecordManufactureFinished(this TestPlayer p, int domikTypeId)
    {
        App.Act<PlayerEventManager>(m => m.RecordManufactureFinished(p.Id, domikTypeId, new() { { ResourceIds.Brick, 1 } }));
        return p;
    }

    public static IReadOnlyList<RecapEventModel> RecentEvents(this TestPlayer p)
    {
        return App.Act<PlayerEventManager, IReadOnlyList<RecapEventModel>>(m => m.GetRecentEvents(p.Id).ToList());
    }
}
