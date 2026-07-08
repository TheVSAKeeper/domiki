namespace Domiki.Web.Business.Models
{
    public class World
    {
        public WorldVillage[] Villages { get; set; }
    }

    public class WorldVillage
    {
        public int? PlayerId { get; set; }
        public string VillageName { get; set; }
        public int CrestIcon { get; set; }
        public int CrestColor { get; set; }
        public int Level { get; set; }
        public bool IsNpc { get; set; }
        public bool IsMe { get; set; }
        public int? NpcResourceTypeId { get; set; }
    }

    public class VillageVisit
    {
        public string VillageName { get; set; }
        public int CrestIcon { get; set; }
        public int CrestColor { get; set; }
        public VillageLevel Level { get; set; }
        public VisitBuilding[] Buildings { get; set; }
    }

    public class VisitBuilding
    {
        public string TypeName { get; set; }
        public int Level { get; set; }
    }
}
