using Domiki.Web.Activities;
using Domiki.Web.Core.Scheduling;
using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Economy;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village;
using Domiki.Web.Village.Models;
using Domiki.Web.Workers;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.RegularExpressions;
using Domik = Domiki.Web.Core.Models.Domik;
using DomikType = Domiki.Web.Core.Models.DomikType;
using Manufacture = Domiki.Web.Core.Models.Manufacture;
using PlayerDecor = Domiki.Web.Village.Models.PlayerDecor;
using Resource = Domiki.Web.Reference.Models.Resource;

namespace Domiki.Web.Core;

public class DomikManager
{
    public const int FatigueThresholdSeconds = 8 * 3600;
    public const int RestSeconds = 2 * 3600;
    public const int RestComfortMaxPercent = 50;
    /// <summary>
    /// Процент шанса хвори за каждую единицу ресурса, добытую сверх базового выхода благодаря погоде.
    /// </summary>
    public const int SickChancePerExtraUnit = 4;

    /// <summary>
    /// Наибольший шанс хвори, который может быть зафиксирован для смены.
    /// </summary>
    public const int MaxSickChancePercent = 15;

    /// <summary>
    /// Наименьший шанс хвори для трудяги, прикрытого плащом.
    /// </summary>
    public const int MinSickChancePercent = 2;

    /// <summary>
    /// Число смен, после которого плащ изнашивается.
    /// </summary>
    public const int CloakLifetimeShifts = 50;

    /// <summary>
    /// Процент от исходного риска хвори, остающийся под защитой плаща.
    /// </summary>
    public const int CloakProtectionPercent = 50;

    /// <summary>
    /// Идентификатор типа ресурса «Плащ».
    /// </summary>
    public const int CloakResourceTypeId = 20;
    public const int SickDurationSeconds = 8 * 3600;
    public const int MaxSickPerPlayer = 2;
    public const int SickImmunitySeconds = 24 * 3600;
    public const int SickMinVillageLevel = 15;
    public const int StartingCoins = 200;
    public const int ZealStartCharges = 24;
    public const int ZealX4Threshold = 16;
    public const int ZealMaxRecipeSeconds = 3600;
    private const int CrestIconCount = 8;
    private const int CrestColorCount = 8;
    private const int InstaFinishSecondsPerGold = 3600;
    private const int InstaFinishMaxGold = 6;
    private const int GoldResourceTypeId = 5;
    private const int StartingBarracksTypeId = 2;
    private const int StartingClayMineTypeId = 5;

    private static readonly Regex SpaceRegex = new(" +", RegexOptions.Compiled);

    private static readonly string[] VillageNameForbiddenWords =
    {
        "admin",
        "moderator",
        "keep",
        "domiki",
        "fuck",
        "shit",
        "админ",
        "модератор",
        "домики",
        "хуй",
        "пизд",
        "еба",
        "ёба",
        "бля",
        "сука",
    };

    private readonly ApplicationDbContext _context;
    private readonly ICalculator _calculator;
    private readonly UnitOfWork _uow;
    private readonly ResourceManager _resourceManager;
    private readonly PlayerResourceManager _playerResourceManager;
    private readonly WorkerManager _workerManager;
    private readonly TavernManager _tavernManager;
    private readonly WeatherManager _weatherManager;
    private readonly VillageLevelCalculator _villageLevelCalculator;
    private readonly BlueprintManager _blueprintManager;
    private readonly TolokaManager _tolokaManager;
    private readonly PlayerEventManager _playerEventManager;
    private readonly GoalManager _goalManager;
    private readonly ILogger<DomikManager> _logger;
    private readonly IncidentManager _incidentManager;
    private readonly ElderHouseManager _elderHouseManager;
    private readonly PerkManager _perkManager;

    public DomikManager(UnitOfWork uow, ApplicationDbContext context, ICalculator calculator, ResourceManager resourceManager, PlayerResourceManager playerResourceManager, WorkerManager workerManager, TavernManager tavernManager, WeatherManager weatherManager, VillageLevelCalculator villageLevelCalculator, BlueprintManager blueprintManager, TolokaManager tolokaManager, PlayerEventManager playerEventManager, GoalManager goalManager, ILogger<DomikManager> logger, IncidentManager incidentManager, ElderHouseManager elderHouseManager, PerkManager perkManager)
    {
        _context = context;
        _calculator = calculator;
        _uow = uow;
        _resourceManager = resourceManager;
        _playerResourceManager = playerResourceManager;
        _workerManager = workerManager;
        _tavernManager = tavernManager;
        _weatherManager = weatherManager;
        _villageLevelCalculator = villageLevelCalculator;
        _blueprintManager = blueprintManager;
        _tolokaManager = tolokaManager;
        _playerEventManager = playerEventManager;
        _goalManager = goalManager;
        _logger = logger;
        _incidentManager = incidentManager;
        _elderHouseManager = elderHouseManager;
        _perkManager = perkManager;
    }

    public int GetPlayerId(string aspNetUserId)
    {
        var dbPlayer = _context.Players.FirstOrDefault(x => x.AspNetUserId == aspNetUserId);
        if (dbPlayer == null)
        {
            dbPlayer = new()
            {
                AspNetUserId = aspNetUserId,
                Name = "Держатель домиков",
                VillageStartedDate = DateTimeHelper.GetNowDate(),
            };

            _context.Players.Add(dbPlayer);
            _context.Resources.Add(new()
                { TypeId = 1, Player = dbPlayer, Value = StartingCoins });

            _context.SaveChanges();

            _context.Domiks.Add(new()
                { PlayerId = dbPlayer.Id, Id = 1, TypeId = StartingBarracksTypeId, Level = 1 });

            _context.Domiks.Add(new()
                { PlayerId = dbPlayer.Id, Id = 2, TypeId = StartingClayMineTypeId, Level = 1 });

            _context.SaveChanges();
        }

        return dbPlayer.Id;
    }

    public VillageState GetVillage(int playerId)
    {
        var dbPlayer = _context.Players.Single(x => x.Id == playerId);
        return new()
        {
            VillageName = dbPlayer.VillageName,
            CrestIcon = dbPlayer.CrestIcon,
            CrestColor = dbPlayer.CrestColor,
            ProfileNeighborId = dbPlayer.ProfileNeighborId,
            ProfileChangeAvailableDate = dbPlayer.ProfileChangedDate?.AddDays(VillageProfileManager.ProfileChangeCooldownDays),
        };
    }

