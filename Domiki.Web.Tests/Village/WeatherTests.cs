using Domiki.Web.Core;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village;
using Domiki.Web.Village.Models;
using WeatherPeriod = Domiki.Web.Data.Entities.WeatherPeriod;

namespace Domiki.Web.Tests;

[NonParallelizable]
public sealed class WeatherTests
{
    [TearDown]
    public void TearDown()
    {
        ClearWeatherSchedule();
    }

    /// <summary>
    /// Мороз усиливает кузницу, но выход рецепта в одну единицу от этого не растёт – и хвори такая смена не стоит.
    /// </summary>
    [Test]
    public void BonusWeatherOnSingleUnitReceiptGrantsNoExtraAndNoRiskTest()
    {
        const int baseIron = 1;
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Ore, 10)
            .WithDomik(DomikIds.Forge)
            .RaiseVillageLevel(DomikManager.SickMinVillageLevel);

        var domikId = player.DomikId(DomikIds.Forge);
        SetWeather(WeatherIds.Frost);

        using (App.PendingEvents())
        {
            player.StartManufacture(domikId, ReceiptIds.MakeIron);
        }

        var manufacture = player.Manufacture(domikId);
        var sickChance = GetManufactureSickChance(manufacture.Id);
        player.FinishManufacture(manufacture.Id, manufacture.FinishDate.AddSeconds(1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Resource(ResourceIds.Iron), Is.EqualTo(baseIron));
            Assert.That(sickChance, Is.Zero);
        }
    }

    /// <summary>
    /// При полностью пустом расписании погоды его достройка заполняет период от текущего момента и покрывает весь горизонт
    /// прогноза.
    /// </summary>
    [Test]
    public void EnsureWeatherScheduleFromEmptyCoversForecastHorizonTest()
    {
        ClearWeatherSchedule();
        var now = DateTimeHelper.GetNowDate();

        EnsureWeatherSchedule();

        var periods = GetWeatherPeriods();
        Assert.That(periods.Length, Is.GreaterThan(0));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(periods.First().StartDate, Is.EqualTo(now));
            Assert.That(periods.Last().EndDate, Is.GreaterThanOrEqualTo(now.AddSeconds(WeatherManager.ForecastHorizonSeconds)));
            Assert.That(periods.Any(x => x.StartDate <= now && now < x.EndDate), Is.True);
        }
    }

    /// <summary>
    /// Если расписание погоды покрывает только ближайшее будущее, его достройка продлевает хвост без разрывов до полного
    /// горизонта прогноза.
    /// </summary>
    [Test]
    public void EnsureWeatherScheduleFromPartialScheduleExtendsTailTest()
    {
        ClearWeatherSchedule();
        var now = DateTimeHelper.GetNowDate();
        InsertWeatherPeriod(WeatherIds.Clear, now, now.AddSeconds(WeatherManager.WeatherPeriodSeconds));

        EnsureWeatherSchedule();

        var periods = GetWeatherPeriods();
        Assert.That(periods.Last().EndDate, Is.GreaterThanOrEqualTo(now.AddSeconds(WeatherManager.ForecastHorizonSeconds)));
        for (var i = 1; i < periods.Length; i++)
        {
            Assert.That(periods[i].StartDate, Is.EqualTo(periods[i - 1].EndDate));
        }
    }

    /// <summary>
    /// Если хвост расписания погоды устарел (закончился в прошлом), его достройка продолжает расписание вперёд через текущий
    /// момент и до полного горизонта прогноза.
    /// </summary>
    [Test]
    public void EnsureWeatherScheduleFromStaleTailContinuesForwardThroughNowTest()
    {
        ClearWeatherSchedule();
        var now = DateTimeHelper.GetNowDate();
        InsertWeatherPeriod(WeatherIds.Clear, now.AddHours(-16), now.AddHours(-8));

        EnsureWeatherSchedule();

        var periods = GetWeatherPeriods();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(periods.Last().EndDate, Is.GreaterThanOrEqualTo(now.AddSeconds(WeatherManager.ForecastHorizonSeconds)));
            Assert.That(periods.Any(x => x.StartDate <= now && now < x.EndDate), Is.True);
        }
    }

    /// <summary>
    /// Дождь ослабляет заготовку дерева на 25% – по завершении производства выдаётся 6 древесины вместо базовых 8.
    /// </summary>
    [Test]
    public void FinishManufactureCutsOutputUnderRainAtLumberMillTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.LumberMill);

        SetWeather(WeatherIds.Rain);
        player.StartManufacture(4, ReceiptIds.WoodDig8h);

        Assert.That(player.Resource(ResourceIds.Wood), Is.EqualTo(6));
    }

    /// <summary>
    /// Дождь усиливает добычу глины на 50% – по завершении производства выдаётся 12 глины вместо базовых 8.
    /// </summary>
    [Test]
    public void FinishManufactureGrantsBonusOutputUnderRainAtClayMineTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.ClayMine);

        SetWeather(WeatherIds.Rain);
        player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);

        Assert.That(player.Resource(ResourceIds.Clay), Is.EqualTo(12));
    }

    /// <summary>
    /// Даже при искусственно заниженном (1%) проценте выхода завершение производства всегда выдаёт хотя бы одну единицу
    /// ресурса, а не ноль.
    /// </summary>
    [Test]
    public void FinishManufactureMaxGuardPreventsZeroGrantTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.ClayMine);

        SetWeather(WeatherIds.Clear);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        SetManufactureOutputPercent(manufacture.Id, 1);

        player.FinishManufacture(manufacture.Id, manufacture.FinishDate.AddSeconds(1));

        Assert.That(player.Resource(ResourceIds.Clay), Is.EqualTo(1));
    }

    /// <summary>
    /// Процент выхода на момент завершения производства берётся тем, что был зафиксирован при запуске, а не текущей погодой –
    /// смена погоды в процессе не влияет на результат.
    /// </summary>
    [Test]
    public void FinishManufactureUsesOutputPercentFixedAtStartNotAtFinishTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.ClayMine);

        SetWeather(WeatherIds.Rain);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        SetWeather(WeatherIds.Clear);
        player.FinishManufacture(manufacture.Id, manufacture.FinishDate.AddSeconds(1));

        Assert.That(player.Resource(ResourceIds.Clay), Is.EqualTo(12));
    }

    /// <summary>
    /// Процент выхода сдвигает выдачу только на целые единицы: дробная часть сдвига отбрасывается, поэтому выход в одну
    /// единицу не меняют ни бонус, ни штраф, а сама выдача никогда не опускается ниже единицы.
    /// </summary>
    /// <param name="baseValue">Базовый выход ресурса по рецепту.</param>
    /// <param name="outputPercent">Процент выхода, зафиксированный за сменой.</param>
    /// <param name="expectedGrant">Ожидаемое число выданных единиц ресурса.</param>
    [TestCase(1, 75, 1)]
    [TestCase(1, 125, 1)]
    [TestCase(1, 140, 1)]
    [TestCase(1, 150, 1)]
    [TestCase(1, 210, 2)]
    [TestCase(2, 75, 2)]
    [TestCase(2, 125, 2)]
    [TestCase(2, 140, 2)]
    [TestCase(2, 150, 3)]
    [TestCase(2, 210, 4)]
    [TestCase(3, 75, 3)]
    [TestCase(3, 125, 3)]
    [TestCase(3, 140, 4)]
    [TestCase(3, 150, 4)]
    [TestCase(3, 210, 6)]
    [TestCase(4, 75, 3)]
    [TestCase(4, 125, 5)]
    [TestCase(4, 140, 5)]
    [TestCase(4, 150, 6)]
    [TestCase(4, 210, 8)]
    [TestCase(8, 75, 6)]
    [TestCase(8, 125, 10)]
    [TestCase(8, 140, 11)]
    [TestCase(8, 150, 12)]
    [TestCase(8, 210, 16)]
    [TestCase(24, 75, 18)]
    [TestCase(24, 125, 30)]
    [TestCase(24, 140, 33)]
    [TestCase(24, 150, 36)]
    [TestCase(24, 210, 50)]
    public void GetOutputGrantShiftsByWholeUnitsTest(int baseValue, int outputPercent, int expectedGrant)
    {
        Assert.That(DomikManager.GetOutputGrant(baseValue, outputPercent), Is.EqualTo(expectedGrant));
    }

    /// <summary>
    /// Запрос погоды возвращает текущий период плюс прогноз из двух периодов, идущих без разрывов и покрывающих весь горизонт
    /// прогноза.
    /// </summary>
    [Test]
    public void GetWeatherReturnsCurrentAndContiguousForecastTest()
    {
        ClearWeatherSchedule();
        var now = DateTimeHelper.GetNowDate();
        EnsureWeatherSchedule();

        var weather = GetWeather();

        Assert.That(weather.Current, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(weather.Current.StartDate, Is.LessThanOrEqualTo(now));
            Assert.That(weather.Current.EndDate, Is.GreaterThan(now));
            Assert.That(weather.Forecast.Length, Is.EqualTo(2));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(weather.Forecast[0].StartDate, Is.EqualTo(weather.Current.EndDate));
            Assert.That(weather.Forecast[1].StartDate, Is.EqualTo(weather.Forecast[0].EndDate));
            Assert.That(weather.Forecast[1].EndDate, Is.EqualTo(weather.Current.StartDate.AddSeconds(WeatherManager.ForecastHorizonSeconds)));
        }
    }

    /// <summary>
    /// Риск хвори делится между трудягами смены: та же прибавка в четыре глины стоит одиночке 15%, а артели из пяти – 3%.
    /// </summary>
    [Test]
    public void SickChanceSplitsAcrossShiftWorkersTest()
    {
        const int soloSickChance = 15;
        const int groupSickChance = 3;

        var solo = TestPlayer.Create()
            .RaiseVillageLevel(DomikManager.SickMinVillageLevel);

        var group = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 4)
            .WithDomik(DomikIds.ClayMine, 2)
            .RaiseVillageLevel(DomikManager.SickMinVillageLevel);

        var groupDomikId = group.DomikId(DomikIds.ClayMine);
        SetWeather(WeatherIds.Rain);

        using (App.PendingEvents())
        {
            solo.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
            group.StartManufacture(groupDomikId, ReceiptIds.ClayDigTogether);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(GetManufactureSickChance(solo.Manufacture(StartingDomikIds.ClayMine).Id), Is.EqualTo(soloSickChance));
            Assert.That(GetManufactureSickChance(group.Manufacture(groupDomikId).Id), Is.EqualTo(groupSickChance));
        }
    }

    /// <summary>
    /// Погода, влияющая на добываемый ресурс (дождь усиливает глину, засуха усиливает дерево, и наоборот – ослабляет
    /// противоположный ресурс), фиксирует процент выхода производства в момент запуска.
    /// </summary>
    /// <param name="weatherTypeId">Тип погоды на момент запуска.</param>
    /// <param name="domikTypeId">Тип домика-добытчика.</param>
    /// <param name="receiptId">Рецепт добычи.</param>
    /// <param name="expectedOutputPercent">Ожидаемый процент выхода ресурса.</param>
    [TestCase(WeatherIds.Rain, DomikIds.ClayMine, ReceiptIds.ClayDig8h, 150)]
    [TestCase(WeatherIds.Rain, DomikIds.LumberMill, ReceiptIds.WoodDig8h, 75)]
    [TestCase(WeatherIds.Drought, DomikIds.LumberMill, ReceiptIds.WoodDig8h, 150)]
    [TestCase(WeatherIds.Drought, DomikIds.ClayMine, ReceiptIds.ClayDig8h, 75)]
    [TestCase(WeatherIds.Drought, DomikIds.StoneMine, ReceiptIds.StoneDig8h, 125)]
    public void StartManufactureAppliesWeatherOutputPercentTest(int weatherTypeId, int domikTypeId, int receiptId, int expectedOutputPercent)
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(domikTypeId);

        SetWeather(weatherTypeId);

        using (App.PendingEvents())
        {
            player.StartManufacture(4, receiptId);
        }

        var manufacture = player.Manufacture(4);
        Assert.That(GetManufactureOutputPercent(manufacture.Id), Is.EqualTo(expectedOutputPercent));
    }

    /// <summary>
    /// Погода, не влияющая на добываемый ресурс (в т.ч. ясная погода), оставляет процент выхода производства на базовых 100.
    /// </summary>
    /// <param name="weatherTypeId">Тип погоды на момент запуска.</param>
    /// <param name="domikTypeId">Тип домика-добытчика.</param>
    /// <param name="receiptId">Рецепт добычи.</param>
    [TestCase(WeatherIds.Clear, DomikIds.ClayMine, ReceiptIds.ClayDig8h)]
    [TestCase(WeatherIds.Clear, DomikIds.LumberMill, ReceiptIds.WoodDig8h)]
    [TestCase(WeatherIds.Rain, DomikIds.StoneMine, ReceiptIds.StoneDig8h)]
    [TestCase(WeatherIds.Wind, DomikIds.StoneMine, ReceiptIds.StoneDig8h)]
    public void StartManufactureWithoutWeatherEffectKeepsOutputPercent100Test(int weatherTypeId, int domikTypeId, int receiptId)
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.Barrack)
            .WithDomik(domikTypeId);

        SetWeather(weatherTypeId);

        using (App.PendingEvents())
        {
            player.StartManufacture(5, receiptId);
        }

        var manufacture = player.Manufacture(5);
        Assert.That(GetManufactureOutputPercent(manufacture.Id), Is.EqualTo(100));
    }

    /// <summary>
    /// Справочник погоды двусторонний: у каждого вида погоды с эффектами есть и усиление, и ослабление, и у каждой
    /// затронутой постройки тоже – ни один вид погоды и ни одна постройка не остаются с одним лишь бонусом или штрафом.
    /// </summary>
    [Test]
    public void WeatherEffectsAreTwoSidedForWeatherAndBuildingTest()
    {
        var effects = GetWeatherTypes()
            .SelectMany(weather => weather.Effects.Select(effect => (Weather: weather.LogicName, effect.DomikTypeId, effect.OutputPercent)))
            .ToArray();

        Assert.That(effects, Is.Not.Empty);
        using (Assert.EnterMultipleScope())
        {
            foreach (var weather in effects.GroupBy(x => x.Weather))
            {
                Assert.That(weather.Any(x => x.OutputPercent > 100), Is.True, $"погода {weather.Key} без усиления");
                Assert.That(weather.Any(x => x.OutputPercent < 100), Is.True, $"погода {weather.Key} без ослабления");
            }

            foreach (var domik in effects.GroupBy(x => x.DomikTypeId))
            {
                Assert.That(domik.Any(x => x.OutputPercent > 100), Is.True, $"постройка {domik.Key} без усиления");
                Assert.That(domik.Any(x => x.OutputPercent < 100), Is.True, $"постройка {domik.Key} без ослабления");
            }
        }
    }

    private static int GetManufactureSickChance(int manufactureId)
    {
        return App.Read(context => context.Manufactures.Single(x => x.Id == manufactureId).SickChance);
    }

    private static WeatherType[] GetWeatherTypes()
    {
        return App.Act<ResourceManager, WeatherType[]>(m => m.GetWeatherTypes());
    }

    private static int GetManufactureOutputPercent(int manufactureId)
    {
        using var scope = App.Scope();
        return scope.Context.Manufactures.Single(x => x.Id == manufactureId).OutputPercent;
    }

    private static void SetManufactureOutputPercent(int manufactureId, int percent)
    {
        using var scope = App.Scope();
        var manufacture = scope.Context.Manufactures.Single(x => x.Id == manufactureId);
        manufacture.OutputPercent = percent;
        scope.Commit();
    }

    private static void SetWeather(int weatherTypeId)
    {
        ClearWeatherSchedule();
        var now = DateTimeHelper.GetNowDate();
        InsertWeatherPeriod(weatherTypeId, now, now.AddSeconds(WeatherManager.WeatherPeriodSeconds));
    }

    private static void ClearWeatherSchedule()
    {
        using var scope = App.Scope();
        scope.Context.WeatherPeriods.RemoveRange(scope.Context.WeatherPeriods);
        scope.Commit();
    }

    private static void InsertWeatherPeriod(int weatherTypeId, DateTime startDate, DateTime endDate)
    {
        using var scope = App.Scope();
        scope.Context.WeatherPeriods.Add(new()
        {
            WeatherTypeId = weatherTypeId,
            StartDate = startDate,
            EndDate = endDate,
        });

        scope.Commit();
    }

    private static WeatherPeriod[] GetWeatherPeriods()
    {
        using var scope = App.Scope();
        return scope.Context.WeatherPeriods.OrderBy(x => x.StartDate).ToArray();
    }

    private static void EnsureWeatherSchedule()
    {
        App.Act<WeatherManager>(m => m.EnsureWeatherSchedule(DateTimeHelper.GetNowDate()));
    }

    private static WeatherState GetWeather()
    {
        return App.Act<WeatherManager, WeatherState>(m => m.GetWeather(DateTimeHelper.GetNowDate()));
    }
}
