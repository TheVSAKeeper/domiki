using Domiki.Web.Core.Scheduling;
using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Economy;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village.Models;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Village;

/// <summary>
/// «Подсобить»: гость при визите чужой деревни раз в день сокращает самую долгую активную работу хозяина.
/// </summary>
/// <remarks>
/// Сокращается <see cref="HelpReducePercent"/> процентов остатка одной работы – улучшения домика или производства,
/// той, что закончится позже всех прочих. Гость ограничен одним «подсобить» в UTC-день, деревня хозяина –
/// <see cref="HostHelpCapPerDay"/> визитами в день. Под конкурентный доступ блокируются строки обоих игроков,
/// упорядоченные по Id (см. <see cref="Economy.MarketManager.AcceptLot"/> – тот же канон парного лока).
/// </remarks>
public class HelpManager
{
    /// <summary>
    /// Обжитость, с которой гость может подсоблять чужим деревням.
    /// </summary>
    public const int HelpUnlockLevel = 20;

    /// <summary>
    /// На сколько процентов остатка сокращается выбранная работа хозяина.
    /// </summary>
    public const int HelpReducePercent = 10;

    /// <summary>
    /// Сколько раз в сутки одной деревне хозяина можно подсобить.
    /// </summary>
    public const int HostHelpCapPerDay = 3;

    /// <summary>
    /// Монеты, выдаваемые гостю за одно «подсобить».
    /// </summary>
    public const int HelpRewardCoins = 5;

    private readonly ApplicationDbContext _context;
    private readonly UnitOfWork _uow;
    private readonly PlayerResourceManager _playerResourceManager;
    private readonly VillageLevelCalculator _villageLevelCalculator;
    private readonly PlayerEventManager _playerEventManager;
    private readonly ResourceManager _resourceManager;
    private readonly GameStateBroker _broker;
    private readonly PushSender _pushSender;
    private readonly ICalculator _calculator;

    public HelpManager(ApplicationDbContext context, UnitOfWork uow, PlayerResourceManager playerResourceManager, VillageLevelCalculator villageLevelCalculator, PlayerEventManager playerEventManager, ResourceManager resourceManager, GameStateBroker broker, PushSender pushSender, ICalculator calculator)
    {
        _context = context;
        _uow = uow;
        _playerResourceManager = playerResourceManager;
        _villageLevelCalculator = villageLevelCalculator;
        _playerEventManager = playerEventManager;
        _resourceManager = resourceManager;
        _broker = broker;
        _pushSender = pushSender;
        _calculator = calculator;
    }

    /// <summary>
    /// Гость сокращает самую долгую активную работу хозяина на <see cref="HelpReducePercent"/> процентов остатка.
    /// </summary>
    /// <param name="guestPlayerId">Id игрока-гостя.</param>
    /// <param name="hostPlayerId">Id игрока-хозяина посещаемой деревни.</param>
    /// <param name="date">Текущий момент в UTC; задаёт календарный день капов и точку отсчёта остатка работы.</param>
    /// <returns>Какая работа сокращена, на сколько секунд и сколько монет выдано гостю.</returns>
    public HelpResult Help(int guestPlayerId, int hostPlayerId, DateTime date)
    {
        if (guestPlayerId == hostPlayerId)
        {
            throw new BusinessException("Нельзя подсобить своей деревне");
        }

        var host = _context.Players.SingleOrDefault(x => x.Id == hostPlayerId);
        if (host == null || host.VillageName == null)
        {
            throw new BusinessException("Деревня не найдена");
        }

        var guest = _context.Players.Single(x => x.Id == guestPlayerId);
        if (guest.VillageName == null)
        {
            throw new BusinessException("Сначала назовите свою деревню");
        }

        if (_villageLevelCalculator.GetLevel(guestPlayerId).Level < HelpUnlockLevel)
        {
            throw new BusinessException($"«Подсобить» откроется на обжитости {HelpUnlockLevel}");
        }

        foreach (var playerId in new[] { guestPlayerId, hostPlayerId }.OrderBy(x => x))
        {
            _playerResourceManager.LockDbPlayerRow(playerId);
        }

        var day = DateOnly.FromDateTime(date);

        var guestLastHelpDate = _context.Players.AsNoTracking().Where(x => x.Id == guestPlayerId).Select(x => x.LastHelpDate).Single();
        if (guestLastHelpDate.HasValue && DateOnly.FromDateTime(guestLastHelpDate.Value) == day)
        {
            throw new BusinessException("Вы уже подсобили сегодня");
        }

        var hostCaps = _context.Players.AsNoTracking()
            .Where(x => x.Id == hostPlayerId)
            .Select(x => new { x.HelpsReceivedDate, x.HelpsReceivedToday })
            .Single();

        var helpsReceivedToday = hostCaps.HelpsReceivedDate.HasValue && DateOnly.FromDateTime(hostCaps.HelpsReceivedDate.Value) == day
            ? hostCaps.HelpsReceivedToday
            : 0;

        if (helpsReceivedToday >= HostHelpCapPerDay)
        {
            throw new BusinessException("Этой деревне сегодня уже подсобили");
        }

        var candidate = GetActiveWorkCandidates(hostPlayerId, date).OrderByDescending(x => x.FinishDate).FirstOrDefault();
        if (candidate == null)
        {
            throw new BusinessException("Сейчас у деревни нет активных работ");
        }

        var reducedSeconds = (int)Math.Ceiling((candidate.FinishDate - date).TotalSeconds * HelpReducePercent / 100.0);
        DateTime newFinishDate;

        var candidateObjectId = (int)candidate.ObjectId;

        if (candidate.Type == CalculateTypes.Domiks)
        {
            var dbDomik = _context.Domiks.Single(x => x.PlayerId == hostPlayerId && x.Id == candidateObjectId);
            dbDomik.UpgradeCalculateDate -= TimeSpan.FromSeconds(reducedSeconds);
            newFinishDate = dbDomik.UpgradeCalculateDate!.Value.AddSeconds(dbDomik.UpgradeSeconds!.Value);
        }
        else
        {
            var dbManufacture = _context.Manufactures.Single(x => x.DomikPlayerId == hostPlayerId && x.Id == candidateObjectId);
            dbManufacture.FinishDate -= TimeSpan.FromSeconds(reducedSeconds);
            newFinishDate = dbManufacture.FinishDate;
        }

        guest.LastHelpDate = date;
        host.HelpsReceivedDate = date;
        host.HelpsReceivedToday = helpsReceivedToday + 1;

        _playerResourceManager.GrantResource(guestPlayerId, MarketManager.CoinResourceTypeId, HelpRewardCoins);

        var guestVillageName = guest.VillageName;
        var domikTypeName = _resourceManager.GetDomikTypes().First(x => x.Id == candidate.DomikTypeId).Name;
        var objectId = candidate.ObjectId;
        var calculateType = candidate.Type;

        _playerEventManager.Record(hostPlayerId, PlayerEventType.VillageHelped, new
        {
            guestVillageName,
            guestCrestIcon = guest.CrestIcon,
            guestCrestColor = guest.CrestColor,
            domikTypeName,
            reducedSeconds,
        });

        var afterEventAction = _uow.AfterEventAction;
        _uow.AfterEventAction = () =>
        {
            afterEventAction?.Invoke();
            _calculator.Reschedule(hostPlayerId, objectId, calculateType, newFinishDate);
            _broker.Publish(hostPlayerId, GameStateScopes.State);
            _pushSender.Notify(hostPlayerId, "Домики", $"{guestVillageName} подсобила: {domikTypeName} освободится раньше", "/domiki-page");
            _broker.Publish(guestPlayerId, GameStateScopes.State);
        };

        return new()
        {
            DomikTypeName = domikTypeName,
            ReducedSeconds = reducedSeconds,
            RewardCoins = HelpRewardCoins,
        };
    }

