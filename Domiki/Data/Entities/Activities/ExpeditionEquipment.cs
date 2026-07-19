using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник снаряжения экспедиции: сколько ресурса списывается при отправке отряда данного типа похода.
/// </summary>
[PrimaryKey(nameof(ExpeditionTypeId), nameof(ResourceTypeId))]
public class ExpeditionEquipment
{
    /// <summary>
    /// Часть составного ключа – тип похода, к которому относится строка снаряжения.
    /// </summary>
    [Column(Order = 1)]
    public int ExpeditionTypeId { get; set; }

    /// <summary>
    /// Часть составного ключа – тип ресурса, который списывается как снаряжение.
    /// </summary>
    [Column(Order = 2)]
    public int ResourceTypeId { get; set; }

    /// <summary>
    /// Сколько единиц ресурса списывается при отправке отряда.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Необязательное снаряжение (провизия).
    /// </summary>
    /// <remarks>
    /// Списывается только если игрок явно включил её при отправке
    /// (см. <see cref="Activities.ExpeditionManager.StartExpedition"/>, флаг <c>provisions</c>); обязательное списывается всегда.
    /// </remarks>
    public bool IsOptional { get; set; }

    /// <summary>
    /// Навигационное свойство к типу похода.
    /// </summary>
    public ExpeditionType ExpeditionType { get; set; } = null!;
}
