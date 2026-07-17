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
}
