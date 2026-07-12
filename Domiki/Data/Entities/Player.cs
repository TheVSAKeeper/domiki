using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data
{
    [Table("Players")]
    public class Player
    {
        [Key]
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(100)]
        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }

        [MaxLength(100)]
        public string VillageName { get; set; }

        public int CrestIcon { get; set; }

        public int CrestColor { get; set; }

        public int ExpeditionsSincePity { get; set; }

        public DateTime? LastSeen { get; set; }

        public DateTime? NextOrderRefillAt { get; set; }

        public int GoldMinedToday { get; set; }

        public DateTime? GoldMinedDate { get; set; }

        public bool FeedWorkers { get; set; }

        public int ZealCharges { get; set; } = 24;

        [MaxLength(450)]
        [Required(AllowEmptyStrings = false)]
        public string AspNetUserId { get; set; }

        [ConcurrencyCheck]
        public Guid Version { get; set; }

        public ICollection<Resource> Resources { get; set; }
    }
}
