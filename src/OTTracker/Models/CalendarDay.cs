namespace OTTracker.Models;

public sealed class CalendarDay
{
    public DateTime? Date { get; init; }

    public bool IsBlank => Date is null;

    public bool HasEntries { get; init; }

    public bool IsSelected { get; init; }

    public bool IsToday { get; init; }

    public string DayText => Date?.Day.ToString() ?? string.Empty;
}
