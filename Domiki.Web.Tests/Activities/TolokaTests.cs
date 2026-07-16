using Domiki.Web.Activities.Models;
using Domiki.Web.Core.Models;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference.Models;
using Domiki.Web.Village;
using Toloka = Domiki.Web.Data.Entities.Toloka;

namespace Domiki.Web.Tests;

[NonParallelizable]
public class TolokaTests : TestBase
{
    private const int BridgeTolokaTypeId = 1;
    private const int GranaryTolokaTypeId = 2;
    private const int KilnTolokaTypeId = 3;
    private const int StoneResourceTypeId = 2;
    private const int ClayDig8hReceiptId = 14;
    private const int RainWeatherTypeId = 2;
    private const int ClearWeatherTypeId = 1;
    private const int FountainDecorTypeId = 4;
    private const int GatheringDomikTypeId = 10;

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
        var playerId = GetPlayerId();
        var playerWithoutBuffId = GetPlayerId();
        CompleteTolokaWithContribution(playerId, DateTimeHelper.GetNowDate(), BridgeTolokaTypeId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(GetOrderRewardBonusPercent(playerId), Is.EqualTo(40));
            Assert.That(GetOrderRewardBonusPercent(playerWithoutBuffId), Is.Zero);
        }
    }

    /// <summary>
    /// Одновременные взносы разных игроков не теряют обновления: итоговое собранное толоки равно точной сумме взносов.
    /// </summary>
    [Test]
    public async Task ConcurrentContributesKeepExactCollectedSumTest()
    {
        var firstPlayerId = GetUnlockedPlayerId();
        var secondPlayerId = GetUnlockedPlayerId();
        GrantResource(firstPlayerId, StoneResourceTypeId, 1000);
        GrantResource(secondPlayerId, StoneResourceTypeId, 1000);

        await Task.WhenAll(Task.Run(() => Contribute(firstPlayerId, 70)),
            Task.Run(() => Contribute(secondPlayerId, 80)));

        Assert.That(GetActiveToloka().Collected, Is.EqualTo(150));
    }

    /// <summary>
    /// Достижение цели завершает текущую толоку и сразу заводит новую активную взамен.
    /// </summary>
    [Test]
    public void ContributeCompletesTolokaAndSeedsNextActiveTest()
    {
        var playerId = GetUnlockedPlayerId();
        GrantResource(playerId, StoneResourceTypeId, 100);
        SetActiveTolokaCollected(1960);
        var activeId = GetActiveToloka().Id;

        Contribute(playerId, 50);

        using var uow = GetUow();
        var completed = uow.Context.Tolokas.Single(x => x.Id == activeId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(completed.CompletedDate, Is.Not.Null);
            Assert.That(completed.Collected, Is.EqualTo(2010));
            Assert.That(uow.Context.Tolokas.Count(x => x.CompletedDate == null), Is.EqualTo(1));
            Assert.That(uow.Context.TolokaContributions.Single(x => x.TolokaId == activeId && x.PlayerId == playerId).Value, Is.EqualTo(50));
        }
    }

    /// <summary>
    /// Взнос в толоку без нужной постройки бросает исключение и не меняет собранное количество.
    /// </summary>
    [Test]
    public void ContributeWithoutBuildingThrowsAndDoesNotChangeTolokaTest()
    {
        var playerId = GetPlayerId();
        GrantResource(playerId, StoneResourceTypeId, 100);

        var ex = Assert.Throws<BusinessException>(() => Contribute(playerId, 50));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("Нужна Сходня"));
            Assert.That(GetActiveToloka().Collected, Is.Zero);
            Assert.That(GetResources(playerId).Single(x => x.Type.Id == StoneResourceTypeId).Value, Is.EqualTo(100));
        }
    }

    /// <summary>
    /// Взнос при нехватке ресурса у игрока бросает исключение и не меняет собранное толоки.
    /// </summary>
    [Test]
    public void ContributeWithoutResourcesThrowsAndDoesNotChangeTolokaTest()
    {
        var playerId = GetUnlockedPlayerId();

        var ex = Assert.Throws<BusinessException>(() => Contribute(playerId, 50));

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
        var playerId = GetUnlockedPlayerId();
        GrantResource(playerId, StoneResourceTypeId, 100);

        Contribute(playerId, 40);

        var toloka = GetToloka(playerId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(toloka.Active.Collected, Is.EqualTo(40));
            Assert.That(toloka.MyContribution, Is.EqualTo(40));
            Assert.That(GetResources(playerId).Single(x => x.Type.Id == StoneResourceTypeId).Value, Is.EqualTo(60));
        }
    }

    /// <summary>
    /// Без нужной постройки состояние толоки недоступно игроку.
    /// </summary>
    [Test]
    public void GetTolokaForNewPlayerReturnsNullTest()
    {
        var playerId = GetPlayerId();

        var toloka = GetToloka(playerId);

        Assert.That(toloka, Is.Null);
    }

    /// <summary>
    /// При наличии нужной постройки видна активная толока типа «мост» с нулевым личным взносом игрока.
    /// </summary>
    [Test]
    public void GetTolokaWithBuildingReturnsActiveTest()
    {
        var playerId = GetUnlockedPlayerId();

        var toloka = GetToloka(playerId);

        Assert.That(toloka.Active, Is.Not.Null);
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
        var playerId = GetPlayerId();
        CompleteTolokaWithContribution(playerId, DateTimeHelper.GetNowDate());
        GrantDecor(playerId, FountainDecorTypeId, 1);
        GrantResource(playerId, 1, 800);
        GrantDomik(playerId, 3, 2);
        GrantDomik(playerId, 4, 1);
        SetWeather(ClearWeatherTypeId);
        GrantResource(playerId, 17, 1);
        GrantResource(playerId, 7, 1);

        StartManufacture(playerId, 4, GetReceiptId("make_tool"));

        var manufacture = GetDomiks(playerId).First(x => x.Id == 4).Manufactures.Single();
        Assert.That(GetManufactureOutputPercent(manufacture.Id), Is.EqualTo(100));
    }

    /// <summary>
    /// Бафф толоки типа «печь» повышает выпуск кузницы.
    /// </summary>
    [Test]
    public void KilnBuffBoostsForgeTest()
    {
        var playerId = GetPlayerId();
        CompleteTolokaWithContribution(playerId, DateTimeHelper.GetNowDate(), KilnTolokaTypeId);
        GrantDecor(playerId, FountainDecorTypeId, 1);
        GrantResource(playerId, 1, 800);
        GrantDomik(playerId, 3, 2);
        GrantDomik(playerId, 4, 1);
        SetWeather(ClearWeatherTypeId);
        GrantResource(playerId, 17, 1);
        GrantResource(playerId, 7, 1);

        StartManufacture(playerId, 4, GetReceiptId("make_tool"));

        var manufacture = GetDomiks(playerId).First(x => x.Id == 4).Manufactures.Single();
        Assert.That(GetManufactureOutputPercent(manufacture.Id), Is.EqualTo(140));
    }

    /// <summary>
    /// Цель следующей толоки растёт пропорционально числу участников предыдущей.
    /// </summary>
    [Test]
    public void NextTolokaGoalScalesWithPreviousContributorsTest()
    {
        var firstPlayerId = GetUnlockedPlayerId();
        var secondPlayerId = GetUnlockedPlayerId();
        var thirdPlayerId = GetUnlockedPlayerId();
        GrantResource(firstPlayerId, StoneResourceTypeId, 40);
        GrantResource(secondPlayerId, StoneResourceTypeId, 40);
        GrantResource(thirdPlayerId, StoneResourceTypeId, 40);
        SetActiveTolokaGoal(100);

        Contribute(firstPlayerId, 40);
        Contribute(secondPlayerId, 40);
        Contribute(thirdPlayerId, 40);

        using var uow = GetUow();
        Assert.That(uow.Context.Tolokas.Single(x => x.CompletedDate == null).Goal, Is.EqualTo(2400));
    }

    /// <summary>
    /// Бафф завершённой толоки повышает выпуск производства, пока действует внутри своего временного окна.
    /// </summary>
    [Test]
    public void StartManufactureAppliesTolokaBuffInsideWindowTest()
    {
        var playerId = GetPlayerId();
        CompleteTolokaWithContribution(playerId, DateTimeHelper.GetNowDate());
        SetWeather(ClearWeatherTypeId);

        StartManufacture(playerId, 2, ClayDig8hReceiptId);

        var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
        Assert.That(GetManufactureOutputPercent(manufacture.Id), Is.EqualTo(140));
    }

    /// <summary>
    /// Бафф толоки перестаёт действовать на производство, когда его временное окно уже истекло.
    /// </summary>
    [Test]
    public void StartManufactureDoesNotApplyTolokaBuffOutsideWindowTest()
    {
        var playerId = GetPlayerId();
        CompleteTolokaWithContribution(playerId, DateTimeHelper.GetNowDate().AddHours(-9));
        SetWeather(ClearWeatherTypeId);

        StartManufacture(playerId, 2, ClayDig8hReceiptId);

        var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
        Assert.That(GetManufactureOutputPercent(manufacture.Id), Is.EqualTo(100));
    }

    /// <summary>
    /// Бафф толоки перемножается с погодным бонусом, а не заменяет его: дождь (150%) вместе с толокой (140%) дают 210% выхода,
    /// а не 190%.
    /// </summary>
    [Test]
    public void StartManufactureStacksTolokaBuffWithWeatherTest()
    {
        var playerId = GetPlayerId();
        CompleteTolokaWithContribution(playerId, DateTimeHelper.GetNowDate());
        SetWeather(RainWeatherTypeId);

        StartManufacture(playerId, 2, ClayDig8hReceiptId);

        var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
        Assert.That(GetManufactureOutputPercent(manufacture.Id), Is.EqualTo(210));
    }

    /// <summary>
    /// Толока общая для всех игроков: взнос любого из них приближает завершение, и бафф после завершения получают все
    /// участники.
    /// </summary>
    [Test]
    public void TwoPlayersContributeToOneTolokaAndBothGetBuffTest()
    {
        var firstPlayerId = GetUnlockedPlayerId();
        var secondPlayerId = GetUnlockedPlayerId();
        GrantResource(firstPlayerId, StoneResourceTypeId, 1000);
        GrantResource(secondPlayerId, StoneResourceTypeId, 1000);
        SetActiveTolokaCollected(1900);

        Contribute(firstPlayerId, 50);
        Contribute(secondPlayerId, 50);

        using (var uow = GetUow())
        {
            var completed = uow.Context.Tolokas.Single(x => x.CompletedDate != null);
            Assert.That(completed.Collected, Is.EqualTo(2000));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(HasActiveBuff(firstPlayerId), Is.True);
            Assert.That(HasActiveBuff(secondPlayerId), Is.True);
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
        var playerId = GetUnlockedPlayerId();
        SetGatheringLevel(playerId, level);

        var state = GetToloka(playerId)!;

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
        var playerId = GetUnlockedPlayerId();
        GrantResource(playerId, StoneResourceTypeId, 100);

        var ex = Assert.Throws<BusinessException>(() => Contribute(playerId, amount));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("Неверное количество"));
            Assert.That(GetActiveToloka().Collected, Is.Zero);
        }
    }

    private int GetPlayerId()
    {
        using var uow = GetUow();
        var domikManager = GetDomikManager(uow);
        var playerId = domikManager.GetPlayerId("testUser_" + Guid.NewGuid());
        uow.Commit();
        return playerId;
    }

    private int GetUnlockedPlayerId()
    {
        var playerId = GetPlayerId();
        GrantDecor(playerId, FountainDecorTypeId, 4);
        GrantResource(playerId, 1, 800);
        BuyDomik(playerId, GatheringDomikTypeId);
        return playerId;
    }

    private TolokaState? GetToloka(int playerId)
    {
        using var uow = GetUow();
        var manager = GetTolokaManager(uow);
        var toloka = manager.GetToloka(DateTimeHelper.GetNowDate(), playerId);
        uow.Commit();
        return toloka;
    }

    private void Contribute(int playerId, int amount)
    {
        using var uow = GetUow();
        var manager = GetTolokaManager(uow);
        manager.Contribute(playerId, amount, DateTimeHelper.GetNowDate());
        uow.Commit();
    }

    private bool HasActiveBuff(int playerId)
    {
        using var uow = GetUow();
        var manager = GetTolokaManager(uow);
        var result = manager.HasActiveBuff(playerId, DateTimeHelper.GetNowDate());
        uow.Commit();
        return result;
    }

    private int GetOrderRewardBonusPercent(int playerId)
    {
        using var uow = GetUow();
        var manager = GetTolokaManager(uow);
        var result = manager.GetOrderRewardBonusPercent(playerId, DateTimeHelper.GetNowDate());
        uow.Commit();
        return result;
    }

    private Domik[] GetDomiks(int playerId)
    {
        using var uow = GetUow();
        var domikManager = GetDomikManager(uow);
        var domiks = domikManager.GetDomiks(playerId).ToArray();
        uow.Commit();
        return domiks;
    }

    private Resource[] GetResources(int playerId)
    {
        using var uow = GetUow();
        var domikManager = GetDomikManager(uow);
        var resources = domikManager.GetResources(playerId).ToArray();
        uow.Commit();
        return resources;
    }

    private void BuyDomik(int playerId, int domikTypeId)
    {
        using var uow = GetUow();
        var domikManager = GetDomikManager(uow);
        domikManager.BuyDomik(playerId, domikTypeId);
        uow.Commit();
    }

    private void SetGatheringLevel(int playerId, int level)
    {
        using var uow = GetUow();
        uow.Context.Domiks.Single(x => x.PlayerId == playerId && x.TypeId == GatheringDomikTypeId).Level = level;
        uow.Commit();
    }

    private void StartManufacture(int playerId, int domikId, int receiptId)
    {
        using var uow = GetUow();
        var domikManager = GetDomikManager(uow, false);
        domikManager.StartManufacture(playerId, domikId, receiptId);
        uow.Commit();
    }

    private int GetManufactureOutputPercent(int manufactureId)
    {
        using var uow = GetUow();
        return uow.Context.Manufactures.Single(x => x.Id == manufactureId).OutputPercent;
    }

    private void GrantResource(int playerId, int resourceTypeId, int value)
    {
        using var uow = GetUow();
        var resource = uow.Context.Resources.SingleOrDefault(x => x.PlayerId == playerId && x.TypeId == resourceTypeId);
        if (resource == null)
        {
            resource = new()
            {
                PlayerId = playerId,
                TypeId = resourceTypeId,
            };

            uow.Context.Resources.Add(resource);
        }

        resource.Value += value;
        uow.Context.SaveChanges();
        uow.Commit();
    }

    private void GrantDecor(int playerId, int decorTypeId, int count)
    {
        using var uow = GetUow();
        var decor = uow.Context.PlayerDecors.SingleOrDefault(x => x.PlayerId == playerId && x.DecorTypeId == decorTypeId);
        if (decor == null)
        {
            decor = new()
            {
                PlayerId = playerId,
                DecorTypeId = decorTypeId,
            };

            uow.Context.PlayerDecors.Add(decor);
        }

        decor.Count += count;
        uow.Context.SaveChanges();
        uow.Commit();
    }

    private Toloka GetActiveToloka()
    {
        using var uow = GetUow();
        return uow.Context.Tolokas.Single(x => x.CompletedDate == null);
    }

    private void SetActiveTolokaCollected(int collected)
    {
        using var uow = GetUow();
        var toloka = uow.Context.Tolokas.Single(x => x.CompletedDate == null);
        toloka.Collected = collected;
        uow.Commit();
    }

    private void SetActiveTolokaGoal(int goal)
    {
        using var uow = GetUow();
        var toloka = uow.Context.Tolokas.Single(x => x.CompletedDate == null);
        toloka.Goal = goal;
        uow.Commit();
    }

    private void CompleteTolokaWithContribution(int playerId, DateTime completedDate, int completedTolokaTypeId = GranaryTolokaTypeId)
    {
        using var uow = GetUow();
        var active = uow.Context.Tolokas.Single(x => x.CompletedDate == null);
        active.TolokaTypeId = completedTolokaTypeId;
        active.Collected = 2000;
        active.Goal = 2000;
        active.CompletedDate = completedDate;
        uow.Context.TolokaContributions.Add(new()
        {
            TolokaId = active.Id,
            PlayerId = playerId,
            Value = 1,
        });

        uow.Context.Tolokas.Add(new()
        {
            TolokaTypeId = BridgeTolokaTypeId,
            Collected = 0,
            Goal = 2000,
            StartDate = completedDate,
            CompletedDate = null,
        });

        uow.Context.SaveChanges();
        uow.Commit();
    }

    private void ResetToloka()
    {
        using var uow = GetUow();
        uow.Context.TolokaContributions.RemoveRange(uow.Context.TolokaContributions);
        uow.Context.Tolokas.RemoveRange(uow.Context.Tolokas);
        uow.Context.SaveChanges();
        uow.Context.Tolokas.Add(new()
        {
            TolokaTypeId = BridgeTolokaTypeId,
            Collected = 0,
            Goal = 2000,
            StartDate = DateTimeHelper.GetNowDate(),
            CompletedDate = null,
        });

        uow.Context.SaveChanges();
        uow.Commit();
    }

    private int GetReceiptId(string logicName)
    {
        using var uow = GetUow();
        return uow.Context.Receipts.Single(x => x.LogicName == logicName).Id;
    }

    private void SetWeather(int weatherTypeId)
    {
        ClearWeatherSchedule();
        var now = DateTimeHelper.GetNowDate();
        InsertWeatherPeriod(weatherTypeId, now, now.AddSeconds(WeatherManager.WeatherPeriodSeconds));
    }

    private void ClearWeatherSchedule()
    {
        using var uow = GetUow();
        uow.Context.WeatherPeriods.RemoveRange(uow.Context.WeatherPeriods);
        uow.Context.SaveChanges();
        uow.Commit();
    }

    private void InsertWeatherPeriod(int weatherTypeId, DateTime startDate, DateTime endDate)
    {
        using var uow = GetUow();
        uow.Context.WeatherPeriods.Add(new()
        {
            WeatherTypeId = weatherTypeId,
            StartDate = startDate,
            EndDate = endDate,
        });

        uow.Context.SaveChanges();
        uow.Commit();
    }
}
