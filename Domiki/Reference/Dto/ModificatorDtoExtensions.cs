using Domiki.Web.Reference.Models;

namespace Domiki.Web.Reference.Dto
{
    public static class ModificatorDtoExtentions
    {
        public static ModificatorDto ToDto(this Modificator res)
        {
            return new ModificatorDto
            {
                Value = res.Value,
                TypeId = res.Type.Id,
            };
        }
    }
}
