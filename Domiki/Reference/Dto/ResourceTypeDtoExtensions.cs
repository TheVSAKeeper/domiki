using Domiki.Web.Reference.Models;

namespace Domiki.Web.Reference.Dto;

public static class ResourceTypeDtoExtentions
{
    public static ResourceTypeDto ToDto(this ResourceType resourceType)
    {
        return new()
        {
            Id = resourceType.Id,
            LogicName = resourceType.LogicName ?? "",
            Name = resourceType.Name ?? "",
            IsFood = resourceType.IsFood,
            MarketValue = ResourceManager.GetMarketValue(resourceType.Id),
        };
    }
}
