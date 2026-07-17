using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник типов домиков (построек) и их уровней.
/// </summary>
[Table("DomikTypes")]
public class DomikType
{
    /// <summary>
    /// Идентификатор типа домика.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название постройки.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Технический код типа – по нему код находит конкретную постройку по смыслу, а не по <see cref="Id"/>.
    /// </summary>
    /// <remarks>
    /// Например, <c>market</c>, <c>market_yard</c>.
    /// </remarks>
    public required string LogicName { get; set; }

    /// <summary>
    /// Сколько экземпляров этого типа домика игрок может построить всего.
    /// </summary>
    public int MaxCount { get; set; }

    /// <summary>
    /// Обжитость деревни, начиная с которой тип домика открывается для покупки.
    /// </summary>
    public int UnlockLevel { get; set; }

    /// <summary>
    /// Уровни этого типа домика – что и когда открывается при улучшении.
    /// </summary>
    public ICollection<DomikTypeLevel> Levels { get; set; } = new List<DomikTypeLevel>();
}
