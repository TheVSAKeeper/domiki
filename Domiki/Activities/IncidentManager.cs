using Domiki.Web.Activities.Models;
using Domiki.Web.Core.Scheduling;
using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Workers;
using IncidentModel = Domiki.Web.Activities.Models.Incident;

namespace Domiki.Web.Activities;

/// <summary>
/// Управляет происшествиями с трудягой, задержавшимся в походе, и загадками в постройках: завязкой, поисками и благополучной развязкой.
/// </summary>
/// <remarks>
/// Создаёт события <see cref="CalculateTypes.Incident"/> при возвращении похода или завершении улучшения постройки, а планировщик
/// <see cref="Calculator"/> обрабатывает самостоятельную развязку либо завершение поисков в <see cref="FinishIncident"/>.
/// </remarks>
public class IncidentManager
{
    /// <summary>
    /// Шанс завязки происшествия при возвращении похода после прохождения всех гейтов.
    /// </summary>
    /// <value>Проценты.</value>
    public const int IncidentChancePercent = 12;

    /// <summary>
    /// Кулдаун между завязками происшествий для одного игрока.
    /// </summary>
    /// <value>Часы.</value>
    public const int IncidentCooldownHours = 72;

    /// <summary>
    /// Минимальное число свободных трудяг после выбытия пропавшего.
    /// </summary>
    public const int IncidentMinFreeWorkers = 3;

    /// <summary>
    /// Минимальный размер отряда, в котором возможна завязка происшествия.
    /// </summary>
    public const int IncidentMinSquadSize = 2;

    /// <summary>
    /// Время до самостоятельного возвращения пропавшего без поисков.
    /// </summary>
    /// <value>Часы.</value>
    public const int IncidentAutoReturnHours = 48;

    /// <summary>
    /// Минимальная обжитость для завязки происшествия в постройке.
    /// </summary>
    public const int DomikIncidentUnlockLevel = 10;

    /// <summary>
    /// Кулдаун между завязками происшествий в постройках для одного игрока.
    /// </summary>
    /// <value>Часы.</value>
    public const int DomikIncidentCooldownHours = 96;

    /// <summary>
    /// Минимальное число свободных трудяг для завязки происшествия в постройке.
    /// </summary>
    public const int DomikIncidentMinFreeWorkers = 2;

    /// <summary>
    /// Время до самостоятельной развязки загадки в постройке без поисков.
    /// </summary>
    /// <value>Часы.</value>
    public const int DomikIncidentAutoResolveHours = 48;

    /// <summary>
    /// Базовое значение для расчёта количества ресурсной находки в постройке.
    /// </summary>
    public const int DomikIncidentFindBaseValue = 6;

    /// <summary>
    /// Максимальное число трудяг, которых можно отправить на поиски.
    /// </summary>
    public const int IncidentSearchMaxWorkers = 2;

    /// <summary>
    /// Число клиентских шаблонов текста происшествия в походе.
    /// </summary>
    public const int IncidentTemplateCount = 6;

    /// <summary>
    /// Длительность поисков по зацепке, индекс = ClueId.
    /// </summary>
    /// <value>Часы.</value>
    public static readonly int[] ClueDurationHours = { 2, 4, 8 };

    /// <summary>
    /// Множитель ценности находки по зацепке, индекс = ClueId.
    /// </summary>
    public static readonly int[] ClueFindMultiplier = { 1, 2, 3 };

    /// <summary>
    /// Базовый шанс улучшения черты по зацепке, индекс = ClueId.
    /// </summary>
    /// <value>Проценты.</value>
    public static readonly int[] ClueTraitChancePercent = { 10, 15, 25 };

    /// <summary>
    /// Базовое значение для расчёта количества ресурсной находки в походе.
    /// </summary>
    /// <remarks>
    /// Масштабируется рыночной стоимостью найденного ресурса по образцу <see cref="Economy.ErrandManager.FinishErrand"/>.
    /// </remarks>
    public const int IncidentFindBaseValue = 6;

