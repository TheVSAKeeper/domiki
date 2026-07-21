using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Состояние обоза одного соседа у конкретного игрока – скользящее суточное окно покупок.
/// </summary>
/// <remarks>
/// Заполняется и читается в <see cref="Economy.ConvoyManager"/>; отдаётся на клиент как часть <see cref="Economy.Dto.ConvoyDto"/>.
/// </remarks>
[PrimaryKey(nameof(PlayerId), nameof(NeighborId))]
public class NeighborConvoy
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
    /// Начало текущего скользящего окна покупок.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// Сбрасывается на текущий момент, когда очередная покупка происходит спустя <see cref="Economy.ConvoyManager.WindowDurationSeconds"/>
    /// после этого значения.
    /// </remarks>
    public DateTime WindowStartDate { get; set; }

    /// <summary>
    /// Число единиц ресурса, купленных у соседа в текущем окне.
    /// </summary>
    public int BoughtCount { get; set; }

    /// <summary>
    /// Игрок, которому принадлежит обоз.
    /// </summary>
    public Player Player { get; set; } = null!;

    /// <summary>
    /// Сосед, чей обоз описывает эта запись.
    /// </summary>
    public Neighbor Neighbor { get; set; } = null!;
}
