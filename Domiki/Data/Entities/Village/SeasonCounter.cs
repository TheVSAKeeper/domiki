using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

[Table("SeasonCounters")]
public class SeasonCounter
{
    [Key]
    [Column(Order = 1)]
    public int SeasonId { get; set; }

    [Key]
    [Column(Order = 2)]
    public int PlayerId { get; set; }

    [Key]
    [Column(Order = 3)]
    public int Metric { get; set; }

    public int Value { get; set; }

    public Player Player { get; set; }
}
