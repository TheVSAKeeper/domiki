using Domiki.Web.Economy.Models;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Economy.Dto;

public static class ConvoyDtoExtensions
{
    public static ConvoyDto ToDto(this Convoy convoy)
    {
        return new()
        {
            NeighborId = convoy.Neighbor.Id,
            NeighborName = convoy.Neighbor.Name,
            NeighborLogicName = convoy.Neighbor.LogicName,
            Items = convoy.Items.Select(x => new ConvoyItemDto
                {
                    ResourceTypeId = x.ResourceTypeId,
                    Price = x.Price,
                })
                .ToArray(),
            Limit = convoy.Limit,
            Remaining = convoy.Remaining,
            WindowResetDate = DateTimeHelper.AsUtc(convoy.WindowResetDate),
            IsLocked = convoy.IsLocked,
        };
    }
}
