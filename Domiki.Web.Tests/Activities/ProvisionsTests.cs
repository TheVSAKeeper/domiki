using Domiki.Web.Activities;
using Domiki.Web.Activities.Models;
using Domiki.Web.Core.Scheduling;
using Domiki.Web.Infrastructure;
using Domiki.Web.Workers.Models;

namespace Domiki.Web.Tests;

public class ProvisionsTests : TestBase
{
    private const int ShortScoutId = 1;
    private const int FootScoutId = 3;
    private const int OrdinaryTraitId = 1;
    private const int BarracksTypeId = 2;
    private const int ScoutHutDomikTypeId = 11;
    private const int GoldResourceTypeId = 5;
    private const int PlankResourceTypeId = 7;
    private const int BreadResourceTypeId = 15;

    /// <summary>
    /// На типе экспедиции без опционального снаряжения флаг провианта не освобождает трудяг от отдыха и хлеб не списывается.
    /// </summary>
    [Test]
    public void ProvisionsFlagOnExpeditionWithoutOptionalEquipmentDoesNotSkipRestTest()
    {
        var playerId = GetPlayerId();
        AddWorkerCapacity(playerId);
        SetAllWorkersOrdinary(playerId);

        StartExpedition(playerId, FootScoutId, true);
        var expedition = GetExpeditions(playerId)!.Active.Single();
        var assignedWorkerIds = GetWorkers(playerId).Where(x => x.ExpeditionId == expedition.Id).Select(x => x.Id).ToArray();
        FinishExpedition(playerId, expedition.Id, expedition.FinishDate.AddSeconds(1));

        var workers = GetWorkers(playerId).Where(x => assignedWorkerIds.Contains(x.Id)).ToArray();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(workers.All(x => x.RestUntil != null), Is.True);
            Assert.That(GetResource(playerId, BreadResourceTypeId), Is.Zero);
        }
    }

    /// <summary>
    /// Запуск экспедиции с провиантом без достаточного запаса хлеба бросает исключение.
    /// </summary>
    [Test]
    public void StartingProvisionedExpeditionWithoutBreadThrowsTest()
    {
        var playerId = GetPlayerId();
        AddWorkerCapacity(playerId);
        GrantResource(playerId, GoldResourceTypeId, 1);
        GrantResource(playerId, PlankResourceTypeId, 2);

        Assert.Throws<BusinessException>(() => StartExpedition(playerId, ShortScoutId, true));
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
        var playerId = GetPlayerId();
        AddWorkerCapacity(playerId);
        SetAllWorkersOrdinary(playerId);
        GrantResource(playerId, GoldResourceTypeId, 1);
        GrantResource(playerId, PlankResourceTypeId, 2);
        GrantResource(playerId, BreadResourceTypeId, 2);

        StartExpedition(playerId, ShortScoutId, provisions);
        var expedition = GetExpeditions(playerId)!.Active.Single();
        var assignedWorkerIds = GetWorkers(playerId).Where(x => x.ExpeditionId == expedition.Id).Select(x => x.Id).ToArray();
        FinishExpedition(playerId, expedition.Id, expedition.FinishDate.AddSeconds(1));

        var workers = GetWorkers(playerId).Where(x => assignedWorkerIds.Contains(x.Id)).ToArray();
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
                    Assert.That((worker.RestUntil!.Value - expedition.FinishDate).TotalSeconds,
                        Is.EqualTo(ExpeditionManager.ExpeditionRestSeconds).Within(2));
                }
            }
        }

        Assert.That(GetResource(playerId, BreadResourceTypeId), Is.EqualTo(expectedBread));
    }

    private int GetPlayerId()
    {
        using var uow = GetUow();
        var playerId = GetDomikManager(uow).GetPlayerId("testUser_" + Guid.NewGuid());
        uow.Commit();
        return playerId;
    }

    private void AddWorkerCapacity(int playerId)
    {
        using (var uow = GetUow())
        {
            var nextId = (uow.Context.Domiks.Where(x => x.PlayerId == playerId).Max(x => (int?)x.Id) ?? 0) + 1;
            uow.Context.Domiks.Add(new()
            {
                PlayerId = playerId,
                Id = nextId,
                TypeId = BarracksTypeId,
                Level = 1,
            });

            uow.Commit();
        }

        using var scoutHutUow = GetUow();
        scoutHutUow.Context.Domiks.Add(new()
        {
            PlayerId = playerId,
            Id = -ScoutHutDomikTypeId,
            TypeId = ScoutHutDomikTypeId,
            Level = 1,
        });

        scoutHutUow.Commit();
    }

    private Worker[] GetWorkers(int playerId)
    {
        using var uow = GetUow();
        var workers = GetWorkerManager(uow).GetWorkers(playerId).ToArray();
        uow.Commit();
        return workers;
    }

    private void SetAllWorkersOrdinary(int playerId)
    {
        var ids = GetWorkers(playerId).Select(x => x.Id).ToArray();
        using var uow = GetUow();
        foreach (var worker in uow.Context.Workers.Where(x => ids.Contains(x.Id)))
        {
            worker.TraitId = OrdinaryTraitId;
        }

        uow.Commit();
    }

    private ExpeditionState? GetExpeditions(int playerId)
    {
        using var uow = GetUow();
        var expeditions = GetExpeditionManager(uow).GetExpeditions(playerId);
        uow.Commit();
        return expeditions;
    }

    private int GetResource(int playerId, int typeId)
    {
        using var uow = GetUow();
        var value = uow.Context.Resources.SingleOrDefault(x => x.PlayerId == playerId && x.TypeId == typeId)?.Value ?? 0;
        uow.Commit();
        return value;
    }

    private void GrantResource(int playerId, int typeId, int value)
    {
        using var uow = GetUow();
        var resource = uow.Context.Resources.SingleOrDefault(x => x.PlayerId == playerId && x.TypeId == typeId);
        if (resource == null)
        {
            resource = new()
            {
                PlayerId = playerId,
                TypeId = typeId,
            };

            uow.Context.Resources.Add(resource);
        }

        resource.Value += value;
        uow.Commit();
    }

    private void StartExpedition(int playerId, int expeditionTypeId, bool provisions)
    {
        using var uow = GetUow();
        GetExpeditionManager(uow, false).StartExpedition(playerId, expeditionTypeId, provisions: provisions);
        uow.Commit();
    }

    private void FinishExpedition(int playerId, int expeditionId, DateTime date)
    {
        using var uow = GetUow();
        var result = GetExpeditionManager(uow)
            .FinishExpedition(date, new()
            {
                PlayerId = playerId,
                ObjectId = expeditionId,
                Date = date,
                Type = CalculateTypes.Expedition,
            });

        Assert.That(result, Is.True);
        uow.Commit();
    }
}
