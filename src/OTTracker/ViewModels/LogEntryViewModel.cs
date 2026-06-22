using CommunityToolkit.Mvvm.Input;
using OTTracker.Models;
using OTTracker.Services;

namespace OTTracker.ViewModels;

public sealed class LogEntryViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IOtEntryRepository _entries;
    private readonly ISettingsService _settings;
    private readonly IOtCalculationService _calculator;
    private readonly AppEvents _events;
    private int _entryId;
    private DateTime _entryDate = DateTime.Today;
    private DayType _selectedDayType = DayType.Regular;
    private TimeSpan _startTime = new(17, 0, 0);
    private TimeSpan _endTime = new(21, 0, 0);
    private int _breakMinutes = 30;
    private string _note = string.Empty;
    private decimal _hourlyRate;
    private decimal _multiplier;
    private decimal _netHours;
    private decimal _estimatedEarnings;

    public LogEntryViewModel(IOtEntryRepository entries, ISettingsService settings, IOtCalculationService calculator, AppEvents events)
    {
        _entries = entries;
        _settings = settings;
        _calculator = calculator;
        _events = events;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SelectDayTypeCommand = new AsyncRelayCommand<string>(SelectDayTypeAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand<string> SelectDayTypeCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public DateTime EntryDate
    {
        get => _entryDate;
        set => SetProperty(ref _entryDate, value);
    }

    public DayType SelectedDayType
    {
        get => _selectedDayType;
        set
        {
            if (SetProperty(ref _selectedDayType, value))
            {
                _ = RecalculateAsync();
            }
        }
    }

    public TimeSpan StartTime
    {
        get => _startTime;
        set
        {
            if (SetProperty(ref _startTime, value))
            {
                _ = RecalculateAsync();
            }
        }
    }

    public TimeSpan EndTime
    {
        get => _endTime;
        set
        {
            if (SetProperty(ref _endTime, value))
            {
                _ = RecalculateAsync();
            }
        }
    }

    public int BreakMinutes
    {
        get => _breakMinutes;
        set
        {
            if (SetProperty(ref _breakMinutes, value))
            {
                _ = RecalculateAsync();
            }
        }
    }

    public string Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    }

    public decimal HourlyRate
    {
        get => _hourlyRate;
        set => SetProperty(ref _hourlyRate, value);
    }

    public decimal Multiplier
    {
        get => _multiplier;
        set => SetProperty(ref _multiplier, value);
    }

    public decimal NetHours
    {
        get => _netHours;
        set
        {
            if (SetProperty(ref _netHours, value))
            {
                OnPropertyChanged(nameof(NetHoursText));
            }
        }
    }

    public decimal EstimatedEarnings
    {
        get => _estimatedEarnings;
        set
        {
            if (SetProperty(ref _estimatedEarnings, value))
            {
                OnPropertyChanged(nameof(EarningsText));
            }
        }
    }

    public string NetHoursText => $"{NetHours:0.##} hrs";

    public string RateText => $"฿{HourlyRate:N2} / hr";

    public string MultiplierText => $"x {Multiplier:0.##} multiplier";

    public string EarningsText => $"฿{EstimatedEarnings:N2}";

    public async Task LoadAsync()
    {
        if (_entryId == 0)
        {
            await ApplyDefaultEntrySettingsAsync();
        }

        await RecalculateAsync();
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var value) && int.TryParse(value?.ToString(), out var id))
        {
            var entry = await _entries.GetByIdAsync(id);
            if (entry is not null)
            {
                _entryId = entry.Id;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    EntryDate = entry.EntryDate;
                });
                SelectedDayType = entry.DayType;
                StartTime = entry.StartTime;
                EndTime = entry.EndTime;
                BreakMinutes = entry.BreakMinutes;
                Note = entry.Note;
            }
        }
        await RecalculateAsync();
    }

    private async Task SelectDayTypeAsync(string? dayType)
    {
        if (Enum.TryParse<DayType>(dayType, out var parsed))
        {
            SelectedDayType = parsed;
            await RecalculateAsync();
        }
    }

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        if (BreakMinutes < 0)
        {
            ErrorMessage = "Break minutes must be 0 or greater.";
            return;
        }

        if (EndTime <= StartTime)
        {
            ErrorMessage = "End time must be later than start time.";
            return;
        }

        var settings = await _settings.GetAsync();
        var entry = _entryId > 0 ? await _entries.GetByIdAsync(_entryId) ?? new OtEntry() : new OtEntry();
        entry.EntryDate = EntryDate.Date;
        entry.DayType = SelectedDayType;
        entry.StartTime = StartTime;
        entry.EndTime = EndTime;
        entry.BreakMinutes = BreakMinutes;
        entry.Note = Note?.Trim() ?? string.Empty;
        _calculator.ApplyCalculation(entry, settings);

        await _entries.SaveAsync(entry);
        _entryId = 0;
        await Shell.Current.GoToAsync("//Dashboard");
        _events.NotifyEntriesChanged();
        await ResetForNewEntryAsync();
    }

    private async Task RecalculateAsync()
    {
        var settings = await _settings.GetAsync();
        HourlyRate = _calculator.GetHourlyRate(settings);
        Multiplier = _calculator.GetMultiplier(settings, SelectedDayType);
        NetHours = _calculator.GetNetHours(StartTime, EndTime, BreakMinutes);
        EstimatedEarnings = _calculator.GetEstimatedEarnings(NetHours, HourlyRate, Multiplier);
        OnPropertyChanged(nameof(RateText));
        OnPropertyChanged(nameof(MultiplierText));
    }

    private async Task ResetForNewEntryAsync()
    {
        EntryDate = DateTime.Today;
        SelectedDayType = DayType.Regular;
        await ApplyDefaultEntrySettingsAsync();
        Note = string.Empty;
    }

    private async Task ApplyDefaultEntrySettingsAsync()
    {
        var settings = await _settings.GetAsync();
        StartTime = settings.DefaultStartTime;
        EndTime = settings.DefaultEndTime;
        BreakMinutes = settings.DefaultBreakMinutes;
    }
}
