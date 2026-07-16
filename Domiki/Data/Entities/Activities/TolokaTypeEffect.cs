using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

[Table("TolokaTypeEffects")]
public class TolokaTypeEffect
{
    [Key]
    [Column(Order = 1)]
    public int TolokaTypeId { get; set; }

    [Key]
    [Column(Order = 2)]
    public int DomikTypeId { get; set; }

    public int OutputPercent { get; set; }

    public TolokaType TolokaType { get; set; }
}
