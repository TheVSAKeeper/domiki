using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities
{
    [Table("DecorTypes")]
    public class DecorType
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        public string LogicName { get; set; }

        public int ComfortPoints { get; set; }

        public bool IsPurchasable { get; set; }

        public int? NeighborId { get; set; }

        public int ReputationThreshold { get; set; }
    }
}
