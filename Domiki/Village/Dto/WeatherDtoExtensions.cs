using Domiki.Web.Village.Models;

namespace Domiki.Web.Village.Dto
{
    public static class WeatherDtoExtensions
    {
        public static WeatherStateDto ToDto(this WeatherState weather)
        {
            return new WeatherStateDto
            {
                Current = weather.Current?.ToDto(),
                Forecast = weather.Forecast.Select(x => x.ToDto()).ToArray(),
            };
        }

        public static WeatherPeriodDto ToDto(this WeatherPeriod period)
        {
            return new WeatherPeriodDto
            {
                WeatherTypeId = period.WeatherType.Id,
                WeatherName = period.WeatherType.Name,
                LogicName = period.WeatherType.LogicName,
                StartDate = DateTime.SpecifyKind(period.StartDate, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(period.EndDate, DateTimeKind.Utc),
                Effects = period.WeatherType.Effects.Select(x => new WeatherEffectDto
                {
                    DomikTypeId = x.DomikTypeId,
                    OutputPercent = x.OutputPercent,
                }).ToArray(),
            };
        }
    }
}
