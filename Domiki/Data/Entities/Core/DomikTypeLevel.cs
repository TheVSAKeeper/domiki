using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities
{
    [Table("DomikTypeLevels")]
    public class DomikTypeLevel
    {
        [Key]
        [Column(Order = 1)]
        public int DomikTypeId { get; set; }

        [Key]
        [Column(Order = 2)]
        public int Value { get; set; }

        public int UpgradeSeconds { get; set; }

        public int MaxManufactureCount { get; set; }

        public DomikType DomikType { get; set; }
    }
}
