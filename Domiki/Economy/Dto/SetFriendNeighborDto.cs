namespace Domiki.Web.Economy.Dto;

/// <summary>
/// Запрос на назначение (или снятие) дружбы игрока с соседом.
/// </summary>
public sealed record SetFriendNeighborDto
{
    /// <summary>
    /// Сосед, с которым назначается дружба – ссылка на справочник <see cref="Economy.Models.Neighbor.Id"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> снимает дружбу (см. <see cref="Economy.OrderManager.SetFriendNeighbor"/>).
    /// </remarks>
    public int? NeighborId { get; init; }
}
