using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OTTracker.Domain.Enums;

namespace OTTracker.Domain.Entities;

public sealed class CalendarDay : INotifyPropertyChanged
{
    private bool _isSelected;

    public DateTime? Date { get; init; }

    public bool IsBlank => Date is null;

    public bool HasEntries { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

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
        Enums.DayType.Weekend => "#FFF8ED",
        Enums.DayType.Holiday => "#FFF2F2",
        Enums.DayType.Regular => "#F0EEFD",
        _ => "#F7F8FC"
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
