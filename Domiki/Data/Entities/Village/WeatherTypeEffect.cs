using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

[Table("WeatherTypeEffects")]
public class WeatherTypeEffect
{
    [Key]
    [Column(Order = 1)]
    public int WeatherTypeId { get; set; }

    [Key]
    [Column(Order = 2)]
    public int DomikTypeId { get; set; }

    public int OutputPercent { get; set; }

    public WeatherType WeatherType { get; set; }
}
