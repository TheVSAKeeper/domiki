namespace Domiki.Web.Activities.Models;

public class Blueprint
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string LogicName { get; set; }
    public int DomikTypeId { get; set; }
    public int NeighborId { get; set; }
    public int ReputationThreshold { get; set; }
}
