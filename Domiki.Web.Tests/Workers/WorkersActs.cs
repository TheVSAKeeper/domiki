using Domiki.Web.Core;
using Domiki.Web.Core.Scheduling;
using Domiki.Web.Infrastructure;
using Domiki.Web.Workers;
using Domiki.Web.Workers.Models;

namespace Domiki.Web.Tests;

public static class WorkersActs
{
    public static IReadOnlyList<Worker> Workers(this TestPlayer p)
    {
        return App.Act<WorkerManager, IReadOnlyList<Worker>>(m => m.GetWorkers(p.Id).ToList());
    }

    public static DateTime RestUntilValue(this Worker worker)
    {
        Assert.That(worker.RestUntil, Is.Not.Null);
        return worker.RestUntil!.Value;
    }

    public static DateTime SickUntilValue(this Worker worker)
    {
        Assert.That(worker.SickUntil, Is.Not.Null);
        return worker.SickUntil!.Value;
    }

    public static TestPlayer StartManufacture(this TestPlayer p, int domikId, int receiptId, int[]? workerIds, bool useOptional = false)
    {
        App.Act<DomikManager>(m => m.StartManufacture(p.Id, domikId, receiptId, useOptional, workerIds));
        return p;
    }

    public static TestPlayer FinishManufacture(this TestPlayer p, int manufactureId, DateTime date)
    {
        var result = App.Act<DomikManager, bool>(m => m.FinishManufacture(date, new()
        {
            PlayerId = p.Id,
            ObjectId = manufactureId,
            Date = date,
            Type = CalculateTypes.Manufacture,
        }));

        Assert.That(result, Is.True);
        return p;
    }

    public static TestPlayer SetWorkerTrait(this TestPlayer p, int workerId, int traitId)
    {
        using var scope = App.Scope();
        scope.Context.Workers.Single(x => x.Id == workerId).TraitId = traitId;
        scope.Commit();
        return p;
    }

    public static TestPlayer SetWorkerRest(this TestPlayer p, int workerId, DateTime? restUntil)
    {
        using var scope = App.Scope();
        scope.Context.Workers.Single(x => x.Id == workerId).RestUntil = restUntil;
        scope.Commit();
        return p;
    }

    public static TestPlayer SetWorkerWorked(this TestPlayer p, int workerId, int workedSeconds)
    {
        using var scope = App.Scope();
        scope.Context.Workers.Single(x => x.Id == workerId).WorkedSeconds = workedSeconds;
        scope.Commit();
        return p;
    }

    public static TestPlayer SetWorkerHireDate(this TestPlayer p, int workerId, DateTime hireDate)
    {
        using var scope = App.Scope();
        scope.Context.Workers.Single(x => x.Id == workerId).HireDate = hireDate;
        scope.Commit();
        return p;
    }

    public static TestPlayer SetWorkerExpeditionCount(this TestPlayer p, int workerId, int count)
    {
        using var scope = App.Scope();
        scope.Context.Workers.Single(x => x.Id == workerId).ExpeditionCount = count;
        scope.Commit();
        return p;
    }

    public static TestPlayer SetWorkerSkill(this TestPlayer p, int workerId, int domikTypeId, int uses)
    {
        using var scope = App.Scope();
        var skill = scope.Context.WorkerSkills.SingleOrDefault(x => x.WorkerId == workerId && x.DomikTypeId == domikTypeId);
        if (skill == null)
        {
            scope.Context.WorkerSkills.Add(new()
            {
                WorkerId = workerId,
                DomikTypeId = domikTypeId,
                Uses = uses,
            });
        }
        else
        {
            skill.Uses = uses;
        }

        scope.Commit();
        return p;
    }
}
