using Domiki.Web.Core;
using Domiki.Web.Core.Scheduling;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Infrastructure.Models;
using System.Text.Json;
using PlayerEventType = Domiki.Web.Data.Entities.PlayerEventType;

namespace Domiki.Web.Tests;

public sealed class AutoRepeatTests
{
    /// <summary>
    /// Несколько циклов автоповтора одного производства схлопываются в одно событие завершения с суммарным выходом.
    /// </summary>
    [Test]
    public void AutoRepeatMergesManufactureFinishedEventsTest()
    {
        const int expectedCycles = 2;
        const int expectedDishes = 2;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.Pottery, 3)
            .WithResource(ResourceIds.Clay, 4);

        player.StartManufacture(4, ReceiptIds.MakeDishes, autoRepeat: true);

        var events = App.Read(context => context.PlayerEvents.Where(x => x.PlayerId == player.Id && x.Type == PlayerEventType.ManufactureFinished).ToList());
        Assert.That(events, Has.Count.EqualTo(1));

        using var data = JsonDocument.Parse(events[0].Data);
        Assert.That(data.RootElement.GetProperty("cycles").GetInt32(), Is.EqualTo(expectedCycles));
        var resource = data.RootElement.GetProperty("resources").EnumerateArray().Single(x => x.GetProperty("resourceTypeId").GetInt32() == ResourceIds.Dishes);
        Assert.That(resource.GetProperty("value").GetInt32(), Is.EqualTo(expectedDishes));

