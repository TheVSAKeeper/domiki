using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник типов ресурсов.
/// </summary>
public class ResourceType
{
    /// <summary>
    /// Идентификатор типа ресурса.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название ресурса.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Технический код типа ресурса – по нему код находит конкретный ресурс по смыслу, а не по <see cref="Id"/>.
    /// </summary>
    public required string LogicName { get; set; }

    /// <summary>
    /// Можно ли использовать ресурс как еду для трудяг и провиант в походах.
    /// </summary>
    public bool IsFood { get; set; }
}
