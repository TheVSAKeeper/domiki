using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities
{
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
}
