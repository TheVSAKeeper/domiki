using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data
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
