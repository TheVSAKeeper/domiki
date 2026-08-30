using Domiki.Web.Infrastructure;
using System.Collections.Concurrent;
using System.Text.Json;
using Domiki.Web.Infrastructure.Models;
using PlayerEventGroup = Domiki.Web.Data.Entities.PlayerEventGroup;
using PlayerEventType = Domiki.Web.Data.Entities.PlayerEventType;

namespace Domiki.Web.Tests;

public sealed class JournalTests
{
    /// <summary>
    /// Витрина «Пока вас не было» ничего не удаляет: журнал держит больше записей, чем отдаёт одна страница.
    /// </summary>
    [Test]
    public void TakeRecapKeepsHistoryBeyondOnePageTest()
    {
        const int eventCount = 60;

        var player = TestPlayer.Create();
        player.RecordGoldCap(eventCount);

        App.Act<PlayerEventManager, RecapModel>(m => m.TakeRecap(player.Id, DateTimeHelper.GetNowDate()));

        Assert.That(player.EventCount(), Is.EqualTo(eventCount));
    }

    /// <summary>
    /// Параллельные витрины одного игрока не отдают событие дважды: курсор доставки не откатывается назад.
    /// </summary>
    [Test]
    public void ConcurrentRecapDeliversEachEventOnceTest()
    {
        const int eventCount = 20;

        var player = TestPlayer.Create();
        player.RecordGoldCap(eventCount);

        var delivered = new ConcurrentBag<long>();
        Parallel.ForEach(Enumerable.Range(0, 6), _ =>
        {
            using var scope = App.Scope();
            scope.Get<UnitOfWork>();
            var recap = scope.Get<PlayerEventManager>().TakeRecap(player.Id, DateTimeHelper.GetNowDate());
            scope.Commit();
            foreach (var playerEvent in recap.Events)
            {
                delivered.Add(playerEvent.Id);
            }
        });

        Assert.That(delivered, Is.Unique);
    }

    /// <summary>
    /// Страница вглубь отдаёт записи строго старше курсора, новейшие первыми, и стыкуется с первой страницей без разрывов.
    /// </summary>
    [Test]
    public void JournalPageContinuesFromCursorTest()
    {
        const int eventCount = 50;
        const int pageSize = 20;

        var player = TestPlayer.Create();
        player.RecordGoldCap(eventCount);

        var firstPage = App.Act<PlayerEventManager, List<RecapEventModel>>(m => m.GetEventsBefore(player.Id, null, pageSize));
        var secondPage = App.Act<PlayerEventManager, List<RecapEventModel>>(m => m.GetEventsBefore(player.Id, firstPage[^1].Id, pageSize));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstPage, Has.Count.EqualTo(pageSize));
            Assert.That(secondPage, Has.Count.EqualTo(pageSize));
            Assert.That(firstPage.Select(x => x.Id), Is.Ordered.Descending);
            Assert.That(secondPage.Select(x => x.Id), Is.Ordered.Descending);
            Assert.That(secondPage[0].Id, Is.LessThan(firstPage[^1].Id));
            Assert.That(firstPage.Select(x => x.Id).Intersect(secondPage.Select(x => x.Id)), Is.Empty);
        }
    }

    /// <summary>
    /// Размер страницы ограничен потолком: запрос сверх него отдаёт не больше <see cref="PlayerEventManager.MaxPageSize"/>.
    /// </summary>
    [Test]
    public void JournalPageIsCappedTest()
    {
        var eventCount = PlayerEventManager.MaxPageSize + 10;

        var player = TestPlayer.Create();
        player.RecordGoldCap(eventCount);

        var page = App.Act<PlayerEventManager, List<RecapEventModel>>(m => m.GetEventsBefore(player.Id, null, eventCount));

        Assert.That(page, Has.Count.EqualTo(PlayerEventManager.MaxPageSize));
    }

    /// <summary>
    /// Витрина, не вместившая всё за раз, всё равно сдвигает отметку последнего захода – по ней выдаётся соседский
    /// подарок, замороженная отметка раздавала бы его на каждом запросе. Время отсутствия для остатка берётся от
    /// самого старого недоставленного события.
    /// </summary>
    [Test]
    public void OverflowingRecapMovesLastSeenTest()
    {
        const int awaySeconds = 600;

        var player = TestPlayer.Create();
        var now = DateTimeHelper.GetNowDate();
        player.SetLastSeen(now.AddSeconds(-awaySeconds));
        player.AddEvents(PlayerEventManager.RecapBatch + 10, now.AddSeconds(-awaySeconds));

        var first = App.Act<PlayerEventManager, RecapModel>(m => m.TakeRecap(player.Id, now));
        var second = App.Act<PlayerEventManager, RecapModel>(m => m.TakeRecap(player.Id, now));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.Events, Has.Count.EqualTo(PlayerEventManager.RecapBatch));
            Assert.That(first.AwaySeconds, Is.EqualTo(awaySeconds));
            Assert.That(second.Events, Has.Count.EqualTo(10));
            Assert.That(second.AwaySeconds, Is.EqualTo(awaySeconds));
            Assert.That(player.LastSeen(), Is.EqualTo(now));
        }
    }

    /// <summary>
    /// Повреждённая нагрузка одной записи не роняет всю страницу: событие отдаётся с пустыми данными.
    /// </summary>
    [Test]
    public void BrokenPayloadDoesNotBreakThePageTest()
    {
        const int eventCount = 3;

        var player = TestPlayer.Create();
        player.RecordGoldCap(eventCount);
        player.BreakOldestEventPayload();

        var page = App.Act<PlayerEventManager, List<RecapEventModel>>(m => m.GetEventsBefore(player.Id, null, eventCount));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(page, Has.Count.EqualTo(eventCount));
            Assert.That(page[^1].Data.ValueKind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(page[^1].Data.EnumerateObject().Any(), Is.False);
        }
    }

    /// <summary>
    /// Фильтр по группе отдаёт только события своей группы, а счётчик называет объём именно этой группы.
    /// </summary>
    [Test]
    public void GroupFilterNarrowsPageAndCountTest()
    {
        const int householdCount = 4;
        const int marketCount = 2;

        var player = TestPlayer.Create();
        player.RecordGoldCap(householdCount);
        player.AddEventsOfType(PlayerEventType.LotExpired, marketCount);

        var marketPage = App.Act<PlayerEventManager, List<RecapEventModel>>(m => m.GetEventsBefore(player.Id, null, 50, PlayerEventGroup.Market));
        var marketTotal = App.Act<PlayerEventManager, int>(m => m.CountEvents(player.Id, PlayerEventGroup.Market));
        var allTotal = App.Act<PlayerEventManager, int>(m => m.CountEvents(player.Id));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(marketPage.Select(x => x.Type), Is.All.EqualTo(PlayerEventType.LotExpired));
            Assert.That(marketTotal, Is.EqualTo(marketCount));
            Assert.That(allTotal, Is.EqualTo(householdCount + marketCount));
        }
    }

    /// <summary>
    /// Ретроспектива считает события по группам за окно и не берёт то, что случилось раньше него.
    /// </summary>
    [Test]
    public void DigestCountsByGroupWithinWindowTest()
    {
        const int householdCount = 5;
        const int oldCount = 2;

        var player = TestPlayer.Create();
        var now = DateTimeHelper.GetNowDate();
        player.RecordGoldCap(householdCount);
        player.SetOldestEventDates(oldCount, now.AddDays(-30));

        var digest = App.Act<PlayerEventManager, Dictionary<PlayerEventGroup, int>>(m => m.CountByGroup(player.Id, now.AddDays(-7)));

        Assert.That(digest, Is.EqualTo(new Dictionary<PlayerEventGroup, int> { [PlayerEventGroup.Household] = householdCount - oldCount }));
    }

    /// <summary>
    /// Чистка удаляет события старше границы окна и не трогает те, что моложе неё.
    /// </summary>
    [Test]
    public void CleanupDeletesOnlyEventsOlderThanCutoffTest()
    {
        const int oldCount = 5;
        const int freshCount = 3;

        var player = TestPlayer.Create();
        var now = DateTimeHelper.GetNowDate();
        player.RecordGoldCap(oldCount + freshCount);
        player.SetOldestEventDates(oldCount, now - PlayerEventManager.Retention - TimeSpan.FromDays(1));

        var deleted = App.Act<PlayerEventManager, int>(m => m.DeleteOlderThan(now - PlayerEventManager.Retention, 500));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deleted, Is.EqualTo(oldCount));
            Assert.That(player.EventCount(), Is.EqualTo(freshCount));
        }
    }
}

