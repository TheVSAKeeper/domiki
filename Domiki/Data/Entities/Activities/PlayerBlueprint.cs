using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Отметка владения чертежом.
/// </summary>
/// <remarks>
/// Наличие строки означает, что игрок получил чертёж и может покупать открытую им постройку.
/// </remarks>
[PrimaryKey(nameof(PlayerId), nameof(BlueprintId))]
public class PlayerBlueprint
{
    /// <summary>
    /// Часть составного ключа – игрок-владелец чертежа.
    /// </summary>
    [Column(Order = 1)]
    public int PlayerId { get; set; }

    /// <summary>
    /// Часть составного ключа – полученный чертёж.
    /// </summary>
    [Column(Order = 2)]
    public int BlueprintId { get; set; }

    /// <summary>
    /// Навигационное свойство к игроку-владельцу.
    /// </summary>
    public Player Player { get; set; } = null!;

    /// <summary>
    /// Навигационное свойство к полученному чертежу.
    /// </summary>
    public Blueprint Blueprint { get; set; } = null!;
}
