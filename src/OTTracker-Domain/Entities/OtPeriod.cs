using System;
using System.Globalization;

namespace OTTracker.Domain.Entities;

public readonly record struct OtPeriod(DateTime Start, DateTime End)
{
    private static readonly Calendar Gregorian = new GregorianCalendar();

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
        var targetYear = Gregorian.GetYear(target);
        var targetMonth = Gregorian.GetMonth(target);

        var start = CreateDate(targetYear, targetMonth, startDay);
        var end = GetEndDate(start, startDay, endDay);

        if (target < start)
        {
            var prevTarget = target.AddMonths(-1);
            start = CreateDate(Gregorian.GetYear(prevTarget), Gregorian.GetMonth(prevTarget), startDay);
            end = GetEndDate(start, startDay, endDay);
        }
        else if (target > end)
        {
            var nextTarget = target.AddMonths(1);
            start = CreateDate(Gregorian.GetYear(nextTarget), Gregorian.GetMonth(nextTarget), startDay);
            end = GetEndDate(start, startDay, endDay);
        }

        return new OtPeriod(start, end);
    }

    private static DateTime GetEndDate(DateTime start, int startDay, int endDay)
    {
        var endMonth = startDay <= endDay ? start : start.AddMonths(1);
        return CreateDate(Gregorian.GetYear(endMonth), Gregorian.GetMonth(endMonth), endDay);
    }

    private static DateTime CreateDate(int year, int month, int day)
    {
        return new DateTime(year, month, Math.Clamp(day, 1, DateTime.DaysInMonth(year, month)));
    }
}
