using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Models
{
    public static class ResourceTypeDtoExtentions
    {
        public static ResourceTypeDto ToDto(this ResourceType resourceType)
        {
            return new ResourceTypeDto { Id = resourceType.Id, LogicName = resourceType.LogicName, Name = resourceType.Name, MarketValue = ResourceManager.GetMarketValue(resourceType.Id) };
        }
    }
}
