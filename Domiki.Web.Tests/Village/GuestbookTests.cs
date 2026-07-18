using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Village;
using Domiki.Web.Village.Dto;
using Domiki.Web.Village.Models;
using System.Text.Json;

namespace Domiki.Web.Tests;

public sealed class GuestbookTests
{
    /// <summary>
    /// Первый визит гостя оставляет след без фразы, повторный визит в тот же день новой строки не создаёт.
    /// </summary>
    [Test]
    public void RecordVisitTracksFirstVisitAndIgnoresRepeatSameDayTest()
    {
        var host = TestPlayer.Create();
        var guest = TestPlayer.Create();
        var date = DateTimeHelper.GetNowDate();

        guest.RecordVisit(host, date);
        guest.RecordVisit(host, date);

        var entry = GuestbookEntryRow(host, guest);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(entry.PhraseId, Is.Null);
            Assert.That(EntryCount(host, guest), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Визит в собственную деревню след в книге гостей не оставляет.
    /// </summary>
    [Test]
    public void RecordVisitSelfDoesNotCreateRowTest()
    {
        var player = TestPlayer.Create();

        player.RecordVisit(player, DateTimeHelper.GetNowDate());

        Assert.That(EntryCount(player, player), Is.Zero);
    }

    /// <summary>
    /// Два одновременных первых визита одной пары гость-хозяин дают ровно одну строку следа.
    /// </summary>
    [Test]
    public async Task RecordVisitConcurrentFirstVisitsInsertSingleRowTest()
    {
        var host = TestPlayer.Create();
        var guest = TestPlayer.Create();
        var date = DateTimeHelper.GetNowDate();

        await Task.WhenAll(Task.Run(() => guest.RecordVisit(host, date)), Task.Run(() => guest.RecordVisit(host, date)));

        Assert.That(EntryCount(host, guest), Is.EqualTo(1));
    }

    /// <summary>
    /// Запись в книге сохраняет выбранную фразу и шлёт хозяину событие
    /// <see cref="PlayerEventType.GuestbookEntryLeft"/> с деревней, гербом и фразой гостя.
    /// </summary>
    [Test]
    public void LeaveEntryRecordsPhraseAndRaisesHostEventTest()
    {
        const int crestIcon = 3;
        const int crestColor = 5;
        const int phraseId = 4;
        var host = TestPlayer.Create();
        var villageName = "Гостья-" + Guid.NewGuid().ToString("N")[..6];
        var guest = TestPlayer.Create()
            .SetVillageIdentity(villageName, crestIcon, crestColor)
            .RaiseVillageLevel(GuestbookManager.GuestbookUnlockLevel);
        var date = DateTimeHelper.GetNowDate();

        guest.LeaveEntry(host, phraseId, date);

        var entry = GuestbookEntryRow(host, guest);
        var hostEvent = App.Read(context => context.PlayerEvents.Single(x => x.PlayerId == host.Id && x.Type == PlayerEventType.GuestbookEntryLeft));
        var payload = JsonDocument.Parse(hostEvent.Data).RootElement;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entry.PhraseId, Is.EqualTo(phraseId));
            Assert.That(payload.GetProperty("guestVillageName").GetString(), Is.EqualTo(villageName));
            Assert.That(payload.GetProperty("guestCrestIcon").GetInt32(), Is.EqualTo(crestIcon));
            Assert.That(payload.GetProperty("guestCrestColor").GetInt32(), Is.EqualTo(crestColor));
            Assert.That(payload.GetProperty("phraseId").GetInt32(), Is.EqualTo(phraseId));
        }
    }

    /// <summary>
    /// В собственной книге гостей расписаться нельзя.
    /// </summary>
    [Test]
    public void LeaveEntrySelfThrowsTest()
    {
        var player = TestPlayer.Create();

        var ex = Throws.Business(() => player.LeaveEntry(player, 1, DateTimeHelper.GetNowDate()));

        Assert.That(ex.Message, Is.EqualTo("Нельзя расписаться в своей книге"));
    }

