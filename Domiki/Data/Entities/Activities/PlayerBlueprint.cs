using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities
{
    [Table("PlayerBlueprints")]
    public class PlayerBlueprint
    {
        [Key]
        [Column(Order = 1)]
        public int PlayerId { get; set; }

        [Key]
        [Column(Order = 2)]
        public int BlueprintId { get; set; }

        public Player Player { get; set; }

        public Blueprint Blueprint { get; set; }
    }
}
