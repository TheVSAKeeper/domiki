using Domiki.Web.Infrastructure;
using Domiki.Web.Workers;

namespace Domiki.Web.Tests;

public sealed class WorkerTests
{
    private const int OrdinaryTraitId = 1;
    private const int HardworkingTraitId = 3;
    private const int SonyaTraitId = 4;

    /// <summary>
    /// Автовыбор трудяги отдаёт предпочтение более эффективному по черте трудяге, а не первому по id.
    /// </summary>
    [Test]
    public void AutoSelectsWorkerWithBestFitnessOverFirstByIdTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2)
            .WithDomik(DomikIds.ClayMine);

        var workers = player.Workers();
        var weakWorker = workers[0];
        var strongWorker = workers[1];
        player.SetWorkerTrait(weakWorker.Id, OrdinaryTraitId);
        player.SetWorkerTrait(strongWorker.Id, HardworkingTraitId);

        using (App.PendingEvents())
        {
            player.StartManufacture(5, ReceiptIds.ClayDig);
        }

        var busyWorker = player.Workers().Single(x => x.ManufactureId != null);
        Assert.That(busyWorker.Id, Is.EqualTo(strongWorker.Id));
    }

    /// <summary>
    /// Параллельные обращения к списку трудяг игрока не приводят к ошибкам и не искажают их количество.
    /// </summary>
    [Test]
    public void ConcurrentGetWorkersDoesNotThrowTest()
    {
        var player = TestPlayer.Create();
        Assert.That(player.Workers().Count, Is.EqualTo(1));

        var errorCount = 0;
        Parallel.ForEach(Enumerable.Range(0, 16), _ =>
        {
            try
            {
                Assert.That(player.Workers().Count, Is.EqualTo(1));
            }
            catch (Exception)
            {
                Interlocked.Increment(ref errorCount);
            }
        });

        Assert.That(errorCount, Is.Zero);
    }

    /// <summary>
    /// Пустой список ручного выбора трудяг откатывается к автоматическому подбору.
    /// </summary>
    [Test]
    public void EmptyManualSelectionFallsBackToAutoTest()
    {
        var player = TestPlayer.Create();

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig, []);
        }

        var busyWorker = player.Workers().Single();
        Assert.That(busyWorker.ManufactureId, Is.Not.Null);
    }

    /// <summary>
    /// Завершение производства прибавляет трудяге фактически отработанные секунды без ухода на отдых, пока порог усталости не
    /// достигнут.
    /// </summary>
    [Test]
    public void FinishManufactureAccumulatesActualWorkerWorkedSecondsTest()
    {
        const int expectedWorkedSeconds = 23040;

        var player = TestPlayer.Create();
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, HardworkingTraitId);

        player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);

        worker = player.Workers().Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(worker.WorkedSeconds, Is.EqualTo(expectedWorkedSeconds));
            Assert.That(worker.RestUntil, Is.Null);
        }
    }

    /// <summary>
    /// Завершение производства заводит трудяге навык по типу постройки и увеличивает счётчик использований.
    /// </summary>
    [Test]
    public void FinishManufactureIncrementsWorkerSkillUsesTest()
    {
        var player = TestPlayer.Create();

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        player.FinishManufacture(manufacture.Id, manufacture.FinishDate.AddSeconds(1));

        var skill = player.Workers().Single().Skills.Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(skill.DomikTypeId, Is.EqualTo(DomikIds.ClayMine));
            Assert.That(skill.Uses, Is.EqualTo(1));
            Assert.That(skill.BonusPercent, Is.EqualTo(WorkerSkillCalculator.GetBonusPercent(1)));
        }
    }

    /// <summary>
    /// Число трудяг у игрока равно вместимости коек барака, новые трудяги изначально свободны от производства.
    /// </summary>
    [Test]
    public void GetWorkersMatchesBedCapacityTest()
    {
        var player = TestPlayer.Create();
        Assert.That(player.Workers().Count, Is.EqualTo(1));

        player.WithDomik(DomikIds.Barrack);

        var workers = player.Workers();
        Assert.That(workers.Count, Is.EqualTo(2));
        Assert.That(workers.All(x => x.ManufactureId == null), Is.True);
    }

    /// <summary>
    /// Групповой рецепт занимает ровно столько трудяг, сколько указано в PlodderCount рецепта, не больше и не меньше.
    /// </summary>
    [Test]
    public void GroupRecipeAssignsReceiptPlodderCountWorkersTest()
    {
        const int expectedBusyWorkers = 5;

        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 100)
            .WithDomiks(DomikIds.Barrack, 4)
            .WithDomik(DomikIds.ClayMine)
            .Upgrade(7);

        using (App.PendingEvents())
        {
            player.StartManufacture(7, ReceiptIds.ClayDigTogether);
        }

        var manufacture = player.Manufacture(7);
        var workers = player.Workers();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(workers.Count(x => x.ManufactureId == manufacture.Id), Is.EqualTo(expectedBusyWorkers));
            Assert.That(workers.Count(x => x.ManufactureId == null), Is.Zero);
        }
    }

    /// <summary>
    /// Ручной выбор назначает на производство именно указанного трудягу, а не автоматически подобранного.
    /// </summary>
    [Test]
    public void ManualSelectionAssignsExplicitWorkerTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.ClayMine);

        var workers = player.Workers();
        var weakWorker = workers[0];
        var strongWorker = workers[1];
        player.SetWorkerTrait(weakWorker.Id, OrdinaryTraitId);
        player.SetWorkerTrait(strongWorker.Id, HardworkingTraitId);

        using (App.PendingEvents())
        {
            player.StartManufacture(4, ReceiptIds.ClayDig, [weakWorker.Id]);
        }

        var busyWorker = player.Workers().Single(x => x.ManufactureId != null);
        Assert.That(busyWorker.Id, Is.EqualTo(weakWorker.Id));
    }

    /// <summary>
    /// Ручной выбор для группового рецепта занимает ровно перечисленных трудяг, остальные остаются свободны.
    /// </summary>
    [Test]
    public void ManualSelectionForGroupRecipeReservesExactWorkersTest()
    {
        const int chosenCount = 5;

        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 200)
            .WithDomiks(DomikIds.Barrack, 4)
            .Upgrade(StartingDomikIds.Barrack)
            .WithDomik(DomikIds.ClayMine)
            .Upgrade(7);

        var workers = player.Workers();
        var chosen = workers.Take(chosenCount).Select(x => x.Id).ToArray();
        var excludedWorkerId = workers[chosenCount].Id;

        using (App.PendingEvents())
        {
            player.StartManufacture(7, ReceiptIds.ClayDigTogether, chosen);
        }

        var updatedWorkers = player.Workers();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(updatedWorkers.Where(x => chosen.Contains(x.Id)).All(x => x.ManufactureId != null), Is.True);
            Assert.That(updatedWorkers.Single(x => x.Id == excludedWorkerId).ManufactureId, Is.Null);
        }
    }

    /// <summary>
    /// Уже занятого производством трудягу нельзя выбрать вручную повторно, ошибка «Трудяга недоступен».
    /// </summary>
    [Test]
    public void ManualSelectionWithBusyWorkerThrowsTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.ClayMine);

        var busyWorkerId = player.Workers()[0].Id;
        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig, [busyWorkerId]);
        }

        var ex = Throws.Business(() => player.StartManufacture(4, ReceiptIds.ClayDig, [busyWorkerId]));
        Assert.That(ex.Message, Is.EqualTo("Трудяга недоступен"));
    }

    /// <summary>
    /// Повторение одного трудяги в списке ручного выбора запрещено ошибкой «Дублирующиеся трудяги».
    /// </summary>
    [Test]
    public void ManualSelectionWithDuplicateWorkerThrowsTest()
    {
        const int chosenCount = 5;

        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 100)
            .WithDomiks(DomikIds.Barrack, 4)
            .WithDomik(DomikIds.ClayMine)
            .Upgrade(7);

        var workerId = player.Workers().First().Id;
        var workerIds = Enumerable.Repeat(workerId, chosenCount).ToArray();

        var ex = Throws.Business(() => player.StartManufacture(7, ReceiptIds.ClayDigTogether, workerIds));
        Assert.That(ex.Message, Is.EqualTo("Дублирующиеся трудяги"));
    }

    /// <summary>
    /// Трудягу другого игрока нельзя выбрать вручную, попытка падает ошибкой «Трудяга недоступен».
    /// </summary>
    [Test]
    public void ManualSelectionWithForeignWorkerThrowsTest()
    {
        var player = TestPlayer.Create();
        var other = TestPlayer.Create();
        var foreignWorkerId = other.Workers().Single().Id;

        var ex = Throws.Business(() => player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig, [foreignWorkerId]));
        Assert.That(ex.Message, Is.EqualTo("Трудяга недоступен"));
    }

    /// <summary>
    /// Отдыхающего трудягу нельзя выбрать вручную, ошибка «Трудяга недоступен».
    /// </summary>
    [Test]
    public void ManualSelectionWithRestingWorkerThrowsTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack);

        var restingWorkerId = player.Workers()[0].Id;
        player.SetWorkerRest(restingWorkerId, DateTimeHelper.GetNowDate().AddHours(1));

        var ex = Throws.Business(() => player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig, [restingWorkerId]));
        Assert.That(ex.Message, Is.EqualTo("Трудяга недоступен"));
    }

    /// <summary>
    /// Число вручную выбранных трудяг должно совпадать с требуемым рецептом, иначе запуск падает ошибкой «Неверное число
    /// трудяг».
    /// </summary>
    [Test]
    public void ManualSelectionWithWrongCountThrowsTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.ClayMine);

        var workerIds = player.Workers().Select(x => x.Id).ToArray();

        var ex = Throws.Business(() => player.StartManufacture(4, ReceiptIds.ClayDig, workerIds));
        Assert.That(ex.Message, Is.EqualTo("Неверное число трудяг"));
    }

    /// <summary>
    /// Черта «Соня» удлиняет длительность производства на 25% в обмен на отсутствие усталости.
    /// </summary>
    [Test]
    public void SonyaTraitLengthensManufactureDurationByTwentyFivePercentTest()
    {
        const int expectedDuration = 36000;

        var player = TestPlayer.Create();
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, SonyaTraitId);

        var start = DateTimeHelper.GetNowDate();
        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        Assert.That((manufacture.FinishDate - start).TotalSeconds, Is.EqualTo(expectedDuration).Within(2));
    }

    /// <summary>
    /// Старт производства занимает трудягу, а его завершение освобождает трудягу обратно.
    /// </summary>
    [Test]
    public void StartManufactureAssignsAndFinishReleasesWorkerTest()
    {
        var player = TestPlayer.Create();

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig);
        }

        var busyWorker = player.Workers().Single();
        Assert.That(busyWorker.ManufactureId, Is.Not.Null);

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        player.FinishManufacture(manufacture.Id, manufacture.FinishDate.AddSeconds(1));

        Assert.That(player.Workers().Single().ManufactureId, Is.Null);
    }

    /// <summary>
    /// Без свободных трудяг запуск производства запрещён ошибкой «Недостаточно трудяг».
    /// </summary>
    [Test]
    public void StartManufactureWithoutFreeWorkersThrowsTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.ClayMine);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig);
        }

        var ex = Throws.Business(() => player.StartManufacture(3, ReceiptIds.ClayDig));
        Assert.That(ex.Message, Is.EqualTo("Недостаточно трудяг"));
    }

    /// <summary>
    /// Торговое производство не накапливает усталость трудяги и не отправляет его отдыхать.
    /// </summary>
    [Test]
    public void TradeManufactureDoesNotAccumulateFatigueTest()
    {
        var player = TestPlayer.Create()
            .Buy(DomikIds.Market)
            .WithResource(ResourceIds.Brick, 1);

        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, OrdinaryTraitId);

        player.StartManufacture(3, ReceiptIds.SellBrick);

        worker = player.Workers().Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(worker.WorkedSeconds, Is.Zero);
            Assert.That(worker.RestUntil, Is.Null);
        }
    }

    /// <summary>
    /// Улучшение барака увеличивает число трудяг вслед за возросшей вместимостью коек.
    /// </summary>
    [Test]
    public void UpgradeBarracksAddsWorkerTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack);

        Assert.That(player.Workers().Count, Is.EqualTo(2));

        player.Upgrade(StartingDomikIds.Barrack);

        Assert.That(player.Workers().Count, Is.EqualTo(3));
    }

    /// <summary>
    /// Бонус навыка не превышает потолок в 15% независимо от числа использований.
    /// </summary>
    [Test]
    public void WorkerSkillBonusHasCapTest()
    {
        Assert.That(WorkerSkillCalculator.GetBonusPercent(10000), Is.EqualTo(15));
    }

    /// <summary>
    /// Прирост бонуса навыка на ранних использованиях больше, чем на поздних: отдача убывает.
    /// </summary>
    [Test]
    public void WorkerSkillBonusHasDiminishingReturnsTest()
    {
        var earlyGain = WorkerSkillCalculator.GetBonusPercent(2) - WorkerSkillCalculator.GetBonusPercent(1);
        var lateGain = WorkerSkillCalculator.GetBonusPercent(41) - WorkerSkillCalculator.GetBonusPercent(40);

        Assert.That(earlyGain, Is.GreaterThan(lateGain));
    }

    /// <summary>
    /// Навык, накопленный по одному типу постройки, не сокращает длительность производства в постройке другого типа.
    /// </summary>
    [Test]
    public void WorkerSkillForOtherDomikTypeDoesNotShortenManufactureDurationTest()
    {
        const int expectedDuration = 28800;

        var player = TestPlayer.Create()
            .Buy(DomikIds.LumberMill);

        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, OrdinaryTraitId);
        player.SetWorkerSkill(worker.Id, DomikIds.ClayMine, 10);

        var start = DateTimeHelper.GetNowDate();
        using (App.PendingEvents())
        {
            player.StartManufacture(3, ReceiptIds.WoodDig8h);
        }

        var manufacture = player.Manufacture(3);
        Assert.That((manufacture.FinishDate - start).TotalSeconds, Is.EqualTo(expectedDuration).Within(2));
    }

    /// <summary>
    /// Навык трудяги по типу постройки сокращает длительность производства.
    /// </summary>
    [Test]
    public void WorkerSkillShortensManufactureDurationTest()
    {
        const int expectedDuration = 26208;

        var player = TestPlayer.Create();
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, OrdinaryTraitId);
        player.SetWorkerSkill(worker.Id, DomikIds.ClayMine, 10);

        var start = DateTimeHelper.GetNowDate();
        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        Assert.That((manufacture.FinishDate - start).TotalSeconds, Is.EqualTo(expectedDuration).Within(2));
    }

    /// <summary>
    /// Черта трудяги и навык по типу постройки сокращают длительность производства мультипликативно, а не складываются.
    /// </summary>
    [Test]
    public void WorkerTraitAndSkillStackMultiplicativelyTest()
    {
        const int expectedDuration = 20967;

        var player = TestPlayer.Create();
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, HardworkingTraitId);
        player.SetWorkerSkill(worker.Id, DomikIds.ClayMine, 10);

        var start = DateTimeHelper.GetNowDate();
        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        Assert.That((manufacture.FinishDate - start).TotalSeconds, Is.EqualTo(expectedDuration).Within(2));
    }

    /// <summary>
    /// Черта «Работящий» сокращает длительность производства на 20%.
    /// </summary>
    [Test]
    public void WorkerTraitShortensManufactureDurationTest()
    {
        const int expectedDuration = 23040;

        var player = TestPlayer.Create();
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, HardworkingTraitId);

        var start = DateTimeHelper.GetNowDate();
        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        Assert.That((manufacture.FinishDate - start).TotalSeconds, Is.EqualTo(expectedDuration).Within(2));
    }

    /// <summary>
    /// По достижении порога усталости трудяга сбрасывает наработку и уходит отдыхать до будущей даты.
    /// </summary>
    /// <param name="receiptId">Рецепт, чья длительность выводит трудягу на порог усталости.</param>
    [TestCase(ReceiptIds.ClayDig8h)]
    public void FinishManufactureFatigueThresholdSendsWorkerToRestTest(int receiptId)
    {
        var player = TestPlayer.Create();
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, OrdinaryTraitId);

        player.StartManufacture(StartingDomikIds.ClayMine, receiptId);

        worker = player.Workers().Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(worker.WorkedSeconds, Is.Zero);
            Assert.That(worker.RestUntil, Is.Not.Null);
        }

        Assert.That(worker.RestUntil, Is.GreaterThan(DateTimeHelper.GetNowDate()));
    }

    /// <summary>
    /// Отдыхающий трудяга недоступен для нового производства, запуск падает ошибкой «Недостаточно трудяг».
    /// </summary>
    /// <param name="receiptId">Рецепт запускаемого производства.</param>
    [TestCase(ReceiptIds.ClayDig8h)]
    public void RestingWorkerDoesNotStartManufactureTest(int receiptId)
    {
        var player = TestPlayer.Create();
        var worker = player.Workers().Single();
        player.SetWorkerRest(worker.Id, DateTimeHelper.GetNowDate().AddHours(1));

        var ex = Throws.Business(() => player.StartManufacture(StartingDomikIds.ClayMine, receiptId));
        Assert.That(ex.Message, Is.EqualTo("Недостаточно трудяг"));
    }

    /// <summary>
    /// Трудяга с истёкшим временем отдыха снова доступен для назначения на производство.
    /// </summary>
    /// <param name="receiptId">Рецепт запускаемого производства.</param>
    [TestCase(ReceiptIds.ClayDig8h)]
    public void WorkerWithExpiredRestUntilStartsManufactureTest(int receiptId)
    {
        var player = TestPlayer.Create();
        var worker = player.Workers().Single();
        player.SetWorkerRest(worker.Id, DateTimeHelper.GetNowDate().AddSeconds(-1));

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, receiptId);
        }

        worker = player.Workers().Single();
        Assert.That(worker.ManufactureId, Is.Not.Null);
    }

    /// <summary>
    /// Трудяга с чертой «Соня» не накапливает усталость и не уходит отдыхать после производства.
    /// </summary>
    /// <param name="receiptId">Рецепт запускаемого производства.</param>
    [TestCase(ReceiptIds.ClayDig24h)]
    public void SonyaWorkerDoesNotAccumulateFatigueTest(int receiptId)
    {
        var player = TestPlayer.Create();
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, SonyaTraitId);
        player.SetWorkerWorked(worker.Id, 0);

        player.StartManufacture(StartingDomikIds.ClayMine, receiptId);

        worker = player.Workers().Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(worker.WorkedSeconds, Is.Zero);
            Assert.That(worker.RestUntil, Is.Null);
        }
    }
}
