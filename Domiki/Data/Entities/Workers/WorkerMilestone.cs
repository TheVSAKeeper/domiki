using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Отметка однократно выданной вехи трудяги.
/// </summary>
[PrimaryKey(nameof(WorkerId), nameof(MilestoneType))]
public class WorkerMilestone
{
    /// <summary>
    /// Идентификатор трудяги.
    /// </summary>
    public int WorkerId { get; set; }

    /// <summary>
    /// Вид выданной вехи.
    /// </summary>
    public WorkerMilestoneType MilestoneType { get; set; }

    /// <summary>
    /// Момент выдачи вехи.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime GrantDate { get; set; }
}
