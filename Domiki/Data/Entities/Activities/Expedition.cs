using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

[Table("Expeditions")]
public class Expedition
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int PlayerId { get; set; }

    public int ExpeditionTypeId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime FinishDate { get; set; }

    public bool Provisioned { get; set; }

    public Player Player { get; set; }

    public ExpeditionType ExpeditionType { get; set; }
}
