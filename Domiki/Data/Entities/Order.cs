using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PlayerId { get; set; }

        public int NeighborId { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime ExpireDate { get; set; }

        public int RewardCoins { get; set; }

        public int RewardGold { get; set; }

        public int RewardReputation { get; set; }

        public Player Player { get; set; }

        public Neighbor Neighbor { get; set; }

        public ICollection<OrderResource> Resources { get; set; }
    }
}
