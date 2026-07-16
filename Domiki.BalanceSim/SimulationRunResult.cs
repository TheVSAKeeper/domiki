namespace Domiki.BalanceSim;

public sealed class SimulationRunResult
{
    public required int Seed { get; init; }
    public required Dictionary<int, int> VillageLevelTimes { get; init; }
    public required Dictionary<int, int> FirstDomikTimes { get; init; }
    public required Dictionary<int, int> MaxDomikLevelTimes { get; init; }
    public required Dictionary<int, int> NeighborOpenTimes { get; init; }
    public required Dictionary<int, int> BlueprintTimes { get; init; }
    public required Dictionary<int, int> FinalResources { get; init; }
    public int? ContentCompleteTime { get; set; }
    public int MaxVillageLevel { get; set; }
    public double IdleShare { get; set; }
    public double RestShare { get; set; }
    public int LongestStallSeconds { get; set; }
    public int? FirstIncomeSeconds { get; set; }
    public int? FirstUpgradeStartSeconds { get; set; }
    public int ActionsFirst15Min { get; set; }
    public int InfeasibleOrdersDay1 { get; set; }
    public int GoalsCompleted48h { get; set; }
}