using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Economy.Models;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village;
using Neighbor = Domiki.Web.Economy.Models.Neighbor;
using Resource = Domiki.Web.Reference.Models.Resource;

namespace Domiki.Web.Economy;

/// <summary>
/// Обоз – разовая покупка ресурса у соседа-NPC по завышенному курсу, ограниченная скользящим суточным окном.
/// </summary>
/// <remarks>
/// Клапан против дефицита ресурсов и сток монет, а не альтернативный маршрут производства: покупка мгновенная, без трудяги,
/// слота производства и планировщика.
/// </remarks>
public class ConvoyManager
{
    /// <summary>
    /// Множитель к рыночной стоимости ресурса – итоговая цена покупки одной единицы в обозе.
    /// </summary>
    public const int PriceMultiplier = 5;

    /// <summary>
    /// Порог репутации у соседа, открывающий его обоз для покупок.
    /// </summary>
    public const int AccessReputationThreshold = 5;

    /// <summary>
    /// Порог репутации, открывающий дополнительный товар соседа (<see cref="Neighbor.SecondaryResourceTypeId"/>).
    /// </summary>
    public const int SecondaryReputationThreshold = 20;

    /// <summary>
    /// Порог репутации, повышающий суточный лимит покупок до <see cref="HighLimit"/>.
    /// </summary>
    public const int HighLimitReputationThreshold = 40;

    /// <summary>
    /// Суточный лимит покупок у соседа при обычной репутации.
    /// </summary>
    public const int BaseLimit = 3;

    /// <summary>
    /// Суточный лимит покупок у соседа при репутации не ниже <see cref="HighLimitReputationThreshold"/>.
    /// </summary>
    public const int HighLimit = 5;

    /// <summary>
    /// Длительность скользящего окна лимита покупок, в секундах.
    /// </summary>
    public const int WindowDurationSeconds = 24 * 60 * 60;

    private const int CoinResourceTypeId = 1;
    private const int GoldResourceTypeId = 5;

    private readonly UnitOfWork _uow;
    private readonly ApplicationDbContext _context;
    private readonly ResourceManager _resourceManager;
    private readonly PlayerResourceManager _playerResourceManager;
    private readonly VillageLevelCalculator _villageLevelCalculator;
    private readonly GameStateBroker _broker;

    public ConvoyManager(UnitOfWork uow, ApplicationDbContext context, ResourceManager resourceManager, PlayerResourceManager playerResourceManager, VillageLevelCalculator villageLevelCalculator, GameStateBroker broker)
    {
        _uow = uow;
        _context = context;
        _resourceManager = resourceManager;
        _playerResourceManager = playerResourceManager;
        _villageLevelCalculator = villageLevelCalculator;
        _broker = broker;
    }

    /// <summary>
    /// Возвращает цену покупки одной единицы ресурса в обозе.
    /// </summary>
    /// <param name="resourceTypeId">Тип ресурса – ссылка на справочник <see cref="Reference.Models.ResourceType.Id"/>.</param>
    /// <returns>Цена в монетах.</returns>
    public static int GetPrice(int resourceTypeId)
    {
        return ResourceManager.GetMarketValue(resourceTypeId) * PriceMultiplier;
    }

    /// <summary>
    /// Собирает обозы всех соседей, открытых по обжитости деревни, с ассортиментом, ценами и остатком суточного лимита.
    /// </summary>
    /// <param name="playerId">Игрок.</param>
    /// <returns>Список обозов, по одному на каждого открытого соседа.</returns>
    public Convoy[] GetConvoys(int playerId)
    {
        var date = DateTimeHelper.GetNowDate();
        var villageLevel = _villageLevelCalculator.GetLevel(playerId).Level;
        var neighbors = _villageLevelCalculator.GetOpenNeighbors(villageLevel);
        var reputations = _context.NeighborReputations.Where(x => x.PlayerId == playerId).ToArray();
        var convoyRows = _context.NeighborConvoys.Where(x => x.PlayerId == playerId).ToArray();

        return neighbors.Select(neighbor =>
            {
                var points = reputations.FirstOrDefault(x => x.NeighborId == neighbor.Id)?.Points ?? 0;
                var isLocked = points < AccessReputationThreshold;
                var row = convoyRows.FirstOrDefault(x => x.NeighborId == neighbor.Id);
                var limit = GetLimit(points);
                var boughtCount = GetEffectiveBoughtCount(row, date);

                return new Convoy
                {
                    Neighbor = neighbor,
                    Items = isLocked
                        ? []
                        : GetAssortment(neighbor, points).Select(resourceTypeId => new ConvoyItem
                            {
                                ResourceTypeId = resourceTypeId,
                                Price = GetPrice(resourceTypeId),
                            })
                            .ToArray(),
                    Limit = limit,
                    Remaining = Math.Max(0, limit - boughtCount),
                    WindowResetDate = boughtCount > 0 ? row!.WindowStartDate.AddSeconds(WindowDurationSeconds) : null,
                    IsLocked = isLocked,
                };
            })
            .ToArray();
    }

