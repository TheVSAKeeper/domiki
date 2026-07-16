using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities
{
    [Table("DomikTypes")]
    public class DomikType
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string LogicName { get; set; }

        public int MaxCount { get; set; }

        public int UnlockLevel { get; set; }

        public ICollection<DomikTypeLevel> Levels { get; set; }
    }
}
