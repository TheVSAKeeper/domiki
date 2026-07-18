using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Village.Models;

namespace Domiki.Web.Village;

/// <summary>
/// Книга гостей: след визитов между деревнями и короткие записи-фразы гостей хозяину.
/// </summary>
/// <remarks>
/// Одна строка <see cref="GuestbookEntry"/> на пару гость-хозяин в UTC-день; счётчик визитов
/// за сезон выводится агрегатом по дням, поэтому накрутка повторными заходами невозможна.
/// Под конкурентный доступ блокируется всегда только гость, хозяин – никогда.
/// </remarks>
public class GuestbookManager
{
    /// <summary>
    /// Обжитость, с которой гость может расписываться в чужих книгах.
    /// </summary>
    public const int GuestbookUnlockLevel = 20;

    /// <summary>
    /// Число фраз в справочнике книги гостей (id 1..8, тексты заданы на фронте).
    /// </summary>
    public const int GuestbookPhraseCount = 8;

    /// <summary>
    /// Сколько последних записей отдаётся в ленту книги.
    /// </summary>
    public const int GuestbookShowCount = 10;

    private readonly ApplicationDbContext _context;
    private readonly VillageLevelCalculator _villageLevelCalculator;
    private readonly SeasonManager _seasonManager;
    private readonly PlayerEventManager _playerEventManager;
    private readonly PlayerResourceManager _playerResourceManager;
    private readonly UnitOfWork _uow;
    private readonly GameStateBroker _broker;
    private readonly PushSender _pushSender;

    public GuestbookManager(ApplicationDbContext context, VillageLevelCalculator villageLevelCalculator, SeasonManager seasonManager, PlayerEventManager playerEventManager, PlayerResourceManager playerResourceManager, UnitOfWork uow, GameStateBroker broker, PushSender pushSender)
    {
        _context = context;
        _villageLevelCalculator = villageLevelCalculator;
        _seasonManager = seasonManager;
        _playerEventManager = playerEventManager;
        _playerResourceManager = playerResourceManager;
        _uow = uow;
        _broker = broker;
        _pushSender = pushSender;
    }

    /// <summary>
    /// Оставляет след визита гостя в деревне хозяина; повтор в тот же день и визит к себе игнорируются.
    /// </summary>
    /// <param name="guestPlayerId">Id игрока-гостя.</param>
    /// <param name="hostPlayerId">Id игрока-хозяина посещаемой деревни.</param>
    /// <param name="date">Текущий момент в UTC; задаёт календарный день следа.</param>
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

    /// <summary>
    /// Записывает фразу гостя в книгу хозяина и шлёт хозяину событие
    /// <see cref="PlayerEventType.GuestbookEntryLeft"/>.
    /// </summary>
    /// <param name="guestPlayerId">Id игрока-гостя, оставляющего запись.</param>
    /// <param name="hostPlayerId">Id игрока-хозяина книги.</param>
    /// <param name="phraseId">Фраза из справочника книги гостей, 1..<see cref="GuestbookPhraseCount"/>.</param>
    /// <param name="date">Текущий момент в UTC; задаёт календарный день записи.</param>
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

        var guestVillageName = guest.VillageName;

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
            guestVillageName,
            guestCrestIcon = guest.CrestIcon,
            guestCrestColor = guest.CrestColor,
            phraseId,
        });

        var afterEventAction = _uow.AfterEventAction;
        _uow.AfterEventAction = () =>
        {
            afterEventAction?.Invoke();
            _broker.Publish(hostPlayerId, GameStateScopes.State);
            _pushSender.Notify(hostPlayerId, "Домики", $"В вашей книге гостей расписались: {guestVillageName}", "/domiki-page");
        };
    }

    /// <summary>
    /// Книга гостей для самого хозяина: счётчик визитов за текущий сезон и лента последних записей.
    /// </summary>
    /// <param name="hostPlayerId">Id игрока-хозяина книги.</param>
    /// <param name="date">Текущий момент в UTC; по нему определяется текущий сезон.</param>
    /// <returns>Модель книги гостей хозяина: счётчик визитов за сезон и лента записей.</returns>
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

    /// <summary>
    /// Книга гостей глазами гостя при визите: лента записей плюс флаги,
    /// может ли гость расписаться сегодня.
    /// </summary>
    /// <param name="hostPlayerId">Id игрока-хозяина посещаемой деревни.</param>
    /// <param name="guestPlayerId">Id игрока-гостя, который смотрит книгу.</param>
    /// <param name="date">Текущий момент в UTC; задаёт календарный день для проверки «уже расписался сегодня».</param>
    /// <returns>Книга гостей глазами гостя: лента записей и флаги доступности записи.</returns>
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
