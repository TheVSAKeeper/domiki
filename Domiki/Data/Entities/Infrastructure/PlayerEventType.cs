namespace Domiki.Web.Data.Entities;

/// <summary>
/// Вид записи в журнале событий игрока (<see cref="PlayerEvent.Type"/>).
/// </summary>
/// <remarks>
/// Определяет формат <see cref="PlayerEvent.Data"/> и раздел витрины «Пока вас не было».
/// </remarks>
public enum PlayerEventType
{
    /// <summary>
    /// Значение не задано.
    /// </summary>
    None = 0,

    /// <summary>
    /// Завершилось производство.
    /// </summary>
    /// <remarks>
    /// Повторные завершения того же типа домика сливаются в одну запись с накоплением ресурсов и счётчиком циклов
    /// (см. <see cref="Infrastructure.PlayerEventManager.RecordManufactureFinished"/>).
    /// </remarks>
    ManufactureFinished = 1,

    /// <summary>
    /// Завершилось улучшение домика до нового уровня.
    /// </summary>
    DomikUpgraded = 2,

    /// <summary>
    /// Отряд вернулся из похода с добычей.
    /// </summary>
    ExpeditionReturned = 3,

    /// <summary>
    /// Лот игрока на рынке куплен другим игроком.
    /// </summary>
    LotSold = 4,

    /// <summary>
    /// Лот игрока на рынке истёк непроданным.
    /// </summary>
    LotExpired = 5,

    /// <summary>
    /// Толока, в которую игрок вносил вклад, завершена.
    /// </summary>
    TolokaCompleted = 6,

    /// <summary>
    /// Игрок выполнил наказ старосты (<see cref="StarterGoal"/>) и получил награду.
    /// </summary>
    GoalCompleted = 7,

    /// <summary>
    /// Сосед оставил гостинец за возврат игрока после отлучки (см. <see cref="Economy.GiftManager.TryGrantGift"/>).
    /// </summary>
    NeighborGift = 8,

    /// <summary>
    /// Гость расписался в книге гостей деревни игрока.
    /// </summary>
    GuestbookEntryLeft = 9,

    /// <summary>
    /// Гость подсобил деревне – активная работа завершится раньше.
    /// </summary>
    VillageHelped = 10,

    /// <summary>
    /// Поручение соседа завершилось – трудяги вернулись с наградой.
    /// </summary>
    /// <remarks>
    /// См. <see cref="Economy.ErrandManager.FinishErrand"/>.
    /// </remarks>
    ErrandResolved = 11,

    /// <summary>
    /// Трудяга задержался в походе – завязка происшествия.
    /// </summary>
    /// <remarks>
    /// См. <see cref="Activities.IncidentManager.TryRollIncident"/>.
    /// </remarks>
    WorkerMissing = 12,

    /// <summary>
    /// Происшествие завершилось – трудяга вернулся.
    /// </summary>
    /// <remarks>
    /// См. <see cref="Activities.IncidentManager.FinishIncident"/>.
    /// </remarks>
    IncidentResolved = 13,

    /// <summary>
    /// В постройке завязалось происшествие-загадка.
    /// </summary>
    /// <remarks>
    /// См. <see cref="Activities.IncidentManager.TryStartDomikIncident"/>.
    /// </remarks>
    DomikIncidentStarted = 14,

    /// <summary>
    /// Происшествие-загадка в постройке завершилось.
    /// </summary>
    /// <remarks>
    /// См. <see cref="Activities.IncidentManager.FinishIncident"/>.
    /// </remarks>
    DomikIncidentResolved = 15,

    /// <summary>
    /// Трудяга достиг вехи (пассивное событие).
    /// </summary>
    /// <remarks>
    /// См. <see cref="Workers.WorkerMilestoneManager"/>.
    /// </remarks>
    WorkerMilestone = 16,

    /// <summary>
    /// Плащ износился после выработки срока службы и был списан.
    /// </summary>
    CloakWornOut = 17,
}
