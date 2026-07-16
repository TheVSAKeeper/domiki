using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

[Table("ExpeditionLoot")]
public class ExpeditionLoot
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ExpeditionTypeId { get; set; }

    public ExpeditionLootKind Kind { get; set; }

    public int? ResourceTypeId { get; set; }

    public int? DecorTypeId { get; set; }

    public int? BlueprintId { get; set; }

    public int MinValue { get; set; }

    public int MaxValue { get; set; }

    public int Weight { get; set; }

    public bool IsRare { get; set; }

    public ExpeditionType ExpeditionType { get; set; }
}
