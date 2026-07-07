namespace Domiki.Web.Business.Models
{
    public class PlayerBlueprint
    {
        public Blueprint Blueprint { get; set; }
        public Neighbor Neighbor { get; set; }
        public int CurrentReputation { get; set; }
        public bool Owned { get; set; }
    }
}
