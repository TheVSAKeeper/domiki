using Domiki.Web.Economy.Models;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Economy.Dto;

public static class OrderDtoExtensions
{
    public static OrderDto ToDto(this Order order)
    {
        return new()
        {
            Id = order.Id,
            NeighborId = order.Neighbor.Id,
            NeighborName = order.Neighbor.Name,
            NeighborLogicName = order.Neighbor.LogicName,
            ExpireDate = DateTimeHelper.AsUtc(order.ExpireDate),
            Required = order.Resources.Select(x => new OrderResourceDto
                {
                    ResourceTypeId = x.Type.Id,
                    Value = x.Value,
                })
                .ToArray(),
            RewardCoins = order.RewardCoins,
            RewardGold = order.RewardGold,
            RewardReputation = order.RewardReputation,
        };
    }

    public static NeighborReputationDto ToDto(this NeighborReputation reputation)
    {
        return new()
        {
            NeighborId = reputation.Neighbor.Id,
            NeighborName = reputation.Neighbor.Name,
            NeighborLogicName = reputation.Neighbor.LogicName,
            Points = reputation.Points,
            NextThreshold = reputation.NextThreshold,
            NextRewardName = reputation.NextRewardName,
            IsFriend = reputation.IsFriend,
            IsOpen = reputation.IsOpen,
        };
    }
}
