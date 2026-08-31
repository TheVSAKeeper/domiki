using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village;
using WorkerSkill = Domiki.Web.Workers.Models.WorkerSkill;

namespace Domiki.Web.Workers;

public class WorkerManager
{
    /// <summary>
    /// Потолок числа трудяг одного игрока.
    /// </summary>
    /// <remarks>
    /// Предохранитель GAMEDESIGN.md §4.4: усталость троттлит артель мягко, кап держит её сверху жёстко. Восемь изб
    /// пятого уровня дают 40 коек, то есть потолок режет самый хвост лестницы (§3.6), а «Запасная койка»
    /// (<see cref="PerkManager.SpareBunkBedsPerStep"/>) упирается в него тем раньше, чем выше подняты избы.
    /// </remarks>
    public const int MaxCapacity = 35;

    /// <summary>
    /// Отказ занять трудягу, которому не хватило койки.
    /// </summary>
    public const string AwayWorkersMessage = "Не хватает коек – трудяги в отходе";

    /// <summary>
    /// Отказ занять трудягу, который отдыхает после смены или хворает.
    /// </summary>
    public const string RestingWorkersMessage = "Трудяги отдыхают";

    private const int PlodderModificatorId = 1;

    private readonly ApplicationDbContext _context;
    private readonly ResourceManager _resourceManager;
    private readonly PlayerResourceManager _playerResourceManager;
    private readonly PerkManager _perkManager;

    public WorkerManager(ApplicationDbContext context, ResourceManager resourceManager, PlayerResourceManager playerResourceManager, PerkManager perkManager)
    {
        _context = context;
        _resourceManager = resourceManager;
        _playerResourceManager = playerResourceManager;
        _perkManager = perkManager;
    }

    public static bool IsFree(Worker worker, DateTime now)
    {
        return worker.ManufactureId == null && worker.ExpeditionId == null && worker.ErrandId == null && worker.IncidentId == null && (worker.RestUntil == null || worker.RestUntil <= now);
    }

    public IEnumerable<Models.Worker> GetWorkers(int playerId)
    {
        var capacity = GetCapacity(playerId);
        var currentCount = _context.Workers.Count(x => x.PlayerId == playerId);
        if (currentCount < capacity)
        {
            _playerResourceManager.LockDbPlayerRow(playerId);
            EnsureWorkers(playerId);
            _context.SaveChanges();
        }

        return MapWorkers(playerId);
    }

    /// <summary>
    /// Возвращает состояние плащей игрока.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <returns>Остаток плащей, число выданных на смены и накопленный износ.</returns>
    public Models.CloakState GetCloakState(int playerId)
    {
        var player = _context.Players.Single(x => x.Id == playerId);
        return new()
        {
            Stock = _context.Resources.Where(x => x.PlayerId == playerId && x.TypeId == Core.DomikManager.CloakResourceTypeId).Select(x => (int?)x.Value).FirstOrDefault() ?? 0,
            OutOnShifts = _context.Manufactures.Where(x => x.DomikPlayerId == playerId).Sum(x => (int?)x.CloakCount) ?? 0,
            WearPoints = player.CloakWearPoints,
            LifetimeShifts = Core.DomikManager.CloakLifetimeShifts,
        };
    }

    public Worker[] EnsureWorkers(int playerId)
    {
        var capacity = GetCapacity(playerId);
        var currentWorkers = _context.Workers.Where(x => x.PlayerId == playerId).OrderBy(x => x.Id).ToArray();
        var usedNames = new HashSet<string>();
        for (var i = 0; i < currentWorkers.Length; i++)
        {
            if (!usedNames.Add(currentWorkers[i].Name))
            {
                currentWorkers[i].Name = GetWorkerName(usedNames, i);
                usedNames.Add(currentWorkers[i].Name);
            }
        }

        var traits = _resourceManager.GetTraits();
        while (currentWorkers.Length < capacity)
        {
            var worker = new Worker
            {
                PlayerId = playerId,
                Name = GetWorkerName(usedNames, currentWorkers.Length),
                TraitId = traits[Random.Shared.Next(traits.Length)].Id,
                HireDate = DateTimeHelper.GetNowDate(),
            };

            usedNames.Add(worker.Name);
            _context.Workers.Add(worker);
            currentWorkers = currentWorkers.Append(worker).ToArray();
        }

        _context.SaveChanges();
        ReconcileManufactures(playerId, currentWorkers, DateTimeHelper.GetNowDate());
        return currentWorkers;
    }

    public int GetCapacity(int playerId)
    {
        var domikTypes = _resourceManager.GetDomikTypes();
        var capacity = 0;
        foreach (var domik in _context.Domiks.Where(x => x.PlayerId == playerId).ToArray())
        {
            if (domik.Level == 0)
            {
                continue;
            }

            var domikType = domikTypes.First(x => x.Id == domik.TypeId);
            var level = domikType.Levels.First(x => x.Value == domik.Level);
            capacity += level.Modificators.FirstOrDefault(x => x.Type.Id == PlodderModificatorId)?.Value ?? 0;
        }

        return Math.Min(MaxCapacity, capacity + _perkManager.GetBedBonus(playerId));
    }

