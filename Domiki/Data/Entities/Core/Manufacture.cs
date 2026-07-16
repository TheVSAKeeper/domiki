using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities
{
    [Table("Manufactures")]
    public class Manufacture
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int DomikId { get; set; }

        public int DomikPlayerId { get; set; }

        public int ReceiptId { get; set; }

        public int PlodderCount { get; set; }

        public Domik Domik { get; set; }

        public DateTime FinishDate { get; set; }

        public int DurationSeconds { get; set; }

        public int OutputPercent { get; set; } = 100;

        public bool AutoRepeat { get; set; }

        public bool UseOptional { get; set; }

        public int SickChance { get; set; }
    }
}
