using Domiki.Web.Core;
using Domiki.Web.Infrastructure;
using Domiki.Web.Village;

namespace Domiki.Web.Tests;

[NonParallelizable]
public sealed class MedicineTests
{
    private const int OrdinaryTraitId = 1;
    private const int SonyaTraitId = 4;
    private const int HardyTraitId = 6;

    [TearDown]
    public void TearDown()
    {
        ClearWeatherSchedule();
    }

    /// <summary>
    /// Число одновременно больных трудяг у игрока ограничено: при уже двух больных новый больной сверх лимита не добавляется.
    /// </summary>
    [Test]
    public void FinishManufactureDoesNotExceedMaxSickPerPlayerTest()
    {
        var player = TestPlayer.Create();
        var defaultWorker = player.Workers().Single();
        player.SetWorkerTrait(defaultWorker.Id, OrdinaryTraitId);
        var alreadySickA = InsertWorker(player.Id, "Хворый-А");
        var alreadySickB = InsertWorker(player.Id, "Хворый-Б");
        var future = DateTimeHelper.GetNowDate().AddHours(24);
        SetWorkerSick(alreadySickA, future);
        SetWorkerSick(alreadySickB, future);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h, [defaultWorker.Id]);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        SetManufactureSickChance(manufacture.Id, 100);
        player.FinishManufacture(manufacture.Id, manufacture.FinishDate.AddSeconds(1));

