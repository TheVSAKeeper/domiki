using Domiki.Web.Core;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Workers;
using Domik = Domiki.Web.Core.Models.Domik;
using DomikType = Domiki.Web.Core.Models.DomikType;
using Manufacture = Domiki.Web.Core.Models.Manufacture;
using Resource = Domiki.Web.Reference.Models.Resource;

namespace Domiki.Web.Tests;

public class TestPlayer
{
    private TestPlayer(int id)
    {
        Id = id;
    }

    public int Id { get; }

    public static TestPlayer Create()
    {
        var id = App.Act<DomikManager, int>(m => m.GetPlayerId($"testUser_{App.RunId}_{Guid.NewGuid()}"));
        MuteFtue(id);
        return new(id);
    }

    public TestPlayer WithResource(int typeId, int value)
    {
        using var scope = App.Scope();
        var resource = scope.Context.Resources.FirstOrDefault(x => x.PlayerId == Id && x.TypeId == typeId);
        if (resource == null)
        {
            resource = new()
            {
                PlayerId = Id,
                TypeId = typeId,
            };

            scope.Context.Resources.Add(resource);
        }

        resource.Value += value;
        scope.Commit();
        return this;
    }

    public TestPlayer WithDomik(int typeId, int level = 1)
    {
        using var scope = App.Scope();
        var nextId = (scope.Context.Domiks.Where(x => x.PlayerId == Id).Max(x => (int?)x.Id) ?? 0) + 1;
        scope.Context.Domiks.Add(new()
        {
            PlayerId = Id,
            Id = nextId,
            TypeId = typeId,
            Level = level,
        });

        scope.Commit();
        return this;
    }

    public TestPlayer WithDomiks(int typeId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            WithDomik(typeId);
        }

        return this;
    }

    public TestPlayer WithBlueprint(int blueprintId)
    {
        using var scope = App.Scope();
        scope.Context.PlayerBlueprints.Add(new()
        {
            PlayerId = Id,
            BlueprintId = blueprintId,
        });

        scope.Commit();
        return this;
    }

    public TestPlayer WithDecor(int decorTypeId, int count = 1)
    {
        using var scope = App.Scope();
        var decor = scope.Context.PlayerDecors.SingleOrDefault(x => x.PlayerId == Id && x.DecorTypeId == decorTypeId);
        if (decor == null)
        {
            decor = new()
            {
                PlayerId = Id,
                DecorTypeId = decorTypeId,
            };

            scope.Context.PlayerDecors.Add(decor);
        }

        decor.Count += count;
        scope.Commit();
        return this;
    }

    public TestPlayer WithWorkerTraits(int traitId = 1)
    {
        using var scope = App.Scope();
        scope.Get<WorkerManager>().EnsureWorkers(Id);
        foreach (var worker in scope.Context.Workers.Where(x => x.PlayerId == Id).ToArray())
        {
            worker.TraitId = traitId;
        }

        scope.Commit();
        return this;
    }

    public IEnumerable<Resource> Resources()
    {
        return App.Act<DomikManager, IEnumerable<Resource>>(m => m.GetResources(Id));
    }

    public IEnumerable<Domik> Domiks()
    {
        return App.Act<DomikManager, IEnumerable<Domik>>(m => m.GetDomiks(Id));
    }

    public IEnumerable<DomikType> DomikTypes()
    {
        return App.Act<ResourceManager, DomikType[]>(m => m.GetDomikTypes());
    }

    public int Resource(int typeId)
    {
        return Resources().FirstOrDefault(x => x.Type.Id == typeId)?.Value ?? 0;
    }

    public int DomikId(int typeId)
    {
        return Domiks().Where(x => x.Type.Id == typeId).Max(x => x.Id);
    }

    public Manufacture Manufacture(int domikId)
    {
        return Domiks().First(x => x.Id == domikId).Manufactures.Single();
    }

    private static void MuteFtue(int playerId)
    {
        using var scope = App.Scope();
        var completed = scope.Context.PlayerGoals
            .Where(x => x.PlayerId == playerId)
            .Select(x => x.GoalId)
            .ToHashSet();

        scope.Context.PlayerGoals.AddRange(scope.Context.StarterGoals.Select(x => x.Id)
            .ToArray()
            .Where(x => !completed.Contains(x))
            .Select(goalId => new PlayerGoal
            {
                PlayerId = playerId,
                GoalId = goalId,
                CompleteDate = DateTimeHelper.GetNowDate(),
            }));

        scope.Context.Players.Single(x => x.Id == playerId).ZealCharges = 0;
        scope.Commit();
    }
}
