using CommunityToolkit.Mvvm.Input;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Enums;
using OTTracker.Domain.Interfaces;
using OTTracker.Infrastructure.Services;

namespace OTTracker.ViewModels;

public sealed partial class LogEntryViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IOtEntryRepository _entries;
    private readonly ISettingsService _settings;
    private readonly IOtCalculationService _calculator;
    private readonly AppEvents _events;
    private int _entryId;
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private DateTime entryDate = DateTime.Today;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private DayType selectedDayType = DayType.Regular;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private TimeSpan startTime = new(17, 0, 0);

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private TimeSpan endTime = new(21, 0, 0);

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private int breakMinutes = 30;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string note = string.Empty;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(RateText))]
    private decimal hourlyRate;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(MultiplierText))]
    private decimal multiplier;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(NetHoursText))]
    private decimal netHours;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(EarningsText))]
    private decimal estimatedEarnings;

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

    public async Task OnBackAsync()
    {
        if (_entryId == 0)
        {
            await Shell.Current.GoToAsync("//Dashboard");
        }
        else
        {
            await Shell.Current.GoToAsync("//History");
        }
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var value) && int.TryParse(value?.ToString(), out var id))
        {
            var entry = await _entries.GetByIdAsync(id);
            if (entry is not null)
            {
                _entryId = entry.Id;
                EntryDate = entry.EntryDate;
                SelectedDayType = entry.DayType;
                StartTime = entry.StartTime;
                EndTime = entry.EndTime;
                BreakMinutes = entry.BreakMinutes;
                Note = entry.Note;
            }
        }
        await RecalculateAsync();
    }

    private Task SelectDayTypeAsync(string? dayType)
    {
        if (Enum.TryParse<DayType>(dayType, out var parsed))
        {
            SelectedDayType = parsed;
        }

        return Task.CompletedTask;
    }

    private async Task SaveAsync()
    {
        try
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
        catch (Exception ex)
        {
            CurrentPage?.DisplayAlert("Error Message", ex.Message, "OK");
        }
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
        ResetEntryDate();
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
        ResetEntryDate();
    }

    private void ResetEntryDate()
    {
        var date = DateTime.Now.AddDays(-1);

        if (date.DayOfWeek == DayOfWeek.Sunday)
        {
            date = date.AddDays(-1);
        }

        EntryDate = date;
    }

    partial void OnSelectedDayTypeChanged(DayType value)
    {
        _ = RecalculateAsync();
    }

    partial void OnStartTimeChanged(TimeSpan value)
    {
        _ = RecalculateAsync();
    }

    partial void OnEndTimeChanged(TimeSpan value)
    {
        _ = RecalculateAsync();
    }

    partial void OnBreakMinutesChanged(int value)
    {
        _ = RecalculateAsync();
    }
}
