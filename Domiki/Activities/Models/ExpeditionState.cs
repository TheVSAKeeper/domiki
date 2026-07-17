namespace Domiki.Web.Activities.Models;

public class ExpeditionState
{
    public Expedition[] Active { get; set; } = [];
    public ExpeditionType[] Types { get; set; } = [];
    public int ExpeditionsSincePity { get; set; }
    public int PityThreshold { get; set; }
    public int MaxActive { get; set; }
}
