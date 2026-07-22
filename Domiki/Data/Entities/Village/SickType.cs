using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник хворей, связанных с погодой.
/// </summary>
public class SickType
{
    /// <summary>
    /// Идентификатор типа хвори.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название хвори.
    /// </summary>
    [MaxLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }

    /// <summary>
    /// Техническое имя хвори.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string LogicName { get; set; }

    /// <summary>
    /// Тип погоды, вызывающий эту хворь.
    /// </summary>
    public int WeatherTypeId { get; set; }

    /// <summary>
    /// Бережёт ли плащ от этой хвори.
    /// </summary>
    public bool CloakProtects { get; set; }

    /// <summary>
    /// Навигационное свойство к погоде, вызывающей хворь.
    /// </summary>
    public WeatherType WeatherType { get; set; } = null!;
}
