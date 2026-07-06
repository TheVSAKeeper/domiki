using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data
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
    }
}
