using Domiki.Web.Activities;
using Domiki.Web.Core;
using Domiki.Web.Core.Scheduling;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;

namespace Domiki.Web.Tests;

public sealed class DomikIncidentTests
{
    /// <summary>
    /// Недостаточная обжитость, неизвестная постройка, неподходящая погода, кулдаун, активная загадка и нехватка
    /// свободных трудяг блокируют детерминированную завязку.
    /// </summary>
    /// <param name="setup">Готовит состояние игрока перед попыткой завязки.</param>
    /// <param name="domikTypeId">Тип постройки для завязки.</param>
    /// <param name="villageLevel">Обжитость деревни.</param>
    /// <param name="weatherLogicName">Техническое имя текущей погоды.</param>
    [TestCaseSource(nameof(DomikIncidentGateCases))]
    public void TryStartDomikIncidentIsBlockedByGatesTest(Func<TestPlayer, DateTime> setup, int domikTypeId, int villageLevel, string? weatherLogicName)
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3);

        var calculateInfo = TryStartDomikIncident(player, domikTypeId, villageLevel, weatherLogicName, setup(player));

        Assert.That(calculateInfo, Is.Null);
    }

    /// <summary>
    /// Подходящая постройка создаёт загадку с шаблоном из карты, ставит кулдаун и планирует авторазвязку.
    /// </summary>
    /// <param name="domikTypeId">Тип постройки для завязки.</param>
    /// <param name="templateId">Ожидаемый шаблон загадки.</param>
    /// <param name="weatherLogicName">Техническое имя подходящей погоды.</param>
    [TestCase(DomikIds.GoldMine, 0, null)]
    [TestCase(DomikIds.Barrack, 1, null)]
    [TestCase(DomikIds.ClayMine, 2, "rain")]
    [TestCase(DomikIds.LumberMill, 3, "drought")]
    [TestCase(DomikIds.StoneMine, 4, null)]
    [TestCase(DomikIds.Forge, 5, null)]
    public void TryStartDomikIncidentCreatesIncidentAndRecordsStartedEventTest(int domikTypeId, int templateId, string? weatherLogicName)
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3);
        _ = player.Workers();
        var date = DateTimeHelper.GetNowDate();

        var calculateInfo = TryStartDomikIncident(player, domikTypeId, IncidentManager.DomikIncidentUnlockLevel, weatherLogicName, date);

        Assert.That(calculateInfo, Is.Not.Null);
        var row = player.IncidentRow(calculateInfo!.ObjectId);
        var lastIncidentDate = App.Read(context => context.Players.Single(x => x.Id == player.Id).LastDomikIncidentDate);
        var startedEvent = player.DomikIncidentStartedEvent();
        using var eventData = System.Text.Json.JsonDocument.Parse(startedEvent.Data);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(calculateInfo.Type, Is.EqualTo(CalculateTypes.Incident));
            Assert.That(calculateInfo.Date, Is.EqualTo(date.AddHours(IncidentManager.DomikIncidentAutoResolveHours)));
            Assert.That(row.SourceType, Is.EqualTo(IncidentSourceType.Domik));
            Assert.That(row.DomikTypeId, Is.EqualTo(domikTypeId));
            Assert.That(row.TemplateId, Is.EqualTo(templateId));
            Assert.That(row.MissingWorkerId, Is.Null);
            Assert.That(row.ExpeditionTypeId, Is.Null);
            Assert.That(row.CreateDate, Is.EqualTo(date));
            Assert.That(row.ResolvedDate, Is.Null);
            Assert.That(lastIncidentDate, Is.EqualTo(date));
            Assert.That(eventData.RootElement.GetProperty("domikTypeId").GetInt32(), Is.EqualTo(domikTypeId));
            Assert.That(eventData.RootElement.GetProperty("templateId").GetInt32(), Is.EqualTo(templateId));
        }
    }

    /// <summary>
    /// GetDomik возвращает исходное состояние загадки и после начала поисков показывает назначенных трудяг.
    /// </summary>
    /// <param name="startSearch">Нужно ли начать поиски перед чтением состояния.</param>
    [TestCase(false)]
    [TestCase(true)]
    public void GetDomikReturnsIncidentStateTest(bool startSearch)
    {
        const int clueId = 2;
        const int templateId = 4;

        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2);
        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var searchWorkerIds = workerIds.Take(2).ToArray();
        var createDate = DateTimeHelper.GetNowDate();
        var incidentId = CreateDomikIncident(player, DomikIds.StoneMine, templateId, createDate);
        if (startSearch)
        {
            player.StartSearch(incidentId, clueId, searchWorkerIds);
        }

        var incident = player.GetDomikIncident();

        Assert.That(incident, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(incident!.Id, Is.EqualTo(incidentId));
            Assert.That(incident.DomikTypeId, Is.EqualTo(DomikIds.StoneMine));
            Assert.That(incident.TemplateId, Is.EqualTo(templateId));
            Assert.That(incident.CreateDate, Is.EqualTo(createDate));
            Assert.That(incident.AutoResolveDate, Is.EqualTo(createDate.AddHours(IncidentManager.DomikIncidentAutoResolveHours)));
            Assert.That(incident.ClueId, Is.EqualTo(startSearch ? clueId : null));
            Assert.That(incident.SearchEndDate.HasValue, Is.EqualTo(startSearch));
            Assert.That(incident.SearchWorkerIds, Is.EquivalentTo(startSearch ? searchWorkerIds : []));
        }
    }

    /// <summary>
    /// Поиски в загадке постройки назначают всех переданных трудяг и используют длительность выбранной зацепки.
    /// </summary>
    [Test]
    public void StartSearchForDomikAssignsAllWorkersAndSetsSearchEndDateTest()
    {
        const int clueId = 1;

        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2);
        var workerIds = player.Workers().Select(x => x.Id).Take(2).ToArray();
        var incidentId = CreateDomikIncident(player, DomikIds.ClayMine, 2, DateTimeHelper.GetNowDate());
        var beforeStart = DateTimeHelper.GetNowDate();

        player.StartSearch(incidentId, clueId, workerIds);

        var incident = player.IncidentRow(incidentId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(incident.ClueId, Is.EqualTo(clueId));
            Assert.That(incident.SearchEndDate, Is.EqualTo(beforeStart.AddHours(IncidentManager.ClueDurationHours[clueId])).Within(TimeSpan.FromSeconds(2)));
            Assert.That(player.Workers().Where(x => workerIds.Contains(x.Id)).Select(x => x.IncidentId), Is.All.EqualTo(incidentId));
        }
    }

    /// <summary>
    /// Одновременные загадки разных источников возвращаются только соответствующими методами чтения.
    /// </summary>
    [Test]
    public void GetMethodsKeepIncidentSourcesIsolatedTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3);
        var workerId = player.Workers()[0].Id;
        var date = DateTimeHelper.GetNowDate();
        var expeditionIncidentId = CreateExpeditionIncident(player, workerId, date);
        var domikIncidentId = CreateDomikIncident(player, DomikIds.StoneMine, 4, date);

        var expeditionIncident = player.GetIncident();
        var domikIncident = player.GetDomikIncident();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(expeditionIncident, Is.Not.Null);
            Assert.That(expeditionIncident!.Id, Is.EqualTo(expeditionIncidentId));
            Assert.That(domikIncident, Is.Not.Null);
            Assert.That(domikIncident!.Id, Is.EqualTo(domikIncidentId));
        }
    }

    /// <summary>
    /// Активное происшествие похода не блокирует загадку постройки, а загадка постройки не блокирует завязку похода.
    /// </summary>
    [Test]
    public void TryStartMethodsIgnoreActiveIncidentOfOtherSourceTest()
    {
        const int maxRolls = 500;

        var playerWithExpedition = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3);
        var expeditionWorkerId = playerWithExpedition.Workers()[0].Id;
        var date = DateTimeHelper.GetNowDate();
        CreateExpeditionIncident(playerWithExpedition, expeditionWorkerId, date);

        var domikCalculateInfo = TryStartDomikIncident(playerWithExpedition, DomikIds.StoneMine, IncidentManager.DomikIncidentUnlockLevel, null, date);

        var playerWithDomik = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3);
        var domikIncidentId = CreateDomikIncident(playerWithDomik, DomikIds.StoneMine, 4, date);
        var workerIds = playerWithDomik.Workers().Select(x => x.Id).ToArray();
        CalculateInfo? expeditionCalculateInfo = null;
        for (var i = 0; i < maxRolls; i++)
        {
            expeditionCalculateInfo = TryRollIncident(playerWithDomik, workerIds, date);
            if (expeditionCalculateInfo != null)
            {
                break;
            }
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(domikCalculateInfo, Is.Not.Null);
            Assert.That(expeditionCalculateInfo, Is.Not.Null);
            Assert.That(playerWithDomik.IncidentRow(domikIncidentId).ResolvedDate, Is.Null);
        }
    }

    /// <summary>
    /// Развязка загадки постройки не меняет одновременно активное происшествие похода.
    /// </summary>
    [Test]
    public void FinishDomikIncidentKeepsExpeditionIncidentActiveTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3);
        var workerId = player.Workers()[0].Id;
        var createDate = DateTimeHelper.GetNowDate();
        var expeditionIncidentId = CreateExpeditionIncident(player, workerId, createDate);
        var domikIncidentId = CreateDomikIncident(player, DomikIds.StoneMine, 4, createDate);

        var finished = player.FinishIncident(createDate.AddHours(IncidentManager.DomikIncidentAutoResolveHours), domikIncidentId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(finished, Is.True);
            Assert.That(player.IncidentRow(domikIncidentId).ResolvedDate, Is.Not.Null);
            Assert.That(player.IncidentRow(expeditionIncidentId).ResolvedDate, Is.Null);
            Assert.That(player.GetIncident()!.Id, Is.EqualTo(expeditionIncidentId));
        }
    }

    /// <summary>
    /// Загадка без поисков не развязывается раньше срока, а в срок не выдаёт ресурсов и пишет авторазвязку.
    /// </summary>
    [Test]
    public void FinishDomikIncidentAutoResolvesWithoutRewardsTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack);
        var createDate = DateTimeHelper.GetNowDate();
        var incidentId = CreateDomikIncident(player, DomikIds.StoneMine, 4, createDate);
        var beforeResources = ResourceSnapshot(player, DomikFindResourceIds);
        var autoResolveDate = createDate.AddHours(IncidentManager.DomikIncidentAutoResolveHours);

        var finishedEarly = player.FinishIncident(autoResolveDate.AddSeconds(-1), incidentId);
        var finished = player.FinishIncident(autoResolveDate, incidentId);

        var resolvedEvent = player.DomikIncidentResolvedEvent();
        using var eventData = System.Text.Json.JsonDocument.Parse(resolvedEvent.Data);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(finishedEarly, Is.False);
            Assert.That(finished, Is.True);
            Assert.That(player.IncidentRow(incidentId).ResolvedDate, Is.EqualTo(autoResolveDate));
            Assert.That(ResourceSnapshot(player, DomikFindResourceIds), Is.EqualTo(beforeResources));
            Assert.That(eventData.RootElement.GetProperty("autoResolved").GetBoolean(), Is.True);
        }
    }

    /// <summary>
    /// Развязка поисков освобождает трудяг, выдаёт находку постройки и повторно не выдаёт вторую.
    /// </summary>
    [Test]
    public void FinishDomikIncidentSearchReleasesWorkersGrantsFindAndIsIdempotentTest()
    {
        const int clueId = 1;

        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2);
        var searchWorkers = player.Workers().Take(2).ToArray();
        var searchWorkerIds = searchWorkers.Select(x => x.Id).ToArray();
        var incidentId = CreateDomikIncident(player, DomikIds.ClayMine, 2, DateTimeHelper.GetNowDate());
        player.StartSearch(incidentId, clueId, searchWorkerIds);
        var searchEndDate = player.IncidentRow(incidentId).SearchEndDate.GetValueOrDefault();
        var beforeResources = ResourceSnapshot(player, [ResourceIds.Clay]);

        var finished = player.FinishIncident(searchEndDate, incidentId);
        var resourcesAfterFirstFinish = ResourceSnapshot(player, [ResourceIds.Clay]);
        var finishedAgain = player.FinishIncident(searchEndDate.AddHours(1), incidentId);
        var resolvedEvent = player.DomikIncidentResolvedEvent();
        using var eventData = System.Text.Json.JsonDocument.Parse(resolvedEvent.Data);
        var value = eventData.RootElement.GetProperty("value").GetInt32();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(finished, Is.True);
            Assert.That(finishedAgain, Is.True);
            Assert.That(player.Workers().Where(x => searchWorkerIds.Contains(x.Id)).Select(x => x.IncidentId), Is.All.Null);
            Assert.That(player.Resource(ResourceIds.Clay) - beforeResources[ResourceIds.Clay], Is.EqualTo(ExpectedFindValue(ResourceIds.Clay, clueId)));
            Assert.That(resourcesAfterFirstFinish, Is.EqualTo(ResourceSnapshot(player, [ResourceIds.Clay])));
            Assert.That(player.IncidentRow(incidentId).ResolvedDate, Is.EqualTo(searchEndDate));
            Assert.That(eventData.RootElement.GetProperty("autoResolved").GetBoolean(), Is.False);
            Assert.That(eventData.RootElement.GetProperty("heroWorkerName").GetString(), Is.EqualTo(searchWorkers[0].Name));
            Assert.That(eventData.RootElement.GetProperty("resourceTypeId").GetInt32(), Is.EqualTo(ResourceIds.Clay));
            Assert.That(value, Is.EqualTo(ExpectedFindValue(ResourceIds.Clay, clueId)));
        }
    }

    private static readonly int[] DomikFindResourceIds = [ResourceIds.Stone, ResourceIds.Wood, ResourceIds.Clay, ResourceIds.Gold];

    private static IEnumerable<TestCaseData> DomikIncidentGateCases()
    {
        yield return new TestCaseData(
                new Func<TestPlayer, DateTime>(_ => DateTimeHelper.GetNowDate()),
                DomikIds.StoneMine,
                IncidentManager.DomikIncidentUnlockLevel - 1,
                null)
            .SetName("LevelBelowUnlock");
        yield return new TestCaseData(
                new Func<TestPlayer, DateTime>(_ => DateTimeHelper.GetNowDate()),
                DomikIds.Workshop,
                IncidentManager.DomikIncidentUnlockLevel,
                null)
            .SetName("UnknownDomikType");
        yield return new TestCaseData(
                new Func<TestPlayer, DateTime>(_ => DateTimeHelper.GetNowDate()),
                DomikIds.ClayMine,
                IncidentManager.DomikIncidentUnlockLevel,
                "drought")
            .SetName("WeatherMismatchClayDrought");
        yield return new TestCaseData(
                new Func<TestPlayer, DateTime>(_ => DateTimeHelper.GetNowDate()),
                DomikIds.ClayMine,
                IncidentManager.DomikIncidentUnlockLevel,
                null)
            .SetName("WeatherMismatchClayMissing");
        yield return new TestCaseData(
                new Func<TestPlayer, DateTime>(_ => DateTimeHelper.GetNowDate()),
                DomikIds.LumberMill,
                IncidentManager.DomikIncidentUnlockLevel,
                "rain")
            .SetName("WeatherMismatchLumber");
        yield return new TestCaseData(
                new Func<TestPlayer, DateTime>(player =>
                {
                    var date = DateTimeHelper.GetNowDate();
                    SetLastDomikIncidentDate(player, date);
                    return date;
                }),
                DomikIds.StoneMine,
                IncidentManager.DomikIncidentUnlockLevel,
                null)
            .SetName("Cooldown");
        yield return new TestCaseData(
                new Func<TestPlayer, DateTime>(player =>
                {
                    var date = DateTimeHelper.GetNowDate();
                    CreateDomikIncident(player, DomikIds.StoneMine, 4, date);
                    return date;
                }),
                DomikIds.StoneMine,
                IncidentManager.DomikIncidentUnlockLevel,
                null)
            .SetName("ActiveDomikIncident");
        yield return new TestCaseData(
                new Func<TestPlayer, DateTime>(player =>
                {
                    var date = DateTimeHelper.GetNowDate();
                    foreach (var workerId in player.Workers().Skip(1).Select(x => x.Id))
                    {
                        player.SetWorkerRest(workerId, date.AddHours(1));
                    }

                    return date;
                }),
                DomikIds.StoneMine,
                IncidentManager.DomikIncidentUnlockLevel,
                null)
            .SetName("NotEnoughFreeWorkers");
    }

    private static int CreateDomikIncident(TestPlayer player, int domikTypeId, int templateId, DateTime createDate, int? clueId = null, DateTime? searchEndDate = null)
    {
        using var scope = App.Scope();
        var incident = new Data.Entities.Incident
        {
            PlayerId = player.Id,
            SourceType = IncidentSourceType.Domik,
            DomikTypeId = domikTypeId,
            TemplateId = templateId,
            CreateDate = createDate,
            ClueId = clueId,
            SearchEndDate = searchEndDate,
        };
        scope.Context.Incidents.Add(incident);
        scope.Context.SaveChanges();
        scope.Commit();
        return incident.Id;
    }

    private static int CreateExpeditionIncident(TestPlayer player, int missingWorkerId, DateTime createDate)
    {
        using var scope = App.Scope();
        var incident = new Data.Entities.Incident
        {
            PlayerId = player.Id,
            SourceType = IncidentSourceType.Expedition,
            MissingWorkerId = missingWorkerId,
            ExpeditionTypeId = ExpeditionTypeIds.ShortScout,
            TemplateId = 0,
            CreateDate = createDate,
        };
        scope.Context.Incidents.Add(incident);
        scope.Context.SaveChanges();
        scope.Context.Workers.Single(x => x.Id == missingWorkerId).IncidentId = incident.Id;
        scope.Commit();
        return incident.Id;
    }

    private static void SetLastDomikIncidentDate(TestPlayer player, DateTime date)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == player.Id).LastDomikIncidentDate = date;
        scope.Commit();
    }

    private static CalculateInfo? TryStartDomikIncident(TestPlayer player, int domikTypeId, int villageLevel, string? weatherLogicName, DateTime date)
    {
        using var scope = App.Scope();
        var dbPlayer = scope.Context.Players.Single(x => x.Id == player.Id);
        var result = scope.Get<IncidentManager>().TryStartDomikIncident(dbPlayer, domikTypeId, villageLevel, weatherLogicName, date);
        scope.Commit();
        return result;
    }

    private static CalculateInfo? TryRollIncident(TestPlayer player, int[] workerIds, DateTime date)
    {
        using var scope = App.Scope();
        var dbPlayer = scope.Context.Players.Single(x => x.Id == player.Id);
        var workers = scope.Context.Workers.Where(x => workerIds.Contains(x.Id)).ToArray();
        var result = scope.Get<IncidentManager>().TryRollIncident(dbPlayer, workers, ExpeditionTypeIds.ShortScout, date);
        scope.Commit();
        return result;
    }

    private static IReadOnlyDictionary<int, int> ResourceSnapshot(TestPlayer player, IEnumerable<int> resourceTypeIds)
    {
        return resourceTypeIds.ToDictionary(x => x, player.Resource);
    }

    private static int ExpectedFindValue(int resourceTypeId, int clueId)
    {
        return Math.Max(1, (int)Math.Round(IncidentManager.DomikIncidentFindBaseValue * IncidentManager.ClueFindMultiplier[clueId] * (double)ResourceManager.BaseMarketValue / ResourceManager.GetMarketValue(resourceTypeId), MidpointRounding.AwayFromZero));
    }
}

