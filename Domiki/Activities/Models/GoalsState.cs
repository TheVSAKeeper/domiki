namespace Domiki.Web.Activities.Models;

public class GoalsState
{
    public ActiveGoal? ActiveGoal { get; set; }
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; }
    public int ZealCharges { get; set; }
}

public class ActiveGoal
{
    public int Id { get; set; }
    public int Ordinal { get; set; }
    public required string Name { get; set; }
    public int RewardCoins { get; set; }
}
