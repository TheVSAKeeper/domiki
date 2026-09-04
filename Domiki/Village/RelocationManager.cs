using Domiki.Web.Core;
using Domiki.Web.Core.Scheduling;
using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village.Models;
using Domiki.Web.Workers;

namespace Domiki.Web.Village;

/// <summary>
/// Переезд в новую долину – полный сброс деревни в обмен на узелки памяти и строку памятного столба.
/// </summary>
/// <remarks>
/// Канон – GAMEDESIGN.md §3 Слой 4 «Престиж». Что едет, что едет частью и что сгорает – список неделимый, любое
/// исключение открывает отмывание прогресса. Сброс обязан снять события игрока из планировщика: очередь
/// <see cref="Calculator"/> живёт в памяти, и удалённые строки оставили бы висящие <see cref="CalculateInfo"/>,
/// поэтому чистка идёт в <see cref="UnitOfWork.AddAfterEventAction"/>, то есть после коммита.
/// </remarks>
public class RelocationManager
{
    /// <summary>
    /// Сколько золота переезжает вместе с артелью – излишек сгорает.
    /// </summary>
    /// <remarks>
    /// Около двух недель крана, хватает на один чертёж (GAMEDESIGN.md §4.4).
    /// </remarks>
    public const int GoldCarryCap = 25;

    /// <summary>
    /// Доля репутации у каждого соседа, которая переезжает.
    /// </summary>
    /// <remarks>
    /// Половина очков, вехи пересчитываются из очков: полный перенос отдал бы половину порога сразу.
    /// </remarks>
    public const int ReputationCarryDivisor = 2;

    /// <summary>
    /// Потолок слагаемого «постройка на своём последнем уровне» в узелках памяти.
    /// </summary>
    public const int MaxLevelBuildingKnotsCap = 3;

    private const int CoinResourceTypeId = 1;
    private const int GoldResourceTypeId = 5;
    private const int MaxEstimatedDays = 999;

    private readonly UnitOfWork _uow;
    private readonly ApplicationDbContext _context;
    private readonly ICalculator _calculator;
    private readonly ResourceManager _resourceManager;
    private readonly PlayerResourceManager _playerResourceManager;
    private readonly VillageLevelCalculator _villageLevelCalculator;
    private readonly PerkManager _perkManager;
    private readonly PlayerEventManager _playerEventManager;
    private readonly DomikManager _domikManager;

    public RelocationManager(UnitOfWork uow, ApplicationDbContext context, ICalculator calculator, ResourceManager resourceManager, PlayerResourceManager playerResourceManager, VillageLevelCalculator villageLevelCalculator, PerkManager perkManager, PlayerEventManager playerEventManager, DomikManager domikManager)
    {
        _uow = uow;
        _context = context;
        _calculator = calculator;
        _resourceManager = resourceManager;
        _playerResourceManager = playerResourceManager;
        _villageLevelCalculator = villageLevelCalculator;
        _perkManager = perkManager;
        _playerEventManager = playerEventManager;
        _domikManager = domikManager;
    }

    /// <summary>
    /// Возвращает состояние переезда: гейт, сводку обеих колонок, узелки и лесенку перков.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="villageLevel">Уже посчитанная обжитость деревни; <see langword="null"/> – посчитать заново.</param>
    /// <returns>Состояние переезда для снимка состояния игры.</returns>
    public Relocation GetState(int playerId, int? villageLevel = null)
    {
        var date = DateTimeHelper.GetNowDate();
        var dbPlayer = _context.Players.Single(x => x.Id == playerId);
        var level = villageLevel ?? _villageLevelCalculator.GetLevel(playerId).Level;
        var threshold = VillageLevelCalculator.GetRelocationThreshold(dbPlayer.RelocationCount);
        var cooldownUntil = dbPlayer.LastRelocationDate?.AddDays(VillageLevelCalculator.RelocationCooldownDays);
        var blockReason = GetBlockReason(playerId, level, threshold, cooldownUntil, date);

        return new()
        {
            Threshold = threshold,
            Level = level,
            EstimatedDays = GetEstimatedDays(level, threshold, dbPlayer.VillageStartedDate, date),
            CooldownUntil = cooldownUntil > date ? cooldownUntil : null,
            CanRelocate = blockReason == null,
            BlockReason = blockReason,
            Knots = dbPlayer.MemoryKnots,
            RelocationCount = dbPlayer.RelocationCount,
            ValleyId = dbPlayer.ValleyId,
            ValleyName = RelocationValleys.Get(dbPlayer.ValleyId).Name,
            Perks = _perkManager.GetPerks(playerId),
        };
    }

