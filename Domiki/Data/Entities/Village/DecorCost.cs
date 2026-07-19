using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник цены декора.
/// </summary>
/// <remarks>
/// Сколько единиц ресурса нужно списать за покупку одной единицы декора данного типа.
/// </remarks>
[Table("DecorCosts")]
[PrimaryKey(nameof(DecorTypeId), nameof(ResourceTypeId))]
public class DecorCost
{
    /// <summary>
    /// Часть составного ключа – тип декора, чью цену описывает строка.
    /// </summary>
    [Column(Order = 1)]
    public int DecorTypeId { get; set; }

    /// <summary>
    /// Часть составного ключа – тип ресурса, входящего в цену.
    /// </summary>
    [Column(Order = 2)]
    public int ResourceTypeId { get; set; }

    /// <summary>
    /// Сколько единиц ресурса ResourceTypeId списывается за покупку.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Навигационное свойство к типу декора.
    /// </summary>
    public DecorType DecorType { get; set; } = null!;

    /// <summary>
    /// Навигационное свойство к типу ресурса цены.
    /// </summary>
    public ResourceType ResourceType { get; set; } = null!;
}
