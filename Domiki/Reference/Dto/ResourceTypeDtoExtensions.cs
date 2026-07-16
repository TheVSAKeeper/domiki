using Domiki.Web.Reference.Models;
using Domiki.Web.Reference;

namespace Domiki.Web.Reference.Dto
{
    public static class ResourceTypeDtoExtentions
    {
        public static ResourceTypeDto ToDto(this ResourceType resourceType)
        {
            return new ResourceTypeDto { Id = resourceType.Id, LogicName = resourceType.LogicName, Name = resourceType.Name, MarketValue = ResourceManager.GetMarketValue(resourceType.Id) };
        }
    }
}
