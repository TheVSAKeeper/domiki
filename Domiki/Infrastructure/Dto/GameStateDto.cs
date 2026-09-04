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
    /// Идентификатор игрока, которому принадлежит снимок.
    /// </summary>
    /// <remarks>
    /// Не секрет: клиент подписывает им офлайн-снимок состояния и не показывает чужой снимок на общем устройстве.
    /// </remarks>
    public required int PlayerId { get; init; }

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
    /// Не более <see cref="OrderBoardSize"/> одновременно.
    /// </remarks>
    public required OrderDto[] Orders { get; init; }

    /// <summary>
    /// Сколько ячеек на доске заказов – по уровню «Ямской избы» игрока.
    /// </summary>
    /// <remarks>
    /// От <see cref="Economy.OrderManager.BoardSizeBase"/> до <see cref="Economy.OrderManager.BoardSizeMax"/>
    /// (см. <see cref="Economy.OrderManager.GetBoardSize(int)"/>).
    /// </remarks>
    public required int OrderBoardSize { get; init; }

    /// <summary>
    /// Осталась ли у игрока бесплатная уступка – та, что не отодвигает пополнение доски.
    /// </summary>
    /// <remarks>
    /// <see langword="false"/> у изб ниже <see cref="Economy.OrderManager.FreeConcessionLevel"/> уровня и пока не
    /// закончилось суточное окно (см. <see cref="Economy.OrderManager.IsFreeConcessionAvailable(int)"/>).
    /// </remarks>
    public required bool OrderFreeConcession { get; init; }

    /// <summary>
    /// Репутация игрока у всех соседей.
    /// </summary>
    public required NeighborReputationDto[] Reputation { get; init; }

    /// <summary>
    /// Незавершённые поручения соседей – офферы и принятые.
    /// </summary>
    /// <remarks>
    /// Пусто, если поручений нет; больше одного – с четвёртого уровня «Ямской избы»
    /// (см. <see cref="Economy.ErrandManager.GetAll"/>).
    /// </remarks>
    public required ErrandDto[] Errands { get; init; }

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
    /// Сколько золота добыто рудником за текущие сутки UTC.
    /// </summary>
    /// <remarks>
    /// Суточный кап добычи равен уровню рудника – клиент показывает остаток дневной жилы на карточке рудника
    /// (см. <see cref="Core.DomikManager.GetGoldMinedToday"/>).
    /// </remarks>
    public required int GoldMinedToday { get; init; }

    /// <summary>
    /// Трудяги игрока.
    /// </summary>
    public required WorkerDto[] Workers { get; init; }

    /// <summary>
    /// Запас, выдача и износ плащей игрока.
    /// </summary>
    public required CloakStateDto Cloaks { get; init; }

    /// <summary>
    /// Кладовая Корчмы: правила подбора еды по каждому съестному припасу.
    /// </summary>
    public required TavernLarderDto Larder { get; init; }

    /// <summary>
    /// Счётная книга Избы старосты за текущие сутки.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – Избы у игрока нет, книга закрыта (см. <see cref="Economy.ElderHouseManager.GetLedger"/>).
    /// </remarks>
    public LedgerDto? Ledger { get; init; }

    /// <summary>
    /// Заповеданные от нарядов припасы игрока.
    /// </summary>
    /// <remarks>
    /// Пусто, если ничего не заповедано или заповедный ларь ещё не открыт
    /// (см. <see cref="Economy.ElderHouseManager.ReserveMinLevel"/>).
    /// </remarks>
    public required ResourceReserveDto[] Reserves { get; init; }

    /// <summary>
    /// Номер домика, помеченного задумкой – целью, на улучшение которой игрок копит.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – задумки нет. Со склада задумка ничего не списывает (§4.4 дизайна).
    /// </remarks>
    public int? IntentDomikId { get; init; }

    /// <summary>
    /// Припас, заповеданный от нарядов самой задумкой.
    /// </summary>
    /// <remarks>
    /// Пусто, если задумки нет или заповедный ларь ещё не открыт (см. <see cref="Economy.ElderHouseManager.ReserveMinLevel"/>).
    /// Складывается с ручной заповедью по правилу «строже из двух».
    /// </remarks>
    public required ResourceReserveDto[] IntentReserves { get; init; }

    /// <summary>
    /// Справочник хворей, связанных с погодой.
    /// </summary>
    public required SickTypeDto[] SickTypes { get; init; }

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
    /// Обозы соседей, открытых по обжитости деревни, с ассортиментом и остатком суточного лимита.
    /// </summary>
    /// <remarks>
    /// См. <see cref="Economy.ConvoyManager.GetConvoys"/>.
    /// </remarks>
    public required ConvoyDto[] Convoys { get; init; }

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

    /// <summary>
    /// Справочник уклада деревни: соседи и постройки их специализации.
    /// </summary>
    /// <remarks>
    /// См. <see cref="Reference.ResourceManager.GetVillageProfileEffects"/>.
    /// </remarks>
    public required VillageProfileDto[] VillageProfiles { get; init; }

    /// <summary>
    /// Переезд в новую долину: гейт, сводка сборов, узелки памяти и лесенка перков.
    /// </summary>
    /// <remarks>
    /// Памятный столб в снимок не входит – он берётся отдельным запросом
    /// (см. <see cref="Village.RelocationController.GetMemorialPost"/>).
    /// </remarks>
    public required RelocationDto Relocation { get; init; }
}
