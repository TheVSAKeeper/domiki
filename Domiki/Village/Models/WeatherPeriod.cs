namespace Domiki.Web.Village.Models;

/// <summary>
/// Один период действия одного вида погоды.
/// </summary>
/// <remarks>
/// Планируется в <see cref="WeatherManager.EnsureWeatherSchedule"/> и отдаётся на клиент как <see cref="Dto.WeatherPeriodDto"/>.
/// </remarks>
public class WeatherPeriod
{
    /// <summary>
    /// Тип погоды, действующий в этот период.
    /// </summary>
    public required WeatherType WeatherType { get; set; }

    /// <summary>
    /// Момент начала периода.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Момент окончания периода.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime EndDate { get; set; }
}