    /// <summary>
    /// Возвращает, сколько золота игрок уже добыл рудником за текущие сутки UTC.
    /// </summary>
    /// <remarks>
    /// Суточный кап добычи равен уровню рудника (см. <see cref="FinishManufacture"/>); счётчик живёт в
    /// <see cref="Data.Entities.Player.GoldMinedToday"/> и здесь нормализуется по дате – за прошлые сутки отдаётся ноль.
    /// </remarks>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <returns>Число единиц золота, добытых за сегодня.</returns>
    public int GetGoldMinedToday(int playerId)
    {
        var dbPlayer = _context.Players.Single(x => x.Id == playerId);
        return dbPlayer.GoldMinedDate == DateTimeHelper.GetNowDate().Date ? dbPlayer.GoldMinedToday : 0;
    }

    /// <summary>
    /// Возвращает остаток суточной жилы рудника, если он вообще ограничивает стартующую смену.
    /// </summary>
    /// <remarks>
    /// Смена, завершающаяся после полуночи UTC, берёт из жилы новых суток и жилой сегодняшних не ограничена.
    /// </remarks>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="dbDomik">Рудник, в котором стартует смена.</param>
    /// <param name="receipt">Рецепт стартующей смены.</param>
    /// <param name="date">Момент старта в UTC.</param>
    /// <param name="finishDate">Ожидаемый момент завершения смены в UTC.</param>
    /// <returns>Остаток жилы на сегодня; <see langword="null"/> – жила смену не ограничивает.</returns>
    private int? GetGoldVeinRemaining(int playerId, Data.Entities.Domik dbDomik, Reference.Models.Receipt receipt, DateTime date, DateTime finishDate)
    {
        if (receipt.OutputResources.All(x => x.Type.Id != GoldResourceTypeId) || finishDate.Date > date.Date)
        {
            return null;
        }

        var dbPlayer = _context.Players.First(x => x.Id == playerId);
        var minedToday = dbPlayer.GoldMinedDate == date.Date ? dbPlayer.GoldMinedToday : 0;
        return dbDomik.Level - minedToday;
    }

    /// <summary>
    /// Проверяет, хватит ли суточной жилы рудника на ещё одну золотую смену.
    /// </summary>
    /// <remarks>
    /// Счётчик добытого – общий на игрока (см. <see cref="Data.Entities.Player.GoldMinedToday"/>), поэтому занятым
    /// считается остаток, который заберут идущие золотые смены **всех** рудников двора, а не только этого.
    /// Просроченная смена (планировщик ещё не довёл её до конца) тоже держит свою долю: она заберёт золото сегодня.
    /// </remarks>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="dbDomik">Рудник, в котором стартует смена.</param>
    /// <param name="receipt">Рецепт стартующей смены.</param>
    /// <param name="date">Момент старта в UTC.</param>
    /// <param name="finishDate">Ожидаемый момент завершения смены в UTC.</param>
    /// <param name="excludeManufactureId">Смена, которую не считать занявшей остаток – та, что сама сейчас завершается.</param>
    /// <returns>Текст причины блокировки для игрока; <see langword="null"/> – смену можно запускать.</returns>
    private string? GetGoldVeinBlockReason(int playerId, Data.Entities.Domik dbDomik, Reference.Models.Receipt receipt, DateTime date, DateTime finishDate, int? excludeManufactureId = null)
    {
        if (GetGoldVeinRemaining(playerId, dbDomik, receipt, date, finishDate) is not int remaining)
        {
            return null;
        }

        if (remaining <= 0)
        {
            return $"Жила на сегодня выбрана – новая через {GetHoursToFreshVein(date)} ч";
        }

        var receipts = _resourceManager.GetReceipts().ToDictionary(x => x.Id);
        var tomorrow = date.Date.AddDays(1);
        var reserved = _context.Manufactures
            .Where(x => x.DomikPlayerId == playerId && x.FinishDate < tomorrow && x.Id != excludeManufactureId)
            .AsEnumerable()
            .Sum(x => receipts.TryGetValue(x.ReceiptId, out var running)
                ? running.OutputResources.Where(r => r.Type.Id == GoldResourceTypeId).Sum(r => r.Value)
                : 0);

        return remaining <= reserved
            ? "Остаток жилы уже на вороте – его заберёт смена, что сейчас идёт"
            : null;
    }

    /// <summary>
    /// Считает, через сколько часов рудник получит свежую жилу – до ближайшей полуночи UTC, но не меньше часа.
    /// </summary>
    /// <param name="date">Момент в UTC, от которого считается ожидание.</param>
    /// <returns>Число часов до новой жилы.</returns>
    private static int GetHoursToFreshVein(DateTime date)
    {
        return Math.Max(1, (int)Math.Ceiling((date.Date.AddDays(1) - date).TotalHours));
    }

