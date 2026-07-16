using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

[Table("Tolokas")]
public class Toloka
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int TolokaTypeId { get; set; }

    public int Collected { get; set; }

    public int Goal { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public TolokaType TolokaType { get; set; }
}
