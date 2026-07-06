using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data
{
    [Table("Workers")]
    public class Worker
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PlayerId { get; set; }

        [MaxLength(100)]
        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }

        public int TraitId { get; set; }

        public int? ManufactureId { get; set; }

        public int WorkedSeconds { get; set; }

        public DateTime? RestUntil { get; set; }
        public Player Player { get; set; }

        public Trait Trait { get; set; }

        public Manufacture Manufacture { get; set; }

        public ICollection<WorkerSkill> Skills { get; set; }
    }
}
