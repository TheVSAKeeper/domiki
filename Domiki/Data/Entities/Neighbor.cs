using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data
{
    [Table("Neighbors")]
    public class Neighbor
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        public string LogicName { get; set; }

        public int PrimaryResourceTypeId { get; set; }

        public int UnlockLevel { get; set; }
    }
}
