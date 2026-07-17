namespace Domiki.Web.Village.Models;

public class VillageLevel
{
    public int Level { get; set; }
    public int Buildings { get; set; }
    public int Residents { get; set; }
    public int Reputation { get; set; }
    public int Comfort { get; set; }
    public int VisitsSinceBigGift { get; set; }
    public VillageLevelUnlock[] UpcomingUnlocks { get; set; }
}

public class VillageLevelUnlock
{
    public int? Level { get; set; }
    public string Label { get; set; }
    public string Requirement { get; set; }
}
