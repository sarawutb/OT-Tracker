using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Enums;

namespace OTTracker.ViewModels;

public sealed partial class EntryDisplay : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EarningsText))]
    private bool maskEarnings;

    public EntryDisplay(OtEntry entry)
    {
        Entry = entry;
    }

    public OtEntry Entry { get; }

    public string DateText => Entry.EntryDate.ToString("ddd,d MMM");

    public string DayBox => Entry.EntryDate.Day.ToString("00");

    public string MonthBox => Entry.EntryDate.ToString("MMM").ToUpperInvariant();

    public string TypeText => $"{Entry.DayLabel} x {Entry.Multiplier:0.##}";

    public string TimeText => string.IsNullOrWhiteSpace(Entry.Note)
        ? $"{Entry.StartTime:hh\\:mm}-{Entry.EndTime:hh\\:mm}"
        : Entry.Note;

    public string HoursText => $"{Entry.NetHours:0.##} hrs";

    public string EarningsText => MaskEarnings ? "+\u0E3F*,***" : $"+\u0E3F{Entry.EstimatedEarnings:N2}";

    public Color AccentColor => Entry.DayType switch
    {
        DayType.Weekend => Color.FromArgb("#EF9F27"),
        DayType.Holiday => Color.FromArgb("#A32D2D"),
        _ => Color.FromArgb("#5B4FE8")
    };
}

public sealed class WeeklyDayDisplayModel
{
    public string Day { get; }
    public decimal Hours { get; }
    public bool IsWeekend { get; }
    public double BarHeight { get; }
    public bool IsToday { get; }

    public Color BarColor => IsWeekend
        ? (IsToday ? Color.FromArgb("#EF9F27") : Color.FromArgb("#FCD34D"))
        : (IsToday ? Color.FromArgb("#1D9E75") : Color.FromArgb("#6EE7B7"));

    public Color ShadowColor => IsWeekend
        ? Color.FromArgb("#EF9F27")
        : Color.FromArgb("#1D9E75");

    public Color TextColor => IsToday
        ? Color.FromArgb("#2A2859")
        : Color.FromArgb("#8E8EA9");

    public FontAttributes FontAttributes => IsToday
        ? FontAttributes.Bold
        : FontAttributes.None;

    public WeeklyDayDisplayModel(string day, decimal hours, bool isWeekend, double barHeight, bool isToday = false)
    {
        Day = day;
        Hours = hours;
        IsWeekend = isWeekend;
        BarHeight = barHeight;
        IsToday = isToday;
    }
}

public sealed class DayTypeDistributionSummary
{
    public string Title { get; }
    public string MultiplierText { get; }
    public decimal Hours { get; }
    public double Percentage { get; }
    public double BarHeight { get; }
    public Color BarColor { get; }
    public bool HasHours => Hours > 0;

    public string HoursText => $"{Hours:0.#}h";
    public string PercentageText => Hours > 0 ? $"{Percentage:P0}" : "0%";
    public Color TextColor => HasHours ? Color.FromArgb("#17162A") : Color.FromArgb("#8E8EA9");
    public FontAttributes FontAttributes => HasHours ? FontAttributes.Bold : FontAttributes.None;

    public DayTypeDistributionSummary(string title, string multiplierText, decimal hours, double percentage, double barHeight, Color barColor)
    {
        Title = title;
        MultiplierText = multiplierText;
        Hours = hours;
        Percentage = percentage;
        BarHeight = barHeight;
        BarColor = barColor;
    }
}

public sealed class MonthlyTrendSummary
{
    public string MonthName { get; }
    public decimal Hours { get; }
    public decimal Earnings { get; }
    public double BarHeight { get; }
    public bool IsCurrentMonth { get; }

    public Color BarColor => IsCurrentMonth
        ? Color.FromArgb("#5B4FE8")
        : Color.FromArgb("#A5B4FC");

    public Color TextColor => IsCurrentMonth
        ? Color.FromArgb("#2A2859")
        : Color.FromArgb("#8E8EA9");

    public FontAttributes FontAttributes => IsCurrentMonth
        ? FontAttributes.Bold
        : FontAttributes.None;

    public MonthlyTrendSummary(string monthName, decimal hours, decimal earnings, double barHeight, bool isCurrentMonth = false)
    {
        MonthName = monthName;
        Hours = hours;
        Earnings = earnings;
        BarHeight = barHeight;
        IsCurrentMonth = isCurrentMonth;
    }
}

public sealed partial class MonthPageModel : ObservableObject
{
    public DateTime MonthDate { get; }
    public OtPeriod Period { get; }
    public string MonthText => Period.DisplayText;
    public ObservableCollection<CalendarDay> CalendarDays { get; } = [];

    public MonthPageModel(DateTime monthDate, OtPeriod period, IEnumerable<CalendarDay> days)
    {
        MonthDate = monthDate;
        Period = period;
        foreach (var day in days)
        {
            CalendarDays.Add(day);
        }
    }
}
