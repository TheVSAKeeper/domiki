using Domiki.Web.Economy.Models;
using Domiki.Web.Reference.Dto;
using Domiki.Web.Village.Models;

namespace Domiki.Web.Village.Dto;

public static class DecorDtoExtensions
{
    public static DecorStateDto ToDto(this DecorState state, Neighbor[] neighbors)
    {
        return new()
        {
            Types = state.Types.Select(x => x.ToDto(neighbors, state.Types)).ToArray(),
            Owned = state.Owned.Select(x => x.ToDto()).ToArray(),
            Comfort = state.Comfort,
        };
    }

    public static DecorTypeDto ToDto(this DecorType type, Neighbor[] neighbors, DecorType[] types)
    {
        return new()
        {
            Id = type.Id,
            Name = type.Name,
            LogicName = type.LogicName,
            ComfortPoints = type.ComfortPoints,
            MaxCount = type.MaxCount,
            IsPurchasable = type.IsPurchasable,
            NeighborId = type.NeighborId,
            NeighborName = type.NeighborId == null ? null : neighbors.First(x => x.Id == type.NeighborId).Name,
            ReputationThreshold = type.ReputationThreshold,
            RequiresDecorTypeId = type.RequiresDecorTypeId,
            RequiresDecorName = type.RequiresDecorTypeId == null ? null : types.First(x => x.Id == type.RequiresDecorTypeId).Name,
            Cost = type.Cost.Select(x => x.ToDto()).ToArray(),
        };
    }

    public static PlayerDecorDto ToDto(this PlayerDecor decor)
    {
        return new()
        {
            DecorTypeId = decor.DecorTypeId,
            Count = decor.Count,
        };
    }
}
