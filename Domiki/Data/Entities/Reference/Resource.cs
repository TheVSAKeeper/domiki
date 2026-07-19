using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Запас ресурса конкретного типа на складе игрока.
/// </summary>
[PrimaryKey(nameof(PlayerId), nameof(TypeId))]
public class Resource
{
    /// <summary>
    /// Часть составного ключа – тип ресурса.
    /// </summary>
    [Column(Order = 1)]
    public int TypeId { get; set; }

    /// <summary>
    /// Часть составного ключа – игрок-владелец.
    /// </summary>
    [Column(Order = 2)]
    public int PlayerId { get; set; }

    /// <summary>
    /// Сколько ресурса этого типа сейчас на складе у игрока.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Игрок-владелец запаса.
    /// </summary>
    public Player Player { get; set; } = null!;
}
