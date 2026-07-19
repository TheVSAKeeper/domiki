using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Ресурс и его количество, которые нужно сдать для выполнения заказа.
/// </summary>
[Table("OrderResources")]
[PrimaryKey(nameof(OrderId), nameof(ResourceTypeId))]
public class OrderResource
{
    /// <summary>
    /// Часть составного ключа – заказ.
    /// </summary>
    [Column(Order = 1)]
    public int OrderId { get; set; }

    /// <summary>
    /// Часть составного ключа – тип требуемого ресурса.
    /// </summary>
    [Column(Order = 2)]
    public int ResourceTypeId { get; set; }

    /// <summary>
    /// Сколько ресурса нужно сдать для выполнения заказа.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Заказ, к которому относится требование.
    /// </summary>
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Тип требуемого ресурса.
    /// </summary>
    public ResourceType ResourceType { get; set; } = null!;
}
