using Domiki.Web.Business.Models;

namespace Domiki.Web.Models
{
    public static class WorldDtoExtensions
    {
        public static WorldDto ToDto(this World world)
        {
            return new WorldDto
            {
                Villages = world.Villages.Select(x => x.ToDto()).ToArray(),
                Season = world.Season.ToDto(),
            };
        }

        public static WorldVillageDto ToDto(this WorldVillage village)
        {
            return new WorldVillageDto
            {
                PlayerId = village.PlayerId,
                VillageName = village.VillageName,
                CrestIcon = village.CrestIcon,
                CrestColor = village.CrestColor,
                Level = village.Level,
                IsNpc = village.IsNpc,
                IsMe = village.IsMe,
                NpcResourceTypeId = village.NpcResourceTypeId,
                SeasonOrders = village.SeasonOrders,
                SeasonToloka = village.SeasonToloka,
                SeasonExpeditions = village.SeasonExpeditions,
                Comfort = village.Comfort,
            };
        }

        public static VillageVisitDto ToDto(this VillageVisit visit)
        {
            return new VillageVisitDto
            {
                VillageName = visit.VillageName,
                CrestIcon = visit.CrestIcon,
                CrestColor = visit.CrestColor,
                Level = visit.Level.ToDto(),
                Buildings = visit.Buildings.Select(x => x.ToDto()).ToArray(),
            };
        }

        public static VisitBuildingDto ToDto(this VisitBuilding building)
        {
            return new VisitBuildingDto
            {
                TypeName = building.TypeName,
                Level = building.Level,
            };
        }
    }
}
