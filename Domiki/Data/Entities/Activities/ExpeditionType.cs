using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник типов похода.
/// </summary>
/// <remarks>
/// Описывает, сколько трудяг и золота требует отправка, сколько длится поход и сколько раз ролльнуть лут-таблицу по возвращении.
/// </remarks>
public class ExpeditionType
{
    /// <summary>
    /// Идентификатор типа похода.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название похода.
    /// </summary>
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// Технический код типа похода – по нему код находит конкретный поход по смыслу, а не по <see cref="Id"/>.
    /// </summary>
    public required string LogicName { get; set; }

    /// <summary>
    /// Сколько секунд длится поход от отправки до возвращения отряда.
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Сколько трудяг требуется для отправки отряда этого типа.
    /// </summary>
    public int WorkerCount { get; set; }

    /// <summary>
    /// Сколько золота списывается при отправке отряда – всегда обязательное снаряжение.
    /// </summary>
    /// <value>Золото.</value>
    public int GoldCost { get; set; }

    /// <summary>
    /// Сколько раз ролльнуть лут-таблицу по возвращении отряда.
    /// </summary>
    /// <remarks>
    /// Столько независимых находок получает игрок.
    /// </remarks>
    public int RollCount { get; set; }
}