        var recap = App.Act<PlayerEventManager, RecapModel>(m => m.TakeRecap(player.Id, DateTimeHelper.GetNowDate()));
        Assert.That(recap.Events.Count(x => x.Type == PlayerEventType.ManufactureFinished), Is.EqualTo(1));
    }

    /// <summary>
    /// Успешный автоповтор оставляет следующий цикл в работе, но завершивший его тик не содержит push-текста.
    /// </summary>
    [Test]
    public void SuccessfulAutoRepeatIsSilentTest()
    {
        const int domikId = 4;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.Pottery, 3)
            .WithResource(ResourceIds.Clay, 4);

        CalculateInfo calcInfo;
        bool result;
        using (App.PendingEvents())
        {
            player.StartManufacture(domikId, ReceiptIds.MakeDishes, autoRepeat: true);
            var manufacture = player.Manufacture(domikId);
            calcInfo = new CalculateInfo
            {
                PlayerId = player.Id,
                ObjectId = manufacture.Id,
                Date = manufacture.FinishDate.AddSeconds(1),
                Type = CalculateTypes.Manufacture,
            };
            result = App.Act<DomikManager, bool>(m => m.FinishManufacture(calcInfo.Date, calcInfo));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(calcInfo.PushTitle, Is.Null);
            Assert.That(calcInfo.PushBody, Is.Null);
            Assert.That(player.ManufactureCount(domikId), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Если автоповтор не запускается из-за сырья, тик сообщает остановку наряда с причиной.
    /// </summary>
    [Test]
    public void AutoRepeatStopContainsReasonPushTest()
    {
        const int domikId = 4;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.Pottery, 3)
            .WithResource(ResourceIds.Clay, 2);

        CalculateInfo calcInfo;
        bool result;
        using (App.PendingEvents())
        {
            player.StartManufacture(domikId, ReceiptIds.MakeDishes, autoRepeat: true);
            var manufacture = player.Manufacture(domikId);
            calcInfo = new CalculateInfo
            {
                PlayerId = player.Id,
                ObjectId = manufacture.Id,
                Date = manufacture.FinishDate.AddSeconds(1),
                Type = CalculateTypes.Manufacture,
            };
            result = App.Act<DomikManager, bool>(m => m.FinishManufacture(calcInfo.Date, calcInfo));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(calcInfo.PushTitle, Is.EqualTo("Наряд остановлен"));
            Assert.That(calcInfo.PushBody, Is.Not.Null.And.Not.Empty);
            Assert.That(calcInfo.PushTag, Is.EqualTo(PushSender.ProductionTag));
            Assert.That(player.ManufactureCount(domikId), Is.Zero);
        }
    }

    /// <summary>
    /// Разовое производство сообщает прибывшие ресурсы, не называя освободившегося трудягу.
    /// </summary>
    [Test]
    public void OneOffManufacturePushDoesNotMentionWorkerTest()
    {
        const int domikId = 4;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.Pottery, 3)
            .WithResource(ResourceIds.Clay, 2);
        var workerNames = player.Workers().Select(x => x.Name).ToArray();

        CalculateInfo calcInfo;
        using (App.PendingEvents())
        {
            player.StartManufacture(domikId, ReceiptIds.MakeDishes);
            var manufacture = player.Manufacture(domikId);
            calcInfo = new CalculateInfo
            {
                PlayerId = player.Id,
                ObjectId = manufacture.Id,
                Date = manufacture.FinishDate.AddSeconds(1),
                Type = CalculateTypes.Manufacture,
            };
            Assert.That(App.Act<DomikManager, bool>(m => m.FinishManufacture(calcInfo.Date, calcInfo)), Is.True);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(calcInfo.PushTitle, Is.Not.Null);
            Assert.That(workerNames, Has.None.Matches<string>(name => calcInfo.PushTitle!.Contains(name, StringComparison.Ordinal)));
            Assert.That(calcInfo.PushBody, Is.Not.Null);
            Assert.That(calcInfo.PushBody, Does.Contain("На складе прибыло"));
            Assert.That(workerNames, Has.None.Matches<string>(name => calcInfo.PushBody!.Contains(name, StringComparison.Ordinal)));
            Assert.That(calcInfo.PushTag, Is.EqualTo(PushSender.ProductionTag));
        }
    }

    /// <summary>
    /// Автоповтор перезапускает производство циклами, пока хватает сырья, и останавливается, когда сырьё заканчивается.
    /// </summary>
    [Test]
    public void AutoRepeatRerunsUntilResourcesRunOutTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.Pottery, 3)
            .WithResource(ResourceIds.Clay, 4);

        player.StartManufacture(4, ReceiptIds.MakeDishes, autoRepeat: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.ManufactureCount(4), Is.Zero);
            Assert.That(player.Workers().All(x => x.ManufactureId == null), Is.True);
            Assert.That(player.Resource(ResourceIds.Clay), Is.Zero);
            Assert.That(player.Resource(ResourceIds.Dishes), Is.EqualTo(2));
        }
    }

    /// <summary>
    /// Наряд, у которого закончилось сырьё для повторного запуска, оставляет игроку ровно одно событие
    /// <see cref="PlayerEventType.ManufactureRepeatFailed"/> с указанием домика и рецепта.
    /// </summary>
    [Test]
    public void AutoRepeatRecordsEventWhenInputsRunOutTest()
    {
        const int domikId = 4;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.Pottery, 3)
            .WithResource(ResourceIds.Clay, 4);

        player.StartManufacture(domikId, ReceiptIds.MakeDishes, autoRepeat: true);

        var events = App.Read(context => context.PlayerEvents.Where(x => x.PlayerId == player.Id && x.Type == PlayerEventType.ManufactureRepeatFailed).ToList());
        Assert.That(events, Has.Count.EqualTo(1));

        using var data = JsonDocument.Parse(events[0].Data);
        Assert.That(data.RootElement.GetProperty("domikId").GetInt32(), Is.EqualTo(domikId));
        Assert.That(data.RootElement.GetProperty("domikTypeId").GetInt32(), Is.EqualTo(DomikIds.Pottery));
        Assert.That(data.RootElement.GetProperty("receiptId").GetInt32(), Is.EqualTo(ReceiptIds.MakeDishes));
    }

    /// <summary>
    /// Завершение автоповторного производства не падает, даже если рецепт больше не привязан к текущему уровню домика:
    /// производство исчезает, а игрок получает ровно одно событие <see cref="PlayerEventType.ManufactureRepeatFailed"/>.
    /// </summary>
    [Test]
    public void AutoRepeatSurvivesRecipeMissingFromLevelTest()
    {
        var player = TestPlayer.Create();
        GrantDomik(player.Id, 4, DomikIds.Forge, 3);
        var manufactureId = CreateManufacture(player.Id, 4, ReceiptIds.MakeBrick);

        var calcInfo = new CalculateInfo
        {
            PlayerId = player.Id,
            ObjectId = manufactureId,
            Type = CalculateTypes.Manufacture,
        };

        Assert.DoesNotThrow(() => App.Act<DomikManager>(m => m.FinishManufacture(DateTimeHelper.GetNowDate(), calcInfo)));

        Assert.That(player.ManufactureCount(4), Is.Zero);
        var exists = App.Read(context => context.Manufactures.Any(x => x.Id == manufactureId));
        Assert.That(exists, Is.False);

        var events = App.Read(context => context.PlayerEvents.Where(x => x.PlayerId == player.Id && x.Type == PlayerEventType.ManufactureRepeatFailed).ToList());
        Assert.That(events, Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Наряд, у которого автоповтор не смог перезапуститься из-за нехватки сырья, теряет заряд рвения только за успешные
    /// циклы, а не за проваленный перезапуск.
    /// </summary>
    [Test]
    public void AutoRepeatDoesNotConsumeZealChargeWhenInputsRunOutTest()
    {
        const int domikId = 4;
        const int initialZealCharges = 5;
        const int successfulCycles = 2;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.Pottery, 3)
            .WithResource(ResourceIds.Clay, 4);

        SetZealCharges(player.Id, initialZealCharges);

        player.StartManufacture(domikId, ReceiptIds.MakeDishes, autoRepeat: true);

        Assert.That(GetZealCharges(player.Id), Is.EqualTo(initialZealCharges - successfulCycles));
    }

    /// <summary>
    /// События завершения производства от построек разного типа не схлопываются между собой, а остаются раздельными.
    /// </summary>
    [Test]
    public void ManufactureFinishedEventsOfDifferentDomikTypesAreNotMergedTest()
    {
        var player = TestPlayer.Create();
        player.RecordManufactureFinished(DomikIds.Forge);
        player.RecordManufactureFinished(DomikIds.Barrack);

        var events = App.Read(context => context.PlayerEvents.Where(x => x.PlayerId == player.Id && x.Type == PlayerEventType.ManufactureFinished).ToList());
        Assert.That(events, Has.Count.EqualTo(2));
    }

    /// <summary>
    /// Если сырья хватает лишь частично на цикл автоповтора, недостающий ресурс не списывается вовсе.
    /// </summary>
    [Test]
    public void PartialShortageDoesNotWriteOffTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.Forge, 3)
            .WithResource(ResourceIds.Iron, 2)
            .WithResource(ResourceIds.Board, 1);

        player.StartManufacture(4, ReceiptIds.MakeTool, autoRepeat: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.ManufactureCount(4), Is.Zero);
            Assert.That(player.Workers().All(x => x.ManufactureId == null), Is.True);
            Assert.That(player.Resource(ResourceIds.Iron), Is.EqualTo(1));
            Assert.That(player.Resource(ResourceIds.Board), Is.Zero);
            Assert.That(player.Resource(ResourceIds.Tool), Is.EqualTo(1));
        }
    }

    private static void GrantDomik(int playerId, int id, int typeId, int level)
    {
        using var scope = App.Scope();
        scope.Context.Domiks.Add(new()
        {
            PlayerId = playerId,
            Id = id,
            TypeId = typeId,
            Level = level,
        });

        scope.Commit();
    }

    private static int CreateManufacture(int playerId, int domikId, int receiptId)
    {
        using var scope = App.Scope();
        var manufacture = new Manufacture
        {
            DomikId = domikId,
            DomikPlayerId = playerId,
            ReceiptId = receiptId,
            PlodderCount = 1,
            FinishDate = DateTimeHelper.GetNowDate().AddSeconds(-3600),
            DurationSeconds = 1800,
            OutputPercent = 100,
            AutoRepeat = true,
            UseOptional = false,
        };

        scope.Context.Manufactures.Add(manufacture);
        scope.Commit();
        return manufacture.Id;
    }

    private static int GetZealCharges(int playerId)
    {
        using var scope = App.Scope();
        return scope.Context.Players.Single(x => x.Id == playerId).ZealCharges;
    }

    private static void SetZealCharges(int playerId, int value)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == playerId).ZealCharges = value;
        scope.Commit();
    }
}

file static class AutoRepeatTestsActs
{
    public static int ManufactureCount(this TestPlayer p, int domikId)
    {
        return App.Read(context => context.Manufactures.Count(x => x.DomikPlayerId == p.Id && x.DomikId == domikId));
    }

    public static TestPlayer RecordManufactureFinished(this TestPlayer p, int domikTypeId)
    {
        App.Act<PlayerEventManager>(m => m.RecordManufactureFinished(p.Id, domikTypeId, new() { { ResourceIds.Brick, 1 } }));
        return p;
    }
}
