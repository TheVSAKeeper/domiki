using Domiki.Web.Business.Models;

namespace Domiki.Web.Models
{
    public static class VillageLevelDtoExtensions
    {
        public static VillageLevelDto ToDto(this VillageLevel villageLevel)
        {
            return new VillageLevelDto
            {
                Level = villageLevel.Level,
                Buildings = villageLevel.Buildings,
                Residents = villageLevel.Residents,
                Reputation = villageLevel.Reputation,
                Comfort = villageLevel.Comfort,
                UpcomingUnlocks = villageLevel.UpcomingUnlocks.Select(x => x.ToDto()).ToArray(),
            };
        }

        public static VillageLevelUnlockDto ToDto(this VillageLevelUnlock unlock)
        {
            return new VillageLevelUnlockDto
            {
                Level = unlock.Level,
                Label = unlock.Label,
                Requirement = unlock.Requirement,
            };
        }
    }
}
