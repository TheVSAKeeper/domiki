using Domiki.Web.Infrastructure;
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
    [TestCase(WeatherIds.Drought, DomikIds.StoneMine, ReceiptIds.StoneDig8h)]
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