    private static readonly IReadOnlyDictionary<string, DomikIncidentTemplate> DomikIncidentTemplates = new Dictionary<string, DomikIncidentTemplate>
    {
        ["gold_mine"] = new(0, null),
        ["barracks"] = new(1, null),
        ["market"] = new(1, null),
        ["clay_mine"] = new(2, "rain"),
        ["lumber_mill"] = new(3, "drought"),
        ["stone_mine"] = new(4, null),
        ["forge"] = new(5, null),
    };

    private readonly ApplicationDbContext _context;
    private readonly UnitOfWork _uow;
    private readonly ICalculator _calculator;
    private readonly ResourceManager _resourceManager;
    private readonly PlayerResourceManager _playerResourceManager;
    private readonly PlayerEventManager _playerEventManager;

    /// <summary>
    /// Создаёт менеджер происшествий.
    /// </summary>
    /// <param name="context">Контекст игровой базы данных.</param>
    /// <param name="uow">Единица работы запроса для отложенной регистрации события.</param>
    /// <param name="calculator">Планировщик игровых событий.</param>
    /// <param name="resourceManager">Справочник походов, построек, ресурсов и черт.</param>
    /// <param name="playerResourceManager">Менеджер ресурсов и блокировки игрока.</param>
    /// <param name="playerEventManager">Журнал игровых событий игрока.</param>
    public IncidentManager(ApplicationDbContext context, UnitOfWork uow, ICalculator calculator, ResourceManager resourceManager, PlayerResourceManager playerResourceManager, PlayerEventManager playerEventManager)
    {
        _context = context;
        _uow = uow;
        _calculator = calculator;
        _resourceManager = resourceManager;
        _playerResourceManager = playerResourceManager;
        _playerEventManager = playerEventManager;
    }

    /// <summary>
    /// Пытается завязать происшествие после возвращения отряда из похода.
    /// </summary>
    /// <param name="player">Игрок, чей отряд вернулся.</param>
    /// <param name="assignedWorkers">Трудяги вернувшегося отряда уже без назначения в поход.</param>
    /// <param name="expeditionTypeId">Идентификатор типа завершившегося похода.</param>
    /// <param name="date">Момент завершения похода.</param>
    /// <returns>Событие для регистрации в планировщике, если происшествие создано; иначе <see langword="null"/>.</returns>
    public CalculateInfo? TryRollIncident(Player player, Worker[] assignedWorkers, int expeditionTypeId, DateTime date)
    {
        if (assignedWorkers.Length < IncidentMinSquadSize || Random.Shared.Next(100) >= IncidentChancePercent)
        {
            return null;
        }

        if (player.LastIncidentDate != null && player.LastIncidentDate.Value.AddHours(IncidentCooldownHours) > date)
        {
            return null;
        }

        if (_context.Incidents.Any(x => x.PlayerId == player.Id && x.SourceType == IncidentSourceType.Expedition && x.ResolvedDate == null))
        {
            return null;
        }

        var missingWorker = assignedWorkers[Random.Shared.Next(assignedWorkers.Length)];
        var freeWorkers = _context.Workers.Where(x => x.PlayerId == player.Id && x.Id != missingWorker.Id)
            .ToArray()
            .Count(x => WorkerManager.IsFree(x, date));
        if (freeWorkers < IncidentMinFreeWorkers)
        {
            return null;
        }

        var incident = new Data.Entities.Incident
        {
            PlayerId = player.Id,
            SourceType = IncidentSourceType.Expedition,
            MissingWorkerId = missingWorker.Id,
            ExpeditionTypeId = expeditionTypeId,
            TemplateId = Random.Shared.Next(IncidentTemplateCount),
            CreateDate = date,
        };
        _context.Incidents.Add(incident);
        _context.SaveChanges();

        missingWorker.IncidentId = incident.Id;
        missingWorker.RestUntil = null;
        player.LastIncidentDate = date;
        _playerEventManager.Record(player.Id, PlayerEventType.WorkerMissing, new
        {
            workerId = missingWorker.Id,
            workerName = missingWorker.Name,
            workerGender = (int)NameGrammar.GenderOf(missingWorker.Name),
            expeditionTypeId,
            templateId = incident.TemplateId,
        });
        _context.SaveChanges();

        var calculateInfo = new CalculateInfo
        {
            PlayerId = player.Id,
            ObjectId = incident.Id,
            Date = date.AddHours(IncidentAutoReturnHours),
            Type = CalculateTypes.Incident,
        };
        (calculateInfo.PushTitle, calculateInfo.PushBody) = GetStartPush(missingWorker.Name);
        return calculateInfo;
    }

