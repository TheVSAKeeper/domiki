using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities
{
    [Table("NeighborReputations")]
    public class NeighborReputation
    {
        [Key]
        [Column(Order = 1)]
        public int PlayerId { get; set; }

        [Key]
        [Column(Order = 2)]
        public int NeighborId { get; set; }

        public int Points { get; set; }

        public Player Player { get; set; }

        public Neighbor Neighbor { get; set; }
    }
}
