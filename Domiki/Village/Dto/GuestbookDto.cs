namespace Domiki.Web.Village.Dto;

public sealed record GuestbookDto
{
    public required int VisitsThisSeason { get; init; }
    public required GuestbookEntryDto[] Entries { get; init; }
}

public sealed record GuestbookEntryDto
{
    public required int GuestPlayerId { get; init; }
    public required string GuestVillageName { get; init; }
    public required int GuestCrestIcon { get; init; }
    public required int GuestCrestColor { get; init; }
    public required int PhraseId { get; init; }
    public required DateTime Date { get; init; }
}