    /// <summary>
    /// Пытается детерминированно завязать происшествие-загадку после завершения улучшения постройки.
    /// </summary>
    /// <param name="player">Игрок, чья постройка улучшена.</param>
    /// <param name="domikTypeId">Идентификатор типа улучшенной постройки.</param>
    /// <param name="villageLevel">Текущая обжитость деревни.</param>
    /// <param name="weatherLogicName">Техническое имя текущей погоды.</param>
    /// <param name="date">Момент завершения улучшения.</param>
    /// <returns>Событие для регистрации в планировщике, если происшествие создано; иначе <see langword="null"/>.</returns>
    public CalculateInfo? TryStartDomikIncident(Player player, int domikTypeId, int villageLevel, string? weatherLogicName, DateTime date)
    {
        if (villageLevel < DomikIncidentUnlockLevel)
        {
            return null;
        }

        var domikType = _resourceManager.GetDomikTypes().FirstOrDefault(x => x.Id == domikTypeId);
        if (domikType == null || !DomikIncidentTemplates.TryGetValue(domikType.LogicName, out var template)
                              || template.WeatherLogicName != null && template.WeatherLogicName != weatherLogicName)
        {
            return null;
        }

        if (player.LastDomikIncidentDate != null && player.LastDomikIncidentDate.Value.AddHours(DomikIncidentCooldownHours) > date)
        {
            return null;
        }

        if (_context.Incidents.Any(x => x.PlayerId == player.Id && x.SourceType == IncidentSourceType.Domik && x.ResolvedDate == null))
        {
            return null;
        }

        var freeWorkers = _context.Workers.Where(x => x.PlayerId == player.Id)
            .ToArray()
            .Count(x => WorkerManager.IsFree(x, date));
        if (freeWorkers < DomikIncidentMinFreeWorkers)
        {
            return null;
        }

        var incident = new Data.Entities.Incident
        {
            PlayerId = player.Id,
            SourceType = IncidentSourceType.Domik,
            DomikTypeId = domikTypeId,
            MissingWorkerId = null,
            ExpeditionTypeId = null,
            TemplateId = template.TemplateId,
            CreateDate = date,
        };
        _context.Incidents.Add(incident);
        _context.SaveChanges();

        player.LastDomikIncidentDate = date;
        _playerEventManager.Record(player.Id, PlayerEventType.DomikIncidentStarted, new
        {
            domikTypeId,
            templateId = incident.TemplateId,
        });
        _context.SaveChanges();

        var calculateInfo = new CalculateInfo
        {
            PlayerId = player.Id,
            ObjectId = incident.Id,
            Date = date.AddHours(DomikIncidentAutoResolveHours),
            Type = CalculateTypes.Incident,
        };
        (calculateInfo.PushTitle, calculateInfo.PushBody) = GetDomikStartPush(domikType.Name);
        return calculateInfo;
    }

    /// <summary>
    /// Возвращает активное происшествие с пропавшим в походе трудягой.
    /// </summary>
    /// <param name="playerId">Id игрока.</param>
    /// <returns>Активное происшествие, если <c>ResolvedDate == null</c>; иначе <see langword="null"/>.</returns>
    public IncidentModel? Get(int playerId)
    {
        var dbIncident = _context.Incidents.FirstOrDefault(x => x.PlayerId == playerId && x.SourceType == IncidentSourceType.Expedition && x.ResolvedDate == null);
        if (dbIncident == null)
        {
            return null;
        }

        var searchWorkerIds = _context.Workers.Where(x => x.IncidentId == dbIncident.Id && x.Id != dbIncident.MissingWorkerId)
            .Select(x => x.Id)
            .ToArray();
        return new()
        {
            Id = dbIncident.Id,
            MissingWorkerId = dbIncident.MissingWorkerId!.Value,
            ExpeditionTypeId = dbIncident.ExpeditionTypeId!.Value,
            TemplateId = dbIncident.TemplateId,
            CreateDate = dbIncident.CreateDate,
            ClueId = dbIncident.ClueId,
            SearchEndDate = dbIncident.SearchEndDate,
            AutoReturnDate = dbIncident.CreateDate.AddHours(IncidentAutoReturnHours),
            SearchWorkerIds = searchWorkerIds,
        };
    }

