namespace Domiki.Web.Village.Models;

public class World
{
    public WorldVillage[] Villages { get; set; } = [];
    public required Season Season { get; set; }
}

public class WorldVillage
{
    public int? PlayerId { get; set; }
    public string? VillageName { get; set; }
    public int CrestIcon { get; set; }
    public int CrestColor { get; set; }
    public int Level { get; set; }
    public bool IsNpc { get; set; }
    public bool IsMe { get; set; }
    public int? NpcResourceTypeId { get; set; }
    public string? NpcLogicName { get; set; }
    public int SeasonOrders { get; set; }
    public int SeasonToloka { get; set; }
    public int SeasonExpeditions { get; set; }
    public int Comfort { get; set; }
}

public class VillageVisit
{
    public required string VillageName { get; set; }
    public int CrestIcon { get; set; }
    public int CrestColor { get; set; }
    public required VillageLevel Level { get; set; }
    public VisitBuilding[] Buildings { get; set; } = [];
}

public class VisitBuilding
{
    public required string TypeName { get; set; }
    public int Level { get; set; }
}
