using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Enums;
using OTTracker.Domain.Interfaces;
using OTTracker.Domain.Interfaces;
using OTTracker.Infrastructure.Services;

namespace OTTracker_Avalonia.ViewModels;

public sealed partial class LogEntryViewModel : ViewModelBase
{
    private readonly IOtEntryRepository _entries;
    private readonly ISettingsService _settings;
    private readonly IOtCalculationService _calculator;
    private readonly AppEvents _events;
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IServiceProvider _services;
    
    private int _entryId;

    [ObservableProperty]
    private DateTime _entryDate = DateTime.Now.AddDays(-1);

    [ObservableProperty]
    private DayType _selectedDayType = DayType.Regular;

    [ObservableProperty]
    private TimeSpan _startTime = new(17, 0, 0);

    [ObservableProperty]
    private TimeSpan _endTime = new(21, 0, 0);

    [ObservableProperty]
    private int _breakMinutes = 30;

    [ObservableProperty]
    private string _note = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RateText))]
    private decimal _hourlyRate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MultiplierText))]
    private decimal _multiplier;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NetHoursText))]
    private decimal _netHours;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EarningsText))]
    private decimal _estimatedEarnings;

    public LogEntryViewModel(
        IOtEntryRepository entries,
        ISettingsService settings,
        IOtCalculationService calculator,
        AppEvents events,
        MainWindowViewModel mainWindowViewModel,
        IServiceProvider services)
    {
        _entries = entries;
        _settings = settings;
        _calculator = calculator;
        _events = events;
        _mainWindowViewModel = mainWindowViewModel;
        _services = services;

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SelectDayTypeCommand = new AsyncRelayCommand<string>(SelectDayTypeAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new AsyncRelayCommand(OnBackAsync);
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand<string> SelectDayTypeCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }
    
    public IAsyncRelayCommand CancelCommand { get; }

    public string NetHoursText => $"{NetHours:0.##} hrs";

    public string RateText => $"฿{HourlyRate:N2} / hr";

    public string MultiplierText => $"x {Multiplier:0.##} multiplier";

    public string EarningsText => $"฿{EstimatedEarnings:N2}";

    public async Task InitializeWithEntryIdAsync(int entryId)
    {
        _entryId = entryId;
        if (entryId > 0)
        {
            var entry = await _entries.GetByIdAsync(entryId);
            if (entry is not null)
            {
                EntryDate = entry.EntryDate;
                SelectedDayType = entry.DayType;
                StartTime = entry.StartTime;
                EndTime = entry.EndTime;
                BreakMinutes = entry.BreakMinutes;
                Note = entry.Note;
            }
        }
        else
        {
            _entryId = 0;
            Note = string.Empty;
            SelectedDayType = DayType.Regular;
            await ApplyDefaultEntrySettingsAsync();
        }
        await RecalculateAsync();
    }

    public async Task LoadAsync()
    {
        if (_entryId == 0)
        {
            await ApplyDefaultEntrySettingsAsync();
        }

        await RecalculateAsync();
    }

    public Task OnBackAsync()
    {
        if (_entryId == 0)
        {
            var dashboard = _services.GetRequiredService<DashboardViewModel>();
            _mainWindowViewModel.NavigateTo(dashboard);
        }
        else
        {
            var history = _services.GetRequiredService<HistoryViewModel>();
            _mainWindowViewModel.NavigateTo(history);
        }
        return Task.CompletedTask;
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
        
        var dashboard = _services.GetRequiredService<DashboardViewModel>();
        _mainWindowViewModel.NavigateTo(dashboard);
        
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
        var date = DateTime.Today.AddDays(-1);

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
