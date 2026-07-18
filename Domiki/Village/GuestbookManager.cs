using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Village.Models;

namespace Domiki.Web.Village;

public class GuestbookManager
{
    public const int GuestbookUnlockLevel = 20;
    public const int GuestbookPhraseCount = 8;
    public const int GuestbookShowCount = 10;

    private readonly ApplicationDbContext _context;
    private readonly VillageLevelCalculator _villageLevelCalculator;
    private readonly SeasonManager _seasonManager;
    private readonly PlayerEventManager _playerEventManager;
    private readonly PlayerResourceManager _playerResourceManager;

    public GuestbookManager(ApplicationDbContext context, VillageLevelCalculator villageLevelCalculator, SeasonManager seasonManager, PlayerEventManager playerEventManager, PlayerResourceManager playerResourceManager)
    {
        _context = context;
        _villageLevelCalculator = villageLevelCalculator;
        _seasonManager = seasonManager;
        _playerEventManager = playerEventManager;
        _playerResourceManager = playerResourceManager;
    }

    public void RecordVisit(int guestPlayerId, int hostPlayerId, DateTime date)
    {
        if (guestPlayerId == hostPlayerId)
        {
            return;
        }

        var day = DateOnly.FromDateTime(date);
        if (EntryExists(hostPlayerId, guestPlayerId, day))
        {
            return;
        }

        _playerResourceManager.LockDbPlayerRow(guestPlayerId);

        if (EntryExists(hostPlayerId, guestPlayerId, day))
        {
            return;
        }

        _context.GuestbookEntries.Add(new()
        {
            HostPlayerId = hostPlayerId,
            GuestPlayerId = guestPlayerId,
            Day = day,
            Date = date,
        });
    }

    public void LeaveEntry(int guestPlayerId, int hostPlayerId, int phraseId, DateTime date)
    {
        if (guestPlayerId == hostPlayerId)
        {
            throw new BusinessException("Нельзя расписаться в своей книге");
        }

        if (phraseId < 1 || phraseId > GuestbookPhraseCount)
        {
            throw new BusinessException("Неизвестная фраза");
        }

        var guest = _context.Players.Single(x => x.Id == guestPlayerId);
        if (guest.VillageName == null)
        {
            throw new BusinessException("Сначала назовите свою деревню");
        }

        if (_villageLevelCalculator.GetLevel(guestPlayerId).Level < GuestbookUnlockLevel)
        {
            throw new BusinessException($"Книга гостей откроется на обжитости {GuestbookUnlockLevel}");
        }

        _playerResourceManager.LockDbPlayerRow(guestPlayerId);

        var day = DateOnly.FromDateTime(date);
        var entry = _context.GuestbookEntries.FirstOrDefault(x => x.HostPlayerId == hostPlayerId && x.GuestPlayerId == guestPlayerId && x.Day == day);
        if (entry?.PhraseId != null)
        {
            throw new BusinessException("Вы уже оставили запись сегодня");
        }

        if (entry == null)
        {
            entry = new()
            {
                HostPlayerId = hostPlayerId,
                GuestPlayerId = guestPlayerId,
                Day = day,
            };
            _context.GuestbookEntries.Add(entry);
        }

        entry.PhraseId = phraseId;
        entry.Date = date;

        _playerEventManager.Record(hostPlayerId, PlayerEventType.GuestbookEntryLeft, new
        {
            guestVillageName = guest.VillageName,
            guestCrestIcon = guest.CrestIcon,
            guestCrestColor = guest.CrestColor,
            phraseId,
        });
    }

    public GuestbookModel GetGuestbook(int hostPlayerId, DateTime date)
    {
        var season = _seasonManager.GetCurrentSeason(date);
        var start = DateOnly.FromDateTime(season.StartDate);
        var end = DateOnly.FromDateTime(season.EndDate);

        var visitsThisSeason = _context.GuestbookEntries
            .Count(x => x.HostPlayerId == hostPlayerId && x.Day >= start && x.Day < end);

        return new()
        {
            VisitsThisSeason = visitsThisSeason,
            Entries = GetEntries(hostPlayerId),
        };
    }

    public VisitGuestbookModel GetVisitGuestbook(int hostPlayerId, int guestPlayerId, DateTime date)
    {
        var day = DateOnly.FromDateTime(date);
        var alreadyLeftToday = _context.GuestbookEntries
            .Any(x => x.HostPlayerId == hostPlayerId && x.GuestPlayerId == guestPlayerId && x.Day == day && x.PhraseId != null);

        var guestVillageName = _context.Players.Where(x => x.Id == guestPlayerId).Select(x => x.VillageName).Single();

        var canLeaveEntry = guestPlayerId != hostPlayerId
                             && guestVillageName != null
                             && !alreadyLeftToday
                             && _villageLevelCalculator.GetLevel(guestPlayerId).Level >= GuestbookUnlockLevel;

        return new()
        {
            Entries = GetEntries(hostPlayerId),
            AlreadyLeftToday = alreadyLeftToday,
            CanLeaveEntry = canLeaveEntry,
            GuestbookUnlockLevel = GuestbookUnlockLevel,
        };
    }

    private bool EntryExists(int hostPlayerId, int guestPlayerId, DateOnly day)
    {
        return _context.GuestbookEntries.Any(x => x.HostPlayerId == hostPlayerId && x.GuestPlayerId == guestPlayerId && x.Day == day);
    }

    private GuestbookEntryModel[] GetEntries(int hostPlayerId)
    {
        return _context.GuestbookEntries
            .Where(x => x.HostPlayerId == hostPlayerId && x.PhraseId != null)
            .OrderByDescending(x => x.Date)
            .Take(GuestbookShowCount)
            .Join(_context.Players, x => x.GuestPlayerId, p => p.Id, (x, p) => new GuestbookEntryModel
            {
                GuestPlayerId = x.GuestPlayerId,
                GuestVillageName = p.VillageName ?? "",
                GuestCrestIcon = p.CrestIcon,
                GuestCrestColor = p.CrestColor,
                PhraseId = x.PhraseId!.Value,
                Date = x.Date,
            })
            .ToArray();
    }
}
