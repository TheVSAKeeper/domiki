namespace Domiki.Web.Models
{
    public class VillageDto
    {
        public string VillageName { get; set; }
        public int CrestIcon { get; set; }
        public int CrestColor { get; set; }
        public bool FeedWorkers { get; set; }
    }

    public class SetVillageDto
    {
        public string Name { get; set; }
        public int CrestIcon { get; set; }
        public int CrestColor { get; set; }
    }

    public class SetFeedWorkersDto
    {
        public bool Enabled { get; set; }
    }
}