    /// <summary>
    /// Возвращает трудяг, которым не хватило коек и которые числятся в отходе.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <returns>Идентификаторы трудяг в отходе, самые поздние по найму.</returns>
    /// <remarks>
    /// Артель переезжает целиком, а двор новой деревни пуст, поэтому трудяг оказывается больше коек. Их не теряют:
    /// они выходят на работу, как только койка появится (GAMEDESIGN.md §3 Слой 4).
    /// </remarks>
    public int[] GetAwayWorkerIds(int playerId)
    {
        var capacity = GetCapacity(playerId);
        return _context.Workers.Where(x => x.PlayerId == playerId)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToArray()
            .Skip(capacity)
            .ToArray();
    }

    /// <summary>
    /// Отбирает трудяг, готовых взять работу: не занятых, отдохнувших и не числящихся в отходе.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="workers">Трудяги игрока, среди которых идёт отбор.</param>
    /// <param name="now">Момент отбора.</param>
    /// <returns>Доступные трудяги по возрастанию идентификатора.</returns>
    /// <remarks>
    /// Единая точка отбора для всех занятий – производств, походов, поручений и поисков: трудяга без койки
    /// (<see cref="GetAwayWorkerIds"/>) работы не берёт, поэтому <see cref="IsFree"/> отдельно от этого отбора
    /// применять только там, где отход уже учтён.
    /// </remarks>
    public Worker[] GetAvailableWorkers(int playerId, Worker[] workers, DateTime now)
    {
        var awayIds = GetAwayWorkerIds(playerId).ToHashSet();
        return workers.Where(x => IsFree(x, now) && !awayIds.Contains(x.Id)).OrderBy(x => x.Id).ToArray();
    }

    /// <summary>
    /// Возвращает текст отказа, когда работу некому взять.
    /// </summary>
    /// <param name="workers">Все трудяги игрока.</param>
    /// <param name="availableWorkers">Отобранные <see cref="GetAvailableWorkers"/> трудяги.</param>
    /// <param name="now">Момент отбора.</param>
    /// <returns>
    /// Текст для <see cref="BusinessException"/>: про отход, если руки есть, но им не хватило коек; про отдых, если руки
    /// заняты усталостью или хворью.
    /// </returns>
    public static string GetNotEnoughWorkersMessage(Worker[] workers, Worker[] availableWorkers, DateTime now)
    {
        if (workers.Count(x => IsFree(x, now)) > availableWorkers.Length)
        {
            return AwayWorkersMessage;
        }

        var occupied = workers.Where(x => !IsFree(x, now)).ToArray();
        return occupied.Length > 0 && occupied.All(x => x.RestUntil > now)
            ? RestingWorkersMessage
            : "Недостаточно трудяг";
    }

    private string GetWorkerName(HashSet<string> usedNames, int ordinal)
    {
        foreach (var name in NameGrammar.Names)
        {
            if (!usedNames.Contains(name))
            {
                return name;
            }
        }

        var suffix = ordinal + 1;
        while (usedNames.Contains($"Трудяга {suffix}"))
        {
            suffix++;
        }

        return $"Трудяга {suffix}";
    }

    private void ReconcileManufactures(int playerId, Worker[] currentWorkers, DateTime now)
    {
        var awayIds = GetAwayWorkerIds(playerId).ToHashSet();
        currentWorkers = currentWorkers.Where(x => !awayIds.Contains(x.Id)).ToArray();
        var manufactures = _context.Manufactures.Where(x => x.DomikPlayerId == playerId).OrderBy(x => x.Id).ToArray();
        foreach (var manufacture in manufactures)
        {
            var assigned = currentWorkers.Where(x => x.ManufactureId == manufacture.Id).OrderBy(x => x.Id).ToArray();
            var missing = manufacture.PlodderCount - assigned.Length;
            if (missing <= 0)
            {
                continue;
            }

            var freeWorkers = currentWorkers.Where(x => IsFree(x, now)).OrderBy(x => x.Id).Take(missing).ToArray();
            foreach (var worker in freeWorkers)
            {
                worker.ManufactureId = manufacture.Id;
            }
        }
    }

    private IEnumerable<Models.Worker> MapWorkers(int playerId)
    {
        var traits = _resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);
        var workers = _context.Workers.Where(x => x.PlayerId == playerId).OrderBy(x => x.Id).ToArray();
        var awayIds = GetAwayWorkerIds(playerId).ToHashSet();
        var workerIds = workers.Select(x => x.Id).ToArray();
        var skills = _context.WorkerSkills.Where(x => workerIds.Contains(x.WorkerId))
            .ToArray()
            .GroupBy(x => x.WorkerId)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.DomikTypeId)
                .Select(y => new WorkerSkill
                {
                    DomikTypeId = y.DomikTypeId,
                    Uses = y.Uses,
                    BonusPercent = WorkerSkillCalculator.GetBonusPercent(y.Uses),
                })
                .ToArray());

        return workers.Select(x => new Models.Worker
            {
                Id = x.Id,
                Name = x.Name,
                Trait = traits[x.TraitId],
                ManufactureId = x.ManufactureId,
                ExpeditionId = x.ExpeditionId,
                ErrandId = x.ErrandId,
                IncidentId = x.IncidentId,
                WorkedSeconds = x.WorkedSeconds,
                RestUntil = x.RestUntil,
                SickUntil = x.SickUntil,
                SickTypeId = x.SickTypeId,
                IsAway = awayIds.Contains(x.Id),
                Skills = skills.GetValueOrDefault(x.Id, Array.Empty<WorkerSkill>()),
            })
            .ToArray();
    }
}
