using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

[Table("ExpeditionEquipment")]
public class ExpeditionEquipment
{
    [Key]
    [Column(Order = 1)]
    public int ExpeditionTypeId { get; set; }

    [Key]
    [Column(Order = 2)]
    public int ResourceTypeId { get; set; }

    public int Value { get; set; }

    public bool IsOptional { get; set; }

    public ExpeditionType ExpeditionType { get; set; }
}
