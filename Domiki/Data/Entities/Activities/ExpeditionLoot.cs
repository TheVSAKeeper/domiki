using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник строк лут-таблицы похода: что может выпасть отряду и с каким весом.
/// </summary>
/// <remarks>
/// См. <see cref="Activities.ExpeditionManager.PickLoot"/>.
/// </remarks>
[Table("ExpeditionLoot")]
public class ExpeditionLoot
{
    /// <summary>
    /// Идентификатор строки лут-таблицы.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Тип похода, которому принадлежит эта строка лут-таблицы.
    /// </summary>
    public int ExpeditionTypeId { get; set; }

    /// <summary>
    /// Что именно выдаёт строка – ресурс, декор, повышение черты трудяги или чертёж.
    /// </summary>
    /// <remarks>
    /// Определяет, какие из полей <see cref="ResourceTypeId"/>/<see cref="DecorTypeId"/>/<see cref="BlueprintId"/>/<see cref="MinValue"/>/<see cref="MaxValue"/> актуальны.
    /// </remarks>
    public ExpeditionLootKind Kind { get; set; }

    /// <summary>
    /// Тип ресурса – заполняется, когда <see cref="Kind"/> равен <see cref="ExpeditionLootKind.Resource"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> для остальных видов лута.
    /// </remarks>
    public int? ResourceTypeId { get; set; }

    /// <summary>
    /// Тип декора – заполняется, когда <see cref="Kind"/> равен <see cref="ExpeditionLootKind.Decor"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> для остальных видов лута.
    /// </remarks>
    public int? DecorTypeId { get; set; }

    /// <summary>
    /// Конкретный чертёж – заполняется, когда <see cref="Kind"/> равен <see cref="ExpeditionLootKind.Blueprint"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – выдаётся случайный из ещё не полученных игроком.
    /// </remarks>
    public int? BlueprintId { get; set; }

    /// <summary>
    /// Нижняя граница выдаваемого количества ресурса (только для <see cref="Kind"/> == <see cref="ExpeditionLootKind.Resource"/>).
    /// </summary>
    public int MinValue { get; set; }

    /// <summary>
    /// Верхняя граница выдаваемого количества ресурса включительно (только для <see cref="Kind"/> == <see cref="ExpeditionLootKind.Resource"/>).
    /// </summary>
    public int MaxValue { get; set; }

    /// <summary>
    /// Вес строки во взвешенном ролле лута.
    /// </summary>
    /// <remarks>
    /// Чем больше, тем чаще выпадает относительно других строк того же похода.
    /// </remarks>
    public int Weight { get; set; }

    /// <summary>
    /// Редкая находка.
    /// </summary>
    /// <remarks>
    /// Участвует в pity-механике (<see cref="Player.ExpeditionsSincePity"/>) и в бонусе к весу от удачи трудяг (<see cref="Trait.LuckWeightPercent"/>).
    /// </remarks>
    public bool IsRare { get; set; }

    /// <summary>
    /// Навигационное свойство к типу похода.
    /// </summary>
    public ExpeditionType ExpeditionType { get; set; } = null!;
}
