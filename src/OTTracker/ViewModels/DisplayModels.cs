using OTTracker.Models;

namespace OTTracker.ViewModels;

public sealed class EntryDisplay
{
    public EntryDisplay(OtEntry entry)
    {
        Entry = entry;
    }

    public OtEntry Entry { get; }

    public string DateText => Entry.EntryDate.ToString("ddd, MMM d");

    public string DayBox => Entry.EntryDate.Day.ToString("00");

    public string MonthBox => Entry.EntryDate.ToString("MMM").ToUpperInvariant();

    public string TypeText => $"{Entry.DayLabel} x {Entry.Multiplier:0.##}";

    public string TimeText => string.IsNullOrWhiteSpace(Entry.Note)
        ? $"{Entry.StartTime:hh\\:mm}-{Entry.EndTime:hh\\:mm}"
        : Entry.Note;

    public string HoursText => $"{Entry.NetHours:0.##} hrs";

    public string EarningsText => $"+฿{Entry.EstimatedEarnings:N2}";

    public Color AccentColor => Entry.DayType switch
    {
        DayType.Weekend => Color.FromArgb("#EF9F27"),
        DayType.Holiday => Color.FromArgb("#A32D2D"),
        _ => Color.FromArgb("#5B4FE8")
    };
}