    /// <summary>
    /// Возвращает сборы обоза: поимённую сводку обеих колонок, узелки за прожитое и выбор долин.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <returns>Сборы для диалога подтверждения.</returns>
    public RelocationPlan GetPlan(int playerId)
    {
        return new()
        {
            KnotsOnRelocate = ComputeKnots(playerId, _villageLevelCalculator.GetLevel(playerId).Level),
            Summary = GetSummary(playerId),
            Valleys = RelocationValleys.Choices,
        };
    }

    /// <summary>
    /// Возвращает прогоны деревень игрока – отрезки времени, каждый со своим именем, от старейшего к текущему.
    /// </summary>
    /// <remarks>
    /// Принадлежность события деревне выводится из его времени по этим отрезкам, а не из ключа в самой записи: столб
    /// уже хранит <see cref="Data.Entities.VillageChronicle.StartDate"/> и <see cref="Data.Entities.VillageChronicle.EndDate"/>
    /// каждой покинутой деревни, а начало текущей лежит в <see cref="Data.Entities.Player.VillageStartedDate"/>. Поэтому
    /// разделитель переезда в журнале работает и для событий, накопленных до появления этой выдачи.
    /// </remarks>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <returns>Отрезки жизни деревень игрока, от старых к новым.</returns>
    public VillageRun[] GetVillageRuns(int playerId)
    {
        var dbPlayer = _context.Players.Single(x => x.Id == playerId);
        var runs = _context.VillageChronicles
            .Where(x => x.PlayerId == playerId)
            .ToArray()
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.Id)
            .Select(x => new VillageRun
            {
                VillageName = x.VillageName,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
            })
            .ToList();

        runs.Add(new()
        {
            VillageName = dbPlayer.VillageName,
            StartDate = dbPlayer.VillageStartedDate ?? DateTime.MinValue,
            EndDate = null,
        });

