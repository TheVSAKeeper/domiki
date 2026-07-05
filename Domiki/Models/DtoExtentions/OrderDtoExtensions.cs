using Domiki.Web.Business.Models;

namespace Domiki.Web.Models
{
    public static class OrderDtoExtensions
    {
        public static OrderDto ToDto(this Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                NeighborId = order.Neighbor.Id,
                NeighborName = order.Neighbor.Name,
                ExpireDate = DateTime.SpecifyKind(order.ExpireDate, DateTimeKind.Utc),
                Required = order.Resources.Select(x => new OrderResourceDto
                {
                    ResourceTypeId = x.Type.Id,
                    Value = x.Value,
                }).ToArray(),
                RewardCoins = order.RewardCoins,
                RewardGold = order.RewardGold,
                RewardReputation = order.RewardReputation,
            };
        }

        public static NeighborReputationDto ToDto(this NeighborReputation reputation)
        {
            return new NeighborReputationDto
            {
                NeighborId = reputation.Neighbor.Id,
                NeighborName = reputation.Neighbor.Name,
                Points = reputation.Points,
            };
        }
    }
}
