
namespace Domiki.Web.Activities.Dto
{
    public class TolokaDto
    {
        public int Id { get; set; }
        public int TolokaTypeId { get; set; }
        public string Name { get; set; }
        public string LogicName { get; set; }
        public int ResourceTypeId { get; set; }
        public int Goal { get; set; }
        public int Collected { get; set; }
        public DateTime StartDate { get; set; }
    }

    public class TolokaStateDto
    {
        public TolokaDto Active { get; set; }
        public int MyContribution { get; set; }
        public TolokaActiveBuffDto[] ActiveBuffs { get; set; }
        public int BuffHours { get; set; }
        public int? NextBuffHours { get; set; }
    }

    public class TolokaActiveBuffDto
    {
        public string LogicName { get; set; }
        public string Label { get; set; }
        public int Percent { get; set; }
        public DateTime BuffUntil { get; set; }
    }
}
