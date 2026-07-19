using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Один уровень домика конкретного типа: длительность улучшения и лимит одновременных производств.
/// </summary>
[Table("DomikTypeLevels")]
[PrimaryKey(nameof(DomikTypeId), nameof(Value))]
public class DomikTypeLevel
{
    /// <summary>
    /// Часть составного ключа – тип домика.
    /// </summary>
    [Column(Order = 1)]
    public int DomikTypeId { get; set; }

    /// <summary>
    /// Часть составного ключа – номер уровня.
    /// </summary>
    [Column(Order = 2)]
    public int Value { get; set; }

    /// <summary>
    /// Сколько секунд занимает улучшение домика до этого уровня.
    /// </summary>
    public int UpgradeSeconds { get; set; }

    /// <summary>
    /// Сколько производств можно запускать в домике одновременно на этом уровне.
    /// </summary>
    public int MaxManufactureCount { get; set; }

    /// <summary>
    /// Тип домика, которому принадлежит уровень.
    /// </summary>
    public DomikType DomikType { get; set; } = null!;
}
