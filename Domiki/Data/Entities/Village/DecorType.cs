using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник типов декора – украшений деревни, дающих уют (comfort).
/// </summary>
/// <remarks>
/// Покупка декора опционально требует ресурсов или репутации соседа.
/// </remarks>
[Table("DecorTypes")]
public class DecorType
{
    /// <summary>
    /// Идентификатор типа декора.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название декора.
    /// </summary>
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// Технический код типа декора – по нему код находит конкретный декор по смыслу, а не по <see cref="Id"/>.
    /// </summary>
    public required string LogicName { get; set; }

    /// <summary>
    /// Сколько очков уюта даёт одна единица этого декора (см. <see cref="Village.DecorCalculator.GetComfort"/>).
    /// </summary>
    public int ComfortPoints { get; set; }

    /// <summary>
    /// Декор можно купить за ресурсы (см. <see cref="Village.DecorManager.BuyDecor"/>).
    /// </summary>
    /// <remarks>
    /// <see langword="false"/> – выдаётся только как награда (лут экспедиции, большой гостинец соседа).
    /// </remarks>
    public bool IsPurchasable { get; set; }

    /// <summary>
    /// Сосед, чья репутация гейтит покупку декора.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – покупка репутацией не гейтится.
    /// </remarks>
    public int? NeighborId { get; set; }

    /// <summary>
    /// Очки репутации с соседом <see cref="NeighborId"/>, необходимые для покупки декора.
    /// </summary>
    /// <remarks>
    /// Не имеет смысла при <see cref="NeighborId"/> == <see langword="null"/>.
    /// </remarks>
    public int ReputationThreshold { get; set; }
}
