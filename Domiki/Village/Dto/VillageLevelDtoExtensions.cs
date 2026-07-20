using Domiki.Web.Village.Models;

namespace Domiki.Web.Village.Dto;

public static class VillageLevelDtoExtensions
{
    public static VillageLevelDto ToDto(this VillageLevel villageLevel)
    {
        return new()
        {
            Level = villageLevel.Level,
            Buildings = villageLevel.Buildings,
            Residents = villageLevel.Residents,
            Reputation = villageLevel.Reputation,
            Comfort = villageLevel.Comfort,
            VisitsSinceBigGift = villageLevel.VisitsSinceBigGift,
            Unlocks = villageLevel.Unlocks.Select(x => x.ToDto()).ToArray(),
        };
    }

    public static VillageLevelUnlockDto ToDto(this VillageLevelUnlock unlock)
    {
        return new()
        {
            Level = unlock.Level,
            Label = unlock.Label,
            Requirement = unlock.Requirement,
            Unlocked = unlock.Unlocked,
            Kind = unlock.Kind,
            LogicName = unlock.LogicName,
        };
    }
}