file static class JournalTestsActs
{
    public static TestPlayer RecordGoldCap(this TestPlayer p, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var mined = i;
            App.Act<PlayerEventManager>(m => m.Record(p.Id, PlayerEventType.ManufactureGoldCapReached, new { domikId = 1, domikTypeId = 1, receiptId = 1, mined, cap = 1 }));
        }

        return p;
    }

    public static TestPlayer BreakOldestEventPayload(this TestPlayer p)
    {
        using var scope = App.Scope();
        var playerEvent = scope.Context.PlayerEvents.Where(x => x.PlayerId == p.Id).OrderBy(x => x.Id).First();
        playerEvent.Data = "{не json";
        scope.Context.SaveChanges();
        return p;
    }

    public static TestPlayer AddEvents(this TestPlayer p, int count, DateTime date)
    {
        using var scope = App.Scope();
        for (var i = 0; i < count; i++)
        {
            scope.Context.PlayerEvents.Add(new()
            {
                PlayerId = p.Id,
                Type = PlayerEventType.ManufactureGoldCapReached,
                Date = date,
                Data = "{}",
            });
        }

        scope.Context.SaveChanges();
        return p;
    }

    public static TestPlayer AddEventsOfType(this TestPlayer p, PlayerEventType type, int count)
    {
        for (var i = 0; i < count; i++)
        {
            App.Act<PlayerEventManager>(m => m.Record(p.Id, type, new { giveResourceTypeId = 1, giveValue = 1 }));
        }

        return p;
    }

    public static DateTime? LastSeen(this TestPlayer p)
    {
        return App.Read(context => context.Players.Single(x => x.Id == p.Id).LastSeen);
    }

    public static TestPlayer SetLastSeen(this TestPlayer p, DateTime date)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == p.Id).LastSeen = date;
        scope.Context.SaveChanges();
        return p;
    }

    public static int EventCount(this TestPlayer p)
    {
        return App.Read(context => context.PlayerEvents.Count(x => x.PlayerId == p.Id));
    }

    public static TestPlayer SetOldestEventDates(this TestPlayer p, int count, DateTime date)
    {
        using var scope = App.Scope();
        var ids = scope.Context.PlayerEvents.Where(x => x.PlayerId == p.Id).OrderBy(x => x.Id).Take(count).Select(x => x.Id).ToList();
        foreach (var playerEvent in scope.Context.PlayerEvents.Where(x => ids.Contains(x.Id)))
        {
            playerEvent.Date = date;
        }

        scope.Context.SaveChanges();
        return p;
    }
}
