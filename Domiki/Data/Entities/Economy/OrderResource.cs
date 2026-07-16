using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities
{
    [Table("OrderResources")]
    public class OrderResource
    {
        [Key]
        [Column(Order = 1)]
        public int OrderId { get; set; }

        [Key]
        [Column(Order = 2)]
        public int ResourceTypeId { get; set; }

        public int Value { get; set; }

        public Order Order { get; set; }

        public ResourceType ResourceType { get; set; }
    }
}
