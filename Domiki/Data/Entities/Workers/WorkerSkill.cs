using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

[Table("WorkerSkills")]
public class WorkerSkill
{
    public int WorkerId { get; set; }

    public int DomikTypeId { get; set; }

    public int Uses { get; set; }

    public Worker Worker { get; set; }
}
