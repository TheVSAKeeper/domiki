using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities
{
    [Table("DecorCosts")]
    public class DecorCost
    {
        [Key]
        [Column(Order = 1)]
        public int DecorTypeId { get; set; }

        [Key]
        [Column(Order = 2)]
        public int ResourceTypeId { get; set; }

        public int Value { get; set; }

        public DecorType DecorType { get; set; }

        public ResourceType ResourceType { get; set; }
    }
}
