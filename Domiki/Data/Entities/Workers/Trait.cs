using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities
{
    [Table("Traits")]
    public class Trait
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }

        [MaxLength(100)]
        [Required(AllowEmptyStrings = false)]
        public string LogicName { get; set; }

        public int DurationPercent { get; set; }

        public bool NoFatigue { get; set; }

        public bool NoSick { get; set; }

        public int LuckWeightPercent { get; set; }
    }
}
