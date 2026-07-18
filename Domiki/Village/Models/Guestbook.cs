namespace Domiki.Web.Village.Models;

public class GuestbookModel
{
    public int VisitsThisSeason { get; set; }
    public GuestbookEntryModel[] Entries { get; set; } = [];
}

public class VisitGuestbookModel
{
    public GuestbookEntryModel[] Entries { get; set; } = [];
    public bool CanLeaveEntry { get; set; }
    public bool AlreadyLeftToday { get; set; }
    public int GuestbookUnlockLevel { get; set; }
}

public class GuestbookEntryModel
{
    public int GuestPlayerId { get; set; }
    public required string GuestVillageName { get; set; }
    public int GuestCrestIcon { get; set; }
    public int GuestCrestColor { get; set; }
    public int PhraseId { get; set; }
    public DateTime Date { get; set; }
}
