using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник видов модификаторов, которые может давать уровень домика (например, добавка вместимости трудяг).
/// </summary>
public class ModificatorType
{
    /// <summary>
    /// Идентификатор вида модификатора.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название модификатора.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Технический код вида модификатора – по нему код находит нужный <see cref="DomikTypeLevelModificator"/>, а не по <see cref="Id"/>.
    /// </summary>
    public required string LogicName { get; set; }
}
