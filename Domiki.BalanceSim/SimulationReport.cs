namespace Domiki.BalanceSim;

public sealed class SimulationReport
{
    public SimulationReport(IReadOnlyDictionary<ScenarioKind, List<SimulationRunResult>> runs)
    {
        Runs = runs;
    }

    public IReadOnlyDictionary<ScenarioKind, List<SimulationRunResult>> Runs { get; }
}
