using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data
{
    [Table("TolokaTypes")]
    public class TolokaType
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        public string LogicName { get; set; }

        public int ResourceTypeId { get; set; }

        public int Goal { get; set; }

        public int RotationWeight { get; set; }

        public ResourceType ResourceType { get; set; }
    }
}
