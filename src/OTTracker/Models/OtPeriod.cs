namespace OTTracker.Models;

public readonly record struct OtPeriod(DateTime Start, DateTime End)
{
    public string DisplayText => $"{Start:dd/MM/yy} - {End:dd/MM/yy}";

    public OtPeriod AddMonths(int months) => new(Start.AddMonths(months), End.AddMonths(months));

    public static OtPeriod LastCompleted(DateTime date, int startDay, int endDay)
    {
        var period = FromDate(date, startDay, endDay);
        return date.Date >= period.End ? period : period.AddMonths(-1);
    }

    public static OtPeriod FromDate(DateTime date, int startDay, int endDay)
    {
        var target = date.Date;
        var start = CreateDate(target.Year, target.Month, startDay);
        var end = GetEndDate(start, startDay, endDay);

        if (target < start)
        {
            start = CreateDate(target.AddMonths(-1).Year, target.AddMonths(-1).Month, startDay);
            end = GetEndDate(start, startDay, endDay);
        }
        else if (target > end)
        {
            start = CreateDate(target.AddMonths(1).Year, target.AddMonths(1).Month, startDay);
            end = GetEndDate(start, startDay, endDay);
        }

        return new OtPeriod(start, end);
    }

    private static DateTime GetEndDate(DateTime start, int startDay, int endDay)
    {
        var endMonth = startDay <= endDay ? start : start.AddMonths(1);
        return CreateDate(endMonth.Year, endMonth.Month, endDay);
    }

    private static DateTime CreateDate(int year, int month, int day)
    {
        return new DateTime(year, month, Math.Clamp(day, 1, DateTime.DaysInMonth(year, month)));
    }
}
