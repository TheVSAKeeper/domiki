namespace Domiki.Web.Infrastructure;

public static class DateTimeHelper
{
    public static DateTime GetNowDate()
    {
        var d = DateTime.UtcNow;
        var date = new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second, DateTimeKind.Utc);
        return date;
    }

    public static DateTime AsUtc(DateTime date)
    {
        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }

    public static DateTime? AsUtc(DateTime? date)
    {
        return date == null ? null : DateTime.SpecifyKind(date.Value, DateTimeKind.Utc);
    }
}
