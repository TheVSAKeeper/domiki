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
    /// Зафиксированный при старте производства шанс заболевания не пересчитывается при последующей смене погоды.
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

        Assert.That(GetManufactureSickChance(manufacture.Id), Is.EqualTo(DomikManager.SickChancePercent));
    }

    /// <summary>
    /// Шанс заболевания фиксируется при старте производства и ненулевой только в дождь, при уровне деревни не ниже порога
    /// простуды и только для восприимчивого типа постройки (глинокарьер, а не лесопилка).
    /// </summary>
    /// <param name="weatherTypeId">Погода на момент старта производства.</param>
    /// <param name="domikTypeId">Тип постройки, где запускается производство.</param>
    /// <param name="receiptId">Рецепт производства.</param>
    /// <param name="highVillage">Поднята ли деревня до порога простуды.</param>
    /// <param name="expectedSickChance">Ожидаемый зафиксированный шанс заболеть.</param>
    [TestCase(WeatherIds.Rain, DomikIds.ClayMine, ReceiptIds.ClayDig8h, true, DomikManager.SickChancePercent)]
    [TestCase(WeatherIds.Clear, DomikIds.ClayMine, ReceiptIds.ClayDig8h, true, 0)]
    [TestCase(WeatherIds.Rain, DomikIds.LumberMill, ReceiptIds.WoodDig8h, true, 0)]
    [TestCase(WeatherIds.Rain, DomikIds.ClayMine, ReceiptIds.ClayDig8h, false, 0)]
    public void StartManufactureFixesSickChanceFromWeatherAndVillageTest(int weatherTypeId, int domikTypeId, int receiptId, bool highVillage, int expectedSickChance)
    {
        var player = TestPlayer.Create()
            .WithDomik(domikTypeId);

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

    private static void SetManufactureSickChance(int manufactureId, int chance)
    {
        using var scope = App.Scope();
        var manufacture = scope.Context.Manufactures.Single(x => x.Id == manufactureId);
        manufacture.SickChance = chance;
        scope.Commit();
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
