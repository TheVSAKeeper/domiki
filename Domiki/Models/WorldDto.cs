namespace Domiki.Web.Models
{
    public class WorldDto
    {
        public WorldVillageDto[] Villages { get; set; }
        public SeasonDto Season { get; set; }
    }

    public class WorldVillageDto
    {
        public int? PlayerId { get; set; }
        public string VillageName { get; set; }
        public int CrestIcon { get; set; }
        public int CrestColor { get; set; }
        public int Level { get; set; }
        public bool IsNpc { get; set; }
        public bool IsMe { get; set; }
        public int? NpcResourceTypeId { get; set; }
        public string NpcLogicName { get; set; }
        public int SeasonOrders { get; set; }
        public int SeasonToloka { get; set; }
        public int SeasonExpeditions { get; set; }
        public int Comfort { get; set; }
    }

    public class VillageVisitDto
    {
        public string VillageName { get; set; }
        public int CrestIcon { get; set; }
        public int CrestColor { get; set; }
        public VillageLevelDto Level { get; set; }
        public VisitBuildingDto[] Buildings { get; set; }
    }

    public class VisitBuildingDto
    {
        public string TypeName { get; set; }
        public int Level { get; set; }
    }
}
