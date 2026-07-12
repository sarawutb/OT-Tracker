using System;
using OTTracker.Domain.Enums;

namespace OTTracker.Domain.Entities;

public sealed class CalendarDay
{
    public DateTime? Date { get; init; }

    public bool IsBlank => Date is null;

    public bool HasEntries { get; init; }

    public bool IsSelected { get; init; }

    public bool IsToday { get; init; }

    public decimal TotalHours { get; init; }

    public DayType? DayType { get; init; }

    public string DayText => Date?.Day.ToString() ?? string.Empty;

    public string HoursText => TotalHours > 0 ? $"+{TotalHours:0.#}h" : string.Empty;

    public string AccentColorHex => DayType switch
    {
        Enums.DayType.Weekend => "#EF9F27",
        Enums.DayType.Holiday => "#A32D2D",
        Enums.DayType.Regular => "#5B4FE8",
        _ => "#00000000"
    };

    public string LightBgColorHex => DayType switch
    {
        Enums.DayType.Weekend => "#FFF8ED", // Very soft amber
        Enums.DayType.Holiday => "#FFF2F2", // Very soft red
        Enums.DayType.Regular => "#F0EEFD", // Very soft blue
        _ => "#F7F8FC" // Standard surface
    };
}
