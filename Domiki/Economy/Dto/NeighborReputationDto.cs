namespace Domiki.Web.Economy.Dto;

/// <summary>
/// Репутация игрока у одного соседа.
/// </summary>
/// <remarks>
/// Очки накапливаются за сданные заказы (см. <see cref="Economy.OrderManager.CompleteOrder"/>) и открывают чертежи
/// (<see cref="Activities.Dto.BlueprintDto.ReputationThreshold"/>) и декор (<see cref="Village.Dto.DecorTypeDto.ReputationThreshold"/>).
/// </remarks>
public sealed record NeighborReputationDto
{
    /// <summary>
    /// Идентификатор соседа.
    /// </summary>
    public required int NeighborId { get; init; }

    /// <summary>
    /// Отображаемое имя соседа.
    /// </summary>
    public required string NeighborName { get; init; }

    /// <summary>
    /// Техническое имя соседа, используется как ключ на клиенте.
    /// </summary>
    public required string NeighborLogicName { get; init; }

    /// <summary>
    /// Очки репутации у соседа.
    /// </summary>
    /// <remarks>
    /// <c>0</c>, если строка репутации у игрока ещё не заведена. Сравниваются с <see cref="Activities.Dto.BlueprintDto.ReputationThreshold"/>
    /// и <see cref="Village.Dto.DecorTypeDto.ReputationThreshold"/> при открытии чертежей и декора.
    /// </remarks>
    public required int Points { get; init; }

    /// <summary>
    /// Ближайший порог репутации у этого соседа, который ещё не достигнут.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – игрок уже прошёл все известные пороги этого соседа (обоз, чертежи, декор).
    /// </remarks>
    public int? NextThreshold { get; init; }

    /// <summary>
    /// Название того, что открывает <see cref="NextThreshold"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> вместе с <see cref="NextThreshold"/>, когда порогов впереди больше нет.
    /// </remarks>
    public string? NextRewardName { get; init; }

    /// <summary>
    /// Признак того, что игрок водит дружбу именно с этим соседом.
    /// </summary>
    /// <remarks>
    /// Сравнивается с <see cref="Data.Entities.Player.FriendNeighborId"/>; см. <see cref="Economy.OrderManager.SetFriendNeighbor"/>.
    /// </remarks>
    public required bool IsFriend { get; init; }

    /// <summary>
    /// Признак того, что дорога к соседу уже открыта по обжитости.
    /// </summary>
    /// <remarks>
    /// <see langword="false"/> – с соседом ещё не знакомы: дружбу назначить нельзя (см. <see cref="Economy.OrderManager.SetFriendNeighbor"/>),
    /// заказы этого соседа на доску не попадают.
    /// </remarks>
    public required bool IsOpen { get; init; }
}
