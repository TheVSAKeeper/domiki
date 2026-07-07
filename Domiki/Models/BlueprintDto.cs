namespace Domiki.Web.Models
{
    public class BlueprintDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int DomikTypeId { get; set; }
        public int NeighborId { get; set; }
        public string NeighborName { get; set; }
        public int ReputationThreshold { get; set; }
        public int CurrentReputation { get; set; }
        public bool Owned { get; set; }
    }
}
