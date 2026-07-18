using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник типов толоки: набор позиций корзины сбора и вес при случайном выборе следующей инстанции.
/// </summary>
/// <remarks>
/// Позиции корзины – в отдельной таблице <see cref="TolokaTypePosition"/> (не навигация, зеркалит <see cref="TolokaTypeEffect"/>).
/// </remarks>
[Table("TolokaTypes")]
public class TolokaType
{
    /// <summary>
    /// Идентификатор типа толоки.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название толоки.
    /// </summary>
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// Технический код типа толоки – по нему код находит конкретную толоку по смыслу, а не по <see cref="Id"/>.
    /// </summary>
    public required string LogicName { get; set; }

    /// <summary>
    /// Вес типа при взвешенном случайном выборе следующей инстанции толоки.
    /// </summary>
    /// <remarks>
    /// См. <see cref="Activities.TolokaManager.PickTolokaType"/>.
    /// </remarks>
    public int RotationWeight { get; set; }
}
