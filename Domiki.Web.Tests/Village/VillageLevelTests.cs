using Domiki.Web.Infrastructure;
using Domiki.Web.Village;

namespace Domiki.Web.Tests;

public sealed class VillageLevelTests
{
    private const int GoldMineNeighborId = 4;

    /// <summary>
    /// После порога умного автоподбора автоматический выбор трудяги на производство берёт того, чья черта характера лучше
    /// всего подходит под работу, а не с меньшим id.
    /// </summary>
    [Test]
    public void AutoWorkerSelectionIsByFitnessAfterSmartAutoThresholdTest()
    {
        var player = TestPlayer.Create()
            .WithDomiks(DomikIds.Barrack, 2);

        var workers = player.Workers();
        var weakWorker = workers[0];
        var strongWorker = workers[1];
        player.SetWorkerTrait(weakWorker.Id, 1);
        player.SetWorkerTrait(strongWorker.Id, 3);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig);
        }

        var busyWorker = player.Workers().Single(x => x.ManufactureId != null);
        Assert.That(busyWorker.Id, Is.EqualTo(strongWorker.Id));
    }

    /// <summary>
    /// До порога умного автоподбора автоматический выбор трудяги на производство берёт того, у кого меньше id, игнорируя
    /// пригодность черты характера.
    /// </summary>
    [Test]
    public void AutoWorkerSelectionIsByIdBeforeSmartAutoThresholdTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack);

        var workers = player.Workers();
        var weakWorker = workers[0];
        var strongWorker = workers[1];
        player.SetWorkerTrait(weakWorker.Id, 1);
        player.SetWorkerTrait(strongWorker.Id, 3);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig);
        }

        var busyWorker = player.Workers().Single(x => x.ManufactureId != null);
        Assert.That(busyWorker.Id, Is.EqualTo(weakWorker.Id));
    }

    /// <summary>
    /// Постройка, открываемая по уровню обжитости деревни, недоступна для покупки до достижения порога и становится доступной
    /// после.
    /// </summary>
    [Test]
    public void BuyDomikGateBlocksBeforeThresholdAndAllowsAfterTest()
    {
        var player = TestPlayer.Create();

        var ex = Assert.Throws<BusinessException>(() => player.Buy(DomikIds.StoneMine));
        Assert.That(ex.Message, Is.EqualTo("Откроется при обжитости 6"));

        player.WithDomiks(DomikIds.Barrack, 2);

        Assert.DoesNotThrow(() => player.Buy(DomikIds.StoneMine));
    }

    /// <summary>
    /// После достижения порога обжитости (8) доска заказов может использовать соседей, открытых вплоть до этого уровня, но не
    /// выше.
    /// </summary>
    [Test]
    public void OrderBoardCanUseUnlockedNeighborsAfterThresholdTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .Buy(DomikIds.StoneMine);

        var sawGatedNeighbor = false;
        for (var i = 0; i < 40 && !sawGatedNeighbor; i++)
        {
            player.ClearOrders();
            var orders = player.Orders();
            Assert.That(orders.All(x => x.Neighbor.UnlockLevel <= 8), Is.True);
            sawGatedNeighbor = orders.Any(x => x.Neighbor.UnlockLevel > 0);
        }

        Assert.That(sawGatedNeighbor, Is.True);
    }

    /// <summary>
    /// Пока порог уровня деревни не достигнут, доска заказов предлагает заказы только от соседей с нулевым уровнем открытия.
    /// </summary>
    [Test]
    public void OrderBoardUsesOnlyUnlockedNeighborsBeforeThresholdTest()
    {
        var player = TestPlayer.Create();

        var orders = player.Orders();

        Assert.That(orders, Is.Not.Empty);
        Assert.That(orders.All(x => x.Neighbor.UnlockLevel == 0), Is.True);
    }

    /// <summary>
    /// Уровень деревни растёт от числа построек, поселившихся трудяг и репутации у соседей – каждый фактор вносит свой вклад с
    /// собственным весом.
    /// </summary>
    [Test]
    public void VillageLevelGrowsFromBuildingsResidentsAndReputationTest()
    {
        var player = TestPlayer.Create();

        var initial = player.GetVillageLevel();
        Assert.That(initial.Level, Is.EqualTo(4));

        player.WithDomik(DomikIds.Barrack);
        var withBarracks = player.GetVillageLevel();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(withBarracks.Buildings, Is.EqualTo(3));
            Assert.That(withBarracks.Residents, Is.EqualTo(2));
            Assert.That(withBarracks.Reputation, Is.Zero);
            Assert.That(withBarracks.Comfort, Is.Zero);
            Assert.That(withBarracks.Level, Is.EqualTo(7));
        }

        player.WithReputation(GoldMineNeighborId, 10);
        var withReputation = player.GetVillageLevel();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(withReputation.Reputation, Is.EqualTo(1));
            Assert.That(withReputation.Level, Is.EqualTo(12));
        }
    }

    /// <summary>
    /// Вклад уюта в уровень деревни ограничен потолком в 50 очков, сколько бы уюта игрок ни накопил.
    /// </summary>
    /// <param name="comfort">Накопленный уют.</param>
    /// <param name="expectedContribution">Ожидаемый вклад уюта в уровень деревни.</param>
    [TestCase(49, 49)]
    [TestCase(50, 50)]
    [TestCase(80, 50)]
    public void ComfortContributionToLevelIsCappedTest(int comfort, int expectedContribution)
    {
        Assert.That(VillageLevelCalculator.ComputeLevel(0, 0, 0, comfort), Is.EqualTo(expectedContribution));
    }
}

file static class VillageLevelTestsActs
{
    public static TestPlayer ClearOrders(this TestPlayer p)
    {
        using var scope = App.Scope();
        var orders = scope.Context.Orders.Where(x => x.PlayerId == p.Id).ToArray();
        scope.Context.Orders.RemoveRange(orders);
        scope.Commit();
        return p;
    }
}
