using Domiki.Web.Economy.Models;

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
            ExpireDate = DateTime.SpecifyKind(order.ExpireDate, DateTimeKind.Utc),
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
        };
    }
}
