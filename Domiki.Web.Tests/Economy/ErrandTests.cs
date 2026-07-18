using Domiki.Web.Core.Scheduling;
using Domiki.Web.Data.Entities;
using Domiki.Web.Economy;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Tests;

public sealed class ErrandTests
{
    private const int ShortScoutExpeditionId = 1;

    /// <summary>
    /// Принятие оффера занимает выбранных трудяг и ставит длительность поисков по выбранной зацепке (4 часа для
    /// clueId=1), отсчитываемую от момента принятия.
    /// </summary>
    [Test]
    public void AcceptAssignsWorkersAndSetsFinishDateByClueTest()
    {
        const int clueId = 1;

        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 1);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var offer = player.CreateErrandOffer(ErrandManager.ErrandUnlockLevel);
        Assert.That(offer, Is.Not.Null);

        player.AcceptErrand(offer!.ObjectId, clueId, workerIds);

        var errand = player.ErrandRow(offer.ObjectId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Workers().Where(x => workerIds.Contains(x.Id)).Select(x => x.ErrandId), Is.All.EqualTo(offer.ObjectId));
            Assert.That(errand.ClueId, Is.EqualTo(clueId));
            Assert.That(errand.AcceptDate, Is.Not.Null);
            Assert.That(errand.FinishDate, Is.EqualTo(errand.AcceptDate!.Value.AddHours(ErrandManager.ClueDurationHours[clueId])));
        }
    }

    /// <summary>
    /// Развязка принятого поручения освобождает трудяг, начисляет монеты (10 монет за трудяго-час, 2 трудяги на 4
    /// часа зацепки) и репутацию соседу (+5 за clueId=1), ставит ResolvedDate и пишет событие ErrandResolved.
    /// </summary>
    [Test]
    public void FinishErrandReleasesWorkersGrantsRewardsAndRecordsEventTest()
    {
        const int clueId = 1;
        const int workerCount = 2;

        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, workerCount - 1);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var offer = player.CreateErrandOffer(ErrandManager.ErrandUnlockLevel);
        Assert.That(offer, Is.Not.Null);
        var errandId = offer!.ObjectId;

        player.AcceptErrand(errandId, clueId, workerIds);
        var neighborId = player.ErrandRow(errandId).NeighborId;
        var finishDate = player.ErrandRow(errandId).FinishDate!.Value;
        var coinsBefore = player.Resource(ResourceIds.Coin);
        var reputationBefore = player.Reputation().Single(x => x.Neighbor.Id == neighborId).Points;

        var finished = player.FinishErrand(finishDate, errandId);

        var errandAfter = player.ErrandRow(errandId);
        var reputationAfter = player.Reputation().Single(x => x.Neighbor.Id == neighborId).Points;
        var errandEvent = App.Read(context => context.PlayerEvents.Single(x => x.PlayerId == player.Id && x.Type == PlayerEventType.ErrandResolved));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(finished, Is.True);
            Assert.That(player.Workers().Where(x => workerIds.Contains(x.Id)).Select(x => x.ErrandId), Is.All.Null);
            Assert.That(player.Resource(ResourceIds.Coin), Is.EqualTo(coinsBefore + ErrandManager.ErrandCoinsPerWorkerHour * workerCount * ErrandManager.ClueDurationHours[clueId]));
            Assert.That(reputationAfter - reputationBefore, Is.EqualTo(ErrandManager.ClueReputation[clueId]));
            Assert.That(errandAfter.ResolvedDate, Is.EqualTo(finishDate));
            Assert.That(errandEvent, Is.Not.Null);
        }
    }

    /// <summary>
    /// Развязка раньше срока завершения поисков не меняет состояние: трудяги остаются заняты, награды не
    /// начисляются, ResolvedDate не проставляется.
    /// </summary>
    [Test]
    public void FinishErrandBeforeFinishDateDoesNothingTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 1);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var offer = player.CreateErrandOffer(ErrandManager.ErrandUnlockLevel);
        Assert.That(offer, Is.Not.Null);
        var errandId = offer!.ObjectId;

        player.AcceptErrand(errandId, 1, workerIds);
        var finishDate = player.ErrandRow(errandId).FinishDate!.Value;
        var coinsBefore = player.Resource(ResourceIds.Coin);

        var finished = player.FinishErrand(finishDate.AddSeconds(-1), errandId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(finished, Is.False);
            Assert.That(player.Workers().Where(x => workerIds.Contains(x.Id)).Select(x => x.ErrandId), Is.All.EqualTo(errandId));
            Assert.That(player.Resource(ResourceIds.Coin), Is.EqualTo(coinsBefore));
            Assert.That(player.ErrandRow(errandId).ResolvedDate, Is.Null);
        }
    }

    /// <summary>
    /// Непринятый оффер удаляется планировщиком только по достижении ExpireDate; до этого момента FinishErrand
    /// возвращает false, и оффер остаётся на месте.
    /// </summary>
    [Test]
    public void FinishErrandExpiresUnacceptedOfferOnlyAtExpireDateTest()
    {
        var player = TestPlayer.Create();

        var offer = player.CreateErrandOffer(ErrandManager.ErrandUnlockLevel);
        Assert.That(offer, Is.Not.Null);
        var errandId = offer!.ObjectId;
        var expireDate = player.ErrandRow(errandId).ExpireDate;

        var tooEarly = player.FinishErrand(expireDate.AddSeconds(-1), errandId);
        Assert.That(tooEarly, Is.False);
        Assert.That(player.HasErrandRow(errandId), Is.True);

        var onTime = player.FinishErrand(expireDate, errandId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(onTime, Is.True);
            Assert.That(player.HasErrandRow(errandId), Is.False);
        }
    }

    /// <summary>
    /// Отмена принятого поручения освобождает трудяг и удаляет запись без начисления монет и репутации.
    /// </summary>
    [Test]
    public void CancelAcceptedErrandFreesWorkersWithoutRewardsTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 1);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var offer = player.CreateErrandOffer(ErrandManager.ErrandUnlockLevel);
        Assert.That(offer, Is.Not.Null);
        var errandId = offer!.ObjectId;

        player.AcceptErrand(errandId, 1, workerIds);
        var neighborId = player.ErrandRow(errandId).NeighborId;
        var coinsBefore = player.Resource(ResourceIds.Coin);
        var reputationBefore = player.Reputation().Single(x => x.Neighbor.Id == neighborId).Points;

        player.CancelErrand(errandId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.HasErrandRow(errandId), Is.False);
            Assert.That(player.Workers().Where(x => workerIds.Contains(x.Id)).Select(x => x.ErrandId), Is.All.Null);
            Assert.That(player.Resource(ResourceIds.Coin), Is.EqualTo(coinsBefore));
            Assert.That(player.Reputation().Single(x => x.Neighbor.Id == neighborId).Points, Is.EqualTo(reputationBefore));
        }
    }

    /// <summary>
    /// Отмена непринятого оффера (без задействованных трудяг) удаляет запись поручения.
    /// </summary>
    [Test]
    public void CancelOfferWithoutWorkersRemovesRowTest()
    {
        var player = TestPlayer.Create();

        var offer = player.CreateErrandOffer(ErrandManager.ErrandUnlockLevel);
        Assert.That(offer, Is.Not.Null);
        var errandId = offer!.ObjectId;

        player.CancelErrand(errandId);

        Assert.That(player.HasErrandRow(errandId), Is.False);
    }

    /// <summary>
    /// Принять поручение с трудягой, занятым производством, походом, отдыхом или уже другим поручением, нельзя.
    /// </summary>
    /// <param name="occupy">Сценарий, занимающий выбранного трудягу перед попыткой принять поручение.</param>
    [TestCaseSource(nameof(BusyWorkerCases))]
    public void AcceptWithBusyChosenWorkerThrowsTest(Action<TestPlayer, int[]> occupy)
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3)
            .WithDomik(DomikIds.ClayMine)
            .WithResource(ResourceIds.Coin, 500)
            .WithResource(ResourceIds.Gold, 5)
            .WithResource(ResourceIds.Board, 10);

        var workerIds = player.Workers().OrderBy(x => x.Id).Select(x => x.Id).ToArray();
        occupy(player, workerIds);

        player.WithErrand(NeighborIds.Zarechye);
        var errandId = player.LastErrandId();

        var ex = Throws.Business(() => player.AcceptErrand(errandId, 0, [workerIds[0]]));
        Assert.That(ex.Message, Is.EqualTo("Трудяга занят"));
    }

    /// <summary>
    /// Трудяга, занятый поручением, недоступен для явного выбора в производство – автоподбор его не видит.
    /// </summary>
    [Test]
    public void WorkerOnErrandIsNotSelectedForManufactureTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.ClayMine);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var offer = player.CreateErrandOffer(ErrandManager.ErrandUnlockLevel);
        Assert.That(offer, Is.Not.Null);
        player.AcceptErrand(offer!.ObjectId, 0, [workerIds[0]]);

        var ex = Throws.Business(() => player.StartManufacture(player.DomikId(DomikIds.ClayMine), ReceiptIds.ClayDig, [workerIds[0]]));
        Assert.That(ex.Message, Is.EqualTo("Трудяга недоступен"));
    }

    /// <summary>
    /// Ниже обжитости открытия механики (10) оффер не создаётся.
    /// </summary>
    [Test]
    public void CreateOfferBelowUnlockLevelReturnsNullTest()
    {
        var player = TestPlayer.Create();

        var offer = player.CreateErrandOffer(ErrandManager.ErrandUnlockLevel - 1);

        Assert.That(offer, Is.Null);
    }

    /// <summary>
    /// Пока у игрока есть незавершённое поручение (непринятый оффер или принятое в поисках), новый оффер не
    /// создаётся.
    /// </summary>
    /// <param name="seed">Сценарий, заводящий незавершённое поручение перед попыткой создать новый оффер.</param>
    [TestCaseSource(nameof(BlockingErrandStates))]
    public void CreateOfferBlockedByExistingUnresolvedErrandTest(Action<TestPlayer> seed)
    {
        var player = TestPlayer.Create();
        seed(player);

        var offer = player.CreateErrandOffer(ErrandManager.ErrandUnlockLevel);

        Assert.That(offer, Is.Null);
    }

    /// <summary>
    /// Развязанное поручение (ResolvedDate проставлен) не мешает созданию нового оффера.
    /// </summary>
    [Test]
    public void CreateOfferNotBlockedByResolvedErrandTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 1);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var firstOffer = player.CreateErrandOffer(ErrandManager.ErrandUnlockLevel);
        Assert.That(firstOffer, Is.Not.Null);
        player.AcceptErrand(firstOffer!.ObjectId, 0, workerIds);
        var finishDate = player.ErrandRow(firstOffer.ObjectId).FinishDate!.Value;
        player.FinishErrand(finishDate, firstOffer.ObjectId);

        var secondOffer = player.CreateErrandOffer(ErrandManager.ErrandUnlockLevel);

        Assert.That(secondOffer, Is.Not.Null);
    }

    /// <summary>
    /// Принять несуществующее, чужое, уже принятое или истёкшее поручение нельзя.
    /// </summary>
    /// <param name="setup">Готовит игрока и возвращает id поручения, которое передаётся в Accept.</param>
    /// <param name="expectedMessage">Ожидаемый текст ошибки.</param>
    [TestCaseSource(nameof(AcceptLookupCases))]
    public void AcceptLookupValidationsThrowTest(Func<TestPlayer, int[], int> setup, string expectedMessage)
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 1);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var errandId = setup(player, workerIds);

        var ex = Throws.Business(() => player.AcceptErrand(errandId, 0, [workerIds[0]]));
        Assert.That(ex.Message, Is.EqualTo(expectedMessage));
    }

    /// <summary>
    /// Неверная зацепка, пустой или задублированный список трудяг и отряд больше двух трудяг отклоняются.
    /// </summary>
    /// <param name="clueId">Зацепка, передаваемая в Accept.</param>
    /// <param name="workerIdsSelector">Строит список id трудяг из полного набора свободных трудяг игрока.</param>
    /// <param name="expectedMessage">Ожидаемый текст ошибки.</param>
    [TestCaseSource(nameof(AcceptWorkerSelectionCases))]
    public void AcceptWorkerSelectionValidationsThrowTest(int clueId, Func<int[], int[]> workerIdsSelector, string expectedMessage)
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var offer = player.CreateErrandOffer(ErrandManager.ErrandUnlockLevel);
        Assert.That(offer, Is.Not.Null);

        var ex = Throws.Business(() => player.AcceptErrand(offer!.ObjectId, clueId, workerIdsSelector(workerIds)));
        Assert.That(ex.Message, Is.EqualTo(expectedMessage));
    }

    /// <summary>
    /// Одновременные попытки принять один и тот же оффер успешны только у одного из конкурирующих запросов.
    /// </summary>
    [Test]
    public async Task ConcurrentAcceptOneOfferAllowsOneSuccessTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 3);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();
        var offer = player.CreateErrandOffer(ErrandManager.ErrandUnlockLevel);
        Assert.That(offer, Is.Not.Null);
        var errandId = offer!.ObjectId;

        var results = await Task.WhenAll(
            Task.Run(() => TryAcceptErrand(player, errandId, 0, [workerIds[0]])),
            Task.Run(() => TryAcceptErrand(player, errandId, 1, [workerIds[1]])));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.Count(x => x), Is.EqualTo(1));
            Assert.That(player.ErrandRow(errandId).AcceptDate, Is.Not.Null);
        }
    }

    private static bool TryAcceptErrand(TestPlayer player, int errandId, int clueId, int[] workerIds)
    {
        try
        {
            player.AcceptErrand(errandId, clueId, workerIds);
            return true;
        }
        catch (BusinessException)
        {
            return false;
        }
    }

    private static IEnumerable<TestCaseData> BusyWorkerCases()
    {
        yield return new TestCaseData(new Action<TestPlayer, int[]>((player, workerIds) =>
        {
            using (App.PendingEvents())
            {
                player.StartManufacture(player.DomikId(DomikIds.ClayMine), ReceiptIds.ClayDig, [workerIds[0]]);
            }
        })).SetName("BusyWithManufacture");

        yield return new TestCaseData(new Action<TestPlayer, int[]>((player, workerIds) =>
        {
            player.Buy(DomikIds.ScoutHut);
            player.StartExpedition(ShortScoutExpeditionId, [workerIds[0], workerIds[1]]);
        })).SetName("BusyWithExpedition");

        yield return new TestCaseData(new Action<TestPlayer, int[]>((player, workerIds) =>
        {
            player.SetWorkerRest(workerIds[0], DateTimeHelper.GetNowDate().AddHours(1));
        })).SetName("Resting");

        yield return new TestCaseData(new Action<TestPlayer, int[]>((player, workerIds) =>
        {
            player.WithErrand(NeighborIds.Borovoe);
            var busyErrandId = player.LastErrandId();
            player.AcceptErrand(busyErrandId, 0, [workerIds[0]]);
        })).SetName("BusyWithErrand");
    }

    private static IEnumerable<TestCaseData> BlockingErrandStates()
    {
        yield return new TestCaseData(new Action<TestPlayer>(player => player.WithErrand(NeighborIds.Zarechye)))
            .SetName("UnacceptedOffer");

        yield return new TestCaseData(new Action<TestPlayer>(player => player.WithErrand(NeighborIds.Zarechye,
                acceptDate: DateTimeHelper.GetNowDate(),
                clueId: 0,
                finishDate: DateTimeHelper.GetNowDate().AddHours(2))))
            .SetName("AcceptedErrand");
    }

    private static IEnumerable<TestCaseData> AcceptLookupCases()
    {
        yield return new TestCaseData(
                new Func<TestPlayer, int[], int>((_, _) => 987654),
                "Поручение не найдено")
            .SetName("UnknownErrand");

        yield return new TestCaseData(
                new Func<TestPlayer, int[], int>((_, _) =>
                {
                    var other = TestPlayer.Create()
                        .WithErrand(NeighborIds.Zarechye);

                    return other.LastErrandId();
                }),
                "Поручение не найдено")
            .SetName("ForeignErrand");

        yield return new TestCaseData(
                new Func<TestPlayer, int[], int>((player, workerIds) =>
                {
                    player.WithErrand(NeighborIds.Zarechye);
                    var errandId = player.LastErrandId();
                    player.AcceptErrand(errandId, 0, [workerIds[0]]);
                    return errandId;
                }),
                "Поручение уже принято")
            .SetName("AlreadyAccepted");

        yield return new TestCaseData(
                new Func<TestPlayer, int[], int>((player, _) =>
                {
                    player.WithErrand(NeighborIds.Zarechye, expireDate: DateTimeHelper.GetNowDate().AddSeconds(-1));
                    return player.LastErrandId();
                }),
                "Предложение истекло")
            .SetName("ExpiredOffer");
    }

    private static IEnumerable<TestCaseData> AcceptWorkerSelectionCases()
    {
        yield return new TestCaseData(99, new Func<int[], int[]>(workerIds => [workerIds[0]]), "Неверная зацепка")
            .SetName("InvalidClue");

        yield return new TestCaseData(0, new Func<int[], int[]>(_ => []), "Нужен хотя бы один трудяга")
            .SetName("EmptyWorkers");

        yield return new TestCaseData(0, new Func<int[], int[]>(workerIds => [workerIds[0], workerIds[0]]), "Дублирующиеся трудяги")
            .SetName("DuplicateWorkers");

        yield return new TestCaseData(0, new Func<int[], int[]>(workerIds => workerIds.Take(3).ToArray()), "Слишком много трудяг")
            .SetName("TooManyWorkers");
    }
}