    /// <summary>
    /// Покупает у соседа ресурс из его ассортимента за монеты по завышенному курсу, в пределах суточного лимита.
    /// </summary>
    /// <param name="playerId">Покупатель.</param>
    /// <param name="neighborId">Сосед – ссылка на справочник <see cref="Neighbor.Id"/>.</param>
    /// <param name="resourceTypeId">Покупаемый тип ресурса – ссылка на справочник <see cref="Reference.Models.ResourceType.Id"/>.</param>
    /// <param name="count">Количество единиц ресурса.</param>
    /// <param name="date">Текущий момент.</param>
    public void Buy(int playerId, int neighborId, int resourceTypeId, int count, DateTime date)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);

        if (count <= 0 || count > HighLimit)
        {
            throw new BusinessException("Неверное количество");
        }

        var neighbor = _resourceManager.GetNeighbors().FirstOrDefault(x => x.Id == neighborId)
            ?? throw new BusinessException("Сосед не найден");

        var villageLevel = _villageLevelCalculator.GetLevel(playerId).Level;
        var points = _context.NeighborReputations
            .Where(x => x.PlayerId == playerId && x.NeighborId == neighborId)
            .Select(x => (int?)x.Points)
            .SingleOrDefault() ?? 0;

        if (neighbor.UnlockLevel > villageLevel || points < AccessReputationThreshold)
        {
            throw new BusinessException("Сосед пока не пригоняет к тебе обоз – заслужи доверие его заказами");
        }

        if (!GetAssortment(neighbor, points).Contains(resourceTypeId))
        {
            throw new BusinessException("Этого товара обоз не возит");
        }

        var row = _context.NeighborConvoys.FirstOrDefault(x => x.PlayerId == playerId && x.NeighborId == neighborId);
        if (row == null)
        {
            row = new()
            {
                PlayerId = playerId,
                NeighborId = neighborId,
                WindowStartDate = date,
                BoughtCount = 0,
            };

            _context.NeighborConvoys.Add(row);
        }
        else if (date - row.WindowStartDate >= TimeSpan.FromSeconds(WindowDurationSeconds))
        {
            row.WindowStartDate = date;
            row.BoughtCount = 0;
        }

        var limit = GetLimit(points);
        if (row.BoughtCount + count > limit)
        {
            throw new BusinessException("Обоз на сегодня распродан – приходи завтра");
        }

        try
        {
            _playerResourceManager.WriteOffResources(playerId, new[]
            {
                new Resource
                {
                    Type = new()
                        { Id = CoinResourceTypeId },
                    Value = GetPrice(resourceTypeId) * count,
                },
            });
        }
        catch (BusinessException)
        {
            throw new BusinessException("Не хватает монет – обоз в долг не торгует");
        }

        _playerResourceManager.GrantResource(playerId, resourceTypeId, count);
        row.BoughtCount += count;
        _context.SaveChanges();

        var afterEventAction = _uow.AfterEventAction;
        _uow.AfterEventAction = () =>
        {
            afterEventAction?.Invoke();
            _broker.Publish(playerId, GameStateScopes.State);
        };
    }

    private static int GetLimit(int points)
    {
        return points >= HighLimitReputationThreshold ? HighLimit : BaseLimit;
    }

    private static int GetEffectiveBoughtCount(NeighborConvoy? row, DateTime date)
    {
        if (row == null || date - row.WindowStartDate >= TimeSpan.FromSeconds(WindowDurationSeconds))
        {
            return 0;
        }

        return row.BoughtCount;
    }

    private static int[] GetAssortment(Neighbor neighbor, int points)
    {
        int[] resourceTypeIds = points >= SecondaryReputationThreshold && neighbor.SecondaryResourceTypeId != null
            ? [neighbor.PrimaryResourceTypeId, neighbor.SecondaryResourceTypeId.Value]
            : [neighbor.PrimaryResourceTypeId];

        return resourceTypeIds.Where(x => x != CoinResourceTypeId && x != GoldResourceTypeId).ToArray();
    }
}
