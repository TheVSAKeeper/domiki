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
public sealed record GameStateDto
{
    /// <summary>
    /// Справочник типов построек вместе с персонализацией под игрока.
    /// </summary>
    /// <remarks>
    /// Персонализация – доступное количество, привязанный чертёж и гейты открытия (см. <see cref="DomikTypeDto"/>).
    /// </remarks>
    public required DomikTypeDto[] DomikTypes { get; init; }

    /// <summary>
    /// Справочник типов ресурсов.
    /// </summary>
    public required ResourceTypeDto[] ResourceTypes { get; init; }

    /// <summary>
    /// Справочник рецептов производства.
    /// </summary>
    public required ReceiptDto[] Receipts { get; init; }

    /// <summary>
    /// Домики игрока.
    /// </summary>
    public required DomikDto[] Domiks { get; init; }

    /// <summary>
    /// Остатки ресурсов на складе игрока.
    /// </summary>
    public required ResourceDto[] Resources { get; init; }

    /// <summary>
    /// Активные заказы на доске заказов игрока.
    /// </summary>
    /// <remarks>
    /// Не более <see cref="Economy.OrderManager.BoardSize"/> одновременно.
    /// </remarks>
    public required OrderDto[] Orders { get; init; }

    /// <summary>
    /// Репутация игрока у всех соседей.
    /// </summary>
    public required NeighborReputationDto[] Reputation { get; init; }

    /// <summary>
    /// Активное поручение соседа – оффер или принятое.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – у игрока нет незавершённого поручения (см. <see cref="Economy.ErrandManager.Get"/>).
    /// </remarks>
    public ErrandDto? Errand { get; init; }

    /// <summary>
    /// Активное происшествие с пропавшим в походе трудягой.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – у игрока нет незавершённого происшествия (см. <see cref="Activities.IncidentManager.Get"/>).
    /// </remarks>
    public IncidentDto? Incident { get; init; }

    /// <summary>
    /// Активное происшествие-загадка в постройке.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – у игрока нет незавершённого происшествия этого источника (см. <see cref="Activities.IncidentManager.GetDomik"/>).
    /// </remarks>
    public DomikIncidentDto? DomikIncident { get; init; }

    /// <summary>
    /// Чертежи и их владение игроком.
    /// </summary>
    public required BlueprintDto[] Blueprints { get; init; }

    /// <summary>
    /// Общее состояние деревни игрока.
    /// </summary>
    public required VillageDto Village { get; init; }

    /// <summary>
    /// Обжитость деревни и её слагаемые.
    /// </summary>
    public required VillageLevelDto VillageLevel { get; init; }

    /// <summary>
    /// Трудяги игрока.
    /// </summary>
    public required WorkerDto[] Workers { get; init; }

    /// <summary>
    /// Типы построек, доступные к покупке прямо сейчас.
    /// </summary>
    /// <remarks>
    /// Учитывает лимиты и гейты количества (см. <see cref="Core.DomikManager.GetPurchaseAvailableDomiks"/>).
    /// </remarks>
    public required DomikTypeDto[] PurchaseAvailableDomiks { get; init; }

    /// <summary>
    /// Текущая погода и её влияние на производство.
    /// </summary>
    public required WeatherStateDto Weather { get; init; }

    /// <summary>
    /// Состояние экспедиций игрока.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – механика экспедиций игроку ещё недоступна.
    /// </remarks>
    public ExpeditionStateDto? Expeditions { get; init; }

    /// <summary>
    /// Декор игрока и накопленный уют.
    /// </summary>
    public required DecorStateDto Decor { get; init; }

    /// <summary>
    /// Текущая толока и вклад игрока в неё.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – толока ещё недоступна игроку.
    /// </remarks>
    public TolokaStateDto? Toloka { get; init; }

    /// <summary>
    /// Состояние ярмарки игрока.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – Торговый двор ещё не построен (см. <see cref="Economy.MarketManager.GetMarket"/>).
    /// </remarks>
    public MarketStateDto? Market { get; init; }

    /// <summary>
    /// Сводка «Пока вас не было».
    /// </summary>
    /// <remarks>
    /// Непоказанные события с прошлого захода; выдаётся один раз и тут же помечается прочитанной
    /// (см. <see cref="Infrastructure.PlayerEventManager.TakeRecap"/>).
    /// </remarks>
    public required RecapDto Recap { get; init; }

    /// <summary>
    /// Журнал последних событий игрока для ленты/истории на клиенте.
    /// </summary>
    /// <remarks>
    /// Включает уже показанные в <see cref="RecapDto"/> (см. <see cref="Infrastructure.PlayerEventManager.GetRecentEvents"/>).
    /// </remarks>
    public required RecapEventDto[] Events { get; init; }

    /// <summary>
    /// Состояние текущих целей игрока.
    /// </summary>
    public required GoalsStateDto Goals { get; init; }
}
