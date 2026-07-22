using Domiki.Web.Village.Models;

namespace Domiki.Web.Village.Dto;

public static class VillageDtoExtensions
{
    public static VillageDto ToDto(this VillageState village)
    {
        return new()
        {
            VillageName = village.VillageName,
            CrestIcon = village.CrestIcon,
            CrestColor = village.CrestColor,
        };
    }
}
