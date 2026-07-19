using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Сколько единиц декора данного типа накоплено у игрока – источник итогового уюта деревни.
/// </summary>
[Table("PlayerDecors")]
[PrimaryKey(nameof(PlayerId), nameof(DecorTypeId))]
public class PlayerDecor
{
    /// <summary>
    /// Часть составного ключа – игрок-владелец декора.
    /// </summary>
    [Column(Order = 1)]
    public int PlayerId { get; set; }

    /// <summary>
    /// Часть составного ключа – тип декора.
    /// </summary>
    [Column(Order = 2)]
    public int DecorTypeId { get; set; }

    /// <summary>
    /// Сколько единиц этого декора у игрока.
    /// </summary>
    /// <remarks>
    /// Повторные покупки и награды складываются в счётчик (см. <see cref="Village.DecorManager.GrantDecor"/>).
    /// </remarks>
    public int Count { get; set; }

    /// <summary>
    /// Навигационное свойство к игроку-владельцу.
    /// </summary>
    public Player Player { get; set; } = null!;

    /// <summary>
    /// Навигационное свойство к типу декора.
    /// </summary>
    public DecorType DecorType { get; set; } = null!;
}
