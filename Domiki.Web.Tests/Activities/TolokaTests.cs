using Domiki.Web.Activities;
using Domiki.Web.Activities.Models;
using Domiki.Web.Infrastructure;
using Domiki.Web.Village;
using Toloka = Domiki.Web.Data.Entities.Toloka;

namespace Domiki.Web.Tests;

[NonParallelizable]
public sealed class TolokaTests
{
    private const int BridgeTolokaTypeId = 1;
    private const int GranaryTolokaTypeId = 2;
    private const int KilnTolokaTypeId = 3;

    [SetUp]
    public void SetUp()
    {
        ResetToloka();
        ClearWeatherSchedule();
    }

    [TearDown]
    public void TearDown()
    {
        ResetToloka();
        ClearWeatherSchedule();
    }

    /// <summary>
    /// Бафф толоки типа «мост» добавляет процентный бонус к награде за заказы; у игрока без баффа бонус нулевой.
    /// </summary>
    [Test]
    public void BridgeBuffAddsOrderRewardBonusPercentTest()
    {
        var player = TestPlayer.Create();
        var playerWithoutBuff = TestPlayer.Create();
        CompleteTolokaWithContribution(player.Id, DateTimeHelper.GetNowDate(), BridgeTolokaTypeId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.OrderRewardBonusPercent(), Is.EqualTo(TolokaManager.BridgeOrderBonusPercent));
            Assert.That(playerWithoutBuff.OrderRewardBonusPercent(), Is.Zero);
        }
    }

    /// <summary>
    /// Одновременные взносы разных игроков не теряют обновления: итоговое собранное толоки равно точной сумме взносов.
    /// </summary>
    [Test]
    public async Task ConcurrentContributesKeepExactCollectedSumTest()
    {
        const int firstContribution = 70;
        const int secondContribution = 80;

        var firstPlayer = TestPlayer.Create()
            .WithTolokaUnlocked()
            .WithResource(ResourceIds.Stone, 1000);

        var secondPlayer = TestPlayer.Create()
            .WithTolokaUnlocked()
            .WithResource(ResourceIds.Stone, 1000);

        await Task.WhenAll(Task.Run(() => firstPlayer.Contribute(firstContribution)),
            Task.Run(() => secondPlayer.Contribute(secondContribution)));

        Assert.That(GetActiveToloka().Collected, Is.EqualTo(firstContribution + secondContribution));
    }

    /// <summary>
    /// Достижение цели завершает текущую толоку и сразу заводит новую активную взамен.
    /// </summary>
    [Test]
    public void ContributeCompletesTolokaAndSeedsNextActiveTest()
    {
        const int collectedBeforeContribution = 1960;
        const int contribution = 50;

        var player = TestPlayer.Create()
            .WithTolokaUnlocked()
            .WithResource(ResourceIds.Stone, 100);

        SetActiveTolokaCollected(collectedBeforeContribution);
        var activeId = GetActiveToloka().Id;

        player.Contribute(contribution);

        using var scope = App.Scope();
        var completed = scope.Context.Tolokas.Single(x => x.Id == activeId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(completed.CompletedDate, Is.Not.Null);
            Assert.That(completed.Collected, Is.EqualTo(collectedBeforeContribution + contribution));
            Assert.That(scope.Context.Tolokas.Count(x => x.CompletedDate == null), Is.EqualTo(1));
            Assert.That(scope.Context.TolokaContributions.Single(x => x.TolokaId == activeId && x.PlayerId == player.Id).Value, Is.EqualTo(contribution));
        }
    }

    /// <summary>
    /// Взнос в толоку без нужной постройки бросает исключение и не меняет собранное количество.
    /// </summary>
    [Test]
    public void ContributeWithoutBuildingThrowsAndDoesNotChangeTolokaTest()
    {
        const int startStone = 100;

        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Stone, startStone);

        var ex = Throws.Business(() => player.Contribute(50));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("Нужна Сходня"));
            Assert.That(GetActiveToloka().Collected, Is.Zero);
            Assert.That(player.Resource(ResourceIds.Stone), Is.EqualTo(startStone));
        }
    }

    /// <summary>
    /// Взнос при нехватке ресурса у игрока бросает исключение и не меняет собранное толоки.
    /// </summary>
    [Test]
    public void ContributeWithoutResourcesThrowsAndDoesNotChangeTolokaTest()
    {
        var player = TestPlayer.Create()
            .WithTolokaUnlocked();

        var ex = Throws.Business(() => player.Contribute(50));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Does.StartWith("Недостаточно "));
            Assert.That(GetActiveToloka().Collected, Is.Zero);
        }
    }

    /// <summary>
    /// Взнос списывает ресурс у игрока и увеличивает и общее собранное толоки, и личный вклад игрока.
    /// </summary>
    [Test]
    public void ContributeWritesOffResourceAndIncreasesCollectedAndMineTest()
    {
        const int startStone = 100;
        const int contribution = 40;

        var player = TestPlayer.Create()
            .WithTolokaUnlocked()
            .WithResource(ResourceIds.Stone, startStone);

        player.Contribute(contribution);

        var toloka = player.Toloka();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(toloka.Active.Collected, Is.EqualTo(contribution));
            Assert.That(toloka.MyContribution, Is.EqualTo(contribution));
            Assert.That(player.Resource(ResourceIds.Stone), Is.EqualTo(startStone - contribution));
        }
    }

    /// <summary>
    /// Без нужной постройки состояние толоки недоступно игроку.
    /// </summary>
    [Test]
    public void GetTolokaForNewPlayerReturnsNullTest()
    {
        var player = TestPlayer.Create();

        Assert.That(player.TolokaOrNull(), Is.Null);
    }

    /// <summary>
    /// При наличии нужной постройки видна активная толока типа «мост» с нулевым личным взносом игрока.
    /// </summary>
    [Test]
    public void GetTolokaWithBuildingReturnsActiveTest()
    {
        var player = TestPlayer.Create()
            .WithTolokaUnlocked();

        var toloka = player.Toloka();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toloka.Active.TolokaType.LogicName, Is.EqualTo("bridge"));
            Assert.That(toloka.MyContribution, Is.Zero);
        }
    }

    /// <summary>
    /// Бафф толоки типа «амбар» не повышает выпуск кузницы, действует только на свою постройку.
    /// </summary>
    [Test]
    public void GranaryBuffDoesNotBoostForgeTest()
    {
        var player = TestPlayer.Create()
            .WithDecor(DecorIds.Fountain, 1)
            .WithResource(ResourceIds.Coin, 800)
            .WithDomik(DomikIds.Barrack, 2)
            .WithDomik(DomikIds.Forge)
            .WithResource(ResourceIds.Iron, 1)
            .WithResource(ResourceIds.Board, 1);

        CompleteTolokaWithContribution(player.Id, DateTimeHelper.GetNowDate());
        SetWeather(WeatherIds.Clear);

        var forgeId = player.DomikId(DomikIds.Forge);
        using (App.PendingEvents())
        {
            player.StartManufacture(forgeId, ReceiptIds.MakeTool);
        }

        Assert.That(player.ManufactureOutputPercent(forgeId), Is.EqualTo(100));
    }

    /// <summary>
    /// Бафф толоки типа «печь» повышает выпуск кузницы.
    /// </summary>
    [Test]
    public void KilnBuffBoostsForgeTest()
    {
        var player = TestPlayer.Create()
            .WithDecor(DecorIds.Fountain, 1)
            .WithResource(ResourceIds.Coin, 800)
            .WithDomik(DomikIds.Barrack, 2)
            .WithDomik(DomikIds.Forge)
            .WithResource(ResourceIds.Iron, 1)
            .WithResource(ResourceIds.Board, 1);

        CompleteTolokaWithContribution(player.Id, DateTimeHelper.GetNowDate(), KilnTolokaTypeId);
        SetWeather(WeatherIds.Clear);

        var forgeId = player.DomikId(DomikIds.Forge);
        using (App.PendingEvents())
        {
            player.StartManufacture(forgeId, ReceiptIds.MakeTool);
        }

        Assert.That(player.ManufactureOutputPercent(forgeId), Is.EqualTo(140));
    }

    /// <summary>
    /// Цель следующей толоки растёт пропорционально числу участников предыдущей.
    /// </summary>
    [Test]
    public void NextTolokaGoalScalesWithPreviousContributorsTest()
    {
        const int contribution = 40;

        var first = TestPlayer.Create().WithTolokaUnlocked().WithResource(ResourceIds.Stone, contribution);
        var second = TestPlayer.Create().WithTolokaUnlocked().WithResource(ResourceIds.Stone, contribution);
        var third = TestPlayer.Create().WithTolokaUnlocked().WithResource(ResourceIds.Stone, contribution);
        SetActiveTolokaGoal(100);

        first.Contribute(contribution);
        second.Contribute(contribution);
        third.Contribute(contribution);

        Assert.That(GetActiveToloka().Goal, Is.EqualTo(2400));
    }

    /// <summary>
    /// Бафф завершённой толоки повышает выпуск производства, пока действует внутри своего временного окна.
    /// </summary>
    [Test]
    public void StartManufactureAppliesTolokaBuffInsideWindowTest()
    {
        var player = TestPlayer.Create();
        CompleteTolokaWithContribution(player.Id, DateTimeHelper.GetNowDate());
        SetWeather(WeatherIds.Clear);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        Assert.That(player.ManufactureOutputPercent(2), Is.EqualTo(140));
    }

    /// <summary>
    /// Бафф толоки перестаёт действовать на производство, когда его временное окно уже истекло.
    /// </summary>
    [Test]
    public void StartManufactureDoesNotApplyTolokaBuffOutsideWindowTest()
    {
        var player = TestPlayer.Create();
        CompleteTolokaWithContribution(player.Id, DateTimeHelper.GetNowDate().AddHours(-9));
        SetWeather(WeatherIds.Clear);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        Assert.That(player.ManufactureOutputPercent(2), Is.EqualTo(100));
    }

    /// <summary>
    /// Бафф толоки перемножается с погодным бонусом, а не заменяет его: дождь (150%) вместе с толокой (140%) дают 210% выхода,
    /// а не 190%.
    /// </summary>
    [Test]
    public void StartManufactureStacksTolokaBuffWithWeatherTest()
    {
        var player = TestPlayer.Create();
        CompleteTolokaWithContribution(player.Id, DateTimeHelper.GetNowDate());
        SetWeather(WeatherIds.Rain);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        Assert.That(player.ManufactureOutputPercent(2), Is.EqualTo(210));
    }

    /// <summary>
    /// Толока общая для всех игроков: взнос любого из них приближает завершение, и бафф после завершения получают все
    /// участники.
    /// </summary>
    [Test]
    public void TwoPlayersContributeToOneTolokaAndBothGetBuffTest()
    {
        const int collectedBeforeContribution = 1900;
        const int contribution = 50;

        var first = TestPlayer.Create().WithTolokaUnlocked().WithResource(ResourceIds.Stone, 1000);
        var second = TestPlayer.Create().WithTolokaUnlocked().WithResource(ResourceIds.Stone, 1000);
        SetActiveTolokaCollected(collectedBeforeContribution);

        first.Contribute(contribution);
        second.Contribute(contribution);

        using (var scope = App.Scope())
        {
            var completed = scope.Context.Tolokas.Single(x => x.CompletedDate != null);
            Assert.That(completed.Collected, Is.EqualTo(collectedBeforeContribution + contribution + contribution));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.HasActiveBuff(), Is.True);
            Assert.That(second.HasActiveBuff(), Is.True);
        }
    }

    /// <summary>
    /// Уровень площадки сбора управляет длительностью баффа толоки в часах; предпросмотр следующего уровня показывается только
    /// до максимального пятого.
    /// </summary>
    /// <param name="level">Уровень площадки сбора.</param>
    /// <param name="expectedHours">Ожидаемая длительность баффа в часах.</param>
    [TestCase(1, 8)]
    [TestCase(2, 10)]
    [TestCase(5, 16)]
    public void GatheringLevelControlsBuffHoursTest(int level, int expectedHours)
    {
        var player = TestPlayer.Create().WithTolokaUnlocked();
        SetGatheringLevel(player.Id, level);

        var state = player.Toloka();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.BuffHours, Is.EqualTo(expectedHours));
            Assert.That(state.NextBuffHours, Is.EqualTo(level < 5 ? expectedHours + 2 : null));
        }
    }

    /// <summary>
    /// Неположительное количество взноса отклоняется исключением и не меняет собранное толоки.
    /// </summary>
    /// <param name="amount">Проверяемое количество взноса.</param>
    [TestCase(0)]
    [TestCase(-1)]
    public void ContributeInvalidAmountThrowsAndDoesNotChangeTolokaTest(int amount)
    {
        var player = TestPlayer.Create()
            .WithTolokaUnlocked()
            .WithResource(ResourceIds.Stone, 100);

        var ex = Throws.Business(() => player.Contribute(amount));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("Неверное количество"));
            Assert.That(GetActiveToloka().Collected, Is.Zero);
        }
    }

    private static Toloka GetActiveToloka()
    {
        return App.Read(context => context.Tolokas.Single(x => x.CompletedDate == null));
    }

    private static void SetActiveTolokaCollected(int collected)
    {
        using var scope = App.Scope();
        scope.Context.Tolokas.Single(x => x.CompletedDate == null).Collected = collected;
        scope.Commit();
    }

    private static void SetActiveTolokaGoal(int goal)
    {
        using var scope = App.Scope();
        scope.Context.Tolokas.Single(x => x.CompletedDate == null).Goal = goal;
        scope.Commit();
    }

    private static void SetGatheringLevel(int playerId, int level)
    {
        using var scope = App.Scope();
        scope.Context.Domiks.Single(x => x.PlayerId == playerId && x.TypeId == DomikIds.Gathering).Level = level;
        scope.Commit();
    }

    private static void CompleteTolokaWithContribution(int playerId, DateTime completedDate, int completedTolokaTypeId = GranaryTolokaTypeId)
    {
        using var scope = App.Scope();
        var active = scope.Context.Tolokas.Single(x => x.CompletedDate == null);
        active.TolokaTypeId = completedTolokaTypeId;
        active.Collected = 2000;
        active.Goal = 2000;
        active.CompletedDate = completedDate;
        scope.Context.TolokaContributions.Add(new()
        {
            TolokaId = active.Id,
            PlayerId = playerId,
            Value = 1,
        });

        scope.Context.Tolokas.Add(new()
        {
            TolokaTypeId = BridgeTolokaTypeId,
            Collected = 0,
            Goal = 2000,
            StartDate = completedDate,
            CompletedDate = null,
        });

        scope.Commit();
    }

    private static void ResetToloka()
    {
        using var scope = App.Scope();
        scope.Context.TolokaContributions.RemoveRange(scope.Context.TolokaContributions);
        scope.Context.Tolokas.RemoveRange(scope.Context.Tolokas);
        scope.Context.Tolokas.Add(new()
        {
            TolokaTypeId = BridgeTolokaTypeId,
            Collected = 0,
            Goal = 2000,
            StartDate = DateTimeHelper.GetNowDate(),
            CompletedDate = null,
        });

        scope.Commit();
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

file static class TolokaTestsActs
{
    public static TestPlayer WithTolokaUnlocked(this TestPlayer player)
    {
        return player.WithDecor(DecorIds.Fountain, 4)
            .WithResource(ResourceIds.Coin, 800)
            .Buy(DomikIds.Gathering);
    }

    public static TestPlayer Contribute(this TestPlayer p, int amount)
    {
        App.Act<TolokaManager>(m => m.Contribute(p.Id, amount, DateTimeHelper.GetNowDate()));
        return p;
    }

    public static TolokaState Toloka(this TestPlayer p)
    {
        var state = p.TolokaOrNull();
        Assert.That(state, Is.Not.Null);
        return state!;
    }

    public static TolokaState? TolokaOrNull(this TestPlayer p)
    {
        return App.Act<TolokaManager, TolokaState?>(m => m.GetToloka(DateTimeHelper.GetNowDate(), p.Id));
    }

    public static bool HasActiveBuff(this TestPlayer p)
    {
        return App.Act<TolokaManager, bool>(m => m.HasActiveBuff(p.Id, DateTimeHelper.GetNowDate()));
    }

    public static int OrderRewardBonusPercent(this TestPlayer p)
    {
        return App.Act<TolokaManager, int>(m => m.GetOrderRewardBonusPercent(p.Id, DateTimeHelper.GetNowDate()));
    }

    public static int ManufactureOutputPercent(this TestPlayer p, int domikId)
    {
        var manufactureId = p.Manufacture(domikId).Id;
        return App.Read(context => context.Manufactures.Single(x => x.Id == manufactureId).OutputPercent);
    }
}
