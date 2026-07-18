using Domiki.Web.Activities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Village;
using Domiki.Web.Village.Models;
using System.Text.Json;

namespace Domiki.Web.Tests;

[NonParallelizable]
public sealed class SeasonTests
{
    private const int ShortScoutId = 1;

    [SetUp]
    public void SetUp()
    {
        ResetToloka();
    }

    [TearDown]
    public void TearDown()
    {
        ResetToloka();
    }

    /// <summary>
    /// Выполнение заказа увеличивает сезонный счётчик заказов игрока на величину денежной награды заказа.
    /// </summary>
    [Test]
    public void CompleteOrderGrowsSeasonOrdersCounterTest()
    {
        var player = TestPlayer.Create();
        var order = player.Orders().First();
        var need = order.Resources.Single();
        player.WithResource(need.Type.Id, need.Value);
        var season = CurrentSeason(DateTimeHelper.GetNowDate());

        player.CompleteOrder(order.Id);

        Assert.That(player.SeasonCounter(season.Number, SeasonMetric.Orders), Is.EqualTo(order.RewardCoins));
    }

    /// <summary>
    /// Параллельные взносы в толоку от одного игрока суммируются в сезонном счётчике точно, без потери обновлений.
    /// </summary>
    [Test]
    public async Task ConcurrentContributesKeepExactSeasonCounterSumTest()
    {
        var player = TestPlayer.Create()
            .WithDecor(DecorIds.Fountain, 4)
            .WithResource(ResourceIds.Coin, 800)
            .Buy(DomikIds.Gathering)
            .WithResource(ResourceIds.Stone, 1000);

        var season = CurrentSeason(DateTimeHelper.GetNowDate());

        await Task.WhenAll(Task.Run(() => player.Contribute(70)), Task.Run(() => player.Contribute(80)));

        Assert.That(player.SeasonCounter(season.Number, SeasonMetric.Toloka), Is.EqualTo(150));
    }

    /// <summary>
    /// Взнос в толоку увеличивает сезонный счётчик толоки игрока на внесённую величину.
    /// </summary>
    [Test]
    public void ContributeGrowsSeasonTolokaCounterTest()
    {
        var player = TestPlayer.Create()
            .WithDecor(DecorIds.Fountain, 4)
            .WithResource(ResourceIds.Coin, 800)
            .Buy(DomikIds.Gathering)
            .WithResource(ResourceIds.Stone, 100);

        var season = CurrentSeason(DateTimeHelper.GetNowDate());

        player.Contribute(40);

        Assert.That(player.SeasonCounter(season.Number, SeasonMetric.Toloka), Is.EqualTo(40));
    }

