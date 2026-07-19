using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Репутация игрока у конкретного соседа.
/// </summary>
[Table("NeighborReputations")]
[PrimaryKey(nameof(PlayerId), nameof(NeighborId))]
public class NeighborReputation
{
    /// <summary>
    /// Часть составного ключа – игрок.
    /// </summary>
    [Column(Order = 1)]
    public int PlayerId { get; set; }

    /// <summary>
    /// Часть составного ключа – сосед.
    /// </summary>
    [Column(Order = 2)]
    public int NeighborId { get; set; }

    /// <summary>
    /// Накопленные очки репутации у этого соседа.
    /// </summary>
    /// <remarks>
    /// Растут за выполненные заказы (<see cref="Order.RewardReputation"/>) и открывают декор и чертежи построек.
    /// </remarks>
    public int Points { get; set; }

    /// <summary>
    /// Игрок, которому принадлежит репутация.
    /// </summary>
    public Player Player { get; set; } = null!;

    /// <summary>
    /// Сосед, у которого накапливается репутация.
    /// </summary>
    public Neighbor Neighbor { get; set; } = null!;
}
