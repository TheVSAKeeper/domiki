namespace Domiki.Web.Models
{
    public class VillageLevelDto
    {
        public int Level { get; set; }
        public int Buildings { get; set; }
        public int Residents { get; set; }
        public int Reputation { get; set; }
        public int Comfort { get; set; }
        public VillageLevelUnlockDto[] UpcomingUnlocks { get; set; }
    }

    public class VillageLevelUnlockDto
    {
        public int Level { get; set; }
        public string Label { get; set; }
    }
}
