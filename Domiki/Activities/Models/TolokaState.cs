namespace Domiki.Web.Activities.Models;

public class Toloka
{
    public int Id { get; set; }
    public required TolokaType TolokaType { get; set; }
    public int Collected { get; set; }
    public int Goal { get; set; }
    public DateTime StartDate { get; set; }
}

public class TolokaState
{
    public required Toloka Active { get; set; }
    public int MyContribution { get; set; }
    public TolokaActiveBuff[] ActiveBuffs { get; set; } = [];
    public int BuffHours { get; set; }
    public int? NextBuffHours { get; set; }
}

public class TolokaActiveBuff
{
    public required string LogicName { get; set; }
    public required string Label { get; set; }
    public int Percent { get; set; }
    public DateTime BuffUntil { get; set; }
}