file static class DomikIncidentTestsActs
{
    public static TestPlayer StartSearch(this TestPlayer p, int incidentId, int clueId, int[] workerIds)
    {
        using (App.PendingEvents())
        {
            App.Act<IncidentManager>(m => m.StartSearch(p.Id, incidentId, clueId, workerIds));
        }

        return p;
    }

    public static bool FinishIncident(this TestPlayer p, DateTime date, int incidentId)
    {
        return App.Act<IncidentManager, bool>(m => m.FinishIncident(date, new()
        {
            PlayerId = p.Id,
            ObjectId = incidentId,
            Date = date,
            Type = CalculateTypes.Incident,
        }));
    }

    public static Domiki.Web.Activities.Models.Incident? GetIncident(this TestPlayer p)
    {
        return App.Act<IncidentManager, Domiki.Web.Activities.Models.Incident?>(m => m.Get(p.Id));
    }

    public static Domiki.Web.Activities.Models.DomikIncident? GetDomikIncident(this TestPlayer p)
    {
        return App.Act<IncidentManager, Domiki.Web.Activities.Models.DomikIncident?>(m => m.GetDomik(p.Id));
    }

    public static Data.Entities.Incident IncidentRow(this TestPlayer p, int incidentId)
    {
        return App.Read(context => context.Incidents.Single(x => x.PlayerId == p.Id && x.Id == incidentId));
    }

    public static PlayerEvent DomikIncidentStartedEvent(this TestPlayer p)
    {
        return App.Read(context => context.PlayerEvents.Single(x => x.PlayerId == p.Id && x.Type == PlayerEventType.DomikIncidentStarted));
    }

    public static PlayerEvent DomikIncidentResolvedEvent(this TestPlayer p)
    {
        return App.Read(context => context.PlayerEvents.Single(x => x.PlayerId == p.Id && x.Type == PlayerEventType.DomikIncidentResolved));
    }
}
