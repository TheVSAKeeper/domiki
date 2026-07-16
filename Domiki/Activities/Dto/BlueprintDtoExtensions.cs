using Domiki.Web.Activities.Models;

namespace Domiki.Web.Activities.Dto;

public static class BlueprintDtoExtensions
{
    public static BlueprintDto ToDto(this PlayerBlueprint blueprint)
    {
        return new()
        {
            Id = blueprint.Blueprint.Id,
            Name = blueprint.Blueprint.Name,
            DomikTypeId = blueprint.Blueprint.DomikTypeId,
            NeighborId = blueprint.Blueprint.NeighborId,
            NeighborName = blueprint.Neighbor.Name,
            ReputationThreshold = blueprint.Blueprint.ReputationThreshold,
            CurrentReputation = blueprint.CurrentReputation,
            Owned = blueprint.Owned,
        };
    }
}