        var worker = player.Workers().Single(x => x.Id == defaultWorker.Id);
        Assert.That(worker.SickUntil, Is.Null);
    }

    /// <summary>
    /// Зафиксированные при старте производства шанс и тип хвори не пересчитываются при последующей смене погоды.
    /// </summary>
    [Test]
    public void SickChanceStaysFixedAtStartWhenWeatherChangesTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.ClayMine);

        var domikId = player.DomikId(DomikIds.ClayMine);
        RaiseVillageToSickGate(player);
        SetWeather(WeatherIds.Rain);

        using (App.PendingEvents())
        {
            player.StartManufacture(domikId, ReceiptIds.ClayDig8h);
        }

        var manufacture = player.Manufacture(domikId);

        SetWeather(WeatherIds.Clear);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(GetManufactureSickChance(manufacture.Id), Is.EqualTo(15));
            Assert.That(GetManufactureSickTypeId(manufacture.Id), Is.EqualTo(SickTypeIds.Cold));
        }
    }

    /// <summary>
    /// Шанс хвори фиксируется при старте производства по величине погодного бонуса, только при обжитости не ниже порога и для
    /// постройки с положительным эффектом погоды.
    /// </summary>
    /// <param name="weatherTypeId">Погода на момент старта производства.</param>
    /// <param name="domikTypeId">Тип постройки, где запускается производство.</param>
    /// <param name="receiptId">Рецепт производства.</param>
    /// <param name="highVillage">Поднята ли деревня до порога простуды.</param>
    /// <param name="expectedSickChance">Ожидаемый зафиксированный шанс заболеть.</param>
    [TestCase(WeatherIds.Rain, DomikIds.ClayMine, ReceiptIds.ClayDig8h, true, 15)]
    [TestCase(WeatherIds.Frost, DomikIds.Forge, ReceiptIds.MakeTool, true, 8)]
    [TestCase(WeatherIds.Wind, DomikIds.LumberMill, ReceiptIds.WoodDig8h, true, 8)]
    [TestCase(WeatherIds.Clear, DomikIds.ClayMine, ReceiptIds.ClayDig8h, true, 0)]
    [TestCase(WeatherIds.Rain, DomikIds.LumberMill, ReceiptIds.WoodDig8h, true, 0)]
    [TestCase(WeatherIds.Rain, DomikIds.ClayMine, ReceiptIds.ClayDig8h, false, 0)]
    public void StartManufactureFixesSickChanceFromWeatherAndVillageTest(int weatherTypeId, int domikTypeId, int receiptId, bool highVillage, int expectedSickChance)
    {
        var player = TestPlayer.Create()
            .WithDomik(domikTypeId)
            .WithResource(ResourceIds.Board, 10)
            .WithResource(ResourceIds.Iron, 10);

        var domikId = player.DomikId(domikTypeId);
        if (highVillage)
        {
            RaiseVillageToSickGate(player);
        }
        else
        {
            Assert.That(GetVillageLevel(player.Id), Is.LessThan(DomikManager.SickMinVillageLevel));
        }

        SetWeather(weatherTypeId);

        using (App.PendingEvents())
        {
            player.StartManufacture(domikId, receiptId);
        }

        var manufacture = player.Manufacture(domikId);
        Assert.That(GetManufactureSickChance(manufacture.Id), Is.EqualTo(expectedSickChance));
    }

    /// <summary>
    /// Плащ вдвое сокращает риск хвори, но никогда не опускает его ниже двух процентов.
    /// </summary>
    [Test]
    public void CloakProtectionHalvesChanceWithFloorTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(DomikManager.GetWorkerSickChance(15, 1, 0), Is.EqualTo(7));
            Assert.That(DomikManager.GetWorkerSickChance(3, 1, 0), Is.EqualTo(DomikManager.MinSickChancePercent));
            Assert.That(DomikManager.GetWorkerSickChance(15, 0, 0), Is.EqualTo(15));
            Assert.That(DomikManager.GetWorkerSickChance(0, 1, 0), Is.Zero);
        }
    }

    /// <summary>
    /// При нехватке плащей защищены только первые по идентификатору трудяги смены.
    /// </summary>
    [Test]
    public void FewerCloaksCoverFirstWorkersTest()
    {
        const int sickChance = 100;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(DomikManager.GetWorkerSickChance(sickChance, 1, 0), Is.EqualTo(50));
            Assert.That(DomikManager.GetWorkerSickChance(sickChance, 1, 1), Is.EqualTo(sickChance));
        }
    }

    /// <summary>
    /// Сушь вызывает перегрев, но плащи на такую смену не выдаются.
    /// </summary>
    [Test]
    public void DroughtDoesNotTakeCloaksTest()
    {
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Cloak, 1)
            .WithDomik(DomikIds.LumberMill);

        RaiseVillageToSickGate(player);
        var domikId = player.DomikId(DomikIds.LumberMill);
        SetWeather(WeatherIds.Drought);

        using (App.PendingEvents())
        {
            player.StartManufacture(domikId, ReceiptIds.WoodDig8h);
        }

        var manufacture = player.Manufacture(domikId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(GetManufactureSickTypeId(manufacture.Id), Is.EqualTo(SickTypeIds.Heatstroke));
            Assert.That(GetManufactureCloakCount(manufacture.Id), Is.Zero);
        }
    }

    /// <summary>
    /// Дождливая смена при открытой хвори резервирует один имеющийся на складе плащ.
    /// </summary>
    [Test]
    public void RainyShiftReservesAvailableCloakTest()
    {
        const int cloakCount = 1;
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Cloak, cloakCount);
        player.SetWorkerTrait(player.Workers().Single().Id, OrdinaryTraitId);

        RaiseVillageToSickGate(player);
        SetWeather(WeatherIds.Rain);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        Assert.That(GetManufactureCloakCount(manufacture.Id), Is.EqualTo(cloakCount));
    }

    /// <summary>
    /// Плащ, выданный первой дождливой смене, не выдаётся второй одновременной смене.
    /// </summary>
    [Test]
    public void ConcurrentRainyShiftsDoNotReuseCloakTest()
    {
        const int cloakCount = 1;
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Cloak, cloakCount)
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.ClayMine);

        foreach (var worker in player.Workers())
        {
            player.SetWorkerTrait(worker.Id, OrdinaryTraitId);
        }

        var firstDomikId = StartingDomikIds.ClayMine;
        var secondDomikId = player.DomikId(DomikIds.ClayMine);
        RaiseVillageToSickGate(player);
        SetWeather(WeatherIds.Rain);

        using (App.PendingEvents())
        {
            player.StartManufacture(firstDomikId, ReceiptIds.ClayDig8h);
            player.StartManufacture(secondDomikId, ReceiptIds.ClayDig8h);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(GetManufactureCloakCount(player.Manufacture(firstDomikId).Id), Is.EqualTo(cloakCount));
            Assert.That(GetManufactureCloakCount(player.Manufacture(secondDomikId).Id), Is.Zero);
        }
    }

    /// <summary>
    /// При нехватке плащей групповая смена получает только число плащей, имеющееся на складе.
    /// </summary>
    [Test]
    public void GroupShiftPartiallyCoveredByCloaksTest()
    {
        const int cloakCount = 1;
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 100)
            .WithResource(ResourceIds.Cloak, cloakCount)
            .WithDomiks(DomikIds.Barrack, 4)
            .WithDomik(DomikIds.ClayMine)
            .Upgrade(7);

        var domikId = player.DomikId(DomikIds.ClayMine);
        foreach (var worker in player.Workers())
        {
            player.SetWorkerTrait(worker.Id, OrdinaryTraitId);
        }

        RaiseVillageToSickGate(player);
        SetWeather(WeatherIds.Rain);

        using (App.PendingEvents())
        {
            player.StartManufacture(domikId, ReceiptIds.ClayDigTogether);
        }

        Assert.That(GetManufactureCloakCount(player.Manufacture(domikId).Id), Is.EqualTo(cloakCount));
    }

    /// <summary>
    /// Пятьдесят смен с плащом изнашивают ровно один плащ и оставляют нулевой остаток износа.
    /// </summary>
    [Test]
    public void FiftyCloakShiftsWriteOffOneCloakTest()
    {
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Cloak, 1);

        for (var shift = 0; shift < DomikManager.CloakLifetimeShifts; shift++)
        {
            var manufactureId = CreateCloakManufacture(player.Id, 1);
            player.FinishManufacture(manufactureId, DateTimeHelper.GetNowDate().AddSeconds(1));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Resource(ResourceIds.Cloak), Is.Zero);
            Assert.That(GetCloakWearPoints(player.Id), Is.Zero);
            Assert.That(GetCloakWornOutEvents(player.Id), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Пустой склад не позволяет списать несуществующий плащ и сбрасывает накопленный износ.
    /// </summary>
    [Test]
    public void EmptyCloakStockDoesNotGoNegativeTest()
    {
        var player = TestPlayer.Create();
        SetCloakWearPoints(player.Id, DomikManager.CloakLifetimeShifts - 1);

        var manufactureId = CreateCloakManufacture(player.Id, 1);
        player.FinishManufacture(manufactureId, DateTimeHelper.GetNowDate().AddSeconds(1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Resource(ResourceIds.Cloak), Is.Zero);
            Assert.That(GetCloakWearPoints(player.Id), Is.Zero);
            Assert.That(GetCloakWornOutEvents(player.Id), Is.Zero);
        }
    }

    /// <summary>
    /// Новый плащ на складе не списывается на ближайшем производстве за износ, оставшийся от пустого склада.
    /// </summary>
    [Test]
    public void NewCloakDoesNotWearOutForClearedDebtTest()
    {
        const int cloakCount = 1;
        var player = TestPlayer.Create();
        SetCloakWearPoints(player.Id, DomikManager.CloakLifetimeShifts - cloakCount);

        var wornManufactureId = CreateCloakManufacture(player.Id, cloakCount);
        player.FinishManufacture(wornManufactureId, DateTimeHelper.GetNowDate().AddSeconds(1));
        player.WithResource(ResourceIds.Cloak, cloakCount);

        var nextManufactureId = CreateCloakManufacture(player.Id, 0);
        player.FinishManufacture(nextManufactureId, DateTimeHelper.GetNowDate().AddSeconds(1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Resource(ResourceIds.Cloak), Is.EqualTo(cloakCount));
            Assert.That(GetCloakWearPoints(player.Id), Is.Zero);
            Assert.That(GetCloakWornOutEvents(player.Id), Is.Zero);
        }
    }

    /// <summary>
    /// Плащи выдаются только восприимчивым к хвори трудягам и изнашиваются ровно на число выданных плащей.
    /// </summary>
    [Test]
    public void CloaksIgnoreImmuneWorkersTest()
    {
        const int cloakCount = 2;
        const int susceptibleWorkerCount = 1;
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 100)
            .WithResource(ResourceIds.Cloak, cloakCount)
            .WithDomiks(DomikIds.Barrack, 4)
            .WithDomik(DomikIds.ClayMine)
            .Upgrade(7);

        var domikId = player.DomikId(DomikIds.ClayMine);
        RaiseVillageToSickGate(player);
        var workers = player.Workers();
        foreach (var worker in workers.Take(workers.Count - susceptibleWorkerCount))
        {
            player.SetWorkerTrait(worker.Id, SonyaTraitId);
        }

        player.SetWorkerTrait(workers[^1].Id, OrdinaryTraitId);
        SetWeather(WeatherIds.Rain);

        using (App.PendingEvents())
        {
            player.StartManufacture(domikId, ReceiptIds.ClayDigTogether, workers.Select(x => x.Id).ToArray());
        }

        var manufacture = player.Manufacture(domikId);
        var reservedCloakCount = GetManufactureCloakCount(manufacture.Id);
        player.FinishManufacture(manufacture.Id, manufacture.FinishDate.AddSeconds(1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reservedCloakCount, Is.EqualTo(susceptibleWorkerCount));
            Assert.That(GetCloakWearPoints(player.Id), Is.EqualTo(susceptibleWorkerCount));
        }
    }

    /// <summary>
    /// Заболевший на смене трудяга хранит тип хвори, зафиксированный при старте производства.
    /// </summary>
    [Test]
    public void SickWorkerCarriesManufactureSickTypeTest()
    {
        var player = TestPlayer.Create();
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, OrdinaryTraitId);
        RaiseVillageToSickGate(player);
        SetWeather(WeatherIds.Rain);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        SetManufactureSickChance(manufacture.Id, 100);
        player.FinishManufacture(manufacture.Id, manufacture.FinishDate.AddSeconds(1));

        worker = player.Workers().Single();
        Assert.That(worker.SickTypeId, Is.EqualTo(SickTypeIds.Cold));
    }

    /// <summary>
    /// Бросок по зафиксированному шансу при завершении производства решает, заболевает ли трудяга: заболевший получает
    /// совпадающие SickUntil и RestUntil длительностью SickDurationSeconds.
    /// </summary>
    /// <param name="sickChance">Зафиксированный шанс заболеть в процентах.</param>
    /// <param name="expectSick">Ожидается ли, что трудяга заболеет.</param>
    [TestCase(100, true)]
    [TestCase(0, false)]
    public void FinishManufactureRollSendsWorkerToSickTest(int sickChance, bool expectSick)
    {
        var player = TestPlayer.Create();
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, OrdinaryTraitId);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        SetManufactureSickChance(manufacture.Id, sickChance);
        var finishDate = manufacture.FinishDate.AddSeconds(1);
        player.FinishManufacture(manufacture.Id, finishDate);

        worker = player.Workers().Single();
        if (expectSick)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(worker.SickUntil, Is.Not.Null);
                Assert.That(worker.RestUntil, Is.EqualTo(worker.SickUntil));
                Assert.That((worker.SickUntilValue() - finishDate).TotalSeconds, Is.EqualTo(DomikManager.SickDurationSeconds).Within(2));
            }
        }
        else
        {
            Assert.That(worker.SickUntil, Is.Null);
        }
    }

    /// <summary>
    /// После выздоровления у трудяги есть временный иммунитет к повторному заболеванию: недавно истёкшая простуда защищает от
    /// 100%-го броска, а давно истёкшая – уже нет.
    /// </summary>
    /// <param name="priorSickOffsetHours">На сколько часов в прошлом истёк предыдущий SickUntil.</param>
    /// <param name="expectResick">Ожидается ли повторное заболевание.</param>
    [TestCase(-1, false)]
    [TestCase(-25, true)]
    public void FinishManufactureRespectsSickImmunityTest(int priorSickOffsetHours, bool expectResick)
    {
        var player = TestPlayer.Create();
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, OrdinaryTraitId);
        var priorSickUntil = DateTimeHelper.GetNowDate().AddHours(priorSickOffsetHours);
        SetWorkerSick(worker.Id, priorSickUntil);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        SetManufactureSickChance(manufacture.Id, 100);
        var finishDate = manufacture.FinishDate.AddSeconds(1);
        player.FinishManufacture(manufacture.Id, finishDate);

        worker = player.Workers().Single();
        if (expectResick)
        {
            Assert.That(worker.SickUntil, Is.GreaterThan(finishDate));
        }
        else
        {
            Assert.That(worker.SickUntil, Is.EqualTo(priorSickUntil));
        }
    }

    /// <summary>
    /// Черты «Соня» и «Здоровяк» дают полный иммунитет к простуде даже при 100%-м шансе заболевания.
    /// </summary>
    /// <param name="traitId">Черта трудяги, проверяемая на иммунитет к простуде.</param>
    [TestCase(SonyaTraitId)]
    [TestCase(HardyTraitId)]
    public void ImmuneTraitsDoNotGetSickTest(int traitId)
    {
        var player = TestPlayer.Create();
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, traitId);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        SetManufactureSickChance(manufacture.Id, 100);
        player.FinishManufacture(manufacture.Id, manufacture.FinishDate.AddSeconds(1));

        worker = player.Workers().Single();
        Assert.That(worker.SickUntil, Is.Null);
    }

    private static int GetVillageLevel(int playerId)
    {
        return App.Act<VillageLevelCalculator, int>(m => m.GetLevel(playerId).Level);
    }

    private static void RaiseVillageToSickGate(TestPlayer player)
    {
        while (GetVillageLevel(player.Id) < DomikManager.SickMinVillageLevel)
        {
            player.WithDomik(DomikIds.ClayMine);
        }
    }

    private static int GetManufactureSickChance(int manufactureId)
    {
        return App.Read(context => context.Manufactures.Single(x => x.Id == manufactureId).SickChance);
    }

    private static int? GetManufactureSickTypeId(int manufactureId)
    {
        return App.Read(context => context.Manufactures.Single(x => x.Id == manufactureId).SickTypeId);
    }

    private static int GetManufactureCloakCount(int manufactureId)
    {
        return App.Read(context => context.Manufactures.Single(x => x.Id == manufactureId).CloakCount);
    }

    private static void SetManufactureSickChance(int manufactureId, int chance)
    {
        using var scope = App.Scope();
        var manufacture = scope.Context.Manufactures.Single(x => x.Id == manufactureId);
        manufacture.SickChance = chance;
        scope.Commit();
    }

    private static int CreateCloakManufacture(int playerId, int cloakCount)
    {
        using var scope = App.Scope();
        var manufacture = new Data.Entities.Manufacture
        {
            DomikId = StartingDomikIds.ClayMine,
            DomikPlayerId = playerId,
            ReceiptId = ReceiptIds.ClayDig,
            FinishDate = DateTimeHelper.GetNowDate(),
            DurationSeconds = 1,
            PlodderCount = 0,
            OutputPercent = 100,
            CloakCount = cloakCount,
        };

        scope.Context.Manufactures.Add(manufacture);
        scope.Commit();
        return manufacture.Id;
    }

    private static int GetCloakWearPoints(int playerId)
    {
        return App.Read(context => context.Players.Single(x => x.Id == playerId).CloakWearPoints);
    }

    private static void SetCloakWearPoints(int playerId, int wearPoints)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == playerId).CloakWearPoints = wearPoints;
        scope.Commit();
    }

    private static int GetCloakWornOutEvents(int playerId)
    {
        return App.Read(context => context.PlayerEvents.Count(x => x.PlayerId == playerId && x.Type == Data.Entities.PlayerEventType.CloakWornOut));
    }

    private static void SetWorkerSick(int workerId, DateTime sickUntil)
    {
        using var scope = App.Scope();
        var worker = scope.Context.Workers.Single(x => x.Id == workerId);
        worker.SickUntil = sickUntil;
        worker.RestUntil = sickUntil;
        scope.Commit();
    }

    private static int InsertWorker(int playerId, string name)
    {
        using var scope = App.Scope();
        var worker = new Data.Entities.Worker
        {
            PlayerId = playerId,
            Name = name,
            TraitId = OrdinaryTraitId,
        };

        scope.Context.Workers.Add(worker);
        scope.Commit();
        return worker.Id;
    }

    private static void SetWeather(int weatherTypeId)
    {
        ClearWeatherSchedule();
        var now = DateTimeHelper.GetNowDate();
        using var scope = App.Scope();
        scope.Context.WeatherPeriods.Add(new()
        {
            WeatherTypeId = weatherTypeId,
            StartDate = now,
            EndDate = now.AddSeconds(WeatherManager.WeatherPeriodSeconds),
        });

        scope.Commit();
    }

    private static void ClearWeatherSchedule()
    {
        using var scope = App.Scope();
        scope.Context.WeatherPeriods.RemoveRange(scope.Context.WeatherPeriods);
        scope.Commit();
    }
}
