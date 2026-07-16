using Domiki.Web.Economy.Models;

namespace Domiki.Web.Activities.Models
{
    public class PlayerBlueprint
    {
        public Blueprint Blueprint { get; set; }
        public Neighbor Neighbor { get; set; }
        public int CurrentReputation { get; set; }
        public bool Owned { get; set; }
    }
}