    /// <summary>
    /// Возвращает активное происшествие-загадку в постройке.
    /// </summary>
    /// <param name="playerId">Id игрока.</param>
    /// <returns>Активное происшествие, если <c>ResolvedDate == null</c>; иначе <see langword="null"/>.</returns>
    public DomikIncident? GetDomik(int playerId)
    {
        var dbIncident = _context.Incidents.FirstOrDefault(x => x.PlayerId == playerId && x.SourceType == IncidentSourceType.Domik && x.ResolvedDate == null);
        if (dbIncident == null)
        {
            return null;
        }

        var searchWorkerIds = _context.Workers.Where(x => x.IncidentId == dbIncident.Id)
            .Select(x => x.Id)
            .ToArray();
        return new()
        {
            Id = dbIncident.Id,
            DomikTypeId = dbIncident.DomikTypeId!.Value,
            TemplateId = dbIncident.TemplateId,
            CreateDate = dbIncident.CreateDate,
            ClueId = dbIncident.ClueId,
            SearchEndDate = dbIncident.SearchEndDate,
            AutoResolveDate = dbIncident.CreateDate.AddHours(DomikIncidentAutoResolveHours),
            SearchWorkerIds = searchWorkerIds,
        };
    }

    /// <summary>
    /// Выбирает зацепку и назначает свободных трудяг на поиски.
    /// </summary>
    /// <param name="playerId">Id игрока.</param>
    /// <param name="incidentId">Id происшествия.</param>
    /// <param name="clueId">Выбранная зацепка, индекс в <see cref="ClueDurationHours"/>.</param>
    /// <param name="workerIds">Id свободных трудяг игрока, отправляемых на поиски.</param>
    public void StartSearch(int playerId, int incidentId, int clueId, int[] workerIds)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);

        var dbIncident = _context.Incidents.FirstOrDefault(x => x.Id == incidentId && x.PlayerId == playerId && x.ResolvedDate == null);
        if (dbIncident == null)
        {
            throw new BusinessException("Происшествие не найдено");
        }

        if (dbIncident.SearchEndDate != null)
        {
            throw new BusinessException("Поиски уже начаты");
        }

        if (clueId < 0 || clueId >= ClueDurationHours.Length)
        {
            throw new BusinessException("Неверная зацепка");
        }

        if (workerIds.Length == 0)
        {
            throw new BusinessException("Нужен хотя бы один трудяга");
        }

        if (workerIds.Length > IncidentSearchMaxWorkers)
        {
            throw new BusinessException("Слишком много трудяг");
        }

        if (workerIds.Distinct().Count() != workerIds.Length)
        {
            throw new BusinessException("Дублирующиеся трудяги");
        }

        var workers = _context.Workers.Where(x => workerIds.Contains(x.Id)).ToArray();
        if (workers.Length != workerIds.Length || workers.Any(x => x.PlayerId != playerId))
        {
            throw new BusinessException("Трудяга недоступен");
        }

        var now = DateTimeHelper.GetNowDate();
        if (workers.Any(x => !WorkerManager.IsFree(x, now)))
        {
            throw new BusinessException("Трудяга занят");
        }

        dbIncident.ClueId = clueId;
        dbIncident.SearchEndDate = now.AddHours(ClueDurationHours[clueId]);
        foreach (var worker in workers)
        {
            worker.IncidentId = dbIncident.Id;
        }

        _context.SaveChanges();

        var searchEndDate = dbIncident.SearchEndDate.Value;
        var afterEventAction = _uow.AfterEventAction;
        _uow.AfterEventAction = () =>
        {
            afterEventAction?.Invoke();
            _calculator.Remove(playerId, incidentId, CalculateTypes.Incident);
            _calculator.Insert(new()
            {
                PlayerId = playerId,
                ObjectId = incidentId,
                Date = searchEndDate,
                Type = CalculateTypes.Incident,
            });
        };
    }

    /// <summary>
    /// Обрабатывает самостоятельную развязку происшествия или завершение поисков.
    /// </summary>
    /// <param name="date">Момент обработки события.</param>
    /// <param name="calcInfo">Событие планировщика.</param>
    /// <returns><see langword="true"/> – событие обработано и снимается с планировщика; <see langword="false"/> – ещё рано.</returns>
    public bool FinishIncident(DateTime date, CalculateInfo calcInfo)
    {
        _playerResourceManager.LockDbPlayerRow(calcInfo.PlayerId);

        var dbIncident = _context.Incidents.FirstOrDefault(x => x.Id == calcInfo.ObjectId && x.PlayerId == calcInfo.PlayerId);
        if (dbIncident == null || dbIncident.ResolvedDate != null)
        {
            return true;
        }

        return dbIncident.SourceType switch
        {
            IncidentSourceType.Expedition => FinishExpeditionIncident(date, calcInfo, dbIncident),
            IncidentSourceType.Domik => FinishDomikIncident(date, calcInfo, dbIncident),
            _ => throw new InvalidOperationException("Неизвестный источник происшествия"),
        };
    }

    private bool FinishExpeditionIncident(DateTime date, CalculateInfo calcInfo, Data.Entities.Incident dbIncident)
    {
        var missingWorkerId = dbIncident.MissingWorkerId ?? throw new InvalidOperationException("У происшествия похода нет пропавшего трудяги");
        var expeditionTypeId = dbIncident.ExpeditionTypeId ?? throw new InvalidOperationException("У происшествия похода нет типа похода");
        var missingWorker = _context.Workers.Single(x => x.Id == missingWorkerId);
        if (dbIncident.SearchEndDate == null)
        {
            if (date < dbIncident.CreateDate.AddHours(IncidentAutoReturnHours))
            {
                return false;
            }

            missingWorker.IncidentId = null;
            missingWorker.RestUntil = date.AddSeconds(ExpeditionManager.ExpeditionRestSeconds);
            dbIncident.ResolvedDate = date;
            _playerEventManager.Record(calcInfo.PlayerId, PlayerEventType.IncidentResolved, new
            {
                autoReturned = true,
                workerId = missingWorker.Id,
                workerName = missingWorker.Name,
                workerGender = (int)NameGrammar.GenderOf(missingWorker.Name),
                templateId = dbIncident.TemplateId,
            });
            (calcInfo.PushTitle, calcInfo.PushBody) = GetAutoReturnPush(missingWorker.Name);
            _context.SaveChanges();
            return true;
        }

        if (date < dbIncident.SearchEndDate.Value)
        {
            return false;
        }

        var workers = _context.Workers.Where(x => x.IncidentId == dbIncident.Id).ToArray();
        var searchWorkers = workers.Where(x => x.Id != missingWorkerId).ToArray();
        foreach (var worker in workers)
        {
            worker.IncidentId = null;
        }

        missingWorker.RestUntil = date.AddSeconds(ExpeditionManager.ExpeditionRestSeconds);
        var clueId = dbIncident.ClueId!.Value;
        var expeditionType = _resourceManager.GetExpeditionTypes().First(x => x.Id == expeditionTypeId);
        var resourcePool = expeditionType.Loot.Where(x => x.Kind == ExpeditionLootKind.Resource && x.ResourceTypeId != null)
            .Select(x => x.ResourceTypeId!.Value)
            .ToArray();
        var (resourceTypeId, value) = GrantFind(calcInfo.PlayerId, resourcePool, clueId, IncidentFindBaseValue);
        var trait = TryUpgradeTrait(missingWorker, searchWorkers, clueId);

        dbIncident.ResolvedDate = date;
        _playerEventManager.Record(calcInfo.PlayerId, PlayerEventType.IncidentResolved, new
        {
            autoReturned = false,
            workerId = missingWorker.Id,
            workerName = missingWorker.Name,
            workerGender = (int)NameGrammar.GenderOf(missingWorker.Name),
            templateId = dbIncident.TemplateId,
            clueId,
            resourceTypeId,
            value,
            traitUpgraded = trait.TraitUpgraded,
            newTrait = trait.NewTrait,
            newTraitLogicName = trait.NewTraitLogicName,
        });
        (calcInfo.PushTitle, calcInfo.PushBody) = GetResolvedPush(missingWorker.Name);
        _context.SaveChanges();
        return true;
    }

    private bool FinishDomikIncident(DateTime date, CalculateInfo calcInfo, Data.Entities.Incident dbIncident)
    {
        var domikTypeId = dbIncident.DomikTypeId ?? throw new InvalidOperationException("У происшествия постройки нет типа постройки");
        var domikName = GetDomikName(domikTypeId);
        if (dbIncident.SearchEndDate == null)
        {
            if (date < dbIncident.CreateDate.AddHours(DomikIncidentAutoResolveHours))
            {
                return false;
            }

            dbIncident.ResolvedDate = date;
            _playerEventManager.Record(calcInfo.PlayerId, PlayerEventType.DomikIncidentResolved, new
            {
                autoResolved = true,
                domikTypeId,
                templateId = dbIncident.TemplateId,
            });
            (calcInfo.PushTitle, calcInfo.PushBody) = GetDomikAutoResolvePush(domikName);
            _context.SaveChanges();
            return true;
        }

        if (date < dbIncident.SearchEndDate.Value)
        {
            return false;
        }

        var searchWorkers = _context.Workers.Where(x => x.IncidentId == dbIncident.Id).ToArray();
        foreach (var worker in searchWorkers)
        {
            worker.IncidentId = null;
        }

        var clueId = dbIncident.ClueId!.Value;
        var (resourceTypeId, value) = GrantFind(calcInfo.PlayerId, GetDomikFindResourcePool(domikTypeId), clueId, DomikIncidentFindBaseValue);
        var heroWorker = searchWorkers.Length == 0 ? null : searchWorkers[0];
        var selectedWorker = searchWorkers.Length == 0 ? null : searchWorkers[Random.Shared.Next(searchWorkers.Length)];
        var trait = TryUpgradeTrait(selectedWorker, searchWorkers, clueId);

        dbIncident.ResolvedDate = date;
        _playerEventManager.Record(calcInfo.PlayerId, PlayerEventType.DomikIncidentResolved, new
        {
            autoResolved = false,
            domikTypeId,
            templateId = dbIncident.TemplateId,
            heroWorkerName = heroWorker?.Name,
            heroWorkerGender = heroWorker == null ? (int?)null : (int)NameGrammar.GenderOf(heroWorker.Name),
            clueId,
            resourceTypeId,
            value,
            traitUpgraded = trait.TraitUpgraded,
            newTrait = trait.NewTrait,
            newTraitLogicName = trait.NewTraitLogicName,
            upgradedWorkerId = trait.UpgradedWorkerId,
            upgradedWorkerName = trait.UpgradedWorkerName,
        });
        (calcInfo.PushTitle, calcInfo.PushBody) = GetDomikResolvedPush(domikName);
        _context.SaveChanges();
        return true;
    }

    private (int? ResourceTypeId, int? Value) GrantFind(int playerId, IReadOnlyList<int> resourcePool, int clueId, int findBase)
    {
        if (resourcePool.Count == 0)
        {
            return (null, null);
        }

        var resourceTypeId = resourcePool[Random.Shared.Next(resourcePool.Count)];
        var value = Math.Max(1, (int)Math.Round(findBase * ClueFindMultiplier[clueId] * (double)ResourceManager.BaseMarketValue / ResourceManager.GetMarketValue(resourceTypeId), MidpointRounding.AwayFromZero));
        _playerResourceManager.GrantResource(playerId, resourceTypeId, value);
        return (resourceTypeId, value);
    }

    private (bool TraitUpgraded, string? NewTrait, string? NewTraitLogicName, int? UpgradedWorkerId, string? UpgradedWorkerName) TryUpgradeTrait(Worker? worker, Worker[] searchWorkers, int clueId)
    {
        if (worker == null)
        {
            return (false, null, null, null, null);
        }

        var traits = _resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);
        var groupLuck = searchWorkers.Length == 0 ? 0 : searchWorkers.Max(x => traits[x.TraitId].LuckWeightPercent);
        var traitChance = Math.Min(100, ClueTraitChancePercent[clueId] * (100 + groupLuck) / 100);
        var ordinaryTrait = traits.Values.First(x => x.LogicName == "ordinary");
        if (worker.TraitId != ordinaryTrait.Id || Random.Shared.Next(100) >= traitChance)
        {
            return (false, null, null, null, null);
        }

        var candidates = traits.Values.Where(x => x.LogicName != "ordinary").ToArray();
        var upgradedTrait = candidates[Random.Shared.Next(candidates.Length)];
        worker.TraitId = upgradedTrait.Id;
        return (true, upgradedTrait.Name, upgradedTrait.LogicName, worker.Id, worker.Name);
    }

    private int[] GetDomikFindResourcePool(int domikTypeId)
    {
        var domikType = _resourceManager.GetDomikTypes().First(x => x.Id == domikTypeId);
        var receiptIds = domikType.Levels.SelectMany(x => x.Receipts).Select(x => x.Id).ToHashSet();
        var resourcePool = _resourceManager.GetReceipts()
            .Where(x => receiptIds.Contains(x.Id))
            .SelectMany(x => x.OutputResources)
            .Select(x => x.Type.Id)
            .Distinct()
            .ToArray();
        if (resourcePool.Length > 0)
        {
            return resourcePool;
        }

        return domikType.Levels.SelectMany(x => x.Resources)
            .Select(x => x.Type.Id)
            .Distinct()
            .ToArray();
    }

    private string GetDomikName(int domikTypeId)
    {
        return _resourceManager.GetDomikTypes().First(x => x.Id == domikTypeId).Name;
    }

    private static (string Title, string Body) GetStartPush(string workerName)
    {
        return ($"{workerName} задержал{NameGrammar.GenderForm(workerName, "ся", "ась")} в походе", $"Отряд вернулся, а {workerName} – нет: на карточке происшествия есть зацепки.");
    }

    private static (string Title, string Body) GetResolvedPush(string workerName)
    {
        return ($"{workerName} до{NameGrammar.GenderForm(workerName, "шёл", "шла")} до дому!", $"Вернул{NameGrammar.GenderForm(workerName, "ся", "ась")} – с историей и находкой. Загляни в журнал.");
    }

    private static (string Title, string Body) GetAutoReturnPush(string workerName)
    {
        return ($"{workerName} вернул{NameGrammar.GenderForm(workerName, "ся", "ась")} сам{NameGrammar.GenderForm(workerName, "", "а")}", $"Дорогу на{NameGrammar.GenderForm(workerName, "шёл", "шла")} без подмоги – отдыхает и рассказывает байки.");
    }

    private static (string Title, string Body) GetDomikStartPush(string domikName)
    {
        return ($"В «{domikName}» что-то неспокойно", "На карточке происшествия – зацепки, если любопытно.");
    }

    private static (string Title, string Body) GetDomikResolvedPush(string domikName)
    {
        return ($"Загадка «{domikName}» разгадана", "Следопыты разобрались и принесли находку. Загляни в журнал.");
    }

    private static (string Title, string Body) GetDomikAutoResolvePush(string domikName)
    {
        return ($"В «{domikName}» снова тихо", "Загадка растворилась сама, не оставив премии.");
    }

    private sealed record DomikIncidentTemplate(int TemplateId, string? WeatherLogicName);
}
