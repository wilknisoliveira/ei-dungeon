namespace ei_back.Core.Application.Service.Sparks;

public static class SparkCalendar
{
    internal static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static DateOnly LocalDate(DateTimeOffset instant) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Zone).DateTime);

    public static DateTimeOffset Midnight(DateOnly date)
    {
        var instant = date.ToDateTime(TimeOnly.MinValue);
        // When a zone advances its clock at midnight, start at the first local instant.
        while (Zone.IsInvalidTime(instant))
            instant = instant.AddMinutes(1);
        var offset = Zone.IsAmbiguousTime(instant) ? Zone.GetAmbiguousTimeOffsets(instant).Max() : Zone.GetUtcOffset(instant);
        return new DateTimeOffset(instant, offset).ToUniversalTime();
    }

    public static (DateTimeOffset Start, DateTimeOffset End) Window(DateTimeOffset now, int closingDay)
    {
        var local = LocalDate(now);
        var month = new DateOnly(local.Year, local.Month, 1);
        var boundary = Boundary(month, closingDay);
        if (now < boundary)
            return (Boundary(month.AddMonths(-1), closingDay), boundary);
        return (boundary, Boundary(month.AddMonths(1), closingDay));
    }

    public static DateTimeOffset NextBoundary(DateTimeOffset after, int closingDay) => Window(after, closingDay).End;

    public static bool IsRenewalWindow(SparkCalendarWindow dates, int lead)
    {
        if (dates.Today >= dates.Next.AddDays(-lead) && dates.Today <= dates.Next.AddDays(1))
            return true;
        return dates.Today >= dates.Previous && dates.Today <= dates.Previous.AddDays(1);
    }

    private static DateTimeOffset Boundary(DateOnly month, int day) =>
        Midnight(new DateOnly(month.Year, month.Month, Math.Min(day, DateTime.DaysInMonth(month.Year, month.Month))));
}

public readonly record struct SparkCalendarWindow(DateOnly Today, DateOnly Previous, DateOnly Next);