file static class ErrandTestsActs
{
    public static CalculateInfo? CreateErrandOffer(this TestPlayer p, int villageLevel)
    {
        return App.Act<ErrandManager, CalculateInfo?>(m => m.CreateOffer(p.Id, villageLevel));
    }

    public static TestPlayer AcceptErrand(this TestPlayer p, int errandId, int clueId, int[] workerIds)
    {
        App.Act<ErrandManager>(m => m.Accept(p.Id, errandId, clueId, workerIds));
        return p;
    }

    public static TestPlayer CancelErrand(this TestPlayer p, int errandId)
    {
        App.Act<ErrandManager>(m => m.Cancel(p.Id, errandId));
        return p;
    }

    public static bool FinishErrand(this TestPlayer p, DateTime date, int errandId)
    {
        return App.Act<ErrandManager, bool>(m => m.FinishErrand(date, new()
        {
            PlayerId = p.Id,
            ObjectId = errandId,
            Date = date,
            Type = CalculateTypes.Errand,
        }));
    }

    public static Errand ErrandRow(this TestPlayer p, int errandId)
    {
        return App.Read(context => context.Errands.Single(x => x.Id == errandId && x.PlayerId == p.Id));
    }

    public static bool HasErrandRow(this TestPlayer p, int errandId)
    {
        return App.Read(context => context.Errands.Any(x => x.Id == errandId && x.PlayerId == p.Id));
    }

    public static int LastErrandId(this TestPlayer p)
    {
        return App.Read(context => context.Errands.Where(x => x.PlayerId == p.Id).OrderByDescending(x => x.Id).Select(x => x.Id).First());
    }
}
