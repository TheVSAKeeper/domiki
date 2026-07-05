using Domiki.Web.Business.Models;

namespace Domiki.Web.Models
{
    public static class VillageDtoExtensions
    {
        public static VillageDto ToDto(this Village village)
        {
            return new VillageDto
            {
                VillageName = village.VillageName,
                CrestIcon = village.CrestIcon,
                CrestColor = village.CrestColor,
            };
        }
    }
}