    /// <summary>
    /// Завершение экспедиции увеличивает сезонный счётчик экспедиций игрока на единицу.
    /// </summary>
    [Test]
    public void FinishExpeditionGrowsSeasonExpeditionsCounterTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2)
            .WithResource(ResourceIds.Gold, 1)
            .WithResource(ResourceIds.Board, 2)
            .WithDomik(DomikIds.ScoutHut);

        using (App.PendingEvents())
        {
            player.StartExpedition(ShortScoutId);
        }

        var expedition = player.Expeditions().Active.Single();
        var season = CurrentSeason(DateTimeHelper.GetNowDate());
        SetExpeditionFinish(expedition.Id, DateTimeHelper.GetNowDate().AddSeconds(-1));

        player.FinishExpedition(expedition.Id, DateTimeHelper.GetNowDate());

        Assert.That(player.SeasonCounter(season.Number, SeasonMetric.Expeditions), Is.EqualTo(1));
    }

    /// <summary>
    /// Список мира отдаёт метаданные текущего сезона и посезонные метрики (заказы, толока, экспедиции, уют) по каждой деревне;
    /// у NPC все сезонные метрики всегда нулевые.
    /// </summary>
    [Test]
    public void GetWorldReturnsSeasonMetaAndPerVillageMetricsTest()
    {
        var first = TestPlayer.Create()
            .SetVillageIdentity("Сезон Первая-" + Guid.NewGuid().ToString("N")[..6], 0, 1);

        var second = TestPlayer.Create()
            .SetVillageIdentity("Сезон Вторая-" + Guid.NewGuid().ToString("N")[..6], 1, 2);

        var season = CurrentSeason(DateTimeHelper.GetNowDate());
        first.SetSeasonCounter(season.Number, SeasonMetric.Orders, 120);
        first.SetSeasonCounter(season.Number, SeasonMetric.Toloka, 30);
        second.SetSeasonCounter(season.Number, SeasonMetric.Expeditions, 4);
        first.WithDecor(DecorIds.Fountain, 2);

        var world = first.World();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(world.Season.Number, Is.EqualTo(season.Number));
            Assert.That(world.Season.StartDate, Is.EqualTo(season.StartDate));
            Assert.That(world.Season.EndDate, Is.EqualTo(season.EndDate));
        }

        var firstVillage = world.Villages.Single(x => x.PlayerId == first.Id);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstVillage.SeasonOrders, Is.EqualTo(120));
            Assert.That(firstVillage.SeasonToloka, Is.EqualTo(30));
            Assert.That(firstVillage.SeasonExpeditions, Is.Zero);
            Assert.That(firstVillage.Comfort, Is.GreaterThan(0));
        }

        var secondVillage = world.Villages.Single(x => x.PlayerId == second.Id);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondVillage.SeasonExpeditions, Is.EqualTo(4));
            Assert.That(secondVillage.Comfort, Is.Zero);
        }

        var npcs = world.Villages.Where(x => x.IsNpc).ToArray();
        Assert.That(npcs.All(x => x.SeasonOrders == 0 && x.SeasonToloka == 0 && x.SeasonExpeditions == 0 && x.Comfort == 0), Is.True);
    }

    /// <summary>
    /// Сезонные счётчики хранятся отдельно по номеру сезона: значения из прошлого сезона не переносятся и не смешиваются со
    /// следующим.
    /// </summary>
    [Test]
    public void SeasonKeyResetsBetweenSeasonsTest()
    {
        var player = TestPlayer.Create();
        var firstSeasonDate = SeasonManager.SeasonEpoch.AddDays(1);
        var secondSeasonDate = SeasonManager.SeasonEpoch.AddSeconds(SeasonManager.SeasonDurationSeconds + 1);

        player.IncrementCounter(SeasonMetric.Toloka, 40, firstSeasonDate);
        player.IncrementCounter(SeasonMetric.Toloka, 15, secondSeasonDate);

        var firstSeasonId = CurrentSeason(firstSeasonDate).Number;
        var secondSeasonId = CurrentSeason(secondSeasonDate).Number;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondSeasonId, Is.EqualTo(firstSeasonId + 1));
            Assert.That(player.SeasonCounter(firstSeasonId, SeasonMetric.Toloka), Is.EqualTo(40));
            Assert.That(player.SeasonCounter(secondSeasonId, SeasonMetric.Toloka), Is.EqualTo(15));
        }

        var firstSeasonCounters = GetCounters(firstSeasonId);
        Assert.That(firstSeasonCounters[(player.Id, SeasonMetric.Toloka)], Is.EqualTo(40));
    }

    /// <summary>
    /// DTO мира с сезонными полями не утекает приватные поля игрока (Name, AspNetUserId) в сериализованный JSON.
    /// </summary>
    [Test]
    public void WorldDtoWithSeasonFieldsDoesNotContainPrivateFieldsTest()
    {
        var player = TestPlayer.Create()
            .SetVillageIdentity("Сезон Приват-" + Guid.NewGuid().ToString("N")[..6], 5, 6);

        player.SetSeasonCounter(CurrentSeason(DateTimeHelper.GetNowDate()).Number, SeasonMetric.Orders, 10);

        var worldDto = player.WorldDto();

        var json = JsonSerializer.Serialize(worldDto);
        Assert.That(json, Does.Not.Contain("\"Name\""));
        Assert.That(json, Does.Not.Contain("AspNetUserId"));
    }

    /// <summary>
    /// Номер сезона увеличивается только по истечении полной длительности сезона от эпохи, а границы сезона точно совпадают с
    /// этой длительностью.
    /// </summary>
    /// <param name="offsetSeconds">Смещение от эпохи сезонов в секундах.</param>
    /// <param name="expectedNumber">Ожидаемый номер сезона.</param>
    [TestCase(0, 0)]
    [TestCase(1, 0)]
    [TestCase(SeasonManager.SeasonDurationSeconds - 1, 0)]
    [TestCase(SeasonManager.SeasonDurationSeconds, 1)]
    public void GetCurrentSeasonBoundariesTest(int offsetSeconds, int expectedNumber)
    {
        var date = SeasonManager.SeasonEpoch.AddSeconds(offsetSeconds);

        var season = CurrentSeason(date);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(season.Number, Is.EqualTo(expectedNumber));
            Assert.That(season.StartDate, Is.EqualTo(SeasonManager.SeasonEpoch.AddSeconds(expectedNumber * SeasonManager.SeasonDurationSeconds)));
            Assert.That((season.EndDate - season.StartDate).TotalSeconds, Is.EqualTo(SeasonManager.SeasonDurationSeconds));
        }
    }

    private static Season CurrentSeason(DateTime date)
    {
        return App.Act<SeasonManager, Season>(m => m.GetCurrentSeason(date));
    }

    private static Dictionary<(int PlayerId, SeasonMetric Metric), int> GetCounters(int seasonId)
    {
        return App.Act<SeasonManager, Dictionary<(int PlayerId, SeasonMetric Metric), int>>(m => m.GetCounters(seasonId));
    }

    private static void SetExpeditionFinish(int expeditionId, DateTime finishDate)
    {
        using var scope = App.Scope();
        var expedition = scope.Context.Expeditions.Single(x => x.Id == expeditionId);
        expedition.FinishDate = finishDate;
        scope.Commit();
    }

    private static void ResetToloka()
    {
        int newTolokaId;
        using (var scope = App.Scope())
        {
            scope.Context.TolokaContributions.RemoveRange(scope.Context.TolokaContributions);
            scope.Context.TolokaPositions.RemoveRange(scope.Context.TolokaPositions);
            scope.Context.Tolokas.RemoveRange(scope.Context.Tolokas);
            var toloka = scope.Context.Tolokas.Add(new()
            {
                TolokaTypeId = TolokaTypeIds.Bridge,
                StartDate = DateTimeHelper.GetNowDate(),
                CompletedDate = null,
            });

            scope.Commit();
            newTolokaId = toloka.Entity.Id;
        }

        using (var scope = App.Scope())
        {
            scope.Context.TolokaPositions.Add(new()
            {
                TolokaId = newTolokaId,
                ResourceTypeId = ResourceIds.Stone,
                Goal = 2000,
                Collected = 0,
            });

            scope.Commit();
        }
    }
}

