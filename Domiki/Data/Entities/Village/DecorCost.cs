using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник цены декора.
/// </summary>
/// <remarks>
/// Сколько единиц ресурса нужно списать за покупку одной единицы декора данного типа.
/// </remarks>
[Table("DecorCosts")]
public class DecorCost
{
    /// <summary>
    /// Часть составного ключа – тип декора, чью цену описывает строка.
    /// </summary>
    [Key]
    [Column(Order = 1)]
    public int DecorTypeId { get; set; }

    /// <summary>
    /// Часть составного ключа – тип ресурса, входящего в цену.
    /// </summary>
    [Key]
    [Column(Order = 2)]
    public int ResourceTypeId { get; set; }

    /// <summary>
    /// Сколько единиц ресурса ResourceTypeId списывается за покупку.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Навигационное свойство к типу декора.
    /// </summary>
    public DecorType DecorType { get; set; }

    /// <summary>
    /// Навигационное свойство к типу ресурса цены.
    /// </summary>
    public ResourceType ResourceType { get; set; }
}
