using CommunityToolkit.Mvvm.ComponentModel;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Enums;

namespace OTTracker_Avalonia.AppServices.ViewModels;

public sealed partial class EntryDisplay : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EarningsText))]
    private bool _maskEarnings;

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

    public string EarningsText => MaskEarnings ? "+\u0E3F*,***" : $"+\u0E3F{Entry.EstimatedEarnings:N2}";

    public string AccentColor => Entry.DayType switch
    {
        DayType.Weekend => "#FFBE3B",
        DayType.Holiday => "#FF5247",
        _ => "#786DF7"
    };
}
