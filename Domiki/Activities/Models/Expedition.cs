namespace Domiki.Web.Activities.Models;

public class Expedition
{
    public int Id { get; set; }
    public required ExpeditionType ExpeditionType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime FinishDate { get; set; }
}
