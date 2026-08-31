using Domiki.Web.Activities.Dto;
using Domiki.Web.Village.Models;

namespace Domiki.Web.Village.Dto;

public static class WorldDtoExtensions
{
    public static WorldDto ToDto(this World world)
    {
        return new()
        {
            Villages = world.Villages.Select(x => x.ToDto()).ToArray(),
            Season = world.Season.ToDto(),
            TolokaArtifacts = world.TolokaArtifacts.Select(x => x.ToDto()).ToArray(),
        };
    }

    public static WorldVillageDto ToDto(this WorldVillage village)
    {
        return new()
        {
            PlayerId = village.PlayerId,
            VillageName = village.VillageName ?? "",
            CrestIcon = village.CrestIcon,
            CrestColor = village.CrestColor,
            Level = village.Level,
            IsNpc = village.IsNpc,
            IsMe = village.IsMe,
            NpcResourceTypeId = village.NpcResourceTypeId,
            NpcResourceName = village.NpcResourceName,
            NpcLogicName = village.NpcLogicName,
            ProfileLogicName = village.ProfileLogicName,
            SeasonOrders = village.SeasonOrders,
            SeasonToloka = village.SeasonToloka,
            SeasonExpeditions = village.SeasonExpeditions,
            Comfort = village.Comfort,
            RelocationCount = village.RelocationCount,
        };
    }

    public static VillageVisitDto ToDto(this VillageVisit visit, VisitGuestbookModel guestbook, VisitHelp help)
    {
        return new()
        {
            VillageName = visit.VillageName,
            CrestIcon = visit.CrestIcon,
            CrestColor = visit.CrestColor,
            Level = visit.Level.ToDto(),
            Buildings = visit.Buildings.Select(x => x.ToDto()).ToArray(),
            ProfileLogicName = visit.ProfileLogicName,
            Guestbook = guestbook.Entries.Select(x => x.ToDto()).ToArray(),
            CanLeaveEntry = guestbook.CanLeaveEntry,
            AlreadyLeftToday = guestbook.AlreadyLeftToday,
            GuestbookUnlockLevel = guestbook.GuestbookUnlockLevel,
            CanHelp = help.CanHelp,
            AlreadyHelpedToday = help.AlreadyHelpedToday,
            HostCapReached = help.HostCapReached,
            HasActiveWork = help.HasActiveWork,
            HelpUnlockLevel = help.UnlockLevel,
            RelocationCount = visit.RelocationCount,
            ChronicleLevelSum = visit.ChronicleLevelSum,
        };
    }

    public static VisitBuildingDto ToDto(this VisitBuilding building)
    {
        return new()
        {
            TypeName = building.TypeName,
            Level = building.Level,
        };
    }
}
