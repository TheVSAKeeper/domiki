using Domiki.Web.Core;
using Domiki.Web.Data.Entities;
using Domiki.Web.Workers;

namespace Domiki.Web.Tests;

public sealed class ZealChargesTests
{
    /// <summary>
    /// Копка глины тратит заряд рвения и идёт вчетверо быстрее обычного.
    /// </summary>
    [Test]
    public void ClayDigUsesFourfoldZealSpeedupAndChargeTest()
    {
        var player = TestPlayer.Create();
        SetWorkersToOrdinary(player.Id);
        SetZealCharges(player.Id, DomikManager.ZealStartCharges);

        var manufacture = StartManufactureWithDuration(player.Id, StartingDomikIds.ClayMine, ReceiptIds.ClayDig);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(manufacture.DurationSeconds, Is.EqualTo(900));
            Assert.That(GetZealCharges(player.Id), Is.EqualTo(23));
        }
    }

    /// <summary>
    /// Долгий восьмичасовой рецепт копки глины не расходует заряды рвения и не ускоряется ими.
    /// </summary>
    [Test]
    public void EightHourClayDigDoesNotUseZealTest()
    {
        var player = TestPlayer.Create();
        SetWorkersToOrdinary(player.Id);
        SetZealCharges(player.Id, DomikManager.ZealStartCharges);

        var manufacture = StartManufactureWithDuration(player.Id, StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(manufacture.DurationSeconds, Is.EqualTo(28800));
            Assert.That(GetZealCharges(player.Id), Is.EqualTo(DomikManager.ZealStartCharges));
        }
    }

    /// <summary>
    /// Продажа ресурса на рынке не расходует заряды рвения и не ускоряется ими.
    /// </summary>
    [Test]
    public void MarketSaleDoesNotUseZealTest()
    {
        var player = TestPlayer.Create();
        GrantAllResources(player.Id, 1000);
        player.Buy(DomikIds.Market);
        player.WithResource(ResourceIds.Clay, 10);
        SetWorkersToOrdinary(player.Id);
        SetZealCharges(player.Id, DomikManager.ZealStartCharges);

        var marketId = player.DomikId(DomikIds.Market);
        var manufacture = StartManufactureWithDuration(player.Id, marketId, ReceiptIds.SellClay);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(manufacture.DurationSeconds, Is.EqualTo(60));
            Assert.That(GetZealCharges(player.Id), Is.EqualTo(DomikManager.ZealStartCharges));
        }
    }

    /// <summary>
    /// Новый игрок получает 24 заряда рвения.
    /// </summary>
    [Test]
    public void NewPlayerStartsWithTwentyFourZealChargesTest()
    {
        var playerId = CreatePlayerWithDefaultFtue();
        Assert.That(GetZealCharges(playerId), Is.EqualTo(DomikManager.ZealStartCharges));
    }

    /// <summary>
    /// Ускорение копки от рвения ступенчато слабеет по мере расхода зарядов и никогда не уводит их счётчик в отрицательные
    /// значения.
    /// </summary>
    /// <param name="initialCharges">Заряды рвения перед запуском.</param>
    /// <param name="expectedDuration">Ожидаемая длительность производства в секундах.</param>
    /// <param name="expectedCharges">Ожидаемый остаток зарядов рвения после запуска.</param>
    [TestCase(17, 900, 16)]
    [TestCase(16, 1800, 15)]
    [TestCase(1, 1800, 0)]
    [TestCase(0, 3600, 0)]
    public void ClayDigAppliesThresholdSpeedupWithoutNegativeChargesTest(int initialCharges, int expectedDuration, int expectedCharges)
    {
        var player = TestPlayer.Create();
        SetWorkersToOrdinary(player.Id);
        SetZealCharges(player.Id, initialCharges);

        var manufacture = StartManufactureWithDuration(player.Id, StartingDomikIds.ClayMine, ReceiptIds.ClayDig);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(manufacture.DurationSeconds, Is.EqualTo(expectedDuration));
            Assert.That(GetZealCharges(player.Id), Is.EqualTo(expectedCharges));
        }
    }

    /// <summary>
    /// Заводит игрока напрямую через DomikManager, минуя TestPlayer.Create – FTUE-квесты не подавляются, а значит заряды
    /// рвения остаются равны стартовому значению из DomikManager, а не обнулёнными.
    /// </summary>
    private static int CreatePlayerWithDefaultFtue()
    {
        using var scope = App.Scope();
        var playerId = scope.Get<DomikManager>().GetPlayerId($"testUser_{App.RunId}_{Guid.NewGuid()}");
        scope.Commit();
        return playerId;
    }

    private static Manufacture StartManufactureWithDuration(int playerId, int domikId, int receiptId)
    {
        using var scope = App.Scope();
        scope.Get<DomikManager>().StartManufacture(playerId, domikId, receiptId);
        var manufacture = scope.Context.Manufactures.Single(x => x.DomikPlayerId == playerId && x.DomikId == domikId);
        scope.Commit();
        return manufacture;
    }

    private static void SetWorkersToOrdinary(int playerId)
    {
        using var scope = App.Scope();
        scope.Get<WorkerManager>().EnsureWorkers(playerId);
        scope.Context.WorkerSkills.RemoveRange(scope.Context.WorkerSkills.Where(x => x.Worker.PlayerId == playerId));
        foreach (var worker in scope.Context.Workers.Where(x => x.PlayerId == playerId))
        {
            worker.TraitId = 1;
        }

        scope.Commit();
    }

    private static int GetZealCharges(int playerId)
    {
        using var scope = App.Scope();
        return scope.Context.Players.Single(x => x.Id == playerId).ZealCharges;
    }

    private static void SetZealCharges(int playerId, int value)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == playerId).ZealCharges = value;
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
