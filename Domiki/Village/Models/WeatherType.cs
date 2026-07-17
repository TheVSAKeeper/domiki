namespace Domiki.Web.Village.Models;

public class WeatherType
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string LogicName { get; set; }
    public int RotationWeight { get; set; }
    public WeatherTypeEffect[] Effects { get; set; } = [];
}

public class WeatherTypeEffect
{
    public int DomikTypeId { get; set; }
    public int OutputPercent { get; set; }
}
