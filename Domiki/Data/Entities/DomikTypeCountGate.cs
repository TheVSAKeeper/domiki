using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data
{
    [Table("DomikTypeCountGates")]
    public class DomikTypeCountGate
    {
        [Key]
        [Column(Order = 1)]
        public int DomikTypeId { get; set; }

        [Key]
        [Column(Order = 2)]
        public int Ordinal { get; set; }

        public int UnlockLevel { get; set; }
    }
}
