using Domiki.Web.Village.Models;

namespace Domiki.Web.Village.Dto;

public static class GuestbookDtoExtensions
{
    public static GuestbookDto ToDto(this GuestbookModel guestbook)
    {
        return new()
        {
            VisitsThisSeason = guestbook.VisitsThisSeason,
            Entries = guestbook.Entries.Select(x => x.ToDto()).ToArray(),
        };
    }

    public static GuestbookEntryDto ToDto(this GuestbookEntryModel entry)
    {
        return new()
        {
            GuestPlayerId = entry.GuestPlayerId,
            GuestVillageName = entry.GuestVillageName,
            GuestCrestIcon = entry.GuestCrestIcon,
            GuestCrestColor = entry.GuestCrestColor,
            PhraseId = entry.PhraseId,
            Date = entry.Date,
        };
    }
}
