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

    /// <summary>
    /// Ближайший порог репутации у этого соседа, который ещё не достигнут.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – игрок уже прошёл все известные пороги этого соседа (обоз, чертежи, декор).
    /// </remarks>
    public int? NextThreshold { get; set; }

    /// <summary>
    /// Название того, что открывает <see cref="NextThreshold"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> вместе с <see cref="NextThreshold"/>, когда порогов впереди больше нет.
    /// </remarks>
    public string? NextRewardName { get; set; }

    /// <summary>
    /// Признак того, что игрок водит дружбу именно с этим соседом.
    /// </summary>
    /// <remarks>
    /// Сравнивается с <see cref="Data.Entities.Player.FriendNeighborId"/>.
    /// </remarks>
    public bool IsFriend { get; set; }

    /// <summary>
    /// Признак того, что дорога к соседу уже открыта по обжитости.
    /// </summary>
    /// <remarks>
    /// Тот же источник, что и у <see cref="Economy.OrderManager.CreateOrder"/>, – <see cref="Village.VillageLevelCalculator.GetOpenNeighbors"/>.
    /// </remarks>
    public bool IsOpen { get; set; }
}
