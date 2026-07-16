namespace Domiki.BalanceSim;

public enum SimulationEventKind
{
    None = 0,
    WeatherBoundary = 1,
    ManufactureFinished = 2,
    DomikFinished = 3,
    ExpeditionReturned = 4,
    OrderExpired = 5,
    Login = 6,
}
