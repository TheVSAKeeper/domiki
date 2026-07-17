namespace Domiki.Web.Village.Models;

public class WeatherPeriod
{
    public required WeatherType WeatherType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
