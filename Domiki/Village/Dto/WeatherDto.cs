namespace Domiki.Web.Village.Dto;

public class WeatherStateDto
{
    public WeatherPeriodDto Current { get; set; }
    public WeatherPeriodDto[] Forecast { get; set; }
}

public class WeatherPeriodDto
{
    public int WeatherTypeId { get; set; }
    public string WeatherName { get; set; }
    public string LogicName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public WeatherEffectDto[] Effects { get; set; }
}

public class WeatherEffectDto
{
    public int DomikTypeId { get; set; }
    public int OutputPercent { get; set; }
}
