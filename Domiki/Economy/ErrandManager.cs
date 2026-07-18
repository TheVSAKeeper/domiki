using Domiki.Web.Core.Scheduling;
using Domiki.Web.Data;
using Domiki.Web.Economy.Models;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village;
using Domiki.Web.Workers;

namespace Domiki.Web.Economy;

/// <summary>
/// Поручения соседей – квест-офферы на доске заказов, оплачиваемые временем трудяг.
/// </summary>
/// <remarks>
/// Оффер подмешивается в добор доски заказов (<see cref="OrderManager.EnsureOrderBoard"/>) с шансом
/// <see cref="ErrandRotationWeightPercent"/>. После принятия трудяги заняты до <see cref="Data.Entities.Errand.FinishDate"/>,
/// развязку считает планировщик <see cref="Core.Scheduling.Calculator"/> (<see cref="CalculateTypes.Errand"/>).
/// </remarks>
public class ErrandManager
{
    /// <summary>
    /// Шанс, с которым добор доски заказов создаёт новый оффер-поручение.
    /// </summary>
    /// <value>Проценты.</value>
    public const int ErrandRotationWeightPercent = 20;

    /// <summary>
    /// Обжитость деревни, с которой открывается механика поручений.
    /// </summary>
    public const int ErrandUnlockLevel = 10;

    /// <summary>
    /// Продолжительность офферной фазы поручения, пока игрок не принял его.
    /// </summary>
    /// <value>Часы.</value>
    public const int ErrandOfferDurationHours = 8;

    /// <summary>
    /// Награда монетами за час поисков одного трудяги.
    /// </summary>
    /// <value>Монеты.</value>
    public const int ErrandCoinsPerWorkerHour = 10;

    /// <summary>
    /// Максимальное число трудяг, которых можно отправить на поручение.
    /// </summary>
    public const int ErrandMaxWorkers = 2;

    /// <summary>
    /// Базовый шанс бонус-находки при развязке поручения.
    /// </summary>
    /// <value>Проценты.</value>
    /// <remarks>
    /// Масштабируется чертой Везучий: итоговый шанс = <see cref="ErrandBonusFindChancePercent"/> × (100 + maxLuckWeightPercent отряда) / 100, кап 100.
    /// </remarks>
    public const int ErrandBonusFindChancePercent = 20;

    /// <summary>
    /// Базовое значение для расчёта количества бонус-находки.
    /// </summary>
    /// <remarks>
    /// Масштабируется рыночной стоимостью ресурса по образцу <see cref="OrderManager.GetOrderQuantity"/>.
    /// </remarks>
    public const int ErrandBonusFindBaseValue = 6;

    /// <summary>
    /// Число клиентских шаблонов текста поручения.
    /// </summary>
    public const int ErrandTemplateCount = 6;

    /// <summary>
    /// Текст пуша при завершении поисков по принятому поручению.
    /// </summary>
    public const string ErrandResolvedPushBody = "Поручение выполнено – соседи благодарят!";

    private const int CoinResourceTypeId = 1;

    /// <summary>
    /// Длительность поисков по зацепке, индекс = ClueId.
    /// </summary>
    /// <value>Часы.</value>
    public static readonly int[] ClueDurationHours = { 2, 4, 8 };

    /// <summary>
    /// Репутация соседа за развязку поручения по зацепке, индекс = ClueId.
    /// </summary>
    public static readonly int[] ClueReputation = { 3, 5, 8 };

    private readonly ApplicationDbContext _context;
    private readonly UnitOfWork _uow;
    private readonly ICalculator _calculator;
    private readonly ResourceManager _resourceManager;
    private readonly PlayerResourceManager _playerResourceManager;
    private readonly VillageLevelCalculator _villageLevelCalculator;
    private readonly PlayerEventManager _playerEventManager;

    public ErrandManager(
        ApplicationDbContext context,
        UnitOfWork uow,
        ICalculator calculator,
        ResourceManager resourceManager,
        PlayerResourceManager playerResourceManager,
        VillageLevelCalculator villageLevelCalculator,
        PlayerEventManager playerEventManager)
    {
        _context = context;
        _uow = uow;
        _calculator = calculator;
        _resourceManager = resourceManager;
        _playerResourceManager = playerResourceManager;
        _villageLevelCalculator = villageLevelCalculator;
        _playerEventManager = playerEventManager;
    }

    /// <summary>
    /// Пытается создать оффер-поручение с шансом <see cref="ErrandRotationWeightPercent"/>.
    /// </summary>
    /// <param name="playerId">Id игрока.</param>
    /// <param name="villageLevel">Текущая обжитость деревни игрока.</param>
    /// <returns>Событие для регистрации в планировщике, если оффер создан; иначе <see langword="null"/>.</returns>
    public CalculateInfo? TryRollOffer(int playerId, int villageLevel)
    {
        return Random.Shared.Next(100) < ErrandRotationWeightPercent ? CreateOffer(playerId, villageLevel) : null;
    }