        return runs.ToArray();
    }

    /// <summary>
    /// Возвращает памятный столб игрока – прожитые деревни и их итог.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <returns>Личная страница столба.</returns>
    public MemorialPost GetMemorialPost(int playerId)
    {
        var dbPlayer = _context.Players.Single(x => x.Id == playerId);
        var chronicles = _context.VillageChronicles
            .Where(x => x.PlayerId == playerId)
            .ToArray()
            .OrderByDescending(x => x.EndDate)
            .ThenByDescending(x => x.Id)
            .ToArray();

        return new()
        {
            Villages = chronicles.Select(x => new MemorialVillage
                {
                    VillageName = x.VillageName,
                    CrestIcon = x.CrestIcon,
                    CrestColor = x.CrestColor,
                    ValleyId = x.ValleyId,
                    ValleyName = RelocationValleys.Get(x.ValleyId).Name,
                    Level = x.VillageLevel,
                    Knots = x.Knots,
                    LivedDays = (int)Math.Max(0, Math.Round((x.EndDate - x.StartDate).TotalDays, MidpointRounding.AwayFromZero)),
                    Date = x.EndDate,
                })
                .ToArray(),
            LevelSum = chronicles.Sum(x => x.VillageLevel),
            RelocationCount = dbPlayer.RelocationCount,
            FirstDayDate = chronicles.Length > 0
                ? chronicles.Min(x => x.StartDate)
                : dbPlayer.VillageStartedDate,
        };
    }

    /// <summary>
    /// Считает узелки памяти, которые деревня оставит на столбе.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="level">Обжитость деревни на день отъезда.</param>
    /// <returns>Число узелков памяти.</returns>
    /// <remarks>
    /// <c>floor(обжитость / 50)</c> плюс узелок за полный набор артельных украс плюс по узелку за каждую постройку
    /// на последнем уровне, но не больше <see cref="MaxLevelBuildingKnotsCap"/> за постройки.
    /// </remarks>
    public int ComputeKnots(int playerId, int level)
    {
        return level / VillageLevelCalculator.RelocationLevelStep
               + (HasFullArtisanSet(playerId) ? 1 : 0)
               + Math.Min(MaxLevelBuildingKnotsCap, GetMaxLevelBuildingCount(playerId));
    }

    /// <summary>
    /// Переезжает в новую долину: оставляет деревню на памятном столбе и заводит пустой двор.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="valleyId">Долина, выбранная из <see cref="RelocationValleys.Choices"/>.</param>
    /// <param name="villageName">Новое имя деревни; <see langword="null"/> – оставить прежнее.</param>
    public void Relocate(int playerId, int valleyId, string? villageName)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);

        if (!RelocationValleys.IsChoice(valleyId))
        {
            throw new BusinessException("Такой долины нет");
        }

        var date = DateTimeHelper.GetNowDate();
        var dbPlayer = _context.Players.Single(x => x.Id == playerId);
        var level = _villageLevelCalculator.GetLevel(playerId).Level;
        var threshold = VillageLevelCalculator.GetRelocationThreshold(dbPlayer.RelocationCount);
        var cooldownUntil = dbPlayer.LastRelocationDate?.AddDays(VillageLevelCalculator.RelocationCooldownDays);
        var blockReason = GetBlockReason(playerId, level, threshold, cooldownUntil, date);
        if (blockReason != null)
        {
            throw new BusinessException(blockReason);
        }

        var knots = ComputeKnots(playerId, level);
        var pendingEvents = GetPendingEvents(playerId);

        _context.VillageChronicles.Add(new()
        {
            PlayerId = playerId,
            VillageName = dbPlayer.VillageName,
            CrestIcon = dbPlayer.CrestIcon,
            CrestColor = dbPlayer.CrestColor,
            ValleyId = dbPlayer.ValleyId,
            VillageLevel = level,
            Knots = knots,
            StartDate = dbPlayer.VillageStartedDate ?? date,
            EndDate = date,
        });

        var leftVillageName = dbPlayer.VillageName;
        var workerCount = _context.Workers.Count(x => x.PlayerId == playerId);
        var blueprintCount = _context.PlayerBlueprints.Count(x => x.PlayerId == playerId);
        var carriedGold = Math.Min(GoldCarryCap, GetResourceValue(playerId, GoldResourceTypeId));

        BurnVillage(playerId);
        _domikManager.SeedPostHouse(playerId);
        CarryArtel(playerId);
        HalveReputation(playerId);

        dbPlayer.ValleyId = valleyId;
        dbPlayer.VillageStartedDate = date;
        dbPlayer.MemoryKnots += knots;
        dbPlayer.RelocationCount++;
        dbPlayer.LastRelocationDate = date;
        dbPlayer.CloakWearPoints = 0;
        dbPlayer.FriendNeighborId = null;
        dbPlayer.ProfileNeighborId = null;
        dbPlayer.ProfileChangedDate = null;
        dbPlayer.NextOrderRefillAt = null;
        dbPlayer.IntentDomikId = null;

        _playerResourceManager.GrantResource(playerId, CoinResourceTypeId, _perkManager.GetStartingCoins(playerId));
        _playerResourceManager.GrantResource(playerId, GoldResourceTypeId, carriedGold);

        if (villageName != null)
        {
            _domikManager.SetVillageIdentity(playerId, villageName, dbPlayer.CrestIcon, dbPlayer.CrestColor);
        }

        _playerEventManager.Record(playerId, PlayerEventType.Relocated, new
        {
            villageName = leftVillageName,
            valleyId,
            workers = workerCount,
            blueprints = blueprintCount,
            knots,
        });

        _uow.AddAfterEventAction(() =>
        {
            foreach (var pending in pendingEvents)
            {
                _calculator.Remove(playerId, pending.ObjectId, pending.Type);
            }
        });
    }

    private string? GetBlockReason(int playerId, int level, int threshold, DateTime? cooldownUntil, DateTime date)
    {
        if (level < threshold)
        {
            return $"Переезд откроется на обжитости {threshold}";
        }

        if (cooldownUntil > date)
        {
            return $"Собраться в новую долину можно не чаще раза в {VillageLevelCalculator.RelocationCooldownDays} суток";
        }

        if (_context.Workers.Any(x => x.PlayerId == playerId && (x.ExpeditionId != null || x.ErrandId != null || x.IncidentId != null)))
        {
            return "Артель не в сборе – кто-то в походе, на поручении или в происшествии";
        }

        if (_context.TradeLots.Any(x => x.SellerId == playerId))
        {
            return "На Торговом дворе остались ваши лоты";
        }

        return null;
    }

    private static int? GetEstimatedDays(int level, int threshold, DateTime? villageStartedDate, DateTime date)
    {
        if (level >= threshold || level <= 0 || villageStartedDate == null)
        {
            return null;
        }

        var livedDays = (date - villageStartedDate.Value).TotalDays;
        if (livedDays < 1)
        {
            return null;
        }

        var days = (threshold - level) * livedDays / level;
        return (int)Math.Min(MaxEstimatedDays, Math.Ceiling(days));
    }

    private RelocationSummary GetSummary(int playerId)
    {
        var resources = _context.Resources.Where(x => x.PlayerId == playerId).ToArray();
        var gold = resources.FirstOrDefault(x => x.TypeId == GoldResourceTypeId)?.Value ?? 0;

        return new()
        {
            Workers = _context.Workers.Count(x => x.PlayerId == playerId),
            Blueprints = _context.PlayerBlueprints.Count(x => x.PlayerId == playerId),
            Gold = Math.Min(GoldCarryCap, gold),
            GoldTotal = gold,
            Coins = resources.FirstOrDefault(x => x.TypeId == CoinResourceTypeId)?.Value ?? 0,
            Resources = resources.Where(x => x.TypeId != CoinResourceTypeId && x.TypeId != GoldResourceTypeId).Sum(x => x.Value),
            Buildings = _context.Domiks.Count(x => x.PlayerId == playerId && x.TypeId != DomikManager.PostHouseTypeId),
            StartingCoins = _perkManager.GetStartingCoins(playerId),
        };
    }

    private bool HasFullArtisanSet(int playerId)
    {
        var artisanTypeIds = _resourceManager.GetDecorTypes().Where(x => x.MaxCount == 1).Select(x => x.Id).ToArray();
        if (artisanTypeIds.Length == 0)
        {
            return false;
        }

        var owned = _context.PlayerDecors
            .Where(x => x.PlayerId == playerId && x.Count > 0)
            .Select(x => x.DecorTypeId)
            .ToArray();

        return artisanTypeIds.All(owned.Contains);
    }

    private int GetMaxLevelBuildingCount(int playerId)
    {
        var domikTypes = _resourceManager.GetDomikTypes();
        return _context.Domiks.Where(x => x.PlayerId == playerId)
            .ToArray()
            .Count(domik => domik.Level >= domikTypes.First(x => x.Id == domik.TypeId).MaxLevel);
    }

    private int GetResourceValue(int playerId, int resourceTypeId)
    {
        return _context.Resources
            .Where(x => x.PlayerId == playerId && x.TypeId == resourceTypeId)
            .Select(x => (int?)x.Value)
            .FirstOrDefault() ?? 0;
    }

    private CalculateInfo[] GetPendingEvents(int playerId)
    {
        var events = new List<CalculateInfo>();
        events.AddRange(_context.Domiks
            .Where(x => x.PlayerId == playerId && x.UpgradeSeconds != null && x.UpgradeCalculateDate != null)
            .Select(x => x.Id)
            .ToArray()
            .Select(id => new CalculateInfo { PlayerId = playerId, ObjectId = id, Type = CalculateTypes.Domiks }));

        events.AddRange(_context.Manufactures
            .Where(x => x.DomikPlayerId == playerId)
            .Select(x => x.Id)
            .ToArray()
            .Select(id => new CalculateInfo { PlayerId = playerId, ObjectId = id, Type = CalculateTypes.Manufacture }));

        events.AddRange(_context.Orders
            .Where(x => x.PlayerId == playerId)
            .Select(x => x.Id)
            .ToArray()
            .Select(id => new CalculateInfo { PlayerId = playerId, ObjectId = id, Type = CalculateTypes.OrderExpire }));

        var nextOrderRefillAt = _context.Players
            .Where(x => x.Id == playerId)
            .Select(x => x.NextOrderRefillAt)
            .SingleOrDefault();
        if (nextOrderRefillAt != null)
        {
            events.Add(new CalculateInfo
            {
                PlayerId = playerId,
                ObjectId = playerId,
                Date = nextOrderRefillAt.Value,
                Type = CalculateTypes.OrderRefill,
            });
        }

        events.AddRange(_context.Expeditions
            .Where(x => x.PlayerId == playerId)
            .Select(x => x.Id)
            .ToArray()
            .Select(id => new CalculateInfo { PlayerId = playerId, ObjectId = id, Type = CalculateTypes.Expedition }));

        events.AddRange(_context.TradeLots
            .Where(x => x.SellerId == playerId)
            .Select(x => x.Id)
            .ToArray()
            .Select(id => new CalculateInfo { PlayerId = playerId, ObjectId = id, Type = CalculateTypes.TradeLotExpire }));

        events.AddRange(_context.Errands
            .Where(x => x.PlayerId == playerId)
            .Select(x => x.Id)
            .ToArray()
            .Select(id => new CalculateInfo { PlayerId = playerId, ObjectId = id, Type = CalculateTypes.Errand }));

        events.AddRange(_context.Incidents
            .Where(x => x.PlayerId == playerId)
            .Select(x => x.Id)
            .ToArray()
            .Select(id => new CalculateInfo { PlayerId = playerId, ObjectId = id, Type = CalculateTypes.Incident }));

        return events.ToArray();
    }

    private void BurnVillage(int playerId)
    {
        foreach (var worker in _context.Workers.Where(x => x.PlayerId == playerId).ToArray())
        {
            worker.ManufactureId = null;
            worker.ExpeditionId = null;
            worker.ErrandId = null;
            worker.IncidentId = null;
        }

        _context.SaveChanges();

        _context.Manufactures.RemoveRange(_context.Manufactures.Where(x => x.DomikPlayerId == playerId));
        _context.Orders.RemoveRange(_context.Orders.Where(x => x.PlayerId == playerId));
        _context.Expeditions.RemoveRange(_context.Expeditions.Where(x => x.PlayerId == playerId));
        _context.Errands.RemoveRange(_context.Errands.Where(x => x.PlayerId == playerId));
        _context.Incidents.RemoveRange(_context.Incidents.Where(x => x.PlayerId == playerId));
        _context.TradeLots.RemoveRange(_context.TradeLots.Where(x => x.SellerId == playerId));
        _context.SaveChanges();

        _context.Domiks.RemoveRange(_context.Domiks.Where(x => x.PlayerId == playerId));
        _context.Resources.RemoveRange(_context.Resources.Where(x => x.PlayerId == playerId));
        _context.PlayerDecors.RemoveRange(_context.PlayerDecors.Where(x => x.PlayerId == playerId));
        _context.PlayerFoodRules.RemoveRange(_context.PlayerFoodRules.Where(x => x.PlayerId == playerId));
        _context.PlayerResourceReserves.RemoveRange(_context.PlayerResourceReserves.Where(x => x.PlayerId == playerId));
        _context.PlayerResourceFlows.RemoveRange(_context.PlayerResourceFlows.Where(x => x.PlayerId == playerId));
        _context.PlayerLaborDays.RemoveRange(_context.PlayerLaborDays.Where(x => x.PlayerId == playerId));
        _context.SaveChanges();
    }

    private void CarryArtel(int playerId)
    {
        foreach (var worker in _context.Workers.Where(x => x.PlayerId == playerId).ToArray())
        {
            worker.WorkedSeconds = 0;
            worker.RestUntil = null;
            worker.SickUntil = null;
            worker.SickTypeId = null;
        }
    }

    private void HalveReputation(int playerId)
    {
        foreach (var reputation in _context.NeighborReputations.Where(x => x.PlayerId == playerId).ToArray())
        {
            reputation.Points /= ReputationCarryDivisor;
        }
    }
}
