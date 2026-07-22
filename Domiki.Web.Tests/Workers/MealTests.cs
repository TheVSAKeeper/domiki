using Domiki.Web.Core;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Tests;

/// <summary>
/// Правила кормления трудяг Корчмой.
/// </summary>
public sealed class MealTests
{
    private const int OrdinaryTraitId = 1;
    private const int SonyaTraitId = 4;

    /// <summary>
    /// Трудяга с чертой «Соня» не устаёт и не ест еду Корчмы при завершении производства.
    /// </summary>
    [Test]
    public void SonyaDoesNotEatWhenFinishingManufactureTest()
    {
        const int startBread = 3;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Tavern)
            .WithResource(ResourceIds.Bread, startBread);

        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, SonyaTraitId);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig24h);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        player.FinishManufacture(manufacture.Id, manufacture.FinishDate.AddSeconds(1));

        worker = player.Workers().Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(worker.RestUntil, Is.Null);
            Assert.That(player.Resource(ResourceIds.Bread), Is.EqualTo(startBread));
        }
    }

    /// <summary>
    /// Корчма первого уровня кормит уставшего трудягу хлебом, а без Корчмы или еды отдых остаётся полным.
    /// </summary>
    /// <param name="tavernLevel">Уровень Корчмы, ноль означает её отсутствие.</param>
    /// <param name="bread">Сколько хлеба выдано игроку перед стартом.</param>
    /// <param name="expectedRestSeconds">Ожидаемая длительность отдыха трудяги.</param>
    /// <param name="expectedBread">Ожидаемый остаток хлеба после завершения производства.</param>
    [TestCase(1, 3, 3600, 2)]
    [TestCase(3, 3, 3600, 2)]
    [TestCase(0, 3, 7200, 3)]
    [TestCase(1, 0, 7200, 0)]
    public void FatiguedWorkerMealDependsOnTavernAndFoodTest(int tavernLevel, int bread, int expectedRestSeconds, int expectedBread)
    {
        var player = TestPlayer.Create();
        if (tavernLevel > 0)
        {
            player.WithDomik(DomikIds.Tavern, tavernLevel);
        }

        if (bread > 0)
        {
            player.WithResource(ResourceIds.Bread, bread);
        }

        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, OrdinaryTraitId);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        var finishDate = manufacture.FinishDate.AddSeconds(1);
        player.FinishManufacture(manufacture.Id, finishDate);

        worker = player.Workers().Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(worker.RestUntil, Is.Not.Null);
            Assert.That((worker.RestUntilValue() - finishDate).TotalSeconds, Is.EqualTo(expectedRestSeconds).Within(2));
            Assert.That(player.Resource(ResourceIds.Bread), Is.EqualTo(expectedBread));
        }
    }

    /// <summary>
    /// Корчма кормит уставшего трудягу сыром, сваренным в только что завершившейся смене, даже когда строки сыра ещё нет в запасах.
    /// </summary>
    [Test]
    public void FreshlyProducedFoodFeedsFatiguedWorkerTest()
    {
        const int cheeseShiftSeconds = 2 * 3600;
        const int cheeseOutput = 2;
        const int mealCount = 1;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Tavern)
            .WithDomik(DomikIds.Sheepfold)
            .WithResource(ResourceIds.Grain, 2);
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, OrdinaryTraitId);
        player.SetWorkerWorked(worker.Id, DomikManager.FatigueThresholdSeconds - cheeseShiftSeconds);

        using (App.PendingEvents())
        {
            player.StartManufacture(player.DomikId(DomikIds.Sheepfold), ReceiptIds.MakeCheese);
        }

        var manufacture = player.Manufacture(player.DomikId(DomikIds.Sheepfold));
        var finishDate = manufacture.FinishDate.AddSeconds(1);
        player.FinishManufacture(manufacture.Id, finishDate);

        worker = player.Workers().Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That((worker.RestUntilValue() - finishDate).TotalSeconds, Is.EqualTo(3600).Within(2));
            Assert.That(player.Resource(ResourceIds.Cheese), Is.EqualTo(cheeseOutput - mealCount));
        }
    }

    /// <summary>
    /// Корчма кормит уставшего трудягу сыром, когда хлеба нет.
    /// </summary>
    [Test]
    public void CheeseFeedsFatiguedWorkerTest()
    {
        const int startCheese = 2;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Tavern)
            .WithResource(ResourceIds.Cheese, startCheese);
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, OrdinaryTraitId);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        var finishDate = manufacture.FinishDate.AddSeconds(1);
        player.FinishManufacture(manufacture.Id, finishDate);

        worker = player.Workers().Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That((worker.RestUntilValue() - finishDate).TotalSeconds, Is.EqualTo(3600).Within(2));
            Assert.That(player.Resource(ResourceIds.Cheese), Is.EqualTo(startCheese - 1));
        }
    }

    /// <summary>
    /// Корчма списывает хлеб раньше сыра, потому что хлеб дешевле на рынке.
    /// </summary>
    [Test]
    public void BreadIsEatenBeforeCheeseTest()
    {
        const int startBread = 1;
        const int startCheese = 1;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Tavern)
            .WithResource(ResourceIds.Bread, startBread)
            .WithResource(ResourceIds.Cheese, startCheese);
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, OrdinaryTraitId);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        player.FinishManufacture(manufacture.Id, manufacture.FinishDate.AddSeconds(1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Resource(ResourceIds.Bread), Is.Zero);
            Assert.That(player.Resource(ResourceIds.Cheese), Is.EqualTo(startCheese));
        }
    }
}