    /// <summary>
    /// Доступность «подсобить» глазами гостя при визите – без блокировок, для витрины визита.
    /// </summary>
    /// <param name="hostPlayerId">Id игрока-хозяина посещаемой деревни.</param>
    /// <param name="guestPlayerId">Id игрока-гостя, который смотрит витрину.</param>
    /// <param name="date">Текущий момент в UTC; задаёт календарный день для проверки капов.</param>
    /// <returns>Флаги доступности «подсобить» и порог открытия механики.</returns>
    public VisitHelp GetVisitHelp(int hostPlayerId, int guestPlayerId, DateTime date)
    {
        var day = DateOnly.FromDateTime(date);

        var guestVillageName = _context.Players.Where(x => x.Id == guestPlayerId).Select(x => x.VillageName).Single();
        var guestLastHelpDate = _context.Players.Where(x => x.Id == guestPlayerId).Select(x => x.LastHelpDate).Single();
        var alreadyHelpedToday = guestLastHelpDate.HasValue && DateOnly.FromDateTime(guestLastHelpDate.Value) == day;

        var hostCaps = _context.Players
            .Where(x => x.Id == hostPlayerId)
            .Select(x => new { x.HelpsReceivedDate, x.HelpsReceivedToday })
            .Single();

        var hostCapReached = hostCaps.HelpsReceivedDate.HasValue
                              && DateOnly.FromDateTime(hostCaps.HelpsReceivedDate.Value) == day
                              && hostCaps.HelpsReceivedToday >= HostHelpCapPerDay;

        var hasActiveWork = GetActiveWorkCandidates(hostPlayerId, date).Length > 0;

        var canHelp = guestPlayerId != hostPlayerId
                      && guestVillageName != null
                      && _villageLevelCalculator.GetLevel(guestPlayerId).Level >= HelpUnlockLevel
                      && !alreadyHelpedToday
                      && !hostCapReached
                      && hasActiveWork;

        return new()
        {
            CanHelp = canHelp,
            AlreadyHelpedToday = alreadyHelpedToday,
            HostCapReached = hostCapReached,
            HasActiveWork = hasActiveWork,
            UnlockLevel = HelpUnlockLevel,
        };
    }

    private HelpCandidate[] GetActiveWorkCandidates(int hostPlayerId, DateTime date)
    {
        var hostDomiks = _context.Domiks.Where(x => x.PlayerId == hostPlayerId).ToArray();

        var domikCandidates = hostDomiks
            .Where(x => x.UpgradeSeconds != null)
            .Select(x => new HelpCandidate(CalculateTypes.Domiks, x.Id, x.TypeId, x.UpgradeCalculateDate!.Value.AddSeconds(x.UpgradeSeconds!.Value)));

        var manufactureCandidates = _context.Manufactures
            .Where(x => x.DomikPlayerId == hostPlayerId)
            .ToArray()
            .Select(x => new HelpCandidate(CalculateTypes.Manufacture, x.Id, hostDomiks.First(d => d.Id == x.DomikId).TypeId, x.FinishDate));

        return domikCandidates.Concat(manufactureCandidates).Where(x => x.FinishDate > date).ToArray();
    }

    private sealed record HelpCandidate(CalculateTypes Type, long ObjectId, int DomikTypeId, DateTime FinishDate);
}
