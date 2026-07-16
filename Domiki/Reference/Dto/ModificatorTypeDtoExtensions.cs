using Domiki.Web.Reference.Models;

namespace Domiki.Web.Reference.Dto
{
    public static class ModificatorTypeDtoExtentions
    {
        public static ModificatorTypeDto ToDto(this ModificatorType resourceType)
        {
            return new ModificatorTypeDto { Id = resourceType.Id, LogicName = resourceType.LogicName, Name = resourceType.Name };
        }
    }
}
