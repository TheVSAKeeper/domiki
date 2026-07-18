namespace Domiki.Web.Village.Models;

/// <summary>
/// Погода – глобальное состояние, одно на всех игроков, с прогнозом на сутки вперёд.
/// </summary>
/// <remarks>
/// Собирается в <see cref="WeatherManager.GetWeather"/> и отдаётся на клиент как <see cref="Dto.WeatherStateDto"/>.
/// </remarks>
public class WeatherState
{
    /// <summary>
    /// Погода, действующая прямо сейчас.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – период ещё не насеян планировщиком.
    /// </remarks>
    public WeatherPeriod? Current { get; set; }

    /// <summary>
    /// Ближайшие будущие периоды погоды – геймплей планирования, не рулетка.
    /// </summary>
    public WeatherPeriod[] Forecast { get; set; } = [];
}
