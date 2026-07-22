using Domiki.Web.Core;
using Domiki.Web.Infrastructure;
using Domiki.Web.Workers;

namespace Domiki.Web.Tests;

/// <summary>
/// Правила уровней Корчмы.
/// </summary>
public sealed class TavernTests
{
    private const int OrdinaryTraitId = 1;

    /// <summary>
    /// Тёплый угол Корчмы третьего уровня сокращает срок хвори на четверть, а второго уровня его не сокращает.
    /// </summary>
    /// <param name="tavernLevel">Уровень Корчмы.</param>
    /// <param name="expectedSickSeconds">Ожидаемый срок хвори трудяги.</param>
    [TestCase(3, 21600)]
    [TestCase(2, DomikManager.SickDurationSeconds)]
    public void WarmCornerChangesOnlySickDurationTest(int tavernLevel, int expectedSickSeconds)
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Tavern, tavernLevel);
        var worker = player.Workers().Single();
        player.SetWorkerTrait(worker.Id, OrdinaryTraitId);

        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        SetManufactureSickChance(manufacture.Id, 100);
        var finishDate = manufacture.FinishDate.AddSeconds(1);
        player.FinishManufacture(manufacture.Id, finishDate);

        worker = player.Workers().Single();
        Assert.That((worker.SickUntilValue() - finishDate).TotalSeconds, Is.EqualTo(expectedSickSeconds).Within(2));
    }

    /// <summary>
    /// Корчма второго уровня автоматически собирает хлебный провиант, а первого уровня не собирает.
    /// </summary>
    /// <param name="tavernLevel">Уровень Корчмы.</param>
    /// <param name="expectedBread">Ожидаемый остаток хлеба.</param>
    /// <param name="expectedProvisioned">Должен ли поход получить провиант.</param>
    [TestCase(2, 0, true)]
    [TestCase(3, 0, true)]
    [TestCase(1, 2, false)]
    public void TavernAutoProvisionsExpeditionTest(int tavernLevel, int expectedBread, bool expectedProvisioned)
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.ScoutHut)
            .WithDomik(DomikIds.Tavern, tavernLevel)
            .WithWorkerTraits(OrdinaryTraitId)
            .WithResource(ResourceIds.Gold, 1)
            .WithResource(ResourceIds.Board, 2)
            .WithResource(ResourceIds.Bread, 2);

        player.StartExpedition(ExpeditionTypeIds.ShortScout);
        var expedition = player.Expeditions().Active.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(IsProvisioned(expedition.Id), Is.EqualTo(expectedProvisioned));
            Assert.That(player.Resource(ResourceIds.Bread), Is.EqualTo(expectedBread));
        }
    }

    /// <summary>
    /// Явно заказанный провиант заменяет хлеб сыром, когда хлеба в запасах нет.
    /// </summary>
    [Test]
    public void ManualProvisionsUseCheeseWhenBreadIsAbsentTest()
    {
        const int provisionFood = 2;
        const int startCheese = provisionFood;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.ScoutHut)
            .WithResource(ResourceIds.Gold, 1)
            .WithResource(ResourceIds.Board, 2)
            .WithResource(ResourceIds.Cheese, startCheese);

        player.StartExpedition(ExpeditionTypeIds.ShortScout, provisions: true);
        var expedition = player.Expeditions().Active.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(IsProvisioned(expedition.Id), Is.True);
            Assert.That(player.Resource(ResourceIds.Cheese), Is.EqualTo(startCheese - provisionFood));
        }
    }

    /// <summary>
    /// Экспедиция стартует без провианта, когда у Корчмы второго уровня нет еды.
    /// </summary>
    [Test]
    public void TavernWithoutFoodStartsUnprovisionedExpeditionTest()
    {
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.ScoutHut)
            .WithDomik(DomikIds.Tavern, TavernManager.ProvisionMinLevel)
            .WithResource(ResourceIds.Gold, 1)
            .WithResource(ResourceIds.Board, 2);

        Assert.DoesNotThrow(() => player.StartExpedition(ExpeditionTypeIds.ShortScout));

        var expedition = player.Expeditions().Active.Single();
        Assert.That(IsProvisioned(expedition.Id), Is.False);
    }

    private static void SetManufactureSickChance(int manufactureId, int chance)
    {
        using var scope = App.Scope();
        var manufacture = scope.Context.Manufactures.Single(x => x.Id == manufactureId);
        manufacture.SickChance = chance;
        scope.Commit();
    }

    private static bool IsProvisioned(int expeditionId)
    {
        return App.Read(context => context.Expeditions.Single(x => x.Id == expeditionId).Provisioned);
    }
}
