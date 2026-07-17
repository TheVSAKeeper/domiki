using Domiki.Web.Activities.Dto;
using Domiki.Web.Core.Dto;
using Domiki.Web.Economy.Dto;
using Domiki.Web.Reference.Dto;
using Domiki.Web.Village.Dto;
using Domiki.Web.Workers.Dto;

namespace Domiki.Web.Infrastructure.Dto;

/// <summary>
/// Полный снимок состояния игры для одного игрока.
/// </summary>
/// <remarks>
/// Единственный ответ, который реально зовёт SPA за игровыми данными – <see cref="Infrastructure.GameStateController.GetGameState"/>.
/// </remarks>
public class GameStateDto
{
    /// <summary>
    /// Справочник типов построек вместе с персонализацией под игрока.
    /// </summary>
    /// <remarks>
    /// Персонализация – доступное количество, привязанный чертёж и гейты открытия (см. <see cref="DomikTypeDto"/>).
    /// </remarks>
    public DomikTypeDto[] DomikTypes { get; set; }

    /// <summary>
    /// Справочник типов ресурсов.
    /// </summary>
    public ResourceTypeDto[] ResourceTypes { get; set; }

    /// <summary>
    /// Справочник рецептов производства.
    /// </summary>
    public ReceiptDto[] Receipts { get; set; }

    /// <summary>
    /// Домики игрока.
    /// </summary>
    public DomikDto[] Domiks { get; set; }

    /// <summary>
    /// Остатки ресурсов на складе игрока.
    /// </summary>
    public ResourceDto[] Resources { get; set; }

    /// <summary>
    /// Активные заказы на доске заказов игрока.
    /// </summary>
    /// <remarks>
    /// Не более <see cref="Economy.OrderManager.BoardSize"/> одновременно.
    /// </remarks>
    public OrderDto[] Orders { get; set; }

    /// <summary>
    /// Репутация игрока у всех соседей.
    /// </summary>
    public NeighborReputationDto[] Reputation { get; set; }

    /// <summary>
    /// Чертежи и их владение игроком.
    /// </summary>
    public BlueprintDto[] Blueprints { get; set; }

    /// <summary>
    /// Общее состояние деревни игрока.
    /// </summary>
    public VillageDto Village { get; set; }

    /// <summary>
    /// Обжитость деревни и её слагаемые.
    /// </summary>
    public VillageLevelDto VillageLevel { get; set; }

    /// <summary>
    /// Трудяги игрока.
    /// </summary>
    public WorkerDto[] Workers { get; set; }

    /// <summary>
    /// Типы построек, доступные к покупке прямо сейчас.
    /// </summary>
    /// <remarks>
    /// Учитывает лимиты и гейты количества (см. <see cref="Core.DomikManager.GetPurchaseAvailableDomiks"/>).
    /// </remarks>
    public DomikTypeDto[] PurchaseAvailableDomiks { get; set; }

    /// <summary>
    /// Текущая погода и её влияние на производство.
    /// </summary>
    public WeatherStateDto Weather { get; set; }

    /// <summary>
    /// Состояние экспедиций игрока.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – механика экспедиций игроку ещё недоступна.
    /// </remarks>
    public ExpeditionStateDto Expeditions { get; set; }

    /// <summary>
    /// Декор игрока и накопленный уют.
    /// </summary>
    public DecorStateDto Decor { get; set; }

    /// <summary>
    /// Текущая толока и вклад игрока в неё.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – толока ещё недоступна игроку.
    /// </remarks>
    public TolokaStateDto Toloka { get; set; }

    /// <summary>
    /// Состояние ярмарки игрока.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – Торговый двор ещё не построен (см. <see cref="Economy.MarketManager.GetMarket"/>).
    /// </remarks>
    public MarketStateDto Market { get; set; }

    /// <summary>
    /// Сводка «Пока вас не было».
    /// </summary>
    /// <remarks>
    /// Непоказанные события с прошлого захода; выдаётся один раз и тут же помечается прочитанной
    /// (см. <see cref="Infrastructure.PlayerEventManager.TakeRecap"/>).
    /// </remarks>
    public RecapDto Recap { get; set; }

    /// <summary>
    /// Журнал последних событий игрока для ленты/истории на клиенте.
    /// </summary>
    /// <remarks>
    /// Включает уже показанные в <see cref="RecapDto"/> (см. <see cref="Infrastructure.PlayerEventManager.GetRecentEvents"/>).
    /// </remarks>
    public RecapEventDto[] Events { get; set; }

    /// <summary>
    /// Состояние текущих целей игрока.
    /// </summary>
    public GoalsStateDto Goals { get; set; }
}