    /// <summary>
    /// Создаёт оффер-поручение без ролла, если пройдены гейты.
    /// </summary>
    /// <param name="playerId">Id игрока.</param>
    /// <param name="villageLevel">Текущая обжитость деревни игрока.</param>
    /// <returns>Событие для регистрации в планировщике, если оффер создан; <see langword="null"/> – деревня недостаточно обжита или у игрока уже есть незавершённое поручение.</returns>
    public CalculateInfo? CreateOffer(int playerId, int villageLevel)
    {
        if (villageLevel < ErrandUnlockLevel)
        {
            return null;
        }

        if (_context.Errands.Any(x => x.PlayerId == playerId && x.ResolvedDate == null))
        {
            return null;
        }

        var neighbors = _villageLevelCalculator.GetOpenNeighbors(villageLevel);
        var neighbor = neighbors[Random.Shared.Next(neighbors.Length)];
        var now = DateTimeHelper.GetNowDate();

        var errand = new Data.Entities.Errand
        {
            PlayerId = playerId,
            NeighborId = neighbor.Id,
            TemplateId = Random.Shared.Next(ErrandTemplateCount),
            ExpireDate = now.AddHours(ErrandOfferDurationHours),
        };

        _context.Errands.Add(errand);
        _context.SaveChanges();

        return new()
        {
            PlayerId = playerId,
            ObjectId = errand.Id,
            Date = errand.ExpireDate,
            Type = CalculateTypes.Errand,
        };
    }

    /// <summary>
    /// Возвращает активное поручение игрока – оффер или принятое.
    /// </summary>
    /// <param name="playerId">Id игрока.</param>
    /// <returns>Поручение, если у игрока есть незавершённое (<c>ResolvedDate == null</c>); иначе <see langword="null"/>.</returns>
    public Errand? Get(int playerId)
    {
        var dbErrand = _context.Errands.FirstOrDefault(x => x.PlayerId == playerId && x.ResolvedDate == null);
        if (dbErrand == null)
        {
            return null;
        }

        var neighbors = _resourceManager.GetNeighbors();
        var workerIds = _context.Workers.Where(x => x.ErrandId == dbErrand.Id).Select(x => x.Id).ToArray();

        return new()
        {
            Id = dbErrand.Id,
            Neighbor = neighbors.First(x => x.Id == dbErrand.NeighborId),
            TemplateId = dbErrand.TemplateId,
            ExpireDate = dbErrand.ExpireDate,
            AcceptDate = dbErrand.AcceptDate,
            ClueId = dbErrand.ClueId,
            FinishDate = dbErrand.FinishDate,
            WorkerIds = workerIds,
        };
    }

