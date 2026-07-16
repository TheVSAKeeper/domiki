using Domiki.Web.Activities;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Tests;

public sealed class ProvisionsTests
{
    private const int ShortScoutId = 1;
    private const int FootScoutId = 3;
    private const int OrdinaryTraitId = 1;

    /// <summary>
    /// На типе экспедиции без опционального снаряжения флаг провианта не освобождает трудяг от отдыха и хлеб не списывается.
    /// </summary>
    [Test]
    public void ProvisionsFlagOnExpeditionWithoutOptionalEquipmentDoesNotSkipRestTest()
    {
        var player = TestPlayer.Create()
            .WithScoutingCapacity()
            .WithWorkerTraits(OrdinaryTraitId);

        player.StartExpedition(FootScoutId, provisions: true);
        var expedition = player.Expeditions().Active.Single();
        var assignedWorkerIds = player.Workers().Where(x => x.ExpeditionId == expedition.Id).Select(x => x.Id).ToArray();
        player.FinishExpedition(expedition.Id, expedition.FinishDate.AddSeconds(1));

        var workers = player.Workers().Where(x => assignedWorkerIds.Contains(x.Id)).ToArray();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(workers.All(x => x.RestUntil != null), Is.True);
            Assert.That(player.Resource(ResourceIds.Bread), Is.Zero);
        }
    }

    /// <summary>
    /// Запуск экспедиции с провиантом без достаточного запаса хлеба бросает исключение.
    /// </summary>
    [Test]
    public void StartingProvisionedExpeditionWithoutBreadThrowsTest()
    {
        var player = TestPlayer.Create()
            .WithScoutingCapacity()
            .WithResource(ResourceIds.Gold, 1)
            .WithResource(ResourceIds.Board, 2);

        Assert.Throws<BusinessException>(() => player.StartExpedition(ShortScoutId, provisions: true));
    }

    /// <summary>
    /// Флаг провианта на экспедиции списывает хлеб и избавляет вернувшихся трудяг от отдыха, без провианта хлеб не тратится и
    /// отдых обязателен.
    /// </summary>
    /// <param name="provisions">Взят ли провиант в экспедицию.</param>
    /// <param name="expectedBread">Ожидаемый остаток хлеба после экспедиции.</param>
    /// <param name="expectedNoRest">Ожидается ли отсутствие отдыха у трудяг.</param>
    [TestCase(true, 0, true)]
    [TestCase(false, 2, false)]
    public void ExpeditionProvisionsControlRestAndBreadWriteOffTest(bool provisions, int expectedBread, bool expectedNoRest)
    {
        var player = TestPlayer.Create()
            .WithScoutingCapacity()
            .WithWorkerTraits(OrdinaryTraitId)
            .WithResource(ResourceIds.Gold, 1)
            .WithResource(ResourceIds.Board, 2)
            .WithResource(ResourceIds.Bread, 2);

        player.StartExpedition(ShortScoutId, provisions: provisions);
        var expedition = player.Expeditions().Active.Single();
        var assignedWorkerIds = player.Workers().Where(x => x.ExpeditionId == expedition.Id).Select(x => x.Id).ToArray();
        player.FinishExpedition(expedition.Id, expedition.FinishDate.AddSeconds(1));

        var workers = player.Workers().Where(x => assignedWorkerIds.Contains(x.Id)).ToArray();
        if (expectedNoRest)
        {
            Assert.That(workers.All(x => x.RestUntil == null), Is.True);
        }
        else
        {
            foreach (var worker in workers)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(worker.RestUntil, Is.Not.Null);
                    Assert.That((worker.RestUntilValue() - expedition.FinishDate).TotalSeconds,
                        Is.EqualTo(ExpeditionManager.ExpeditionRestSeconds).Within(2));
                }
            }
        }

        Assert.That(player.Resource(ResourceIds.Bread), Is.EqualTo(expectedBread));
    }
}

file static class ProvisionsTestsActs
{
    public static TestPlayer WithScoutingCapacity(this TestPlayer player)
    {
        return player.WithDomik(DomikIds.Barrack)
            .WithDomik(DomikIds.ScoutHut);
    }
}
