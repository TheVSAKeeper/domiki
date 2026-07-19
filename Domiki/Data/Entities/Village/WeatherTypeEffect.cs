using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник погодных модификаторов производства: на какой тип домика влияет погода и какой процент выхода она даёт.
/// </summary>
[Table("WeatherTypeEffects")]
[PrimaryKey(nameof(WeatherTypeId), nameof(DomikTypeId))]
public class WeatherTypeEffect
{
    /// <summary>
    /// Часть составного ключа – тип погоды, чей эффект описывает строка.
    /// </summary>
    [Column(Order = 1)]
    public int WeatherTypeId { get; set; }

    /// <summary>
    /// Часть составного ключа – тип домика, на который влияет погода.
    /// </summary>
    [Column(Order = 2)]
    public int DomikTypeId { get; set; }

    /// <summary>
    /// Процент выхода производства при этой погоде.
    /// </summary>
    /// <value>Проценты, где <c>100</c> – без изменений, меньше <c>100</c> – просадка (например, засуха для карьера).</value>
    public int OutputPercent { get; set; }

    /// <summary>
    /// Навигационное свойство к типу погоды.
    /// </summary>
    public WeatherType WeatherType { get; set; } = null!;
}
