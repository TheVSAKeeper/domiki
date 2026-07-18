using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Village;
using Domiki.Web.Village.Models;
using System.Text.Json;

namespace Domiki.Web.Tests;

public sealed class HelpVillageTests
{
    /// <summary>
    /// «Подсобить» сокращает остаток активного улучшения домика хозяина на 10 процентов остатка, сдвигая начало
    /// улучшения на этот же срок назад, начисляет гостю 5 монет и дату помощи, а хозяину – счётчик дневных визитов
    /// и событие <see cref="PlayerEventType.VillageHelped"/> с гербом гостя, названием постройки и величиной сокращения.
    /// </summary>
    [Test]
    public void HelpReducesDomikUpgradeRemainingByTenPercentTest()
    {
        const int remainingSeconds = 3600;
        const int crestIcon = 4;
        const int crestColor = 6;

        // Ceiling(remainingSeconds * HelpManager.HelpReducePercent / 100.0)
        const int expectedReducedSeconds = 360;

        var host = TestPlayer.Create()
            .SetVillageIdentity("Хозяин-" + Guid.NewGuid().ToString("N")[..6], 1, 1);

        var guestVillageName = "Гостья-" + Guid.NewGuid().ToString("N")[..6];
        var guest = TestPlayer.Create()
            .SetVillageIdentity(guestVillageName, crestIcon, crestColor)
            .RaiseVillageLevel(HelpManager.HelpUnlockLevel);

        var date = DateTimeHelper.GetNowDate();
        StartDomikUpgrade(host, StartingDomikIds.Barrack, date, remainingSeconds);
        var expectedTypeName = host.DomikTypes().First(x => x.Id == DomikIds.Barrack).Name;
        var coinsBefore = guest.Resource(ResourceIds.Coin);

        var result = guest.Help(host, date);

        var hostEvent = App.Read(context => context.PlayerEvents.Single(x => x.PlayerId == host.Id && x.Type == PlayerEventType.VillageHelped));
        var payload = JsonDocument.Parse(hostEvent.Data).RootElement;
        var hostCap = HostCapState(host);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ReducedSeconds, Is.EqualTo(expectedReducedSeconds));
            Assert.That(result.RewardCoins, Is.EqualTo(HelpManager.HelpRewardCoins));
            Assert.That(result.DomikTypeName, Is.EqualTo(expectedTypeName));
            Assert.That(DomikUpgradeCalculateDate(host, StartingDomikIds.Barrack), Is.EqualTo(date.AddSeconds(-expectedReducedSeconds)));
            Assert.That(guest.Resource(ResourceIds.Coin), Is.EqualTo(coinsBefore + HelpManager.HelpRewardCoins));
            Assert.That(LastHelpDate(guest), Is.EqualTo(date));
            Assert.That(hostCap.HelpsReceivedToday, Is.EqualTo(1));
            Assert.That(hostCap.HelpsReceivedDate, Is.EqualTo(date));
            Assert.That(payload.GetProperty("guestVillageName").GetString(), Is.EqualTo(guestVillageName));
            Assert.That(payload.GetProperty("guestCrestIcon").GetInt32(), Is.EqualTo(crestIcon));
            Assert.That(payload.GetProperty("guestCrestColor").GetInt32(), Is.EqualTo(crestColor));
            Assert.That(payload.GetProperty("domikTypeName").GetString(), Is.EqualTo(expectedTypeName));
            Assert.That(payload.GetProperty("reducedSeconds").GetInt32(), Is.EqualTo(expectedReducedSeconds));
        }
    }

    /// <summary>
    /// «Подсобить» сокращает остаток активного производства хозяина на 10 процентов остатка.
    /// </summary>
    [Test]
    public void HelpReducesManufactureRemainingByTenPercentTest()
    {
        const int remainingSeconds = 5000;

        // Ceiling(remainingSeconds * HelpManager.HelpReducePercent / 100.0)
        const int expectedReducedSeconds = 500;

        var host = TestPlayer.Create()
            .SetVillageIdentity("Хозяин-" + Guid.NewGuid().ToString("N")[..6], 1, 1);

        var guest = TestPlayer.Create()
            .SetVillageIdentity("Гостья-" + Guid.NewGuid().ToString("N")[..6], 2, 2)
            .RaiseVillageLevel(HelpManager.HelpUnlockLevel);

        var date = DateTimeHelper.GetNowDate();
        var manufactureId = InsertManufacture(host, StartingDomikIds.ClayMine, date, remainingSeconds);
        var expectedTypeName = host.DomikTypes().First(x => x.Id == DomikIds.ClayMine).Name;

        var result = guest.Help(host, date);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ReducedSeconds, Is.EqualTo(expectedReducedSeconds));
            Assert.That(result.DomikTypeName, Is.EqualTo(expectedTypeName));
            Assert.That(ManufactureFinishDate(manufactureId), Is.EqualTo(date.AddSeconds(remainingSeconds - expectedReducedSeconds)));
        }
    }

    /// <summary>
    /// Из двух активных работ хозяина «подсобить» сокращает ту, у которой остаток больше, а не ту, что запущена в
    /// домике с меньшим остатком.
    /// </summary>
    [Test]
    public void HelpReducesTheWorkWithLargerRemainingWhenBothActiveTest()
    {
        const int domikRemainingSeconds = 1000;
        const int manufactureRemainingSeconds = 5000;

        // Ceiling(manufactureRemainingSeconds * HelpManager.HelpReducePercent / 100.0)
        const int expectedReducedSeconds = 500;

        var host = TestPlayer.Create()
            .SetVillageIdentity("Хозяин-" + Guid.NewGuid().ToString("N")[..6], 1, 1);

        var guest = TestPlayer.Create()
            .SetVillageIdentity("Гостья-" + Guid.NewGuid().ToString("N")[..6], 2, 2)
            .RaiseVillageLevel(HelpManager.HelpUnlockLevel);

        var date = DateTimeHelper.GetNowDate();
        StartDomikUpgrade(host, StartingDomikIds.Barrack, date, domikRemainingSeconds);
        var manufactureId = InsertManufacture(host, StartingDomikIds.ClayMine, date, manufactureRemainingSeconds);
        var expectedTypeName = host.DomikTypes().First(x => x.Id == DomikIds.ClayMine).Name;

        var result = guest.Help(host, date);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ReducedSeconds, Is.EqualTo(expectedReducedSeconds));
            Assert.That(result.DomikTypeName, Is.EqualTo(expectedTypeName));
            Assert.That(ManufactureFinishDate(manufactureId), Is.EqualTo(date.AddSeconds(manufactureRemainingSeconds - expectedReducedSeconds)));
            Assert.That(DomikUpgradeCalculateDate(host, StartingDomikIds.Barrack), Is.EqualTo(date));
        }
    }

    /// <summary>
    /// Нельзя подсобить своей же деревне.
    /// </summary>
    [Test]
    public void HelpSelfThrowsTest()
    {
        var player = TestPlayer.Create();

        var ex = Throws.Business(() => player.Help(player, DateTimeHelper.GetNowDate()));

        Assert.That(ex.Message, Is.EqualTo("Нельзя подсобить своей деревне"));
    }

    /// <summary>
    /// Подсобить нельзя деревне хозяина, которая ещё не названа.
    /// </summary>
    [Test]
    public void HelpHostVillageNotFoundThrowsTest()
    {
        var host = TestPlayer.Create();
        var guest = TestPlayer.Create();

        var ex = Throws.Business(() => guest.Help(host, DateTimeHelper.GetNowDate()));

        Assert.That(ex.Message, Is.EqualTo("Деревня не найдена"));
    }

    /// <summary>
    /// Гость без названной деревни подсобить не может.
    /// </summary>
    [Test]
    public void HelpGuestWithoutVillageNameThrowsTest()
    {
        var host = TestPlayer.Create()
            .SetVillageIdentity("Хозяин-" + Guid.NewGuid().ToString("N")[..6], 1, 1);

        var guest = TestPlayer.Create();

        var ex = Throws.Business(() => guest.Help(host, DateTimeHelper.GetNowDate()));

        Assert.That(ex.Message, Is.EqualTo("Сначала назовите свою деревню"));
    }

    /// <summary>
    /// «Подсобить» открывается только с обжитости гостя 20.
    /// </summary>
    [Test]
    public void HelpBelowUnlockLevelThrowsTest()
    {
        var host = TestPlayer.Create()
            .SetVillageIdentity("Хозяин-" + Guid.NewGuid().ToString("N")[..6], 1, 1);

        var guest = TestPlayer.Create()
            .SetVillageIdentity("Гостья Низкая-" + Guid.NewGuid().ToString("N")[..6], 2, 2);

        var ex = Throws.Business(() => guest.Help(host, DateTimeHelper.GetNowDate()));

        Assert.That(ex.Message, Is.EqualTo($"«Подсобить» откроется на обжитости {HelpManager.HelpUnlockLevel}"));
    }

    /// <summary>
    /// Повторное «подсобить» тем же гостем в тот же день отклоняется, активная работа хозяина не сокращается.
    /// </summary>
    [Test]
    public void HelpRepeatSameDayThrowsTest()
    {
        const int remainingSeconds = 3600;

        var host = TestPlayer.Create()
            .SetVillageIdentity("Хозяин-" + Guid.NewGuid().ToString("N")[..6], 1, 1);

        var guest = TestPlayer.Create()
            .SetVillageIdentity("Гостья-" + Guid.NewGuid().ToString("N")[..6], 2, 2)
            .RaiseVillageLevel(HelpManager.HelpUnlockLevel);

        var date = DateTimeHelper.GetNowDate();
        StartDomikUpgrade(host, StartingDomikIds.Barrack, date, remainingSeconds);
        SetLastHelpDate(guest, date);

        var ex = Throws.Business(() => guest.Help(host, date));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("Вы уже подсобили сегодня"));
            Assert.That(DomikUpgradeCalculateDate(host, StartingDomikIds.Barrack), Is.EqualTo(date));
        }
    }

    /// <summary>
    /// Дневной кап деревни хозяина на «подсобить» отклоняет визит сверх лимита, активная работа хозяина не
    /// сокращается.
    /// </summary>
    [Test]
    public void HelpHostCapReachedThrowsTest()
    {
        const int remainingSeconds = 3600;

        var host = TestPlayer.Create()
            .SetVillageIdentity("Хозяин-" + Guid.NewGuid().ToString("N")[..6], 1, 1);

        var guest = TestPlayer.Create()
            .SetVillageIdentity("Гостья-" + Guid.NewGuid().ToString("N")[..6], 2, 2)
            .RaiseVillageLevel(HelpManager.HelpUnlockLevel);

        var date = DateTimeHelper.GetNowDate();
        StartDomikUpgrade(host, StartingDomikIds.Barrack, date, remainingSeconds);
        SetHostCap(host, HelpManager.HostHelpCapPerDay, date);

        var ex = Throws.Business(() => guest.Help(host, date));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("Этой деревне сегодня уже подсобили"));
            Assert.That(DomikUpgradeCalculateDate(host, StartingDomikIds.Barrack), Is.EqualTo(date));
        }
    }

    /// <summary>
    /// Без активных работ у деревни хозяина «подсобить» отклоняется.
    /// </summary>
    [Test]
    public void HelpNoActiveWorkThrowsTest()
    {
        var host = TestPlayer.Create()
            .SetVillageIdentity("Хозяин-" + Guid.NewGuid().ToString("N")[..6], 1, 1);

        var guest = TestPlayer.Create()
            .SetVillageIdentity("Гостья-" + Guid.NewGuid().ToString("N")[..6], 2, 2)
            .RaiseVillageLevel(HelpManager.HelpUnlockLevel);

        var ex = Throws.Business(() => guest.Help(host, DateTimeHelper.GetNowDate()));

        Assert.That(ex.Message, Is.EqualTo("Сейчас у деревни нет активных работ"));
    }

    /// <summary>
    /// Вчерашняя дата последнего «подсобить» у гостя новой помощи не мешает, а вчерашний кап хозяина сбрасывается –
    /// после помощи счётчик визитов хозяина за сегодня снова равен единице.
    /// </summary>
    [Test]
    public void HelpIgnoresYesterdaysDatesAndResetsHostDailyCapTest()
    {
        const int remainingSeconds = 3600;

        var host = TestPlayer.Create()
            .SetVillageIdentity("Хозяин-" + Guid.NewGuid().ToString("N")[..6], 1, 1);

        var guest = TestPlayer.Create()
            .SetVillageIdentity("Гостья-" + Guid.NewGuid().ToString("N")[..6], 2, 2)
            .RaiseVillageLevel(HelpManager.HelpUnlockLevel);

        var date = DateTimeHelper.GetNowDate();
        var yesterday = date.AddDays(-1);
        StartDomikUpgrade(host, StartingDomikIds.Barrack, date, remainingSeconds);
        SetLastHelpDate(guest, yesterday);
        SetHostCap(host, HelpManager.HostHelpCapPerDay, yesterday);

        Assert.DoesNotThrow(() => guest.Help(host, date));

        var hostCap = HostCapState(host);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(LastHelpDate(guest), Is.EqualTo(date));
            Assert.That(hostCap.HelpsReceivedToday, Is.EqualTo(1));
            Assert.That(hostCap.HelpsReceivedDate, Is.EqualTo(date));
        }
    }

    /// <summary>
    /// Витрина визита показывает «подсобить» доступным у годного гостя, а после помощи – уже недоступным на
    /// сегодня.
    /// </summary>
    [Test]
    public void HelpVisitFlagsReflectAvailabilityBeforeAndAfterHelpTest()
    {
        const int remainingSeconds = 3600;

        var host = TestPlayer.Create()
            .SetVillageIdentity("Хозяин-" + Guid.NewGuid().ToString("N")[..6], 1, 1);

        var guest = TestPlayer.Create()
            .SetVillageIdentity("Гостья-" + Guid.NewGuid().ToString("N")[..6], 2, 2)
            .RaiseVillageLevel(HelpManager.HelpUnlockLevel);

        var date = DateTimeHelper.GetNowDate();
        StartDomikUpgrade(host, StartingDomikIds.Barrack, date, remainingSeconds);

        var before = host.GetVisitHelp(guest, date);
        guest.Help(host, date);
        var after = host.GetVisitHelp(guest, date);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(before.CanHelp, Is.True);
            Assert.That(before.AlreadyHelpedToday, Is.False);
            Assert.That(before.HostCapReached, Is.False);
            Assert.That(before.HasActiveWork, Is.True);
            Assert.That(before.UnlockLevel, Is.EqualTo(HelpManager.HelpUnlockLevel));
            Assert.That(after.CanHelp, Is.False);
            Assert.That(after.AlreadyHelpedToday, Is.True);
        }
    }

    /// <summary>
    /// Без активных работ у деревни хозяина витрина визита отмечает «подсобить» недоступным.
    /// </summary>
    [Test]
    public void HelpVisitHasActiveWorkFalseWithoutActiveWorkTest()
    {
        var host = TestPlayer.Create()
            .SetVillageIdentity("Хозяин-" + Guid.NewGuid().ToString("N")[..6], 1, 1);

        var guest = TestPlayer.Create()
            .SetVillageIdentity("Гостья-" + Guid.NewGuid().ToString("N")[..6], 2, 2)
            .RaiseVillageLevel(HelpManager.HelpUnlockLevel);

        var visit = host.GetVisitHelp(guest, DateTimeHelper.GetNowDate());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(visit.HasActiveWork, Is.False);
            Assert.That(visit.CanHelp, Is.False);
        }
    }

    /// <summary>
    /// При исчерпанном дневном капе деревни хозяина витрина визита отмечает кап исчерпанным.
    /// </summary>
    [Test]
    public void HelpVisitHostCapReachedTrueAtDailyCapTest()
    {
        const int remainingSeconds = 3600;

        var host = TestPlayer.Create()
            .SetVillageIdentity("Хозяин-" + Guid.NewGuid().ToString("N")[..6], 1, 1);

        var guest = TestPlayer.Create()
            .SetVillageIdentity("Гостья-" + Guid.NewGuid().ToString("N")[..6], 2, 2)
            .RaiseVillageLevel(HelpManager.HelpUnlockLevel);

        var date = DateTimeHelper.GetNowDate();
        StartDomikUpgrade(host, StartingDomikIds.Barrack, date, remainingSeconds);
        SetHostCap(host, HelpManager.HostHelpCapPerDay, date);

        var visit = host.GetVisitHelp(guest, date);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(visit.HostCapReached, Is.True);
            Assert.That(visit.CanHelp, Is.False);
        }
    }

    /// <summary>
    /// Два одновременных «подсобить» одного гостя тому же хозяину сокращают работу и выдают награду ровно один раз –
    /// второй запрос отклоняется как повтор за день.
    /// </summary>
    [Test]
    public async Task HelpConcurrentRequestsFromSameGuestApplyReductionOnceTest()
    {
        const int remainingSeconds = 3600;

        // Ceiling(remainingSeconds * HelpManager.HelpReducePercent / 100.0)
        const int expectedReducedSeconds = 360;

        var host = TestPlayer.Create()
            .SetVillageIdentity("Хозяин-" + Guid.NewGuid().ToString("N")[..6], 1, 1);

        var guest = TestPlayer.Create()
            .SetVillageIdentity("Гостья-" + Guid.NewGuid().ToString("N")[..6], 2, 2)
            .RaiseVillageLevel(HelpManager.HelpUnlockLevel);

        var date = DateTimeHelper.GetNowDate();
        StartDomikUpgrade(host, StartingDomikIds.Barrack, date, remainingSeconds);
        var coinsBefore = guest.Resource(ResourceIds.Coin);

        var results = await Task.WhenAll(
            Task.Run(() => TryHelp(guest, host, date)),
            Task.Run(() => TryHelp(guest, host, date)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.Count(x => x), Is.EqualTo(1));
            Assert.That(DomikUpgradeCalculateDate(host, StartingDomikIds.Barrack), Is.EqualTo(date.AddSeconds(-expectedReducedSeconds)));
            Assert.That(guest.Resource(ResourceIds.Coin), Is.EqualTo(coinsBefore + HelpManager.HelpRewardCoins));
        }
    }

    private static bool TryHelp(TestPlayer guest, TestPlayer host, DateTime date)
    {
        try
        {
            guest.Help(host, date);
            return true;
        }
        catch (BusinessException)
        {
            return false;
        }
    }

    private static void StartDomikUpgrade(TestPlayer host, int domikId, DateTime date, int remainingSeconds)
    {
        using var scope = App.Scope();
        var domik = scope.Context.Domiks.Single(x => x.PlayerId == host.Id && x.Id == domikId);
        domik.UpgradeSeconds = remainingSeconds;
        domik.UpgradeCalculateDate = date;
        scope.Commit();
    }

    private static DateTime? DomikUpgradeCalculateDate(TestPlayer host, int domikId)
    {
        return App.Read(context => context.Domiks.Single(x => x.PlayerId == host.Id && x.Id == domikId).UpgradeCalculateDate);
    }

    private static int InsertManufacture(TestPlayer host, int domikId, DateTime date, int remainingSeconds)
    {
        using var scope = App.Scope();
        var manufacture = new Manufacture
        {
            DomikId = domikId,
            DomikPlayerId = host.Id,
            ReceiptId = ReceiptIds.ClayDig,
            PlodderCount = 1,
            FinishDate = date.AddSeconds(remainingSeconds),
            DurationSeconds = remainingSeconds,
        };

        scope.Context.Manufactures.Add(manufacture);
        scope.Commit();
        return manufacture.Id;
    }

    private static DateTime ManufactureFinishDate(int manufactureId)
    {
        return App.Read(context => context.Manufactures.Single(x => x.Id == manufactureId).FinishDate);
    }

    private static void SetLastHelpDate(TestPlayer guest, DateTime date)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == guest.Id).LastHelpDate = date;
        scope.Commit();
    }

    private static DateTime? LastHelpDate(TestPlayer guest)
    {
        return App.Read(context => context.Players.Where(x => x.Id == guest.Id).Select(x => x.LastHelpDate).Single());
    }

    private static void SetHostCap(TestPlayer host, int helpsReceivedToday, DateTime helpsReceivedDate)
    {
        using var scope = App.Scope();
        var player = scope.Context.Players.Single(x => x.Id == host.Id);
        player.HelpsReceivedToday = helpsReceivedToday;
        player.HelpsReceivedDate = helpsReceivedDate;
        scope.Commit();
    }

    private static (int HelpsReceivedToday, DateTime? HelpsReceivedDate) HostCapState(TestPlayer host)
    {
        return App.Read(context =>
        {
            var row = context.Players.Where(x => x.Id == host.Id).Select(x => new { x.HelpsReceivedToday, x.HelpsReceivedDate }).Single();
            return (row.HelpsReceivedToday, row.HelpsReceivedDate);
        });
    }
}

file static class HelpVillageTestsActs
{
    public static HelpResult Help(this TestPlayer guest, TestPlayer host, DateTime date)
    {
        return App.Act<HelpManager, HelpResult>(m => m.Help(guest.Id, host.Id, date));
    }

    public static VisitHelp GetVisitHelp(this TestPlayer host, TestPlayer guest, DateTime date)
    {
        return App.Act<HelpManager, VisitHelp>(m => m.GetVisitHelp(host.Id, guest.Id, date));
    }

    public static TestPlayer RaiseVillageLevel(this TestPlayer p, int target)
    {
        while (p.GetVillageLevel().Level < target)
        {
            p.WithDomik(DomikIds.Market);
        }

        return p;
    }
}
