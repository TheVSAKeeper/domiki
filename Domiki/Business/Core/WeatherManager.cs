using Domiki.Web.Business.Models;

namespace Domiki.Web.Business.Core
{
    public class WeatherManager
    {
        public const int WeatherPeriodSeconds = 8 * 3600;
        public const int ForecastHorizonSeconds = 24 * 3600;
        private const int ForecastCount = 4;

        private Data.ApplicationDbContext _context;
        private Data.UnitOfWork _uow;
        private ICalculator _calculator;
        private ResourceManager _resourceManager;

        public WeatherManager(Data.ApplicationDbContext context, Data.UnitOfWork uow, ICalculator calculator, ResourceManager resourceManager)
        {
            _context = context;
            _uow = uow;
            _calculator = calculator;
            _resourceManager = resourceManager;
        }

        public bool RotateWeather(DateTime date)
        {
            EnsureWeatherSchedule(date);
            var dbPeriod = _context.WeatherPeriods.Single(x => x.StartDate <= date && date < x.EndDate);

            _uow.AfterEventAction = () =>
            {
                _calculator.Insert(new CalculateInfo
                {
                    PlayerId = 0,
                    ObjectId = dbPeriod.Id,
                    Date = dbPeriod.EndDate,
                    Type = CalculateTypes.WeatherRotation,
                });
            };

            return true;
        }

        public void EnsureWeatherSchedule(DateTime date)
        {
            var weatherTypes = _resourceManager.GetWeatherTypes();
            var horizon = date.AddSeconds(ForecastHorizonSeconds);

            var last = _context.WeatherPeriods.OrderByDescending(x => x.EndDate).FirstOrDefault();
            if (last == null)
            {
                last = CreatePeriod(weatherTypes, date);
            }

            while (last.EndDate < horizon)
            {
                last = CreatePeriod(weatherTypes, last.EndDate);
            }
        }

        public WeatherState GetWeather(DateTime date)
        {
            var weatherTypes = _resourceManager.GetWeatherTypes();
            var periods = _context.WeatherPeriods
                .Where(x => x.EndDate > date)
                .OrderBy(x => x.StartDate)
                .Take(ForecastCount)
                .ToArray()
                .Select(x => ToModel(x, weatherTypes))
                .ToArray();

            return new WeatherState
            {
                Current = periods.FirstOrDefault(),
                Forecast = periods.Skip(1).ToArray(),
            };
        }

        public WeatherPeriod GetCurrentPeriod(DateTime date)
        {
            var dbPeriod = _context.WeatherPeriods.SingleOrDefault(x => x.StartDate <= date && date < x.EndDate);
            if (dbPeriod == null)
            {
                return null;
            }

            return ToModel(dbPeriod, _resourceManager.GetWeatherTypes());
        }

        public int GetOutputPercent(DateTime date, int domikTypeId)
        {
            var current = GetCurrentPeriod(date);
            var effect = current?.WeatherType.Effects.FirstOrDefault(x => x.DomikTypeId == domikTypeId);
            return effect?.OutputPercent ?? 100;
        }

        private Data.WeatherPeriod CreatePeriod(WeatherType[] weatherTypes, DateTime start)
        {
            var weatherType = PickWeatherType(weatherTypes);
            var period = new Data.WeatherPeriod
            {
                WeatherTypeId = weatherType.Id,
                StartDate = start,
                EndDate = start.AddSeconds(WeatherPeriodSeconds),
            };
            _context.WeatherPeriods.Add(period);
            _context.SaveChanges();
            return period;
        }

        private static WeatherType PickWeatherType(WeatherType[] weatherTypes)
        {
            var totalWeight = weatherTypes.Sum(x => x.RotationWeight);
            var roll = Random.Shared.Next(totalWeight);
            var cumulative = 0;
            foreach (var weatherType in weatherTypes)
            {
                cumulative += weatherType.RotationWeight;
                if (roll < cumulative)
                {
                    return weatherType;
                }
            }

            return weatherTypes[^1];
        }

        private static WeatherPeriod ToModel(Data.WeatherPeriod dbPeriod, WeatherType[] weatherTypes)
        {
            return new WeatherPeriod
            {
                WeatherType = weatherTypes.First(x => x.Id == dbPeriod.WeatherTypeId),
                StartDate = dbPeriod.StartDate,
                EndDate = dbPeriod.EndDate,
            };
        }
    }
}
