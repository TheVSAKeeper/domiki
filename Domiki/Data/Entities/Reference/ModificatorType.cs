using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities
{
    [Table("ModificatorTypes")]
    public class ModificatorType
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string LogicName { get; set; }
    }
}
