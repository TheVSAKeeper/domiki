using Domiki.Web.Data.Entities;

namespace Domiki.Web.Infrastructure;

/// <summary>
/// Каноническая карта «тип события → группа журнала».
/// </summary>
/// <remarks>
/// Источник истины для серверной фильтрации и для подписей на клиенте. Новый <see cref="PlayerEventType"/> обязан
/// попасть сюда: непокрытый тип валит <c>PlayerEventGroupsTests</c>, иначе он молча выпал бы из любого фильтра.
/// </remarks>
public static class PlayerEventGroups
{
    private static readonly Dictionary<PlayerEventType, PlayerEventGroup> GroupByType = new()
    {
        [PlayerEventType.ManufactureFinished] = PlayerEventGroup.Household,
        [PlayerEventType.ManufactureRepeatFailed] = PlayerEventGroup.Household,
        [PlayerEventType.ManufactureMeasureMet] = PlayerEventGroup.Household,
        [PlayerEventType.ManufactureReserveHeld] = PlayerEventGroup.Household,
        [PlayerEventType.ManufactureGoldCapReached] = PlayerEventGroup.Household,

        [PlayerEventType.WorkerMeal] = PlayerEventGroup.Workers,
        [PlayerEventType.CloakWornOut] = PlayerEventGroup.Workers,
        [PlayerEventType.WorkerMissing] = PlayerEventGroup.Workers,
        [PlayerEventType.IncidentResolved] = PlayerEventGroup.Workers,
        [PlayerEventType.WorkerMilestone] = PlayerEventGroup.Workers,

        [PlayerEventType.DomikUpgraded] = PlayerEventGroup.Village,
        [PlayerEventType.DomikIncidentStarted] = PlayerEventGroup.Village,
        [PlayerEventType.DomikIncidentResolved] = PlayerEventGroup.Village,
        [PlayerEventType.GoalCompleted] = PlayerEventGroup.Village,
        [PlayerEventType.ExpeditionReturned] = PlayerEventGroup.Village,
        [PlayerEventType.TolokaCompleted] = PlayerEventGroup.Village,
        [PlayerEventType.Relocated] = PlayerEventGroup.Village,

        [PlayerEventType.NeighborGift] = PlayerEventGroup.Guests,
        [PlayerEventType.GuestbookEntryLeft] = PlayerEventGroup.Guests,
        [PlayerEventType.VillageHelped] = PlayerEventGroup.Guests,
        [PlayerEventType.ErrandResolved] = PlayerEventGroup.Guests,

        [PlayerEventType.LotSold] = PlayerEventGroup.Market,
        [PlayerEventType.LotExpired] = PlayerEventGroup.Market,
    };

    /// <summary>
    /// Типы событий, входящие в группу.
    /// </summary>
    /// <param name="group">Группа журнала.</param>
    /// <returns>Типы событий группы.</returns>
    public static PlayerEventType[] TypesOf(PlayerEventGroup group)
    {
        return GroupByType.Where(x => x.Value == group).Select(x => x.Key).ToArray();
    }

    /// <summary>
    /// Группа, к которой относится тип события.
    /// </summary>
    /// <param name="type">Тип события.</param>
    /// <returns><see cref="PlayerEventGroup.None"/> для типа, не попавшего в карту.</returns>
    public static PlayerEventGroup Of(PlayerEventType type)
    {
        return GroupByType.GetValueOrDefault(type, PlayerEventGroup.None);
    }
}
