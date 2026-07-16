using Domiki.Web.Activities;
using Domiki.Web.Activities.Models;
using Domiki.Web.Core;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using System.Text.Json;

namespace Domiki.Web.Tests;

public sealed class GoalsTests
{
    /// <summary>
    /// Прокачка казармы закрывает пятую цель, но только после того, как цели с первой по четвёртую уже выполнены.
    /// </summary>
    [Test]
    public void BarracksUpgradeCompletesFifthGoalAfterEarlierGoalsTest()
    {
        const int fifthGoalReward = 50;

        var player = TestPlayer.Create(muteFtue: false);
        GrantAllResources(player.Id, 1000);
        player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig);
        player.Buy(DomikIds.Market);
        player.Goals();
        player.WithResource(ResourceIds.Clay, 10);
        player.StartManufacture(player.DomikId(DomikIds.Market), ReceiptIds.SellClay);
        player.Buy(DomikIds.LumberMill);
        player.Goals();

        player.Upgrade(StartingDomikIds.Barrack);
        var coinsBeforeGoalsRead = player.Resource(ResourceIds.Coin);
        var state = player.Goals();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.CompletedGoalIds(), Is.EqualTo([1, 2, 3, 4, 5]));
            Assert.That(state.ActiveGoal().Ordinal, Is.EqualTo(6));
            Assert.That(player.Resource(ResourceIds.Coin) - coinsBeforeGoalsRead, Is.EqualTo(fifthGoalReward));
        }
    }

    /// <summary>
    /// Выполнение заказа закрывает шестую цель.
    /// </summary>
    [Test]
    public void CompletedOrderCompletesSixthGoalTest()
    {
        var player = TestPlayer.Create(muteFtue: false);
        SeedCompletedGoals(player.Id, 1, 2, 3, 4, 5);
        var order = player.Orders().First(x => x.Resources.Single().Type.Id == ResourceIds.Clay);
        var required = order.Resources.Single();
        player.WithResource(required.Type.Id, required.Value);

        player.CompleteOrder(order.Id);
        var state = player.Goals();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.CompletedGoalIds(), Does.Contain(6));
            Assert.That(state.ActiveGoal().Ordinal, Is.EqualTo(7));
        }
    }

    /// <summary>
    /// Седьмую цель закрывает только восьмичасовая смена добычи глины, обычная часовая смена её не выполняет.
    /// </summary>
    [Test]
    public void EightHourShiftCompletesSeventhGoalButOneHourShiftDoesNotTest()
    {
        var player = TestPlayer.Create(muteFtue: false);
        SeedCompletedGoals(player.Id, 1, 2, 3, 4, 5, 6);

        player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig);
        Assert.That(player.Goals().ActiveGoal().Ordinal, Is.EqualTo(7));

        player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Goals().ActiveGoal().Ordinal, Is.EqualTo(8));
            Assert.That(player.CompletedGoalIds(), Does.Contain(7));
        }
    }

    /// <summary>
    /// Из 9 целей первая закрывается добычей глины, платит награду монетами и пишет запись в журнал с id цели и наградой.
    /// </summary>
    [Test]
    public void FirstGoalCompletesOnClayDigAndWritesJournalEntryTest()
    {
        const int firstGoalRewardCoins = 10;

        var player = TestPlayer.Create(muteFtue: false);

        var initial = player.Goals();
        player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig);
        var state = player.Goals();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(initial.ActiveGoal().Ordinal, Is.EqualTo(1));
            Assert.That(initial.CompletedCount, Is.Zero);
            Assert.That(initial.TotalCount, Is.EqualTo(9));
            Assert.That(player.CompletedGoalIds(), Is.EqualTo([1]));
            Assert.That(state.ActiveGoal().Ordinal, Is.EqualTo(2));
            Assert.That(state.CompletedCount, Is.EqualTo(1));
            Assert.That(player.Resource(ResourceIds.Coin), Is.EqualTo(DomikManager.StartingCoins + firstGoalRewardCoins));
        }

        var entry = player.GoalEvent(1);
        using var data = JsonDocument.Parse(entry.Data);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(data.RootElement.GetProperty("goalId").GetInt32(), Is.EqualTo(1));
            Assert.That(data.RootElement.GetProperty("rewardCoins").GetInt32(), Is.EqualTo(firstGoalRewardCoins));
        }
    }

    /// <summary>
    /// Покупка рынка выполняет условие второй цели, но начисление награды и её видимость в списке завершённых происходит
    /// только при чтении состояния целей.
    /// </summary>
    [Test]
    public void MarketPurchaseCompletesSecondGoalOnlyWhenGoalsAreReadTest()
    {
        const int secondGoalReward = 20;

        var player = TestPlayer.Create(muteFtue: false);
        player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig);

        player.Buy(DomikIds.Market);
        Assert.That(player.CompletedGoalIds(), Is.EqualTo([1]));

        var coinsBeforeGoalsRead = player.Resource(ResourceIds.Coin);
        var state = player.Goals();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.ActiveGoal().Ordinal, Is.EqualTo(3));
            Assert.That(state.CompletedCount, Is.EqualTo(2));
            Assert.That(player.Resource(ResourceIds.Coin) - coinsBeforeGoalsRead, Is.EqualTo(secondGoalReward));
        }
    }

    /// <summary>
    /// Продажа глины, случившаяся до активации третьей цели, в зачёт не идёт: цель закрывает только следующая продажа после
    /// активации.
    /// </summary>
    [Test]
    public void SaleBeforeItsActivationNeedsSecondSaleToCompleteThirdGoalTest()
    {
        const int thirdGoalReward = 15;

        var player = TestPlayer.Create(muteFtue: false);
        GrantAllResources(player.Id, 1000);
        player.Buy(DomikIds.Market);
        player.WithResource(ResourceIds.Clay, 10);
        var marketId = player.DomikId(DomikIds.Market);

        player.StartManufacture(marketId, ReceiptIds.SellClay);
        var afterFirstSale = player.Goals();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.CompletedGoalIds(), Is.EqualTo([1, 2]));
            Assert.That(afterFirstSale.ActiveGoal().Ordinal, Is.EqualTo(3));
        }

        var coinsBeforeSecondSale = player.Resource(ResourceIds.Coin);
        using (App.PendingEvents())
        {
            player.StartManufacture(marketId, ReceiptIds.SellClay);
        }

        var afterSecondSale = player.Goals();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterSecondSale.ActiveGoal().Ordinal, Is.EqualTo(4));
            Assert.That(player.CompletedGoalIds(), Is.EqualTo([1, 2, 3]));
            Assert.That(player.Resource(ResourceIds.Coin) - coinsBeforeSecondSale, Is.EqualTo(thirdGoalReward));
        }
    }

    /// <summary>
    /// Одна продажа глины может одновременно закрыть цель-состояние «есть рынок» и цель-действие «продать глину», если
    /// предыдущие цели уже выполнены.
    /// </summary>
    [Test]
    public void SaleCompletesMarketStateGoalAndSaleGoalTogetherTest()
    {
        var player = TestPlayer.Create(muteFtue: false);
        SeedCompletedGoals(player.Id, 1);
        GrantAllResources(player.Id, 1000);
        player.Buy(DomikIds.Market);
        player.WithResource(ResourceIds.Clay, 10);

        player.StartManufacture(player.DomikId(DomikIds.Market), ReceiptIds.SellClay);
        var state = player.Goals();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.CompletedGoalIds(), Is.EqualTo([1, 2, 3]));
            Assert.That(state.ActiveGoal().Ordinal, Is.EqualTo(4));
        }
    }

    /// <summary>
    /// Цели-состояния закрываются каскадом автоматически, но каскад останавливается на первой цели-действии, требующей
    /// отдельного игрового события.
    /// </summary>
    [Test]
    public void StateGoalsCascadeOnlyUntilActionGoalTest()
    {
        const int firstAndSecondGoalReward = 220;

        var player = TestPlayer.Create(muteFtue: false);
        SeedCompletedGoals(player.Id, 1);
        player.WithDomik(DomikIds.Market)
            .WithDomik(DomikIds.LumberMill);

        SetDomikLevel(player.Id, StartingDomikIds.Barrack, 2);

        var state = player.Goals();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.ActiveGoal().Ordinal, Is.EqualTo(3));
            Assert.That(state.CompletedCount, Is.EqualTo(2));
            Assert.That(player.Resource(ResourceIds.Coin), Is.EqualTo(firstAndSecondGoalReward));
        }
    }

    /// <summary>
    /// Покупка каменоломни закрывает восьмую цель, а награда материализуется при следующем чтении состояния целей.
    /// </summary>
    [Test]
    public void StoneMinePurchaseCompletesEighthGoalWhenGoalsAreReadTest()
    {
        const int eighthGoalReward = 40;

        var player = TestPlayer.Create(muteFtue: false);
        SeedCompletedGoals(player.Id, 1, 2, 3, 4, 5, 6, 7);
        GrantAllResources(player.Id, 1000);
        player.WithDomik(DomikIds.Market)
            .WithDomik(DomikIds.LumberMill);

        player.Buy(DomikIds.StoneMine);
        var coinsBeforeGoalsRead = player.Resource(ResourceIds.Coin);
        var state = player.Goals();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.ActiveGoal().Ordinal, Is.EqualTo(9));
            Assert.That(player.CompletedGoalIds(), Does.Contain(8));
            Assert.That(player.Resource(ResourceIds.Coin) - coinsBeforeGoalsRead, Is.EqualTo(eighthGoalReward));
        }
    }

    /// <summary>
    /// Достижение 10 уровня деревни закрывает девятую, последнюю цель – активных целей после этого не остаётся.
    /// </summary>
    [Test]
    public void VillageLevelTenCompletesNinthGoalTest()
    {
        const int neighborId = 1;
        const int allGoalsRewardCoins = 250;

        var player = TestPlayer.Create(muteFtue: false);
        SeedCompletedGoals(player.Id, 1, 2, 3, 4, 5, 6, 7, 8);

        player.WithReputation(neighborId, 20);

        Assert.That(player.GetVillageLevel().Level, Is.GreaterThanOrEqualTo(10));
        var state = player.Goals();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.ActiveGoal, Is.Null);
            Assert.That(state.CompletedCount, Is.EqualTo(9));
            Assert.That(player.Resource(ResourceIds.Coin), Is.EqualTo(allGoalsRewardCoins));
        }
    }

    private static void SeedCompletedGoals(int playerId, params int[] goalIds)
    {
        using var scope = App.Scope();
        scope.Context.PlayerGoals.AddRange(goalIds.Select(goalId => new PlayerGoal
        {
            PlayerId = playerId,
            GoalId = goalId,
            CompleteDate = DateTimeHelper.GetNowDate(),
        }));

        scope.Commit();
    }

    private static void SetDomikLevel(int playerId, int domikId, int level)
    {
        using var scope = App.Scope();
        scope.Context.Domiks.Single(x => x.PlayerId == playerId && x.Id == domikId).Level = level;
        scope.Commit();
    }

    private static void GrantAllResources(int playerId, int value)
    {
        using var scope = App.Scope();
        foreach (var typeId in scope.Context.ResourceTypes.Select(x => x.Id).ToArray())
        {
            var resource = scope.Context.Resources.SingleOrDefault(x => x.PlayerId == playerId && x.TypeId == typeId);
            if (resource == null)
            {
                resource = new()
                {
                    PlayerId = playerId,
                    TypeId = typeId,
                };

                scope.Context.Resources.Add(resource);
            }

            resource.Value += value;
        }

        scope.Commit();
    }
}

file static class GoalsTestsActs
{
    public static GoalsState Goals(this TestPlayer p)
    {
        return App.Act<GoalManager, GoalsState>(m => m.GetGoalsState(p.Id));
    }

    public static ActiveGoal ActiveGoal(this GoalsState state)
    {
        Assert.That(state.ActiveGoal, Is.Not.Null);
        return state.ActiveGoal!;
    }

    public static IReadOnlyList<int> CompletedGoalIds(this TestPlayer p)
    {
        return App.Read(context => context.PlayerGoals.Where(x => x.PlayerId == p.Id).OrderBy(x => x.GoalId).Select(x => x.GoalId).ToList());
    }

    public static PlayerEvent GoalEvent(this TestPlayer p, int goalId)
    {
        return App.Read(context => context.PlayerEvents.Single(x => x.PlayerId == p.Id && x.Type == PlayerEventType.GoalCompleted && x.Data.Contains($"\"goalId\":{goalId}")));
    }
}
