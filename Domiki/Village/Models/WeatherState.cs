namespace Domiki.Web.Business.Models
{
    public class WeatherState
    {
        public WeatherPeriod Current { get; set; }
        public WeatherPeriod[] Forecast { get; set; }
    }
}
