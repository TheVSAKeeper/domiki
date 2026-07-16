using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

[Table("PlayerDecors")]
public class PlayerDecor
{
    [Key]
    [Column(Order = 1)]
    public int PlayerId { get; set; }

    [Key]
    [Column(Order = 2)]
    public int DecorTypeId { get; set; }

    public int Count { get; set; }

    public Player Player { get; set; }

    public DecorType DecorType { get; set; }
}