    /// <summary>
    /// Принимает оффер-поручение: выбирает зацепку и назначает трудяг на поиски.
    /// </summary>
    /// <param name="playerId">Id игрока.</param>
    /// <param name="errandId">Id поручения.</param>
    /// <param name="clueId">Выбранная зацепка (0..2), задаёт длительность поисков.</param>
    /// <param name="workerIds">Id свободных трудяг игрока, отправляемых на поиски (1..<see cref="ErrandMaxWorkers"/>).</param>
    public void Accept(int playerId, int errandId, int clueId, int[] workerIds)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);

        var dbErrand = _context.Errands.FirstOrDefault(x => x.Id == errandId && x.PlayerId == playerId);
        if (dbErrand == null)
        {
            throw new BusinessException("Поручение не найдено");
        }

        if (dbErrand.AcceptDate != null)
        {
            throw new BusinessException("Поручение уже принято");
        }

        var now = DateTimeHelper.GetNowDate();
        if (now >= dbErrand.ExpireDate)
        {
            throw new BusinessException("Предложение истекло");
        }

        if (clueId < 0 || clueId >= ClueDurationHours.Length)
        {
            throw new BusinessException("Неверная зацепка");
        }

        if (workerIds.Length == 0)
        {
            throw new BusinessException("Нужен хотя бы один трудяга");
        }

        if (workerIds.Distinct().Count() != workerIds.Length)
        {
            throw new BusinessException("Дублирующиеся трудяги");
        }

        if (workerIds.Length > ErrandMaxWorkers)
        {
            throw new BusinessException("Слишком много трудяг");
        }

        var workers = _context.Workers.Where(x => workerIds.Contains(x.Id)).ToArray();
        if (workers.Length != workerIds.Length || workers.Any(x => x.PlayerId != playerId))
        {
            throw new BusinessException("Трудяга недоступен");
        }

        if (workers.Any(x => !WorkerManager.IsFree(x, now)))
        {
            throw new BusinessException("Трудяга занят");
        }

        dbErrand.AcceptDate = now;
        dbErrand.ClueId = clueId;
        dbErrand.FinishDate = now.AddHours(ClueDurationHours[clueId]);

        foreach (var worker in workers)
        {
            worker.ErrandId = dbErrand.Id;
        }

        _context.SaveChanges();

        var finishDate = dbErrand.FinishDate.Value;
        var afterEventAction = _uow.AfterEventAction;
        _uow.AfterEventAction = () =>
        {
            afterEventAction?.Invoke();
            _calculator.Remove(playerId, errandId, CalculateTypes.Errand);
            _calculator.Insert(new()
            {
                PlayerId = playerId,
                ObjectId = errandId,
                Date = finishDate,
                Type = CalculateTypes.Errand,
                PushBody = ErrandResolvedPushBody,
            });
        };
    }

    /// <summary>
    /// Отменяет поручение: отклонённый оффер или принятое в процессе поисков.
    /// </summary>
    /// <param name="playerId">Id игрока.</param>
    /// <param name="errandId">Id поручения.</param>
    /// <remarks>
    /// Трудяги освобождаются, запись поручения удаляется без наград и штрафов.
    /// </remarks>
    public void Cancel(int playerId, int errandId)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);

        var dbErrand = _context.Errands.FirstOrDefault(x => x.Id == errandId && x.PlayerId == playerId && x.ResolvedDate == null);
        if (dbErrand == null)
        {
            throw new BusinessException("Поручение не найдено");
        }

        var workers = _context.Workers.Where(x => x.ErrandId == dbErrand.Id).ToArray();
        foreach (var worker in workers)
        {
            worker.ErrandId = null;
        }

        _context.Errands.Remove(dbErrand);
        _context.SaveChanges();

        var afterEventAction = _uow.AfterEventAction;
        _uow.AfterEventAction = () =>
        {
            afterEventAction?.Invoke();
            _calculator.Remove(playerId, errandId, CalculateTypes.Errand);
        };
    }

    /// <summary>
    /// Обрабатывает событие планировщика для поручения: протухание оффера или развязку принятого поручения.
    /// </summary>
    /// <param name="date">Момент обработки события.</param>
    /// <param name="calcInfo">Событие планировщика.</param>
    /// <returns><see langword="true"/> – событие обработано и снимается с планировщика; <see langword="false"/> – ещё рано.</returns>
    public bool FinishErrand(DateTime date, CalculateInfo calcInfo)
    {
        _playerResourceManager.LockDbPlayerRow(calcInfo.PlayerId);

        var dbErrand = _context.Errands.FirstOrDefault(x => x.Id == calcInfo.ObjectId && x.PlayerId == calcInfo.PlayerId);
        if (dbErrand == null || dbErrand.ResolvedDate != null)
        {
            return true;
        }

        if (dbErrand.AcceptDate == null)
        {
            if (date < dbErrand.ExpireDate)
            {
                return false;
            }

            _context.Errands.Remove(dbErrand);
            _context.SaveChanges();
            return true;
        }

        if (date < dbErrand.FinishDate)
        {
            return false;
        }

        var workers = _context.Workers.Where(x => x.ErrandId == dbErrand.Id).ToArray();
        foreach (var worker in workers)
        {
            worker.ErrandId = null;
        }

        var clueId = dbErrand.ClueId!.Value;
        var hours = ClueDurationHours[clueId];
        var coins = ErrandCoinsPerWorkerHour * workers.Length * hours;
        var reputation = ClueReputation[clueId];
        _playerResourceManager.GrantResource(calcInfo.PlayerId, CoinResourceTypeId, coins);
        _playerResourceManager.GrantReputation(calcInfo.PlayerId, dbErrand.NeighborId, reputation);

        var traits = _resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);
        var groupLuck = workers.Length == 0 ? 0 : workers.Max(x => traits[x.TraitId].LuckWeightPercent);
        var bonusChance = Math.Min(100, ErrandBonusFindChancePercent * (100 + groupLuck) / 100);

        int? bonusResourceTypeId = null;
        int? bonusValue = null;
        if (Random.Shared.Next(100) < bonusChance)
        {
            var neighbor = _resourceManager.GetNeighbors().First(x => x.Id == dbErrand.NeighborId);
            bonusResourceTypeId = neighbor.PrimaryResourceTypeId;
            bonusValue = Math.Max(1, (int)Math.Round(ErrandBonusFindBaseValue * (double)ResourceManager.BaseMarketValue / ResourceManager.GetMarketValue(bonusResourceTypeId.Value), MidpointRounding.AwayFromZero));
            _playerResourceManager.GrantResource(calcInfo.PlayerId, bonusResourceTypeId.Value, bonusValue.Value);
        }

        dbErrand.ResolvedDate = date;
        _playerEventManager.Record(calcInfo.PlayerId, Data.Entities.PlayerEventType.ErrandResolved, new
        {
            neighborId = dbErrand.NeighborId,
            templateId = dbErrand.TemplateId,
            clueId,
            coins,
            reputation,
            bonusResourceTypeId,
            bonusValue,
        });

        _context.SaveChanges();
        return true;
    }
}
