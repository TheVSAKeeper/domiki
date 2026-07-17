namespace Domiki.Web.Village.Models;

public class WeatherState
{
    public WeatherPeriod? Current { get; set; }
    public WeatherPeriod[] Forecast { get; set; } = [];
}
