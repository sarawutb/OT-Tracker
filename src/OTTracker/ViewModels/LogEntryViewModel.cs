using CommunityToolkit.Mvvm.Input;
using OTTracker.Services.GlobalExceptions;
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
    private DateTime? entryDate;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private DayType selectedDayType = DayType.Regular;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private TimeSpan startTime = new(17, 0, 0);

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private TimeSpan endTime = new(21, 0, 0);

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(BreakMinutes))]
    private string breakMinutesText = "30";

    public int BreakMinutes
    {
        get => int.TryParse(BreakMinutesText, out var v) ? Math.Max(0, v) : 0;
        set => BreakMinutesText = value.ToString();
    }

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
        BackCommand = new AsyncRelayCommand(OnBackAsync);
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand<string> SelectDayTypeCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public IAsyncRelayCommand BackCommand { get; }

    public string NetHoursText => $"{NetHours:0.##} hrs";

    public string RateText => $"฿{HourlyRate:N2} / hr";

    public string MultiplierText => $"x {Multiplier:0.##} multiplier";

    public string EarningsText => $"฿{EstimatedEarnings:N2}";

    public string PageTitle => _entryId > 0 ? "Edit OT" : "Add OT";

    public async Task LoadAsync()
    {
        if (_entryId == 0)
        {
            await ApplyDefaultEntrySettingsAsync();
        }

        OnPropertyChanged(nameof(EntryDate));
        await RecalculateAsync();
    }

    public async Task OnBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ApplyQueryAttributesAsync(query).SafeFireAndForget(ex =>
        {
            CurrentPage?.DisplayAlert("Error", ex.Message, "OK");
        });
    }

    private async Task ApplyQueryAttributesAsync(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var value) && int.TryParse(value?.ToString(), out var id))
        {
            _entryId = id;
            OnPropertyChanged(nameof(PageTitle));
            var entry = await _entries.GetByIdAsync(id);
            if (entry is not null)
            {
                _entryId = entry.Id;
                SetEntryDateWithForce(entry.EntryDate);
                SelectedDayType = entry.DayType;
                StartTime = entry.StartTime;
                EndTime = entry.EndTime;
                BreakMinutes = entry.BreakMinutes;
                Note = entry.Note;
            }
            else
            {
                _entryId = 0;
            }
            OnPropertyChanged(nameof(PageTitle));
        }
        else
        {
            _entryId = 0;
            Note = string.Empty;
            SelectedDayType = DayType.Regular;
            await ApplyDefaultEntrySettingsAsync();
            if (query.TryGetValue("date", out var dateVal) && DateTime.TryParse(dateVal?.ToString(), out var parsedDate))
            {
                SetEntryDateWithForce(parsedDate);
            }
            else
            {
                ResetEntryDate();
            }
            OnPropertyChanged(nameof(PageTitle));
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
            entry.EntryDate = (EntryDate ?? DateTime.Today).Date;
            entry.DayType = SelectedDayType;
            entry.StartTime = StartTime;
            entry.EndTime = EndTime;
            entry.BreakMinutes = BreakMinutes;
            entry.Note = Note?.Trim() ?? string.Empty;
            _calculator.ApplyCalculation(entry, settings);

            await _entries.SaveAsync(entry);
            _entryId = 0;
            OnPropertyChanged(nameof(PageTitle));
            await Shell.Current.GoToAsync("..");
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
    }

    private void SetEntryDateWithForce(DateTime date)
    {
        if (date.Date == DateTime.Today)
        {
            EntryDate = DateTime.Today.AddDays(1);
        }
        EntryDate = date.Date;
        OnPropertyChanged(nameof(EntryDate));
    }

    private void ResetEntryDate()
    {
        SetEntryDateWithForce(DateTime.Today);
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

    partial void OnBreakMinutesTextChanged(string value)
    {
        if (!string.IsNullOrEmpty(value) && value.Length > 1 && value.StartsWith('0'))
        {
            if (int.TryParse(value, out var parsed))
            {
                var normalized = parsed.ToString();
                if (normalized != value)
                {
                    BreakMinutesText = normalized;
                    return;
                }
            }
        }

        _ = RecalculateAsync();
    }
}
