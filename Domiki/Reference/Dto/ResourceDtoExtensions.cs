using Domiki.Web.Reference.Models;

namespace Domiki.Web.Reference.Dto;

public static class ResourceDtoExtentions
{
    public static ResourceDto ToDto(this Resource res)
    {
        return new()
        {
            Value = res.Value,
            TypeId = res.Type.Id,
        };
    }
}
