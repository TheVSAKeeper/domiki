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
/// Управляет происшествиями с трудягой, задержавшимся в походе: завязкой, поисками и благополучной развязкой.
/// </summary>
/// <remarks>
/// Создаёт событие <see cref="CalculateTypes.Incident"/> при возвращении похода, а планировщик <see cref="Calculator"/>
/// обрабатывает самостоятельное возвращение либо завершение поисков в <see cref="FinishIncident"/>.
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
    /// Максимальное число трудяг, которых можно отправить на поиски.
    /// </summary>
    public const int IncidentSearchMaxWorkers = 2;

    /// <summary>
    /// Число клиентских шаблонов текста происшествия.
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
    /// Базовый шанс улучшения черты пропавшего по зацепке, индекс = ClueId.
    /// </summary>
    /// <value>Проценты.</value>
    public static readonly int[] ClueTraitChancePercent = { 10, 15, 25 };

    /// <summary>
    /// Базовое значение для расчёта количества ресурсной находки.
    /// </summary>
    /// <remarks>
    /// Масштабируется рыночной стоимостью найденного ресурса по образцу <see cref="Economy.ErrandManager.FinishErrand"/>.
    /// </remarks>
    public const int IncidentFindBaseValue = 6;

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
    /// <param name="resourceManager">Справочник походов, ресурсов и черт.</param>
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

        if (_context.Incidents.Any(x => x.PlayerId == player.Id && x.ResolvedDate == null))
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
    /// Возвращает активное происшествие игрока.
    /// </summary>
    /// <param name="playerId">Id игрока.</param>
    /// <returns>Активное происшествие, если <c>ResolvedDate == null</c>; иначе <see langword="null"/>.</returns>
    public IncidentModel? Get(int playerId)
    {
        var dbIncident = _context.Incidents.FirstOrDefault(x => x.PlayerId == playerId && x.ResolvedDate == null);
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
            MissingWorkerId = dbIncident.MissingWorkerId,
            ExpeditionTypeId = dbIncident.ExpeditionTypeId,
            TemplateId = dbIncident.TemplateId,
            CreateDate = dbIncident.CreateDate,
            ClueId = dbIncident.ClueId,
            SearchEndDate = dbIncident.SearchEndDate,
            AutoReturnDate = dbIncident.CreateDate.AddHours(IncidentAutoReturnHours),
            SearchWorkerIds = searchWorkerIds,
        };
    }

    /// <summary>
    /// Выбирает зацепку и назначает свободных трудяг на поиски пропавшего.
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
    /// Обрабатывает самостоятельное возвращение пропавшего или завершение поисков.
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

        var missingWorker = _context.Workers.Single(x => x.Id == dbIncident.MissingWorkerId);
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
        var searchWorkers = workers.Where(x => x.Id != dbIncident.MissingWorkerId).ToArray();
        foreach (var worker in workers)
        {
            worker.IncidentId = null;
        }

        missingWorker.RestUntil = date.AddSeconds(ExpeditionManager.ExpeditionRestSeconds);
        var clueId = dbIncident.ClueId!.Value;
        var expeditionType = _resourceManager.GetExpeditionTypes().First(x => x.Id == dbIncident.ExpeditionTypeId);
        var resourceEntries = expeditionType.Loot.Where(x => x.Kind == ExpeditionLootKind.Resource && x.ResourceTypeId != null).ToArray();
        int? resourceTypeId = null;
        int? value = null;
        if (resourceEntries.Length > 0)
        {
            resourceTypeId = resourceEntries[Random.Shared.Next(resourceEntries.Length)].ResourceTypeId!.Value;
            value = Math.Max(1, (int)Math.Round(IncidentFindBaseValue * ClueFindMultiplier[clueId] * (double)ResourceManager.BaseMarketValue / ResourceManager.GetMarketValue(resourceTypeId.Value), MidpointRounding.AwayFromZero));
            _playerResourceManager.GrantResource(calcInfo.PlayerId, resourceTypeId.Value, value.Value);
        }

        var traits = _resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);
        var groupLuck = searchWorkers.Length == 0 ? 0 : searchWorkers.Max(x => traits[x.TraitId].LuckWeightPercent);
        var traitChance = Math.Min(100, ClueTraitChancePercent[clueId] * (100 + groupLuck) / 100);
        var ordinaryTrait = traits.Values.First(x => x.LogicName == "ordinary");
        var traitUpgraded = false;
        string? newTrait = null;
        string? newTraitLogicName = null;
        if (missingWorker.TraitId == ordinaryTrait.Id && Random.Shared.Next(100) < traitChance)
        {
            var candidates = traits.Values.Where(x => x.LogicName != "ordinary").ToArray();
            var upgradedTrait = candidates[Random.Shared.Next(candidates.Length)];
            missingWorker.TraitId = upgradedTrait.Id;
            traitUpgraded = true;
            newTrait = upgradedTrait.Name;
            newTraitLogicName = upgradedTrait.LogicName;
        }

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
            traitUpgraded,
            newTrait,
            newTraitLogicName,
        });
        (calcInfo.PushTitle, calcInfo.PushBody) = GetResolvedPush(missingWorker.Name);
        _context.SaveChanges();
        return true;
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

}
