namespace Domiki.Web.Economy.Dto;

/// <summary>
/// Репутация игрока у одного соседа.
/// </summary>
/// <remarks>
/// Очки накапливаются за сданные заказы (см. <see cref="Economy.OrderManager.CompleteOrder"/>) и открывают чертежи
/// (<see cref="Activities.Dto.BlueprintDto.ReputationThreshold"/>) и декор (<see cref="Village.Dto.DecorTypeDto.ReputationThreshold"/>).
/// </remarks>
public class NeighborReputationDto
{
    /// <summary>
    /// Идентификатор соседа.
    /// </summary>
    public int NeighborId { get; set; }

    /// <summary>
    /// Отображаемое имя соседа.
    /// </summary>
    public string NeighborName { get; set; }

    /// <summary>
    /// Техническое имя соседа, используется как ключ на клиенте.
    /// </summary>
    public string NeighborLogicName { get; set; }

    /// <summary>
    /// Очки репутации у соседа.
    /// </summary>
    /// <remarks>
    /// <c>0</c>, если строка репутации у игрока ещё не заведена. Сравниваются с <see cref="Activities.Dto.BlueprintDto.ReputationThreshold"/>
    /// и <see cref="Village.Dto.DecorTypeDto.ReputationThreshold"/> при открытии чертежей и декора.
    /// </remarks>
    public int Points { get; set; }
}