    /// <summary>
    /// Фраза за пределами справочника из 8 штук отклоняется.
    /// </summary>
    /// <param name="phraseId">Идентификатор фразы за границей допустимого диапазона.</param>
    [TestCase(0)]
    [TestCase(GuestbookManager.GuestbookPhraseCount + 1)]
    public void LeaveEntryInvalidPhraseThrowsTest(int phraseId)
    {
        var host = TestPlayer.Create();
        var guest = TestPlayer.Create();

        var ex = Throws.Business(() => guest.LeaveEntry(host, phraseId, DateTimeHelper.GetNowDate()));

        Assert.That(ex.Message, Is.EqualTo("Неизвестная фраза"));
    }

    /// <summary>
    /// Гость без названной деревни расписаться не может.
    /// </summary>
    [Test]
    public void LeaveEntryWithoutVillageNameThrowsTest()
    {
        var host = TestPlayer.Create();
        var guest = TestPlayer.Create();

        var ex = Throws.Business(() => guest.LeaveEntry(host, 1, DateTimeHelper.GetNowDate()));

        Assert.That(ex.Message, Is.EqualTo("Сначала назовите свою деревню"));
    }

    /// <summary>
    /// Книга гостей открывается только с обжитости 20.
    /// </summary>
    [Test]
    public void LeaveEntryBelowUnlockLevelThrowsTest()
    {
        var host = TestPlayer.Create();
        var guest = TestPlayer.Create()
            .SetVillageIdentity("Гостья Низкая-" + Guid.NewGuid().ToString("N")[..6], 1, 1);

        var ex = Throws.Business(() => guest.LeaveEntry(host, 1, DateTimeHelper.GetNowDate()));

        Assert.That(ex.Message, Is.EqualTo($"Книга гостей откроется на обжитости {GuestbookManager.GuestbookUnlockLevel}"));
    }

    /// <summary>
    /// Вторая запись той же паре гость-хозяин в один день отклоняется.
    /// </summary>
    [Test]
    public void LeaveEntryRepeatSameDayThrowsTest()
    {
        var host = TestPlayer.Create();
        var guest = TestPlayer.Create()
            .SetVillageIdentity("Гостья Повтор-" + Guid.NewGuid().ToString("N")[..6], 2, 2)
            .RaiseVillageLevel(GuestbookManager.GuestbookUnlockLevel);
        var date = DateTimeHelper.GetNowDate();
        guest.LeaveEntry(host, 1, date);

        var ex = Throws.Business(() => guest.LeaveEntry(host, 2, date));

        Assert.That(ex.Message, Is.EqualTo("Вы уже оставили запись сегодня"));
    }

