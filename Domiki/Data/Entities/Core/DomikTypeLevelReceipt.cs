using Domiki.Web.Economy.Models;
using Domiki.Web.Reference.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities
{
    [Table("DomikTypeLevelReceipts")]
    public class DomikTypeLevelReceipt
    {
        [Key]
        [Column(Order = 1)]
        public int DomikTypeLevelDomikTypeId { get; set; }

        [Key]
        [Column(Order = 2)]
        public int DomikTypeLevelValue { get; set; }

        [Key]
        [Column(Order = 3)]
        public int ReceiptId { get; set; }

        public DomikTypeLevel DomikTypeLevel { get; set; }

        public Receipt Receipt { get; set; }
    }
}
