using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник баффов завершённой толоки: на какой тип домика распространяется эффект и какой процент выхода производства он даёт участникам.
/// </summary>
[Table("TolokaTypeEffects")]
public class TolokaTypeEffect
{
    /// <summary>
    /// Часть составного ключа – тип толоки, чей бафф описывает строка.
    /// </summary>
    [Key]
    [Column(Order = 1)]
    public int TolokaTypeId { get; set; }

    /// <summary>
    /// Часть составного ключа – тип домика, на который распространяется бафф.
    /// </summary>
    [Key]
    [Column(Order = 2)]
    public int DomikTypeId { get; set; }

    /// <summary>
    /// Процент выхода производства при действующем баффе.
    /// </summary>
    /// <value>Проценты, где <c>100</c> – без изменений, больше <c>100</c> – усиление.</value>
    public int OutputPercent { get; set; }

    /// <summary>
    /// Навигационное свойство к типу толоки.
    /// </summary>
    public TolokaType TolokaType { get; set; } = null!;
}