    /// <summary>
    /// Визит в деревню отдаёт состояние книги гостей (флаги и записи),
    /// а приватные поля игрока (Name, AspNetUserId) наружу не утекают.
    /// </summary>
    [Test]
    public void VisitVillageReflectsGuestbookStateAndHidesPrivateFieldsTest()
    {
        const int phraseId = 3;
        var host = TestPlayer.Create()
            .SetVillageIdentity("Двор Хозяин-" + Guid.NewGuid().ToString("N")[..6], 2, 3);
        var guest = TestPlayer.Create()
            .SetVillageIdentity("Двор Гостья-" + Guid.NewGuid().ToString("N")[..6], 4, 5)
            .RaiseVillageLevel(GuestbookManager.GuestbookUnlockLevel);

        var firstVisit = guest.VisitVillageAsGuest(host);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstVisit.CanLeaveEntry, Is.True);
            Assert.That(firstVisit.AlreadyLeftToday, Is.False);
            Assert.That(firstVisit.GuestbookUnlockLevel, Is.EqualTo(GuestbookManager.GuestbookUnlockLevel));
            Assert.That(firstVisit.Guestbook, Is.Empty);
        }

        guest.LeaveEntry(host, phraseId, DateTimeHelper.GetNowDate());
        var secondVisit = guest.VisitVillageAsGuest(host);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondVisit.CanLeaveEntry, Is.False);
            Assert.That(secondVisit.AlreadyLeftToday, Is.True);
            Assert.That(secondVisit.Guestbook.Single().GuestPlayerId, Is.EqualTo(guest.Id));
            Assert.That(secondVisit.Guestbook.Single().PhraseId, Is.EqualTo(phraseId));
        }

        var json = JsonSerializer.Serialize(secondVisit);
        Assert.That(json, Does.Not.Contain("\"Name\""));
        Assert.That(json, Does.Not.Contain("AspNetUserId"));
    }

    /// <summary>
    /// Счётчик визитов за сезон не учитывает след, датированный до начала текущего сезона.
    /// </summary>
    [Test]
    public void GetGuestbookVisitCounterExcludesEntriesOutsideCurrentSeasonTest()
    {
        var host = TestPlayer.Create();
        var guest = TestPlayer.Create();
        var now = DateTimeHelper.GetNowDate();
        var season = App.Act<SeasonManager, Season>(m => m.GetCurrentSeason(now));
        InsertGuestbookEntry(host, guest, season.StartDate.AddSeconds(-1));

        var guestbook = host.GetGuestbook(now);

        Assert.That(guestbook.VisitsThisSeason, Is.Zero);
    }

    /// <summary>
    /// Лента книги гостей отдаёт не больше 10 записей, свежие первыми.
    /// </summary>
    [Test]
    public void GetGuestbookFeedIsLimitedToShowCountOrderedByMostRecentTest()
    {
        var host = TestPlayer.Create();
        var guest = TestPlayer.Create();
        var now = DateTimeHelper.GetNowDate();
        var oldestExcludedDate = now.AddDays(-GuestbookManager.GuestbookShowCount);

        for (var i = 0; i <= GuestbookManager.GuestbookShowCount; i++)
        {
            InsertGuestbookEntry(host, guest, now.AddDays(-i), 1);
        }

        var guestbook = host.GetGuestbook(now);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(guestbook.Entries.Length, Is.EqualTo(GuestbookManager.GuestbookShowCount));
            Assert.That(guestbook.Entries.Select(x => x.Date), Is.Ordered.Descending);
            Assert.That(guestbook.Entries.First().Date, Is.EqualTo(now));
            Assert.That(guestbook.Entries.Any(x => x.Date == oldestExcludedDate), Is.False);
        }
    }

    private static GuestbookEntry GuestbookEntryRow(TestPlayer host, TestPlayer guest)
    {
        var entry = App.Read(context => context.GuestbookEntries.SingleOrDefault(x => x.HostPlayerId == host.Id && x.GuestPlayerId == guest.Id));
        Assert.That(entry, Is.Not.Null);
        return entry!;
    }

    private static int EntryCount(TestPlayer host, TestPlayer guest)
    {
        return App.Read(context => context.GuestbookEntries.Count(x => x.HostPlayerId == host.Id && x.GuestPlayerId == guest.Id));
    }

    private static void InsertGuestbookEntry(TestPlayer host, TestPlayer guest, DateTime date, int? phraseId = null)
    {
        using var scope = App.Scope();
        scope.Context.GuestbookEntries.Add(new()
        {
            HostPlayerId = host.Id,
            GuestPlayerId = guest.Id,
            Day = DateOnly.FromDateTime(date),
            PhraseId = phraseId,
            Date = date,
        });

        scope.Commit();
    }
}

file static class GuestbookTestsActs
{
    public static TestPlayer RecordVisit(this TestPlayer guest, TestPlayer host, DateTime date)
    {
        App.Act<GuestbookManager>(m => m.RecordVisit(guest.Id, host.Id, date));
        return guest;
    }

    public static TestPlayer LeaveEntry(this TestPlayer guest, TestPlayer host, int phraseId, DateTime date)
    {
        App.Act<GuestbookManager>(m => m.LeaveEntry(guest.Id, host.Id, phraseId, date));
        return guest;
    }

    public static GuestbookModel GetGuestbook(this TestPlayer host, DateTime date)
    {
        return App.Act<GuestbookManager, GuestbookModel>(m => m.GetGuestbook(host.Id, date));
    }

    public static TestPlayer RaiseVillageLevel(this TestPlayer p, int target)
    {
        while (p.GetVillageLevel().Level < target)
        {
            p.WithDomik(DomikIds.Market);
        }

        return p;
    }

    public static VillageVisitDto VisitVillageAsGuest(this TestPlayer guest, TestPlayer host)
    {
        using var scope = App.Scope();
        var date = DateTimeHelper.GetNowDate();
        var visit = scope.Get<WorldManager>().VisitVillage(host.Id);
        scope.Get<GuestbookManager>().RecordVisit(guest.Id, host.Id, date);
        var guestbook = scope.Get<GuestbookManager>().GetVisitGuestbook(host.Id, guest.Id, date);
        var help = scope.Get<HelpManager>().GetVisitHelp(host.Id, guest.Id, date);
        scope.Commit();
        return visit.ToDto(guestbook, help);
    }
}