    public void SetVillageIdentity(int playerId, string? name, int crestIcon, int crestColor)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);

        var villageName = NormalizeVillageName(name);
        ValidateVillageName(villageName);
        ValidateCrest(crestIcon, crestColor);

        if (_context.Players.Any(x => x.Id != playerId && x.VillageName == villageName))
        {
            throw new BusinessException("Имя деревни занято");
        }

        var dbPlayer = _context.Players.Single(x => x.Id == playerId);
        dbPlayer.VillageName = villageName;
        dbPlayer.CrestIcon = crestIcon;
        dbPlayer.CrestColor = crestColor;

        try
        {
            _context.SaveChanges();
        }
        catch (DbUpdateException ex) when (IsVillageNameUniqueViolation(ex))
        {
            throw new BusinessException("Имя деревни занято");
        }
    }

    public IEnumerable<(DomikType Type, int AvailableCount, int? NextCountGateLevel)> GetPurchaseAvailableDomiks(int playerId)
    {
        var available = new List<(DomikType Type, int AvailableCount, int? NextCountGateLevel)>();
        var domiks = GetDomiks(playerId);
        var gates = _resourceManager.GetDomikTypeCountGates();
        var villageLevel = _villageLevelCalculator.GetLevel(playerId).Level;
        foreach (var domikType in _resourceManager.GetDomikTypes())
        {
            var current = domiks.Count(x => x.Type.Id == domikType.Id);
            var typeGates = gates.Where(x => x.DomikTypeId == domikType.Id).ToDictionary(x => x.Ordinal, x => x.UnlockLevel);

            var allowed = domikType.MaxCount;
            for (var ordinal = 2; ordinal <= domikType.MaxCount; ordinal++)
            {
                if (typeGates.TryGetValue(ordinal, out var gateLevel) && gateLevel > villageLevel)
                {
                    allowed = ordinal - 1;
                    break;
                }
            }

            allowed = Math.Max(allowed, current);

            var availableCount = Math.Max(0, Math.Min(domikType.MaxCount, allowed) - current);
            int? nextCountGateLevel = null;
            if (typeGates.TryGetValue(current + 1, out var nextGateLevel) && nextGateLevel > villageLevel)
            {
                nextCountGateLevel = nextGateLevel;
            }

            if (availableCount > 0 || nextCountGateLevel != null)
            {
                available.Add((domikType, availableCount, nextCountGateLevel));
            }
        }

        return available;
    }

    public IEnumerable<Domik> GetDomiks(int playerId)
    {
        var manufactureGroups = _context.Manufactures.Where(x => x.DomikPlayerId == playerId)
            .ToArray()
            .GroupBy(x => x.DomikId);

        var domikTypes = _resourceManager.GetDomikTypes();
        return _context.Domiks.Where(x => x.PlayerId == playerId)
            .OrderBy(x => x.TypeId)
            .ThenBy(x => x.Id)
            .ToArray()
            .Select(domik =>
                new Domik
                {
                    Id = domik.Id,
                    Type = domikTypes.First(y => y.Id == domik.TypeId),
                    Level = domik.Level,
                    FinishDate = domik.UpgradeSeconds == null ? null : domik.UpgradeCalculateDate!.Value.AddSeconds((int)domik.UpgradeSeconds),
                    UpgradeSeconds = (int?)domik.UpgradeSeconds,
                    Manufactures = manufactureGroups.FirstOrDefault(m => m.Key == domik.Id)
                        ?.Select(x => new Manufacture
                        {
                            Id = x.Id,
                            FinishDate = x.FinishDate,
                            DurationSeconds = x.DurationSeconds,
                            PlodderCount = x.PlodderCount,
                            ReceiptId = x.ReceiptId,
                            AutoRepeat = x.AutoRepeat,
                            MeasureResourceTypeId = x.MeasureResourceTypeId,
                            MeasureValue = x.MeasureValue,
                        })
                        .ToArray() ?? [],
                })
            .ToList();
    }

    public void BuyDomik(int playerId, int typeId)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);
        var available = GetPurchaseAvailableDomiks(playerId).ToArray();
        var entry = available.FirstOrDefault(x => x.Type.Id == typeId);
        if (entry.Type != null && entry.AvailableCount > 0)
        {
            var domikType = entry.Type;
            if (!_villageLevelCalculator.CanBuyDomik(playerId, domikType))
            {
                throw new BusinessException($"Откроется при обжитости {domikType.UnlockLevel}");
            }

            _blueprintManager.EnsureBlueprints(playerId);
            var blueprint = _resourceManager.GetBlueprints().FirstOrDefault(x => x.DomikTypeId == typeId);
            if (blueprint != null && !_blueprintManager.IsOwned(playerId, blueprint.Id))
            {
                var neighbor = _resourceManager.GetNeighbors().First(x => x.Id == blueprint.NeighborId);
                throw new BusinessException($"Нужен чертёж (репутация {neighbor.Name} {blueprint.ReputationThreshold})");
            }

            var domikLevel = domikType.Levels.First(x => x.Value == 1);
            _playerResourceManager.WriteOffResources(playerId, domikLevel.Resources);

            var currentId = _context.Domiks.Where(x => x.PlayerId == playerId).Max(x => (int?)x.Id) ?? 0;
            var nextId = currentId + 1;
            var date = DateTimeHelper.GetNowDate();
            _context.Domiks.Add(new()
                { PlayerId = playerId, TypeId = typeId, Level = 0, Id = nextId, UpgradeSeconds = domikLevel.UpgradeSeconds, UpgradeCalculateDate = date });

            _uow.AfterEventAction = () =>
            {
                _calculator.Insert(new()
                {
                    PlayerId = playerId,
                    ObjectId = nextId,
                    Type = CalculateTypes.Domiks,
                    Date = date.AddSeconds(domikLevel.UpgradeSeconds),
                });
            };
        }
        else if (entry.Type != null && entry.NextCountGateLevel != null)
        {
            throw new BusinessException($"Постройка «{entry.Type.Name}» откроется при обжитости {entry.NextCountGateLevel}");
        }
        else
        {
            throw new BusinessException("Превышено максимальное количество");
        }
    }

    public void UpgradeDomik(int playerId, int id)
    {
        var date = DateTimeHelper.GetNowDate();

        _playerResourceManager.LockDbPlayerRow(playerId);

        var dbDomik = _context.Domiks.First(x => x.PlayerId == playerId && x.Id == id);
        var domikType = _resourceManager.GetDomikTypes().First(x => x.Id == dbDomik.TypeId);
        if (dbDomik.Level >= domikType.MaxLevel)
        {
            throw new BusinessException("Максимальный уровень");
        }

        if (dbDomik.UpgradeSeconds != null)
        {
            throw new BusinessException("Домик уже улучшается");
        }

        var nextLevel = dbDomik.Level + 1;
        var domikLevel = domikType.Levels.First(x => x.Value == nextLevel);
        _playerResourceManager.WriteOffResources(playerId, domikLevel.Resources);
        dbDomik.UpgradeSeconds = domikLevel.UpgradeSeconds;
        dbDomik.UpgradeCalculateDate = date;

        _uow.AfterEventAction = () =>
        {
            _calculator.Insert(new()
            {
                PlayerId = playerId,
                ObjectId = dbDomik.Id,
                Type = CalculateTypes.Domiks,
                Date = date.AddSeconds(domikLevel.UpgradeSeconds),
            });
        };
    }

    public IEnumerable<Resource> GetResources(int playerId)
    {
        var resourceTypes = _resourceManager.GetResourceTypes().ToDictionary(x => x.Id, x => x);

        return _context.Resources.Where(x => x.PlayerId == playerId)
            .ToArray()
            .Select(x =>
                new Resource
                {
                    Type = resourceTypes[x.TypeId],
                    Value = x.Value,
                })
            .ToList();
    }

    public bool FinishDomik(DateTime date, CalculateInfo calcInfo)
    {
        _playerResourceManager.LockDbPlayerRow(calcInfo.PlayerId);

        var dbDomik = _context.Domiks.SingleOrDefault(x => x.Id == calcInfo.ObjectId && x.PlayerId == calcInfo.PlayerId);
        if (dbDomik == null)
        {
            _logger.LogWarning("FinishDomik: домик {DomikId} игрока {PlayerId} не найден – осиротевшее событие снимается", calcInfo.ObjectId, calcInfo.PlayerId);
            return true;
        }

        if (dbDomik.UpgradeSeconds != null)
        {
            var period = (date - dbDomik.UpgradeCalculateDate!.Value).TotalSeconds;
            var lostTime = dbDomik.UpgradeSeconds - period;
            if (lostTime <= 0)
            {
                dbDomik.UpgradeCalculateDate = null;
                dbDomik.UpgradeSeconds = null;
                dbDomik.Level++;
                _playerEventManager.Record(calcInfo.PlayerId, PlayerEventType.DomikUpgraded, new { domikTypeId = dbDomik.TypeId, level = dbDomik.Level });
                var dbPlayer = _context.Players.Single(x => x.Id == calcInfo.PlayerId);
                var villageLevel = _villageLevelCalculator.GetLevel(calcInfo.PlayerId).Level;
                var weatherLogicName = _weatherManager.GetCurrentPeriod(date)?.WeatherType.LogicName;
                var incidentCalcInfo = _incidentManager.TryStartDomikIncident(dbPlayer, dbDomik.TypeId, villageLevel, weatherLogicName, date);

                var domikName = _resourceManager.GetDomikTypes().First(x => x.Id == dbDomik.TypeId).Name;
                (calcInfo.PushTitle, calcInfo.PushBody) = (dbDomik.Level % 3) switch
                {
                    0 => ($"«{domikName}»: уровень {dbDomik.Level}!", "Стены крепче, дело шире – заглядывай оценить обновку."),
                    1 => ("Обновка в хозяйстве", $"«{domikName}» теперь уровня {dbDomik.Level} – самое время развернуться пошире."),
                    _ => ("Плотники своё дело знают", $"«{domikName}» – уже уровень {dbDomik.Level}: ладно скроено, крепко сшито."),
                };

                if (incidentCalcInfo != null)
                {
                    (calcInfo.PushTitle, calcInfo.PushBody, calcInfo.PushTag) = (incidentCalcInfo.PushTitle, incidentCalcInfo.PushBody, incidentCalcInfo.PushTag);
                    var afterEventAction = _uow.AfterEventAction;
                    _uow.AfterEventAction = () =>
                    {
                        afterEventAction?.Invoke();
                        _calculator.Insert(incidentCalcInfo);
                    };
                }

                return true;
            }

            return false;
        }

        return true;
    }

    public void HurryDomik(int playerId, int domikId)
    {
        var date = DateTimeHelper.GetNowDate();
        _playerResourceManager.LockDbPlayerRow(playerId);

        var dbDomik = _context.Domiks.SingleOrDefault(x => x.Id == domikId && x.PlayerId == playerId);
        if (dbDomik == null)
        {
            throw new BusinessException("Домик не найден");
        }

        if (dbDomik.UpgradeSeconds == null || dbDomik.UpgradeCalculateDate == null)
        {
            throw new BusinessException("Домик не улучшается");
        }

        var finishDate = dbDomik.UpgradeCalculateDate.Value.AddSeconds((int)dbDomik.UpgradeSeconds);
        var cost = GetInstaFinishCost(finishDate, date);
        if (cost <= 0)
        {
            return;
        }

        WriteOffGold(playerId, cost);
        dbDomik.UpgradeCalculateDate = date.AddSeconds(-(double)dbDomik.UpgradeSeconds);

        FinishDomik(date, new()
        {
            PlayerId = playerId,
            ObjectId = domikId,
            Type = CalculateTypes.Domiks,
            Date = date,
        });

        var previousAfterEventAction = _uow.AfterEventAction;
        _uow.AfterEventAction = () =>
        {
            previousAfterEventAction?.Invoke();
            _calculator.Remove(playerId, domikId, CalculateTypes.Domiks);
        };
    }

    public void StartManufacture(int playerId, int domikId, int receiptId, bool useOptional = false, int[]? workerIds = null, bool autoRepeat = false, int? measureResourceTypeId = null, int? measureValue = null)
    {
        var date = DateTimeHelper.GetNowDate();

        _playerResourceManager.LockDbPlayerRow(playerId);

        var dbManufactures = _context.Manufactures.Where(x => x.DomikPlayerId == playerId);
        var currentManufactureCount = dbManufactures.Where(x => x.DomikId == domikId).Count();

        var workers = _workerManager.EnsureWorkers(playerId);
        var freeWorkers = _workerManager.GetAvailableWorkers(playerId, workers, date);

        var domiks = _context.Domiks.Where(x => x.PlayerId == playerId).ToArray();
        var domikTypes = _resourceManager.GetDomikTypes();

        var dbDomik = domiks.First(x => x.PlayerId == playerId && x.Id == domikId);
        if (dbDomik.Level == 0)
        {
            throw new BusinessException("Домик ещё строится");
        }

        var domikType = domikTypes.First(x => x.Id == dbDomik.TypeId);
        var domikLevel = domikType.Levels.First(x => x.Value == dbDomik.Level);
        var levelReceipt = domikLevel.Receipts.FirstOrDefault(x => x.Id == receiptId) ?? throw new BusinessException("Рецепт больше не доступен на этом уровне");
        var receipt = _resourceManager.GetReceipts().First(x => x.Id == levelReceipt.Id);

        _blueprintManager.EnsureBlueprints(playerId);
        var receiptBlueprint = _resourceManager.GetBlueprints().FirstOrDefault(x => x.ReceiptId == receipt.Id);
        if (receiptBlueprint != null && !_blueprintManager.IsOwned(playerId, receiptBlueprint.Id))
        {
            var blueprintNeighbor = _resourceManager.GetNeighbors().First(x => x.Id == receiptBlueprint.NeighborId);
            throw new BusinessException($"Нужен чертёж (репутация {blueprintNeighbor.Name} {receiptBlueprint.ReputationThreshold})");
        }

        var needPlodderCount = receipt.PlodderCount;
        if (freeWorkers.Length < needPlodderCount)
        {
            throw new BusinessException(WorkerManager.GetNotEnoughWorkersMessage(workers, freeWorkers, date));
        }

        var freeIds = freeWorkers.Select(x => x.Id).ToArray();
        var skillByWorkerId = _context.WorkerSkills
            .Where(x => x.DomikTypeId == domikType.Id && freeIds.Contains(x.WorkerId))
            .ToDictionary(x => x.WorkerId, x => x.Uses);

        var traits = _resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);

        if (domikLevel.MaxManufactureCount < currentManufactureCount + 1)
        {
            throw new BusinessException("Максимальное количество одновременных производств");
        }

        var writeOffResources = receipt.InputResources;
        var duration = receipt.DurationSeconds;
        var useOptionalApplied = useOptional && receipt.OptionalInputResources is not null && receipt.OptionalInputResources.Length > 0;
        if (useOptionalApplied)
        {
            writeOffResources = writeOffResources.Concat(receipt.OptionalInputResources!).ToArray();
        }

        Worker[] selectedWorkers;
        if (workerIds == null || workerIds.Length == 0)
        {
            var autoWorkers = freeWorkers.AsEnumerable();
            if (_villageLevelCalculator.IsSmartAutoUnlocked(playerId))
            {
                autoWorkers = autoWorkers
                    .OrderByDescending(w => -traits[w.TraitId].DurationPercent + WorkerSkillCalculator.GetBonusPercent(skillByWorkerId.GetValueOrDefault(w.Id)))
                    .ThenBy(w => w.Id);
            }
            else
            {
                autoWorkers = autoWorkers.OrderBy(w => w.Id);
            }

            selectedWorkers = autoWorkers
                .Take(needPlodderCount)
                .ToArray();
        }
        else
        {
            if (workerIds.Length != needPlodderCount)
            {
                throw new BusinessException("Неверное число трудяг");
            }

            if (workerIds.Distinct().Count() != workerIds.Length)
            {
                throw new BusinessException("Дублирующиеся трудяги");
            }

            var freeById = freeWorkers.ToDictionary(x => x.Id);
            selectedWorkers = workerIds.Select(id =>
                    freeById.TryGetValue(id, out var w) ? w : throw new BusinessException("Трудяга недоступен"))
                .ToArray();
        }

        var avgSpeedup = selectedWorkers.Average(x => -traits[x.TraitId].DurationPercent);
        duration = (int)Math.Ceiling(duration * (100 - avgSpeedup) / 100);
        var avgSkill = selectedWorkers.Average(x => WorkerSkillCalculator.GetBonusPercent(skillByWorkerId.GetValueOrDefault(x.Id)));
        duration = (int)Math.Ceiling(duration * (100 - avgSkill) / 100);

        var dbPlayer = _context.Players.First(x => x.Id == playerId);
        var profilePercent = dbPlayer.ProfileNeighborId is int profileNeighborId
            ? _resourceManager.GetVillageProfileEffects().FirstOrDefault(x => x.NeighborId == profileNeighborId && x.DomikTypeId == domikType.Id)?.DurationPercent ?? 100
            : 100;
        duration = (int)Math.Ceiling(duration * profilePercent / 100.0);
        duration = (int)Math.Ceiling(duration * _perkManager.GetDurationPercent(playerId) / 100.0);

        duration = Math.Max(duration, (int)Math.Ceiling(receipt.DurationSeconds * 0.6));

        var marketDomikTypeId = _resourceManager.GetDomikTypes().First(x => x.LogicName == "market").Id;
        var zealChargeOwed = false;
        if (receipt.DurationSeconds <= ZealMaxRecipeSeconds && dbDomik.TypeId != marketDomikTypeId)
        {
            if (dbPlayer.ZealCharges > 0)
            {
                var mult = dbPlayer.ZealCharges > ZealX4Threshold ? 4 : 2;
                duration = Math.Max(1, duration / mult);
                zealChargeOwed = true;
            }
        }

        var weatherPercent = _weatherManager.GetOutputPercent(date, domikType.Id);
        var tolokaPercent = _tolokaManager.GetTolokaOutputPercent(playerId, domikType.Id, date);
        var outputPercent = (int)Math.Round(weatherPercent * tolokaPercent / 100.0);
        if (useOptionalApplied)
        {
            outputPercent += receipt.OutputBonusPercent;
        }

        var weatherExtra = receipt.OutputResources.Sum(x => GetOutputGrant(x.Value, weatherPercent) - x.Value);
        var sickType = weatherExtra > 0 && _villageLevelCalculator.GetLevel(playerId).Level >= SickMinVillageLevel
            ? _weatherManager.GetCurrentPeriod(date) is { } weatherPeriod
                ? _resourceManager.GetSickTypes().FirstOrDefault(x => x.WeatherTypeId == weatherPeriod.WeatherType.Id)
                : null
            : null;
        var sickChance = sickType == null
            ? 0
            : Math.Min(MaxSickChancePercent, (int)Math.Round(SickChancePerExtraUnit * weatherExtra / (double)selectedWorkers.Length, MidpointRounding.AwayFromZero));
        var cloakCount = 0;
        if (sickChance > 0 && sickType?.CloakProtects == true)
        {
            var cloakStock = _context.Resources.Where(x => x.PlayerId == playerId && x.TypeId == CloakResourceTypeId).Select(x => (int?)x.Value).FirstOrDefault() ?? 0;
            var cloaksOut = _context.Manufactures.Where(x => x.DomikPlayerId == playerId).Sum(x => (int?)x.CloakCount) ?? 0;
            var eligibleWorkerCount = selectedWorkers.Count(x => !traits[x.TraitId].NoSick && !traits[x.TraitId].NoFatigue);
            cloakCount = Math.Min(eligibleWorkerCount, Math.Max(0, cloakStock - cloaksOut));
        }

        if (GetGoldVeinBlockReason(playerId, dbDomik, receipt, date, date.AddSeconds(duration)) is string goldVeinBlockReason)
        {
            throw new BusinessException(goldVeinBlockReason);
        }

        _playerResourceManager.WriteOffResources(playerId, writeOffResources);

        if (zealChargeOwed)
        {
            dbPlayer.ZealCharges--;
        }

        var manufacture = new Data.Entities.Manufacture
        {
            DomikId = domikId,
            DomikPlayerId = playerId,
            ReceiptId = receiptId,
            FinishDate = date.AddSeconds(duration),
            DurationSeconds = duration,
            PlodderCount = needPlodderCount,
            OutputPercent = outputPercent,
            AutoRepeat = autoRepeat,
            MeasureResourceTypeId = measureResourceTypeId,
            MeasureValue = measureValue,
            UseOptional = useOptionalApplied,
            SickChance = sickChance,
            SickTypeId = sickType?.Id,
            CloakCount = cloakCount,
        };

        _context.Manufactures.Add(manufacture);
        _context.SaveChanges();
        foreach (var worker in selectedWorkers)
        {
            worker.ManufactureId = manufacture.Id;
        }

        _uow.AfterEventAction = () =>
        {
            _calculator.Insert(new()
            {
                PlayerId = playerId,
                ObjectId = manufacture.Id,
                Type = CalculateTypes.Manufacture,
                Date = manufacture.FinishDate,
            });
        };

        _goalManager.OnManufactureStarted(playerId, receipt);
    }

    public bool FinishManufacture(DateTime date, CalculateInfo calcInfo)
    {
        _playerResourceManager.LockDbPlayerRow(calcInfo.PlayerId);

        var dbManufacture = _context.Manufactures.SingleOrDefault(x => x.Id == calcInfo.ObjectId);
        if (dbManufacture == null)
        {
            _logger.LogWarning("FinishManufacture: производство {ManufactureId} игрока {PlayerId} не найдено – осиротевшее событие снимается", calcInfo.ObjectId, calcInfo.PlayerId);
            return true;
        }

        if (date >= dbManufacture.FinishDate)
        {
            var recept = _resourceManager.GetReceipts().First(x => x.Id == dbManufacture.ReceiptId);
            var receiptId = dbManufacture.ReceiptId;
            var useOptional = dbManufacture.UseOptional;
            var autoRepeat = dbManufacture.AutoRepeat;
            var measureResourceTypeId = dbManufacture.MeasureResourceTypeId;
            var measureValue = dbManufacture.MeasureValue;
            var domikId = dbManufacture.DomikId;
            var playerId = calcInfo.PlayerId;
            var dbDomik = _context.Domiks.SingleOrDefault(x => x.PlayerId == calcInfo.PlayerId && x.Id == dbManufacture.DomikId);
            var dbPlayer = _context.Players.FirstOrDefault(x => x.Id == calcInfo.PlayerId);
            if (dbDomik == null || dbPlayer == null)
            {
                _logger.LogWarning("FinishManufacture: производство {ManufactureId} осиротело (игрок {PlayerId} или домик {DomikId} удалён) – событие снимается", dbManufacture.Id, calcInfo.PlayerId, dbManufacture.DomikId);
                return true;
            }
            var produced = new Dictionary<int, int>();
            foreach (var resource in recept.OutputResources)
            {
                var granted = GetOutputGrant(resource.Value, dbManufacture.OutputPercent);
                if (resource.Type.Id == GoldResourceTypeId)
                {
                    var today = date.Date;
                    if (dbPlayer.GoldMinedDate != today)
                    {
                        dbPlayer.GoldMinedDate = today;
                        dbPlayer.GoldMinedToday = 0;
                    }

                    var allowed = Math.Max(0, dbDomik.Level - dbPlayer.GoldMinedToday);
                    granted = Math.Min(granted, allowed);
                    dbPlayer.GoldMinedToday += granted;
                    if (granted == 0)
                    {
                        continue;
                    }
                }

                _playerResourceManager.GrantResource(calcInfo.PlayerId, resource.Type.Id, granted);
                if (produced.TryGetValue(resource.Type.Id, out var value))
                {
                    produced[resource.Type.Id] = value + granted;
                }
                else
                {
                    produced[resource.Type.Id] = granted;
                }
            }

            var comfort = DecorCalculator.GetComfort(_context.PlayerDecors.Where(x => x.PlayerId == dbManufacture.DomikPlayerId).Select(x => new PlayerDecor { DecorTypeId = x.DecorTypeId, Count = x.Count }).ToArray(),
                _resourceManager.GetDecorTypes());

            var restSeconds = RestSeconds * (100 - Math.Min(RestComfortMaxPercent, comfort)) / 100;
            var sickSeconds = SickDurationSeconds * (100 - Math.Min(RestComfortMaxPercent, comfort)) / 100;
            var tavernLevel = _tavernManager.GetLevel(playerId);
            if (tavernLevel >= TavernManager.WarmCornerMinLevel)
            {
                sickSeconds = sickSeconds * (100 - TavernManager.WarmCornerRecoveryPercent) / 100;
            }

            var traits = _resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);
            var sickChance = dbManufacture.SickChance;
            var currentlySick = _context.Workers.Count(x => x.PlayerId == playerId && x.SickUntil > date);
            var isTradeDomik = _resourceManager.GetDomikTypes().First(x => x.LogicName == "market").Id == dbDomik.TypeId;
            var assignedWorkers = _context.Workers.Where(x => x.ManufactureId == dbManufacture.Id).OrderBy(x => x.Id).ToArray();
            var eligibleWorkerIndex = 0;
            for (var workerIndex = 0; workerIndex < assignedWorkers.Length; workerIndex++)
            {
                var worker = assignedWorkers[workerIndex];
                var trait = traits[worker.TraitId];
                IncrementWorkerSkill(worker.Id, dbDomik.TypeId);
                if (!trait.NoFatigue && !isTradeDomik)
                {
                    worker.WorkedSeconds += dbManufacture.DurationSeconds;
                    if (worker.WorkedSeconds >= FatigueThresholdSeconds)
                    {
                        var food = tavernLevel >= TavernManager.MealMinLevel
                            ? _tavernManager.CollectFood(playerId, 1)
                            : [];
                        var fed = food.Length > 0;
                        if (fed)
                        {
                            _playerResourceManager.WriteOffResources(playerId, food);
                            _tavernManager.RegisterMeal(playerId, food);
                        }

                        if (tavernLevel >= TavernManager.MealMinLevel)
                        {
                            var mealReason = fed ? null : (_tavernManager.HasForbiddenOrReservedFood(playerId) ? "forbidden" : "empty");
                            _playerEventManager.RecordWorkerMeal(playerId, fed ? worker.Name : null, fed ? (int)NameGrammar.GenderOf(worker.Name) : null, mealReason, food.ToDictionary(x => x.Type.Id, x => x.Value));
                        }

                        worker.RestUntil = date.AddSeconds(fed ? restSeconds / 2 : restSeconds);
                        worker.WorkedSeconds = 0;
                    }
                }

                var canGetSick = !trait.NoSick && !trait.NoFatigue;
                var workerSickChance = canGetSick
                    ? GetWorkerSickChance(sickChance, dbManufacture.CloakCount, eligibleWorkerIndex++)
                    : 0;
                if (workerSickChance > 0
                    && currentlySick < MaxSickPerPlayer
                    && (worker.SickUntil == null || date >= worker.SickUntil.Value.AddSeconds(SickImmunitySeconds))
                    && Random.Shared.Next(100) < workerSickChance)
                {
                    worker.SickUntil = date.AddSeconds(sickSeconds);
                    worker.SickTypeId = dbManufacture.SickTypeId;
                    if (worker.RestUntil == null || worker.RestUntil < worker.SickUntil)
                    {
                        worker.RestUntil = worker.SickUntil;
                    }

                    currentlySick++;
                }

                worker.ManufactureId = null;
            }

            dbPlayer.CloakWearPoints += dbManufacture.CloakCount;
            var cloakStock = _context.Resources.Local.FirstOrDefault(x => x.PlayerId == playerId && x.TypeId == CloakResourceTypeId)
                             ?? _context.Resources.FirstOrDefault(x => x.PlayerId == playerId && x.TypeId == CloakResourceTypeId);
            while (dbPlayer.CloakWearPoints >= CloakLifetimeShifts && cloakStock?.Value > 0)
            {
                _playerResourceManager.WriteOffResources(playerId, [new Resource { Type = new() { Id = CloakResourceTypeId }, Value = 1 }]);
                dbPlayer.CloakWearPoints -= CloakLifetimeShifts;
                _playerEventManager.Record(playerId, PlayerEventType.CloakWornOut, new { resourceTypeId = CloakResourceTypeId, value = 1 });
            }

            if (cloakStock == null || cloakStock.Value <= 0)
            {
                dbPlayer.CloakWearPoints = 0;
            }

            _context.Manufactures.Remove(dbManufacture);
            _elderHouseManager.RecordShift(playerId, dbManufacture.FinishDate, dbManufacture.DurationSeconds, 1);
            _playerEventManager.RecordManufactureFinished(calcInfo.PlayerId, dbDomik.TypeId, produced);

            var manufactureDomikName = _resourceManager.GetDomikTypes().First(x => x.Id == dbDomik.TypeId).Name;
            calcInfo.PushTitle = null;
            calcInfo.PushBody = null;
            calcInfo.PushTag = PushSender.ProductionTag;

            if (autoRepeat)
            {
                _context.SaveChanges();
                string? repeatStopBody = null;
                if (measureResourceTypeId is int measureType && measureValue is int measureTarget
                    && _elderHouseManager.IsMeasureMet(playerId, measureType, measureTarget))
                {
                    _playerEventManager.Record(playerId, PlayerEventType.ManufactureMeasureMet, new { domikId, domikTypeId = dbDomik.TypeId, receiptId, resourceTypeId = measureType, value = measureTarget });
                    repeatStopBody = $"«{manufactureDomikName}»: достигнута заданная мера запаса.";
                }
                else
                {
                    var manufactureInputs = useOptional && recept.OptionalInputResources is { Length: > 0 }
                        ? recept.InputResources.Concat(recept.OptionalInputResources).ToArray()
                        : recept.InputResources;
                    if (_elderHouseManager.GetHeldResourceTypeId(playerId, manufactureInputs) is int heldResourceTypeId)
                    {
                        _playerEventManager.Record(playerId, PlayerEventType.ManufactureReserveHeld, new { domikId, domikTypeId = dbDomik.TypeId, receiptId, resourceTypeId = heldResourceTypeId });
                        repeatStopBody = $"«{manufactureDomikName}»: наряд остановлен, чтобы сохранить заповедный припас.";
                    }
                    else if (GetGoldVeinRemaining(playerId, dbDomik, recept, date, date.AddSeconds(dbManufacture.DurationSeconds)) <= 0)
                    {
                        _playerEventManager.Record(playerId, PlayerEventType.ManufactureGoldCapReached, new { domikId, domikTypeId = dbDomik.TypeId, receiptId, mined = dbPlayer.GoldMinedToday, cap = dbDomik.Level });
                        repeatStopBody = $"«{manufactureDomikName}»: суточная жила исчерпана; продолжить можно завтра.";
                    }
                    else
                    {
                        try
                        {
                            StartManufacture(playerId, domikId, receiptId, useOptional, null, true, measureResourceTypeId, measureValue);
                        }
                        catch (BusinessException ex)
                        {
                            _playerEventManager.RecordManufactureRepeatFailed(playerId, domikId, dbDomik.TypeId, receiptId, ex.Message);
                            repeatStopBody = ex.Message == WorkerManager.RestingWorkersMessage
                                ? $"«{manufactureDomikName}»: свободных рук нет – трудяги отдыхают."
                                : $"«{manufactureDomikName}»: {ex.Message}";
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "FinishManufacture: наряд {ManufactureId} игрока {PlayerId} не смог возобновиться", dbManufacture.Id, playerId);
                            repeatStopBody = $"«{manufactureDomikName}»: автоповтор не запустился; проверь припасы и свободные руки.";
                        }
                    }
                }

                if (repeatStopBody != null)
                {
                    calcInfo.PushTitle = "Наряд остановлен";
                    calcInfo.PushBody = repeatStopBody;
                }
            }
            else if (produced.Count > 0)
            {
                var resourceNames = _resourceManager.GetResourceTypes().ToDictionary(x => x.Id, x => x.Name);
                var producedList = string.Join(", ", produced.Select(x => x.Value + " × " + resourceNames[x.Key]));
                calcInfo.PushTitle = $"«{manufactureDomikName}» – новая партия";
                calcInfo.PushBody = $"На складе прибыло: {producedList}. Заглянешь запустить ещё?";
            }
            else if (recept.OutputResources.Any(x => x.Type.Id == GoldResourceTypeId))
            {
                calcInfo.PushTitle = "Жила на сегодня выбрана";
                calcInfo.PushBody = $"Намыли {dbPlayer.GoldMinedToday} из {dbDomik.Level} – столько рудник за сутки и отдаёт. Свежая порода подойдёт завтра.";
            }

            return true;
        }

        return false;
    }

    public void HurryManufacture(int playerId, int manufactureId)
    {
        var date = DateTimeHelper.GetNowDate();
        _playerResourceManager.LockDbPlayerRow(playerId);

        var dbManufacture = _context.Manufactures.SingleOrDefault(x => x.Id == manufactureId && x.DomikPlayerId == playerId);
        if (dbManufacture == null)
        {
            throw new BusinessException("Производство не найдено");
        }

        var cost = GetInstaFinishCost(dbManufacture.FinishDate, date);
        if (cost <= 0)
        {
            return;
        }

        var dbHurriedDomik = _context.Domiks.Single(x => x.PlayerId == playerId && x.Id == dbManufacture.DomikId);
        var hurriedReceipt = _resourceManager.GetReceipts().First(x => x.Id == dbManufacture.ReceiptId);
        if (GetGoldVeinBlockReason(playerId, dbHurriedDomik, hurriedReceipt, date, date, dbManufacture.Id) is string hurryBlockReason)
        {
            throw new BusinessException(hurryBlockReason);
        }

        WriteOffGold(playerId, cost);
        dbManufacture.FinishDate = date;

        FinishManufacture(date, new()
        {
            PlayerId = playerId,
            ObjectId = manufactureId,
            Type = CalculateTypes.Manufacture,
            Date = date,
        });

        var afterFinishAction = _uow.AfterEventAction;
        _uow.AfterEventAction = () =>
        {
            afterFinishAction?.Invoke();
            _calculator.Remove(playerId, manufactureId, CalculateTypes.Manufacture);
        };
    }

    public void SetManufactureAutoRepeat(int playerId, int manufactureId, bool autoRepeat)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);

        var dbManufacture = _context.Manufactures.SingleOrDefault(x => x.Id == manufactureId && x.DomikPlayerId == playerId);
        if (dbManufacture == null)
        {
            throw new BusinessException("Производство не найдено");
        }

        dbManufacture.AutoRepeat = autoRepeat;
        if (!autoRepeat)
        {
            dbManufacture.MeasureResourceTypeId = null;
            dbManufacture.MeasureValue = null;
        }
    }

    /// <summary>
    /// Назначает наряду меру или снимает её.
    /// </summary>
    /// <remarks>
    /// Мера – ответ на вопрос «до каких пор повторять»: наряд снимется сам, когда запаса ресурса станет не меньше
    /// <paramref name="value"/>. Открывается уровнем <see cref="ElderHouseManager.MeasureMinLevel"/> Избы старосты.
    /// </remarks>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="manufactureId">Идентификатор смены, на которой стоит наряд.</param>
    /// <param name="resourceTypeId">Тип ресурса меры; <see langword="null"/> снимает меру.</param>
    /// <param name="value">Сколько единиц набрать; <see langword="null"/> снимает меру.</param>
    /// <exception cref="BusinessException">Смены нет, наряд не поставлен, Изба не доросла до меры либо мера бессмысленна.</exception>
    public void SetManufactureMeasure(int playerId, int manufactureId, int? resourceTypeId, int? value)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);

        var dbManufacture = _context.Manufactures.SingleOrDefault(x => x.Id == manufactureId && x.DomikPlayerId == playerId);
        if (dbManufacture == null)
        {
            throw new BusinessException("Производство не найдено");
        }

        if (resourceTypeId == null || value == null)
        {
            dbManufacture.MeasureResourceTypeId = null;
            dbManufacture.MeasureValue = null;
            return;
        }

        if (_elderHouseManager.GetLevel(playerId) < ElderHouseManager.MeasureMinLevel)
        {
            throw new BusinessException("Меру назначать нечем: в Избе старосты нет мерной рейки");
        }

        if (!dbManufacture.AutoRepeat)
        {
            throw new BusinessException("Мера ставится наряду: сперва поставьте наряд");
        }

        if (_resourceManager.GetResourceTypes().All(x => x.Id != resourceTypeId.Value))
        {
            throw new BusinessException("Такого припаса не бывает");
        }

        if (value.Value <= 0)
        {
            throw new BusinessException("Мера должна быть больше нуля");
        }

        dbManufacture.MeasureResourceTypeId = resourceTypeId.Value;
        dbManufacture.MeasureValue = value.Value;
    }

    private void IncrementWorkerSkill(int workerId, int domikTypeId)
    {
        var skill = _context.WorkerSkills.SingleOrDefault(x => x.WorkerId == workerId && x.DomikTypeId == domikTypeId);
        if (skill == null)
        {
            _context.WorkerSkills.Add(new()
            {
                WorkerId = workerId,
                DomikTypeId = domikTypeId,
                Uses = 1,
            });

            return;
        }

        skill.Uses++;
    }

    private int GetInstaFinishCost(DateTime finishDate, DateTime date)
    {
        var remaining = (finishDate - date).TotalSeconds;
        if (remaining <= 0)
        {
            return 0;
        }

        var cost = (int)Math.Ceiling(remaining / InstaFinishSecondsPerGold);
        if (cost > InstaFinishMaxGold)
        {
            throw new BusinessException("До конца ещё далеко");
        }

        return cost;
    }

    private void WriteOffGold(int playerId, int cost)
    {
        var goldType = _resourceManager.GetResourceTypes().First(x => x.Id == GoldResourceTypeId);
        _playerResourceManager.WriteOffResources(playerId, new[]
        {
            new Resource
            {
                Type = goldType,
                Value = cost,
            },
        });
    }

    /// <summary>
    /// Возвращает выдачу ресурса за смену: базовый выход, сдвинутый процентом выхода на целое число единиц. Дробный
    /// остаток сдвига отбрасывается, поэтому на выходе в одну единицу модификатор ничего не меняет, а сама выдача
    /// никогда не опускается ниже одной единицы.
    /// </summary>
    /// <param name="baseValue">Базовый выход ресурса по рецепту.</param>
    /// <param name="outputPercent">Процент выхода, зафиксированный за сменой.</param>
    /// <returns>Число выданных единиц ресурса.</returns>
    public static int GetOutputGrant(int baseValue, int outputPercent)
    {
        return Math.Max(1, baseValue + baseValue * (outputPercent - 100) / 100);
    }

    /// <summary>
    /// Возвращает шанс хвори для трудяги смены с учётом выданного ему плаща.
    /// </summary>
    /// <param name="sickChance">Зафиксированный шанс хвори для смены в процентах.</param>
    /// <param name="cloakCount">Число плащей, выданных на смену.</param>
    /// <param name="workerIndex">Порядковый номер восприимчивого трудяги в смене по идентификатору.</param>
    /// <returns>Шанс хвори трудяги в процентах.</returns>
    internal static int GetWorkerSickChance(int sickChance, int cloakCount, int workerIndex)
    {
        if (sickChance <= 0)
        {
            return 0;
        }

        return workerIndex < cloakCount
            ? Math.Max(MinSickChancePercent, sickChance * CloakProtectionPercent / 100)
            : sickChance;
    }

    private string NormalizeVillageName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        return SpaceRegex.Replace(name.Trim(), " ");
    }

    private void ValidateVillageName(string name)
    {
        if (name.Length < 3 || name.Length > 24)
        {
            throw new BusinessException("Имя деревни должно быть 3–24 символа");
        }

        if (name.Any(x => !IsAllowedVillageNameChar(x)))
        {
            throw new BusinessException("В имени деревни можно использовать буквы, цифры, пробел и дефис");
        }

        var lowerName = name.ToLowerInvariant();
        if (VillageNameForbiddenWords.Any(x => lowerName.Contains(x)))
        {
            throw new BusinessException("Имя деревни содержит запрещённое слово");
        }
    }

    private bool IsAllowedVillageNameChar(char value)
    {
        return value == ' '
               || value == '-'
               || char.IsDigit(value)
               || value >= 'A' && value <= 'Z'
               || value >= 'a' && value <= 'z'
               || value >= 'А' && value <= 'я'
               || value == 'Ё'
               || value == 'ё';
    }

    private void ValidateCrest(int crestIcon, int crestColor)
    {
        if (crestIcon < 0 || crestIcon >= CrestIconCount)
        {
            throw new BusinessException("Неизвестная пиктограмма герба");
        }

        if (crestColor < 0 || crestColor >= CrestColorCount)
        {
            throw new BusinessException("Неизвестный цвет герба");
        }
    }

    private bool IsVillageNameUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException postgresException
               && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
               && postgresException.ConstraintName == "IX_Players_VillageName";
    }
}
