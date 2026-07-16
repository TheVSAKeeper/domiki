using Domiki.Web.Business.Models;

namespace Domiki.Web.Models
{
    public static class DecorDtoExtensions
    {
        public static DecorStateDto ToDto(this DecorState state, Neighbor[] neighbors)
        {
            return new DecorStateDto
            {
                Types = state.Types.Select(x => x.ToDto(neighbors)).ToArray(),
                Owned = state.Owned.Select(x => x.ToDto()).ToArray(),
                Comfort = state.Comfort,
            };
        }

        public static DecorTypeDto ToDto(this DecorType type, Neighbor[] neighbors)
        {
            return new DecorTypeDto
            {
                Id = type.Id,
                Name = type.Name,
                LogicName = type.LogicName,
                ComfortPoints = type.ComfortPoints,
                IsPurchasable = type.IsPurchasable,
                NeighborId = type.NeighborId,
                NeighborName = type.NeighborId == null ? null : neighbors.First(x => x.Id == type.NeighborId).Name,
                ReputationThreshold = type.ReputationThreshold,
                Cost = type.Cost.Select(x => x.ToDto()).ToArray(),
            };
        }

        public static PlayerDecorDto ToDto(this PlayerDecor decor)
        {
            return new PlayerDecorDto
            {
                DecorTypeId = decor.DecorTypeId,
                Count = decor.Count,
            };
        }
    }
}