file static class SeasonTestsActs
{
    public static TestPlayer Contribute(this TestPlayer p, int amount) => p.Contribute(ResourceIds.Stone, amount);

    public static TestPlayer Contribute(this TestPlayer p, int resourceTypeId, int amount)
    {
        App.Act<TolokaManager>(m => m.Contribute(p.Id, resourceTypeId, amount, DateTimeHelper.GetNowDate()));
        return p;
    }

    public static TestPlayer IncrementCounter(this TestPlayer p, SeasonMetric metric, int value, DateTime date)
    {
        App.Act<SeasonManager>(m => m.IncrementCounter(p.Id, metric, value, date));
        return p;
    }

    public static TestPlayer SetSeasonCounter(this TestPlayer p, int seasonId, SeasonMetric metric, int value)
    {
        using var scope = App.Scope();
        var counter = scope.Context.SeasonCounters.SingleOrDefault(x => x.SeasonId == seasonId && x.PlayerId == p.Id && x.Metric == (int)metric);
        if (counter == null)
        {
            counter = new()
            {
                SeasonId = seasonId,
                PlayerId = p.Id,
                Metric = (int)metric,
            };

            scope.Context.SeasonCounters.Add(counter);
        }

        counter.Value = value;
        scope.Commit();
        return p;
    }

    public static int SeasonCounter(this TestPlayer p, int seasonId, SeasonMetric metric)
    {
        using var scope = App.Scope();
        return scope.Context.SeasonCounters
            .Where(x => x.SeasonId == seasonId && x.PlayerId == p.Id && x.Metric == (int)metric)
            .Select(x => x.Value)
            .SingleOrDefault();
    }
}
