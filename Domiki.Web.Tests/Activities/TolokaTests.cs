using Domiki.Web.Activities;
using Domiki.Web.Activities.Models;
using Domiki.Web.Infrastructure;
using Domiki.Web.Village;
using Toloka = Domiki.Web.Data.Entities.Toloka;
using TolokaPositionEntity = Domiki.Web.Data.Entities.TolokaPosition;

namespace Domiki.Web.Tests;

[NonParallelizable]
public sealed class TolokaTests
{
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
        CompleteTolokaWithContribution(player.Id, DateTimeHelper.GetNowDate(), TolokaTypeIds.Bridge);

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

        Assert.That(ActiveStonePosition().Collected, Is.EqualTo(firstContribution + secondContribution));
    }

    /// <summary>
    /// Достижение цели завершает текущую толоку и сразу заводит новую активную взамен.
    /// </summary>
    [Test]
    public void ContributeCompletesTolokaAndSeedsNextActiveTest()
    {
        const int collectedBeforeContribution = 1950;
        const int contribution = 50;

        var player = TestPlayer.Create()
            .WithTolokaUnlocked()
            .WithResource(ResourceIds.Stone, 100);

        SetActiveTolokaCollected(collectedBeforeContribution);
        var activeId = GetActiveToloka().Id;

        player.Contribute(contribution);

        using var scope = App.Scope();
        var completed = scope.Context.Tolokas.Single(x => x.Id == activeId);
        var completedPosition = scope.Context.TolokaPositions.Single(x => x.TolokaId == activeId && x.ResourceTypeId == ResourceIds.Stone);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(completed.CompletedDate, Is.Not.Null);
            Assert.That(completedPosition.Collected, Is.EqualTo(collectedBeforeContribution + contribution));
            Assert.That(scope.Context.Tolokas.Count(x => x.CompletedDate == null), Is.EqualTo(1));
            Assert.That(scope.Context.TolokaContributions.Single(x => x.TolokaId == activeId && x.PlayerId == player.Id && x.ResourceTypeId == ResourceIds.Stone).Value, Is.EqualTo(contribution));
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
            Assert.That(ActiveStonePosition().Collected, Is.Zero);
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
            Assert.That(ActiveStonePosition().Collected, Is.Zero);
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
        var position = toloka.Active.Positions.Single(p => p.ResourceTypeId == ResourceIds.Stone);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(position.Collected, Is.EqualTo(contribution));
            Assert.That(position.MyContribution, Is.EqualTo(contribution));
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
            Assert.That(toloka.Active.Positions.Single(p => p.ResourceTypeId == ResourceIds.Stone).MyContribution, Is.Zero);
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

        CompleteTolokaWithContribution(player.Id, DateTimeHelper.GetNowDate(), TolokaTypeIds.Kiln);
        SetWeather(WeatherIds.Clear);

        var forgeId = player.DomikId(DomikIds.Forge);
        using (App.PendingEvents())
        {
            player.StartManufacture(forgeId, ReceiptIds.MakeTool);
        }

        Assert.That(player.ManufactureOutputPercent(forgeId), Is.EqualTo(140));
    }

    /// <summary>
    /// Цель каждой позиции корзины следующей толоки растёт пропорционально числу участников предыдущей.
    /// </summary>
    [Test]
    public void NextTolokaGoalScalesWithPreviousContributorsTest()
    {
        const int contribution = 40;
        const int distinctContributors = 3;

        var first = TestPlayer.Create().WithTolokaUnlocked().WithResource(ResourceIds.Stone, contribution);
        var second = TestPlayer.Create().WithTolokaUnlocked().WithResource(ResourceIds.Stone, contribution);
        var third = TestPlayer.Create().WithTolokaUnlocked().WithResource(ResourceIds.Stone, contribution);
        SetActiveTolokaGoal(100);

        first.Contribute(contribution);
        second.Contribute(contribution);
        third.Contribute(contribution);

        using var scope = App.Scope();
        var next = scope.Context.Tolokas.Single(x => x.CompletedDate == null);
        var nextPositions = scope.Context.TolokaPositions.Where(x => x.TolokaId == next.Id).ToArray();
        var basePositions = scope.Context.TolokaTypePositions.Where(x => x.TolokaTypeId == next.TolokaTypeId).ToArray();

        Assert.That(nextPositions, Has.Length.EqualTo(basePositions.Length));
        foreach (var position in nextPositions)
        {
            var basePosition = basePositions.Single(x => x.ResourceTypeId == position.ResourceTypeId);
            Assert.That(position.Goal, Is.EqualTo(basePosition.Goal * distinctContributors));
        }
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
            var completedPosition = scope.Context.TolokaPositions.Single(x => x.TolokaId == completed.Id && x.ResourceTypeId == ResourceIds.Stone);
            Assert.That(completedPosition.Collected, Is.EqualTo(collectedBeforeContribution + contribution + contribution));
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
            Assert.That(ActiveStonePosition().Collected, Is.Zero);
        }
    }

    /// <summary>
    /// Взнос сверх остатка позиции списывает только принятую часть, а не всё внесённое количество.
    /// </summary>
    [Test]
    public void ContributeClampsToRemainingAndKeepsOverflowTest()
    {
        const int positionGoal = 100;
        const int startStone = 200;
        const int contribution = 150;

        var player = TestPlayer.Create()
            .WithTolokaUnlocked()
            .WithResource(ResourceIds.Stone, startStone);

        SetActiveTolokaGoal(positionGoal);
        var activeId = GetActiveToloka().Id;

        player.Contribute(contribution);

        var completedPosition = App.Read(context => context.TolokaPositions.Single(x => x.TolokaId == activeId && x.ResourceTypeId == ResourceIds.Stone));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(completedPosition.Collected, Is.EqualTo(positionGoal));
            Assert.That(player.Resource(ResourceIds.Stone), Is.EqualTo(startStone - positionGoal));
        }
    }

    /// <summary>
    /// Взнос ресурсом, для которого у активной толоки нет позиции, отклоняется исключением и не списывает ресурс.
    /// </summary>
    [Test]
    public void ContributeWrongResourceThrowsTest()
    {
        const int startWood = 50;

        var player = TestPlayer.Create()
            .WithTolokaUnlocked()
            .WithResource(ResourceIds.Wood, startWood);

        var ex = Throws.Business(() => player.Contribute(ResourceIds.Wood, 20));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("Этот ресурс толоке не нужен"));
            Assert.That(ActiveStonePosition().Collected, Is.Zero);
            Assert.That(player.Resource(ResourceIds.Wood), Is.EqualTo(startWood));
        }
    }

    /// <summary>
    /// Взнос в позицию, уже набравшую свою цель, отклоняется исключением.
    /// </summary>
    [Test]
    public void ContributeToFilledPositionThrowsTest()
    {
        const int positionGoal = 50;

        var player = TestPlayer.Create()
            .WithTolokaUnlocked()
            .WithResource(ResourceIds.Stone, 10);

        SetActiveTolokaGoal(positionGoal);
        SetActiveTolokaCollected(positionGoal);

        var ex = Throws.Business(() => player.Contribute(10));

        Assert.That(ex.Message, Is.EqualTo("Позиция уже собрана"));
    }

    /// <summary>
    /// Многопозиционная толока (караван) завершается только тогда, когда набраны все её позиции, а не первая из них.
    /// </summary>
    [Test]
    public void MultiPositionTolokaCompletesOnlyWhenAllPositionsFilledTest()
    {
        const int brickGoal = 2;
        const int boardGoal = 2;
        const int toolGoal = 2;

        var player = TestPlayer.Create()
            .WithTolokaUnlocked()
            .WithResource(ResourceIds.Brick, brickGoal)
            .WithResource(ResourceIds.Board, boardGoal)
            .WithResource(ResourceIds.Tool, toolGoal);

        var tolokaId = SetActiveTolokaToCaravan(brickGoal, boardGoal, toolGoal);

        player.Contribute(ResourceIds.Brick, brickGoal);
        player.Contribute(ResourceIds.Board, boardGoal);

        Assert.That(GetTolokaCompletedDate(tolokaId), Is.Null);

        player.Contribute(ResourceIds.Tool, toolGoal);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(GetTolokaCompletedDate(tolokaId), Is.Not.Null);
            Assert.That(App.Read(context => context.Tolokas.Count(x => x.CompletedDate == null)), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Каждая позиция корзины хранит вклад игрока отдельно: взнос в одну позицию не отражается на другой.
    /// </summary>
    [Test]
    public void PerResourcePositionTracksOwnContributionTest()
    {
        const int positionGoal = 1000;
        const int brickContribution = 5;
        const int boardContribution = 3;

        var player = TestPlayer.Create()
            .WithTolokaUnlocked()
            .WithResource(ResourceIds.Brick, brickContribution)
            .WithResource(ResourceIds.Board, boardContribution);

        SetActiveTolokaToCaravan(positionGoal, positionGoal, positionGoal);

        player.Contribute(ResourceIds.Brick, brickContribution);
        player.Contribute(ResourceIds.Board, boardContribution);

        var toloka = player.Toloka();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(toloka.Active.Positions.Single(p => p.ResourceTypeId == ResourceIds.Brick).MyContribution, Is.EqualTo(brickContribution));
            Assert.That(toloka.Active.Positions.Single(p => p.ResourceTypeId == ResourceIds.Board).MyContribution, Is.EqualTo(boardContribution));
            Assert.That(toloka.Active.Positions.Single(p => p.ResourceTypeId == ResourceIds.Tool).MyContribution, Is.Zero);
        }
    }

    /// <summary>
    /// Бафф завершённой толоки типа «караван» повышает выпуск только у Магазина, у Кузницы выпуск остаётся базовым.
    /// </summary>
    [Test]
    public void CaravanBuffBoostsMarketSellOutputTest()
    {
        const int expectedMarketPercent = 140;
        const int expectedForgePercent = 100;

        var player = TestPlayer.Create();
        CompleteTolokaWithContribution(player.Id, DateTimeHelper.GetNowDate(), TolokaTypeIds.Caravan);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.TolokaOutputPercent(DomikIds.Market), Is.EqualTo(expectedMarketPercent));
            Assert.That(player.TolokaOutputPercent(DomikIds.Forge), Is.EqualTo(expectedForgePercent));
        }
    }

    /// <summary>
    /// Голос за следующую толоку без постройки Сходня отклоняется исключением.
    /// </summary>
    [Test]
    public void VoteRequiresGatheringBuildingTest()
    {
        var player = TestPlayer.Create();

        var ex = Throws.Business(() => player.Vote(TolokaTypeIds.Kiln));

        Assert.That(ex.Message, Is.EqualTo("Нужна Сходня"));
    }

    /// <summary>
    /// Голос за несуществующий тип толоки отклоняется исключением.
    /// </summary>
    [Test]
    public void VoteForUnknownCandidateThrowsTest()
    {
        const int unknownTolokaTypeId = 999;

        var player = TestPlayer.Create()
            .WithTolokaUnlocked();

        var ex = Throws.Business(() => player.Vote(unknownTolokaTypeId));

        Assert.That(ex.Message, Is.EqualTo("Нет такого проекта толоки"));
    }

    /// <summary>
    /// Повторный голос игрока заменяет прежний выбор: остаётся одна строка голоса, а счётчики отражают только последний.
    /// </summary>
    [Test]
    public void RevoteReplacesPreviousChoiceTest()
    {
        var player = TestPlayer.Create()
            .WithTolokaUnlocked();

        player.Vote(TolokaTypeIds.Kiln);
        player.Vote(TolokaTypeIds.Granary);

        var toloka = player.Toloka();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(toloka.MyVoteTolokaTypeId, Is.EqualTo(TolokaTypeIds.Granary));
            Assert.That(CandidateVotes(toloka, TolokaTypeIds.Granary), Is.EqualTo(1));
            Assert.That(CandidateVotes(toloka, TolokaTypeIds.Kiln), Is.Zero);
            Assert.That(App.Read(context => context.TolokaVotes.Count()), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// При завершении толоки следующей становится тип с наибольшим числом голосов.
    /// </summary>
    [Test]
    public void MajorityVoteDecidesNextTolokaTypeTest()
    {
        const int collectedBeforeContribution = 1950;
        const int contribution = 50;

        var first = TestPlayer.Create().WithTolokaUnlocked().WithResource(ResourceIds.Stone, 100);
        var second = TestPlayer.Create().WithTolokaUnlocked();
        var third = TestPlayer.Create().WithTolokaUnlocked();

        first.Vote(TolokaTypeIds.Kiln);
        second.Vote(TolokaTypeIds.Kiln);
        third.Vote(TolokaTypeIds.Granary);
        SetActiveTolokaCollected(collectedBeforeContribution);

        first.Contribute(contribution);

        Assert.That(ActiveTolokaTypeId(), Is.EqualTo(TolokaTypeIds.Kiln));
    }

    /// <summary>
    /// Состояние толоки перечисляет все справочные типы как кандидатов и показывает суммарное число голосов за каждого.
    /// </summary>
    [Test]
    public void CandidatesListEveryTypeWithVoteCountsTest()
    {
        var first = TestPlayer.Create().WithTolokaUnlocked();
        var second = TestPlayer.Create().WithTolokaUnlocked();

        first.Vote(TolokaTypeIds.Kiln);
        second.Vote(TolokaTypeIds.Kiln);

        var candidates = first.Toloka().Candidates;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(candidates.Select(c => c.TolokaTypeId),
                Is.EquivalentTo(new[] { TolokaTypeIds.Bridge, TolokaTypeIds.Granary, TolokaTypeIds.Kiln, TolokaTypeIds.Caravan }));
            Assert.That(candidates.Single(c => c.TolokaTypeId == TolokaTypeIds.Kiln).Votes, Is.EqualTo(2));
            Assert.That(candidates.Single(c => c.TolokaTypeId == TolokaTypeIds.Bridge).Votes, Is.Zero);
        }
    }

    /// <summary>
    /// Без единого голоса следующая толока выбирается ротацией из справочных типов, а не остаётся незасеянной.
    /// </summary>
    [Test]
    public void NoVotesFallBackToRotationOnCompletionTest()
    {
        const int collectedBeforeContribution = 1950;
        const int contribution = 50;

        var player = TestPlayer.Create()
            .WithTolokaUnlocked()
            .WithResource(ResourceIds.Stone, 100);

        SetActiveTolokaCollected(collectedBeforeContribution);

        player.Contribute(contribution);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(new[] { TolokaTypeIds.Bridge, TolokaTypeIds.Granary, TolokaTypeIds.Kiln, TolokaTypeIds.Caravan },
                Does.Contain(ActiveTolokaTypeId()));
            Assert.That(App.Read(context => context.Tolokas.Count(x => x.CompletedDate == null)), Is.EqualTo(1));
        }
    }

    private static Toloka GetActiveToloka()
    {
        return App.Read(context => context.Tolokas.Single(x => x.CompletedDate == null));
    }

    private static int ActiveTolokaTypeId()
    {
        return App.Read(context => context.Tolokas.Single(x => x.CompletedDate == null).TolokaTypeId);
    }

    private static int CandidateVotes(TolokaState state, int tolokaTypeId)
    {
        return state.Candidates.Single(c => c.TolokaTypeId == tolokaTypeId).Votes;
    }

    private static TolokaPositionEntity ActiveStonePosition()
    {
        return App.Read(context =>
        {
            var activeId = context.Tolokas.Single(x => x.CompletedDate == null).Id;
            return context.TolokaPositions.Single(x => x.TolokaId == activeId && x.ResourceTypeId == ResourceIds.Stone);
        });
    }

    private static void SetActiveTolokaCollected(int collected)
    {
        using var scope = App.Scope();
        var activeId = scope.Context.Tolokas.Single(x => x.CompletedDate == null).Id;
        scope.Context.TolokaPositions.Single(x => x.TolokaId == activeId && x.ResourceTypeId == ResourceIds.Stone).Collected = collected;
        scope.Commit();
    }

    private static void SetActiveTolokaGoal(int goal)
    {
        using var scope = App.Scope();
        var activeId = scope.Context.Tolokas.Single(x => x.CompletedDate == null).Id;
        scope.Context.TolokaPositions.Single(x => x.TolokaId == activeId && x.ResourceTypeId == ResourceIds.Stone).Goal = goal;
        scope.Commit();
    }

    private static int SetActiveTolokaToCaravan(int brickGoal, int boardGoal, int toolGoal)
    {
        using var scope = App.Scope();
        var active = scope.Context.Tolokas.Single(x => x.CompletedDate == null);
        active.TolokaTypeId = TolokaTypeIds.Caravan;
        scope.Context.TolokaPositions.RemoveRange(scope.Context.TolokaPositions.Where(x => x.TolokaId == active.Id));
        scope.Context.TolokaPositions.Add(new() { TolokaId = active.Id, ResourceTypeId = ResourceIds.Brick, Goal = brickGoal, Collected = 0 });
        scope.Context.TolokaPositions.Add(new() { TolokaId = active.Id, ResourceTypeId = ResourceIds.Board, Goal = boardGoal, Collected = 0 });
        scope.Context.TolokaPositions.Add(new() { TolokaId = active.Id, ResourceTypeId = ResourceIds.Tool, Goal = toolGoal, Collected = 0 });
        scope.Commit();
        return active.Id;
    }

    private static DateTime? GetTolokaCompletedDate(int tolokaId)
    {
        return App.Read(context => context.Tolokas.Single(x => x.Id == tolokaId).CompletedDate);
    }

    private static void SetGatheringLevel(int playerId, int level)
    {
        using var scope = App.Scope();
        scope.Context.Domiks.Single(x => x.PlayerId == playerId && x.TypeId == DomikIds.Gathering).Level = level;
        scope.Commit();
    }

    private static void CompleteTolokaWithContribution(int playerId, DateTime completedDate, int completedTolokaTypeId = TolokaTypeIds.Granary)
    {
        int nextTolokaId;
        using (var scope = App.Scope())
        {
            var active = scope.Context.Tolokas.Single(x => x.CompletedDate == null);
            active.TolokaTypeId = completedTolokaTypeId;
            active.CompletedDate = completedDate;

            var basePositions = scope.Context.TolokaTypePositions.Where(x => x.TolokaTypeId == completedTolokaTypeId).ToArray();
            scope.Context.TolokaPositions.RemoveRange(scope.Context.TolokaPositions.Where(x => x.TolokaId == active.Id));
            foreach (var basePosition in basePositions)
            {
                scope.Context.TolokaPositions.Add(new()
                {
                    TolokaId = active.Id,
                    ResourceTypeId = basePosition.ResourceTypeId,
                    Goal = basePosition.Goal,
                    Collected = basePosition.Goal,
                });
            }

            scope.Context.TolokaContributions.Add(new()
            {
                TolokaId = active.Id,
                PlayerId = playerId,
                ResourceTypeId = basePositions[0].ResourceTypeId,
                Value = 1,
            });

            var next = scope.Context.Tolokas.Add(new()
            {
                TolokaTypeId = TolokaTypeIds.Bridge,
                StartDate = completedDate,
                CompletedDate = null,
            });

            scope.Commit();
            nextTolokaId = next.Entity.Id;
        }

        using (var scope = App.Scope())
        {
            scope.Context.TolokaPositions.Add(new()
            {
                TolokaId = nextTolokaId,
                ResourceTypeId = ResourceIds.Stone,
                Goal = 2000,
                Collected = 0,
            });

            scope.Commit();
        }
    }

    private static void ResetToloka()
    {
        int newTolokaId;
        using (var scope = App.Scope())
        {
            scope.Context.TolokaVotes.RemoveRange(scope.Context.TolokaVotes);
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

    public static TestPlayer Contribute(this TestPlayer p, int amount) => p.Contribute(ResourceIds.Stone, amount);

    public static TestPlayer Contribute(this TestPlayer p, int resourceTypeId, int amount)
    {
        App.Act<TolokaManager>(m => m.Contribute(p.Id, resourceTypeId, amount, DateTimeHelper.GetNowDate()));
        return p;
    }

    public static TestPlayer Vote(this TestPlayer p, int tolokaTypeId)
    {
        App.Act<TolokaManager>(m => m.Vote(p.Id, tolokaTypeId, DateTimeHelper.GetNowDate()));
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

    public static int TolokaOutputPercent(this TestPlayer p, int domikTypeId)
    {
        return App.Act<TolokaManager, int>(m => m.GetTolokaOutputPercent(p.Id, domikTypeId, DateTimeHelper.GetNowDate()));
    }

    public static int ManufactureOutputPercent(this TestPlayer p, int domikId)
    {
        var manufactureId = p.Manufacture(domikId).Id;
        return App.Read(context => context.Manufactures.Single(x => x.Id == manufactureId).OutputPercent);
    }
}
