using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

[Table("TolokaContributions")]
public class TolokaContribution
{
    [Key]
    [Column(Order = 1)]
    public int TolokaId { get; set; }

    [Key]
    [Column(Order = 2)]
    public int PlayerId { get; set; }

    public int Value { get; set; }

    public Toloka Toloka { get; set; }

    public Player Player { get; set; }
}
