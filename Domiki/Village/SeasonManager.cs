using Domiki.Web.Data;
using Domiki.Web.Village.Models;

namespace Domiki.Web.Village;

public class SeasonManager
{
    public const int SeasonDurationSeconds = 14 * 24 * 3600;
    public static readonly DateTime SeasonEpoch = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _context;

    public SeasonManager(ApplicationDbContext context)
    {
        _context = context;
    }

    public Season GetCurrentSeason(DateTime date)
    {
        var number = (int)Math.Floor((date - SeasonEpoch).TotalSeconds / SeasonDurationSeconds);
        var startDate = SeasonEpoch.AddSeconds(number * SeasonDurationSeconds);

        return new()
        {
            Number = number,
            StartDate = startDate,
            EndDate = startDate.AddSeconds(SeasonDurationSeconds),
        };
    }

    public void IncrementCounter(int playerId, SeasonMetric metric, int value, DateTime date)
    {
        if (value <= 0)
        {
            return;
        }

        var seasonId = GetCurrentSeason(date).Number;
        var metricValue = (int)metric;

        var counter = _context.SeasonCounters.Local.FirstOrDefault(x => x.SeasonId == seasonId && x.PlayerId == playerId && x.Metric == metricValue)
                      ?? _context.SeasonCounters.FirstOrDefault(x => x.SeasonId == seasonId && x.PlayerId == playerId && x.Metric == metricValue);

        if (counter == null)
        {
            counter = new()
                { SeasonId = seasonId, PlayerId = playerId, Metric = metricValue };

            _context.SeasonCounters.Add(counter);
        }

        counter.Value += value;
    }

    public Dictionary<(int PlayerId, SeasonMetric Metric), int> GetCounters(int seasonId)
    {
        return _context.SeasonCounters
            .Where(x => x.SeasonId == seasonId)
            .ToArray()
            .ToDictionary(x => (x.PlayerId, (SeasonMetric)x.Metric), x => x.Value);
    }
}
