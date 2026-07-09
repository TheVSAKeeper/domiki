namespace Domiki.Web.Business.Models
{
    public class Toloka
    {
        public int Id { get; set; }
        public TolokaType TolokaType { get; set; }
        public int Collected { get; set; }
        public DateTime StartDate { get; set; }
    }

    public class TolokaState
    {
        public Toloka Active { get; set; }
        public int MyContribution { get; set; }
        public bool BuffActive { get; set; }
        public DateTime? BuffUntil { get; set; }
        public int BuffPercent { get; set; }
        public int BuffHours { get; set; }
        public int? NextBuffHours { get; set; }
    }
}
