using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Запас ресурса конкретного типа на складе игрока.
/// </summary>
[Table("Resources")]
public class Resource
{
    /// <summary>
    /// Часть составного ключа – тип ресурса.
    /// </summary>
    [Key]
    [Column(Order = 1)]
    public int TypeId { get; set; }

    /// <summary>
    /// Часть составного ключа – игрок-владелец.
    /// </summary>
    [Key]
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
