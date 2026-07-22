using Domiki.Web.Data;
using Domiki.Web.Reference;
using Resource = Domiki.Web.Reference.Models.Resource;

namespace Domiki.Web.Workers;

/// <summary>
/// Правила Корчмы: её уровень и подбор еды из запасов игрока.
/// </summary>
public class TavernManager
{
    /// <summary>
    /// Уровень Корчмы, с которого она кормит уставших трудяг.
    /// </summary>
    public const int MealMinLevel = 1;

    /// <summary>
    /// Уровень Корчмы, с которого она автоматически собирает провиант в поход.
    /// </summary>
    public const int ProvisionMinLevel = 2;

    /// <summary>
    /// Уровень Корчмы, с которого открывается тёплый угол.
    /// </summary>
    public const int WarmCornerMinLevel = 3;

    /// <summary>
    /// Доля срока хвори, которую сокращает тёплый угол Корчмы.
    /// </summary>
    /// <value>Проценты.</value>
    public const int WarmCornerRecoveryPercent = 25;

    private readonly ApplicationDbContext _context;
    private readonly ResourceManager _resourceManager;

    /// <summary>
    /// Создаёт менеджер правил Корчмы.
    /// </summary>
    /// <param name="context">Контекст данных текущего запроса.</param>
    /// <param name="resourceManager">Кэш справочников игры.</param>
    public TavernManager(ApplicationDbContext context, ResourceManager resourceManager)
    {
        _context = context;
        _resourceManager = resourceManager;
    }

    /// <summary>
    /// Возвращает наивысший уровень Корчмы игрока.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <returns>Наивысший уровень Корчмы либо <c>0</c>, если её нет.</returns>
    public int GetLevel(int playerId)
    {
        var typeId = _resourceManager.GetDomikTypes().First(x => x.LogicName == "tavern").Id;
        return _context.Domiks
                   .Where(x => x.PlayerId == playerId && x.TypeId == typeId)
                   .Select(x => (int?)x.Level)
                   .Max()
               ?? 0;
    }

    /// <summary>
    /// Подбирает еду из запасов игрока, начиная с наименее ценной на рынке.
    /// </summary>
    /// <remarks>
    /// Метод только рассчитывает списание. Само списание выполняет <see cref="Infrastructure.PlayerResourceManager.WriteOffResources"/>.
    /// </remarks>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="count">Число нужных единиц еды.</param>
    /// <returns>Набор ресурсов для списания либо пустой массив, когда еды недостаточно.</returns>
    public Resource[] CollectFood(int playerId, int count)
    {
        var stocks = _context.Resources.Where(x => x.PlayerId == playerId).ToArray()
            .Union(_context.Resources.Local.Where(x => x.PlayerId == playerId))
            .Where(x => x.Value > 0)
            .ToDictionary(x => x.TypeId, x => x.Value);
        var foodTypes = _resourceManager.GetResourceTypes()
            .Where(x => x.IsFood && stocks.ContainsKey(x.Id))
            .OrderBy(x => ResourceManager.GetMarketValue(x.Id))
            .ThenBy(x => x.Id)
            .ToArray();
        var food = new List<Resource>();
        var remaining = count;
        foreach (var type in foodTypes)
        {
            var value = Math.Min(stocks[type.Id], remaining);
            if (value > 0)
            {
                food.Add(new() { Type = type, Value = value });
                remaining -= value;
            }

            if (remaining == 0)
            {
                return food.ToArray();
            }
        }

        return [];
    }
}
