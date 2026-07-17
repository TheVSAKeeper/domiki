using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Ресурс, который нужно списать, чтобы улучшить домик до этого уровня.
/// </summary>
[Table("DomikTypeLevelResources")]
public class DomikTypeLevelResource
{
    /// <summary>
    /// Часть составного ключа – тип домика (первая половина составного FK на <see cref="DomikTypeLevel"/>).
    /// </summary>
    [Key]
    [Column(Order = 1)]
    public int DomikTypeLevelDomikTypeId { get; set; }

    /// <summary>
    /// Часть составного ключа – номер уровня домика (вторая половина составного FK на <see cref="DomikTypeLevel"/>).
    /// </summary>
    [Key]
    [Column(Order = 2)]
    public int DomikTypeLevelValue { get; set; }

    /// <summary>
    /// Часть составного ключа – тип ресурса, нужного для перехода на этот уровень.
    /// </summary>
    [Key]
    [Column(Order = 3)]
    public int ResourceTypeId { get; set; }

    /// <summary>
    /// Сколько ресурса данного типа нужно списать для перехода на этот уровень.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Уровень домика, для перехода на который нужен ресурс.
    /// </summary>
    public DomikTypeLevel DomikTypeLevel { get; set; }

    /// <summary>
    /// Тип требуемого ресурса.
    /// </summary>
    public ResourceType ResourceType { get; set; }
}
