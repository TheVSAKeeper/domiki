using Domiki.Web.Activities;
using Domiki.Web.Core;
using Domiki.Web.Core.Scheduling;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Workers;

namespace Domiki.Web.Tests;

public sealed class IncidentTests
{
    /// <summary>
    /// Пропавший трудяга недоступен для производства, похода и поисков, пока его IncidentId указывает на активное
    /// происшествие.
    /// </summary>
    [Test]
    public void MissingWorkerIsUnavailableForActivitiesTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3)
            .WithDomik(DomikIds.ClayMine)
            .WithDomik(DomikIds.ScoutHut)
            .WithResource(ResourceIds.Gold, 1)
            .WithResource(ResourceIds.Board, 2);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var missingWorkerId = workerIds[0];
        var incidentId = CreateIncident(player, missingWorkerId, ExpeditionTypeIds.ShortScout, 0, DateTimeHelper.GetNowDate());

        var manufacture = Throws.Business(() => player.StartManufacture(player.DomikId(DomikIds.ClayMine), ReceiptIds.ClayDig, [missingWorkerId]));
        var expedition = Throws.Business(() => player.StartExpedition(ExpeditionTypeIds.ShortScout, [missingWorkerId, workerIds[1]]));
        var search = Throws.Business(() => player.StartSearch(incidentId, 0, [missingWorkerId]));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(manufacture.Message, Is.EqualTo("Трудяга недоступен"));
            Assert.That(expedition.Message, Is.EqualTo("Трудяга недоступен"));
            Assert.That(search.Message, Is.EqualTo("Трудяга занят"));
        }
    }

    /// <summary>
    /// Выбранная зацепка задаёт срок поисков, а назначенные на них трудяги получают IncidentId.
    /// </summary>
    /// <param name="clueId">Индекс выбранной зацепки.</param>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void StartSearchAssignsWorkersAndSetsSearchEndDateTest(int clueId)
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var missingWorkerId = workerIds[0];
        var searchWorkerIds = workerIds.Skip(1).Take(2).ToArray();
        var incidentId = CreateIncident(player, missingWorkerId, ExpeditionTypeIds.ShortScout, 0, DateTimeHelper.GetNowDate());
        var beforeStart = DateTimeHelper.GetNowDate();

        player.StartSearch(incidentId, clueId, searchWorkerIds);

        var incident = player.IncidentRow(incidentId);
        var expectedSearchEndDate = beforeStart.AddHours(IncidentManager.ClueDurationHours[clueId]);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(incident.ClueId, Is.EqualTo(clueId));
            Assert.That(incident.SearchEndDate, Is.EqualTo(expectedSearchEndDate).Within(TimeSpan.FromSeconds(2)));
            Assert.That(player.Workers().Where(x => searchWorkerIds.Contains(x.Id)).Select(x => x.IncidentId), Is.All.EqualTo(incidentId));
        }
    }

    /// <summary>
    /// Неверная зацепка, пустой, слишком большой или задублированный список трудяг отклоняются до начала поисков.
    /// </summary>
    /// <param name="clueId">Зацепка, передаваемая в StartSearch.</param>
    /// <param name="workerIdsSelector">Строит список id из свободных трудяг игрока.</param>
    /// <param name="expectedMessage">Ожидаемый текст ошибки.</param>
    [TestCaseSource(nameof(InvalidSearchSelectionCases))]
    public void StartSearchRejectsInvalidClueAndWorkerSelectionTest(int clueId, Func<int[], int[]> workerIdsSelector, string expectedMessage)
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var incidentId = CreateIncident(player, workerIds[0], ExpeditionTypeIds.ShortScout, 0, DateTimeHelper.GetNowDate());

        var exception = Throws.Business(() => player.StartSearch(incidentId, clueId, workerIdsSelector(workerIds)));

        Assert.That(exception.Message, Is.EqualTo(expectedMessage));
    }

    /// <summary>
    /// Нельзя начать поиски повторно, по чужому или несуществующему происшествию.
    /// </summary>
    /// <param name="setup">Готовит игрока и возвращает id происшествия для StartSearch.</param>
    /// <param name="expectedMessage">Ожидаемый текст ошибки.</param>
    [TestCaseSource(nameof(SearchLookupCases))]
    public void StartSearchRejectsInvalidIncidentTest(Func<TestPlayer, int[], int> setup, string expectedMessage)
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var incidentId = setup(player, workerIds);

        var exception = Throws.Business(() => player.StartSearch(incidentId, 0, [workerIds[1]]));

        Assert.That(exception.Message, Is.EqualTo(expectedMessage));
    }

    /// <summary>
    /// Чужой или занятый трудяга не может быть назначен на поиски.
    /// </summary>
    /// <param name="occupyWorker">Занимает трудягу игрока; false оставляет его свободным для проверки чужого id.</param>
    /// <param name="expectedMessage">Ожидаемый текст ошибки.</param>
    [TestCase(true, "Трудяга занят")]
    [TestCase(false, "Трудяга недоступен")]
    public void StartSearchRejectsBusyAndForeignWorkerTest(bool occupyWorker, string expectedMessage)
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.ClayMine)
            .WithDomik(DomikIds.Barrack);

        var other = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var incidentId = CreateIncident(player, workerIds[0], ExpeditionTypeIds.ShortScout, 0, DateTimeHelper.GetNowDate());
        var searchWorkerId = workerIds[1];
        if (occupyWorker)
        {
            using (App.PendingEvents())
            {
                player.StartManufacture(player.DomikId(DomikIds.ClayMine), ReceiptIds.ClayDig, [searchWorkerId]);
            }
        }

        var requestedWorkerId = occupyWorker ? searchWorkerId : other.Workers()[0].Id;
        var exception = Throws.Business(() => player.StartSearch(incidentId, 0, [requestedWorkerId]));

        Assert.That(exception.Message, Is.EqualTo(expectedMessage));
    }

    /// <summary>
    /// Развязка поисков освобождает всех участников, даёт одну ресурсную находку, отправляет пропавшего отдыхать,
    /// записывает IncidentResolved и скрывает активное происшествие.
    /// </summary>
    [Test]
    public void FinishIncidentReleasesWorkersGrantsFindAndRecordsEventTest()
    {
        const int clueId = 1;

        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var missingWorkerId = workerIds[0];
        var searchWorkerIds = workerIds.Skip(1).Take(2).ToArray();
        var incidentId = CreateIncident(player, missingWorkerId, ExpeditionTypeIds.ShortScout, 0, DateTimeHelper.GetNowDate());
        player.StartSearch(incidentId, clueId, searchWorkerIds);
        var searchEndDate = player.IncidentRow(incidentId).SearchEndDate.GetValueOrDefault();
        var beforeResources = ResourceSnapshot(player, IncidentResourceIds);

        var finished = player.FinishIncident(searchEndDate, incidentId);

        var incident = player.IncidentRow(incidentId);
        var changes = ResourceChanges(player, beforeResources, IncidentResourceIds);
        var resolvedEvent = player.IncidentResolvedEvent();
        var changedResource = changes.Single();
        var expectedValue = ExpectedFindValue(changedResource.ResourceTypeId, clueId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(finished, Is.True);
            Assert.That(player.Workers().Where(x => workerIds.Contains(x.Id)).Select(x => x.IncidentId), Is.All.Null);
            Assert.That(player.Workers().Single(x => x.Id == missingWorkerId).RestUntil, Is.EqualTo(searchEndDate.AddSeconds(ExpeditionManager.ExpeditionRestSeconds)));
            Assert.That(changedResource.ResourceTypeId, Is.AnyOf(IncidentResourceIds));
            Assert.That(changedResource.Value, Is.EqualTo(expectedValue));
            Assert.That(incident.ResolvedDate, Is.EqualTo(searchEndDate));
            Assert.That(IncidentAutoReturned(resolvedEvent), Is.False);
            Assert.That(player.GetIncident(), Is.Null);
        }
    }

    /// <summary>
    /// Повторная развязка уже развязанного происшествия возвращает true и не выдаёт вторую находку.
    /// </summary>
    [Test]
    public void FinishIncidentIsIdempotentTest()
    {
        const int clueId = 0;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var incidentId = CreateIncident(player, workerIds[0], ExpeditionTypeIds.ShortScout, 0, DateTimeHelper.GetNowDate());
        player.StartSearch(incidentId, clueId, [workerIds[1]]);
        var searchEndDate = player.IncidentRow(incidentId).SearchEndDate.GetValueOrDefault();
        player.FinishIncident(searchEndDate, incidentId);
        var resourcesAfterFirstFinish = ResourceSnapshot(player, IncidentResourceIds);

        var finished = player.FinishIncident(searchEndDate.AddHours(1), incidentId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(finished, Is.True);
            Assert.That(ResourceSnapshot(player, IncidentResourceIds), Is.EqualTo(resourcesAfterFirstFinish));
        }
    }

    /// <summary>
    /// Развязка до SearchEndDate возвращает false и не освобождает участников, не выдаёт находку и не ставит
    /// ResolvedDate.
    /// </summary>
    [Test]
    public void FinishIncidentBeforeSearchEndDateDoesNothingTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var incidentId = CreateIncident(player, workerIds[0], ExpeditionTypeIds.ShortScout, 0, DateTimeHelper.GetNowDate());
        player.StartSearch(incidentId, 0, [workerIds[1]]);
        var searchEndDate = player.IncidentRow(incidentId).SearchEndDate.GetValueOrDefault();
        var beforeResources = ResourceSnapshot(player, IncidentResourceIds);

        var finished = player.FinishIncident(searchEndDate.AddSeconds(-1), incidentId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(finished, Is.False);
            Assert.That(player.Workers().Where(x => workerIds.Contains(x.Id)).Select(x => x.IncidentId), Is.All.EqualTo(incidentId));
            Assert.That(ResourceSnapshot(player, IncidentResourceIds), Is.EqualTo(beforeResources));
            Assert.That(player.IncidentRow(incidentId).ResolvedDate, Is.Null);
        }
    }

    /// <summary>
    /// Пропавший без начатых поисков возвращается ровно через IncidentAutoReturnHours без находки, отдыхает и
    /// оставляет IncidentResolved с autoReturned=true.
    /// </summary>
    [Test]
    public void FinishIncidentAutoReturnsWithoutRewardsTest()
    {
        const int templateId = 2;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack);

        var workerId = player.Workers()[0].Id;
        var createDate = DateTimeHelper.GetNowDate();
        var incidentId = CreateIncident(player, workerId, ExpeditionTypeIds.ShortScout, templateId, createDate);
        var autoReturnDate = createDate.AddHours(IncidentManager.IncidentAutoReturnHours);
        var beforeResources = ResourceSnapshot(player, IncidentResourceIds);

        var finished = player.FinishIncident(autoReturnDate, incidentId);

        var incident = player.IncidentRow(incidentId);
        var resolvedEvent = player.IncidentResolvedEvent();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(finished, Is.True);
            Assert.That(player.Workers().Single(x => x.Id == workerId).IncidentId, Is.Null);
            Assert.That(player.Workers().Single(x => x.Id == workerId).RestUntil, Is.EqualTo(autoReturnDate.AddSeconds(ExpeditionManager.ExpeditionRestSeconds)));
            Assert.That(ResourceSnapshot(player, IncidentResourceIds), Is.EqualTo(beforeResources));
            Assert.That(incident.ResolvedDate, Is.EqualTo(autoReturnDate));
            Assert.That(IncidentAutoReturned(resolvedEvent), Is.True);
        }
    }

    /// <summary>
    /// Самостоятельное возвращение раньше IncidentAutoReturnHours возвращает false и не меняет происшествие.
    /// </summary>
    [Test]
    public void FinishIncidentBeforeAutoReturnDateDoesNothingTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack);

        var workerId = player.Workers()[0].Id;
        var createDate = DateTimeHelper.GetNowDate();
        var incidentId = CreateIncident(player, workerId, ExpeditionTypeIds.ShortScout, 0, createDate);
        var beforeResources = ResourceSnapshot(player, IncidentResourceIds);
        var tooEarlyDate = createDate.AddHours(IncidentManager.IncidentAutoReturnHours).AddSeconds(-1);

        var finished = player.FinishIncident(tooEarlyDate, incidentId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(finished, Is.False);
            Assert.That(player.Workers().Single(x => x.Id == workerId).IncidentId, Is.EqualTo(incidentId));
            Assert.That(ResourceSnapshot(player, IncidentResourceIds), Is.EqualTo(beforeResources));
            Assert.That(player.IncidentRow(incidentId).ResolvedDate, Is.Null);
        }
    }

    /// <summary>
    /// Активное происшествие, свежий кулдаун, недостаток свободных трудяг и отряд из одного трудяги всегда
    /// блокируют завязку.
    /// </summary>
    /// <remarks>
    /// Кейсы ActiveIncident, Cooldown и NotEnoughFreeWorkers зависят от 12 % предролла Random.Shared: сломанный
    /// гейт даст примерно 12 % флака, а не надёжное падение. Детерминирован только SingleWorkerSquad, так как
    /// отряд меньше IncidentMinSquadSize коротко замыкается до ролла; Random.Shared не сидируется.
    /// </remarks>
    /// <param name="setup">Готовит игрока, отряд и дату возврата для TryRollIncident.</param>
    [TestCaseSource(nameof(IncidentRollGateCases))]
    public void TryRollIncidentIsBlockedByGatesTest(Func<TestPlayer, (int[] WorkerIds, DateTime Date)> setup)
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3);

        var roll = setup(player);
        var calculateInfo = TryRollIncident(player, roll.WorkerIds, roll.Date);

        Assert.That(calculateInfo, Is.Null);
    }

    /// <summary>
    /// Успешная завязка сажает происшествие на случайного трудягу отряда, помечает его пропавшим, пишет
    /// WorkerMissing и планирует автовозврат через 48 часов.
    /// </summary>
    [Test]
    public void TryRollIncidentCreatesIncidentAndRecordsMissingEventTest()
    {
        const int MaxRolls = 500;

        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3);
        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var date = DateTimeHelper.GetNowDate();
        CalculateInfo? calculateInfo = null;

        // При null состояние не меняется, поэтому повтор безопасен, а вероятность 0.88^500 ничтожна.
        for (var i = 0; i < MaxRolls; i++)
        {
            calculateInfo = TryRollIncident(player, workerIds, date);
            if (calculateInfo != null)
            {
                break;
            }
        }

        if (calculateInfo == null)
        {
            Assert.Fail($"Не удалось завязать происшествие за {MaxRolls} попыток");
            return;
        }

        var row = player.IncidentRow(calculateInfo.ObjectId);
        var missingWorker = player.Workers().Single(x => x.Id == row.MissingWorkerId);
        var lastIncidentDate = App.Read(context => context.Players.Single(x => x.Id == player.Id).LastIncidentDate);
        var missingEvent = player.WorkerMissingEvent();
        using var eventData = System.Text.Json.JsonDocument.Parse(missingEvent.Data);
        var eventWorkerId = eventData.RootElement.GetProperty("workerId").GetInt32();
        var eventTemplateId = eventData.RootElement.GetProperty("templateId").GetInt32();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(calculateInfo, Is.Not.Null);
            Assert.That(calculateInfo.Type, Is.EqualTo(CalculateTypes.Incident));
            Assert.That(calculateInfo.Date, Is.EqualTo(date.AddHours(IncidentManager.IncidentAutoReturnHours)));
            Assert.That(workerIds, Does.Contain(row.MissingWorkerId));
            Assert.That(row.CreateDate, Is.EqualTo(date));
            Assert.That(row.ResolvedDate, Is.Null);
            Assert.That(row.ClueId, Is.Null);
            Assert.That(row.SearchEndDate, Is.Null);
            Assert.That(missingWorker.IncidentId, Is.EqualTo(row.Id));
            Assert.That(missingWorker.RestUntil, Is.Null);
            Assert.That(lastIncidentDate, Is.EqualTo(date));
            Assert.That(eventWorkerId, Is.EqualTo(row.MissingWorkerId));
            Assert.That(eventTemplateId, Is.EqualTo(row.TemplateId));
        }
    }

    /// <summary>
    /// Из двух одновременных попыток начать поиски одного происшествия успешна ровно одна, а в IncidentId остаётся
    /// только один поисковик.
    /// </summary>
    [Test]
    public async Task ConcurrentStartSearchAllowsOneSuccessTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var incidentId = CreateIncident(player, workerIds[0], ExpeditionTypeIds.ShortScout, 0, DateTimeHelper.GetNowDate());

        var results = await Task.WhenAll(
            Task.Run(() => TryStartSearch(player, incidentId, 0, [workerIds[1]])),
            Task.Run(() => TryStartSearch(player, incidentId, 1, [workerIds[2]])));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.Count(x => x), Is.EqualTo(1));
            Assert.That(player.IncidentRow(incidentId).SearchEndDate, Is.Not.Null);
            Assert.That(player.Workers().Count(x => x.IncidentId == incidentId), Is.EqualTo(2));
        }
    }

    /// <summary>
    /// Get возвращает сохранённые TemplateId и ExpeditionTypeId, дату автовозврата и только назначенных поисковиков.
    /// </summary>
    [Test]
    public void GetReturnsIncidentStateTest()
    {
        const int templateId = 4;
        const int clueId = 2;

        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var missingWorkerId = workerIds[0];
        var searchWorkerIds = workerIds.Skip(1).Take(2).ToArray();
        var createDate = DateTimeHelper.GetNowDate();
        var incidentId = CreateIncident(player, missingWorkerId, ExpeditionTypeIds.ShortScout, templateId, createDate);
        player.StartSearch(incidentId, clueId, searchWorkerIds);

        var incident = player.GetIncident();

        Assert.That(incident, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(incident.Id, Is.EqualTo(incidentId));
            Assert.That(incident.TemplateId, Is.EqualTo(templateId));
            Assert.That(incident.ExpeditionTypeId, Is.EqualTo(ExpeditionTypeIds.ShortScout));
            Assert.That(incident.AutoReturnDate, Is.EqualTo(createDate.AddHours(IncidentManager.IncidentAutoReturnHours)));
            Assert.That(incident.SearchWorkerIds, Is.EquivalentTo(searchWorkerIds));
            Assert.That(incident.SearchWorkerIds, Does.Not.Contain(missingWorkerId));
        }
    }

    private static readonly int[] IncidentResourceIds = [ResourceIds.Stone, ResourceIds.Wood, ResourceIds.Clay, ResourceIds.Tool, ResourceIds.Ore];

    private static IEnumerable<TestCaseData> InvalidSearchSelectionCases()
    {
        yield return new TestCaseData(-1, new Func<int[], int[]>(workerIds => [workerIds[1]]), "Неверная зацепка")
            .SetName("NegativeClue");
        yield return new TestCaseData(IncidentManager.ClueDurationHours.Length, new Func<int[], int[]>(workerIds => [workerIds[1]]), "Неверная зацепка")
            .SetName("TooLargeClue");
        yield return new TestCaseData(0, new Func<int[], int[]>(_ => []), "Нужен хотя бы один трудяга")
            .SetName("NoWorkers");
        yield return new TestCaseData(0, new Func<int[], int[]>(workerIds => workerIds.Skip(1).Take(3).ToArray()), "Слишком много трудяг")
            .SetName("TooManyWorkers");
        yield return new TestCaseData(0, new Func<int[], int[]>(workerIds => [workerIds[1], workerIds[1]]), "Дублирующиеся трудяги")
            .SetName("DuplicateWorkers");
    }

    private static IEnumerable<TestCaseData> SearchLookupCases()
    {
        yield return new TestCaseData(
                new Func<TestPlayer, int[], int>((_, _) => int.MaxValue),
                "Происшествие не найдено")
            .SetName("UnknownIncident");

        yield return new TestCaseData(
                new Func<TestPlayer, int[], int>((_, _) =>
                {
                    var other = TestPlayer.Create()
                        .WithDomik(DomikIds.Barrack);

                    var otherWorkerId = other.Workers()[0].Id;
                    return CreateIncident(other, otherWorkerId, ExpeditionTypeIds.ShortScout, 0, DateTimeHelper.GetNowDate());
                }),
                "Происшествие не найдено")
            .SetName("ForeignIncident");

        yield return new TestCaseData(
                new Func<TestPlayer, int[], int>((player, workerIds) =>
                {
                    var incidentId = CreateIncident(player, workerIds[0], ExpeditionTypeIds.ShortScout, 0, DateTimeHelper.GetNowDate());
                    player.StartSearch(incidentId, 0, [workerIds[1]]);
                    return incidentId;
                }),
                "Поиски уже начаты")
            .SetName("SearchAlreadyStarted");
    }

    private static IEnumerable<TestCaseData> IncidentRollGateCases()
    {
        yield return new TestCaseData(new Func<TestPlayer, (int[] WorkerIds, DateTime Date)>(player =>
        {
            var workerIds = player.Workers().Select(x => x.Id).ToArray();
            var date = DateTimeHelper.GetNowDate();
            CreateIncident(player, workerIds[0], ExpeditionTypeIds.ShortScout, 0, date);
            return ([workerIds[1], workerIds[2]], date);
        })).SetName("ActiveIncident");

        yield return new TestCaseData(new Func<TestPlayer, (int[] WorkerIds, DateTime Date)>(player =>
        {
            var workerIds = player.Workers().Select(x => x.Id).ToArray();
            var date = DateTimeHelper.GetNowDate();
            SetLastIncidentDate(player, date);
            return ([workerIds[0], workerIds[1]], date);
        })).SetName("Cooldown");

        yield return new TestCaseData(new Func<TestPlayer, (int[] WorkerIds, DateTime Date)>(player =>
        {
            var workerIds = player.Workers().Select(x => x.Id).ToArray();
            var date = DateTimeHelper.GetNowDate();
            foreach (var workerId in workerIds.Skip(2))
            {
                player.SetWorkerRest(workerId, date.AddHours(1));
            }

            return ([workerIds[0], workerIds[1]], date);
        })).SetName("NotEnoughFreeWorkers");

        yield return new TestCaseData(new Func<TestPlayer, (int[] WorkerIds, DateTime Date)>(player =>
        {
            var workerId = player.Workers()[0].Id;
            return ([workerId], DateTimeHelper.GetNowDate());
        })).SetName("SingleWorkerSquad");
    }

    private static int CreateIncident(TestPlayer player, int missingWorkerId, int expeditionTypeId, int templateId, DateTime createDate, int? clueId = null, DateTime? searchEndDate = null)
    {
        using var scope = App.Scope();
        var incident = new Data.Entities.Incident
        {
            PlayerId = player.Id,
            SourceType = IncidentSourceType.Expedition,
            MissingWorkerId = missingWorkerId,
            ExpeditionTypeId = expeditionTypeId,
            TemplateId = templateId,
            CreateDate = createDate,
            ClueId = clueId,
            SearchEndDate = searchEndDate,
        };
        scope.Context.Incidents.Add(incident);
        scope.Context.SaveChanges();
        scope.Context.Workers.Single(x => x.Id == missingWorkerId).IncidentId = incident.Id;
        scope.Commit();
        return incident.Id;
    }

    private static void SetLastIncidentDate(TestPlayer player, DateTime date)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == player.Id).LastIncidentDate = date;
        scope.Commit();
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

    private static (int ResourceTypeId, int Value)[] ResourceChanges(TestPlayer player, IReadOnlyDictionary<int, int> beforeResources, IEnumerable<int> resourceTypeIds)
    {
        return resourceTypeIds
            .Select(resourceTypeId => (ResourceTypeId: resourceTypeId, Value: player.Resource(resourceTypeId) - beforeResources[resourceTypeId]))
            .Where(x => x.Value != 0)
            .ToArray();
    }

    private static int ExpectedFindValue(int resourceTypeId, int clueId)
    {
        return Math.Max(1, (int)Math.Round(IncidentManager.IncidentFindBaseValue * IncidentManager.ClueFindMultiplier[clueId] * (double)ResourceManager.BaseMarketValue / ResourceManager.GetMarketValue(resourceTypeId), MidpointRounding.AwayFromZero));
    }

    private static bool IncidentAutoReturned(PlayerEvent playerEvent)
    {
        using var data = System.Text.Json.JsonDocument.Parse(playerEvent.Data);
        return data.RootElement.GetProperty("autoReturned").GetBoolean();
    }

    private static bool TryStartSearch(TestPlayer player, int incidentId, int clueId, int[] workerIds)
    {
        try
        {
            player.StartSearch(incidentId, clueId, workerIds);
            return true;
        }
        catch (BusinessException)
        {
            return false;
        }
    }
}

file static class IncidentTestsActs
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

    public static Data.Entities.Incident IncidentRow(this TestPlayer p, int incidentId)
    {
        return App.Read(context => context.Incidents.Single(x => x.PlayerId == p.Id && x.Id == incidentId));
    }

    public static PlayerEvent IncidentResolvedEvent(this TestPlayer p)
    {
        return App.Read(context => context.PlayerEvents.Single(x => x.PlayerId == p.Id && x.Type == PlayerEventType.IncidentResolved));
    }

    public static PlayerEvent WorkerMissingEvent(this TestPlayer p)
    {
        return App.Read(context => context.PlayerEvents.Single(x => x.PlayerId == p.Id && x.Type == PlayerEventType.WorkerMissing));
    }
}
