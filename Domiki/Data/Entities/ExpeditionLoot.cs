using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data
{
    [Table("ExpeditionLoot")]
    public class ExpeditionLoot
    {
        [Key]
        [Column(Order = 1)]
        public int ExpeditionTypeId { get; set; }

        [Key]
        [Column(Order = 2)]
        public int ResourceTypeId { get; set; }

        public int MinValue { get; set; }

        public int MaxValue { get; set; }

        public int Weight { get; set; }

        public bool IsRare { get; set; }

        public ExpeditionType ExpeditionType { get; set; }
    }
}
