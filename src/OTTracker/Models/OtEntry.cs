using SQLite;

namespace OTTracker.Models;

public sealed class OtEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime EntryDate { get; set; } = DateTime.Today;

    public DayType DayType { get; set; } = DayType.Regular;

    public TimeSpan StartTime { get; set; } = new(17, 0, 0);

    public TimeSpan EndTime { get; set; } = new(21, 0, 0);

    public int BreakMinutes { get; set; } = 30;

    public string Note { get; set; } = string.Empty;

    public decimal NetHours { get; set; }

    public decimal HourlyRate { get; set; }

    public decimal Multiplier { get; set; }

    public decimal EstimatedEarnings { get; set; }

    public DateTime CreateDate { get; set; } = DateTime.Now;

    public DateTime ReviseDate { get; set; } = DateTime.Now;

    [Ignore]
    public string DayLabel => DayType switch
    {
        DayType.Weekend => "Weekend",
        DayType.Holiday => "Holiday",
        _ => "Regular"
    };
}
