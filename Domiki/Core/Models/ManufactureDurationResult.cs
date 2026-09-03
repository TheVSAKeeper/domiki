namespace Domiki.Web.Core.Models;

/// <summary>
/// Итог расчёта длительности смены.
/// </summary>
public sealed record ManufactureDurationResult
{
    /// <summary>
    /// Длительность смены в секундах.
    /// </summary>
    public required int Seconds { get; init; }

    /// <summary>
    /// Смена ускорена рвением, и заряд списывается.
    /// </summary>
    public required bool ZealSpent { get; init; }
}
