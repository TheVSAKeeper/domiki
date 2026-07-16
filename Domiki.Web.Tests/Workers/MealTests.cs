using Domiki.Web.Core;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Tests;

public sealed class MealTests
{
    private const int OrdinaryTraitId = 1;
    private const int SonyaTraitId = 4;

    /// <summary>
    /// Трудяга с чертой «Соня» не устаёт и не ест хлеб при завершении производства, хлеб остаётся нетронутым.
    /// </summary>
    [Test]
    public void SonyaDoesNotEatWhenFinishingManufactureTest()
    {
        const int startBread = 3;

        var player = TestPlayer.Create()
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
    /// Кормление хлебом при включённой опции и наличии запаса сокращает отдых уставшего трудяги вдвое ценой одного хлеба; без
    /// опции или без хлеба отдых остаётся полным, а хлеб не тратится.
    /// </summary>
    /// <param name="feedWorkers">Включена ли опция кормления трудяг.</param>
    /// <param name="bread">Сколько хлеба выдано игроку перед стартом.</param>
    /// <param name="expectedRestSeconds">Ожидаемая длительность отдыха трудяги.</param>
    /// <param name="expectedBread">Ожидаемый остаток хлеба после завершения производства.</param>
    [TestCase(true, 3, 3600, 2)]
    [TestCase(false, 3, 7200, 3)]
    [TestCase(true, 0, 7200, 0)]
    public void FatiguedWorkerMealDependsOnFeedSettingAndBreadTest(bool feedWorkers, int bread, int expectedRestSeconds, int expectedBread)
    {
        var player = TestPlayer.Create();
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, OrdinaryTraitId);
        SetFeedWorkers(player.Id, feedWorkers);
        if (bread > 0)
        {
            player.WithResource(ResourceIds.Bread, bread);
        }

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
            Assert.That((worker.RestUntil!.Value - finishDate).TotalSeconds, Is.EqualTo(expectedRestSeconds).Within(2));
            Assert.That(player.Resource(ResourceIds.Bread), Is.EqualTo(expectedBread));
        }
    }

    private static void SetFeedWorkers(int playerId, bool enabled)
    {
        App.Act<DomikManager>(m => m.SetFeedWorkers(playerId, enabled));
    }
}
