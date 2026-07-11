namespace Domiki.Web.Business.Models
{
    public class Toloka
    {
        public int Id { get; set; }
        public TolokaType TolokaType { get; set; }
        public int Collected { get; set; }
        public int Goal { get; set; }
        public DateTime StartDate { get; set; }
    }

    public class TolokaState
    {
        public Toloka Active { get; set; }
        public int MyContribution { get; set; }
        public TolokaActiveBuff[] ActiveBuffs { get; set; }
        public int BuffHours { get; set; }
        public int? NextBuffHours { get; set; }
    }

    public class TolokaActiveBuff
    {
        public string LogicName { get; set; }
        public string Label { get; set; }
        public int Percent { get; set; }
        public DateTime BuffUntil { get; set; }
    }
}
