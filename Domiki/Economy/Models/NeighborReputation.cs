namespace Domiki.Web.Economy.Models;

/// <summary>
/// Репутация игрока у одного соседа.
/// </summary>
/// <remarks>
/// Собирается в <see cref="Economy.OrderManager.GetReputation"/> и отдаётся на клиент как <see cref="Dto.NeighborReputationDto"/>.
/// </remarks>
public class NeighborReputation
{
    /// <summary>
    /// Сосед, у которого накапливается репутация.
    /// </summary>
    public required Neighbor Neighbor { get; set; }

    /// <summary>
    /// Очки репутации у соседа.
    /// </summary>
    /// <remarks>
    /// <c>0</c>, если строка репутации у игрока ещё не заведена.
    /// </remarks>
    public int Points { get; set; }
}
