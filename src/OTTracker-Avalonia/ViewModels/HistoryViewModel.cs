using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OTTracker_Avalonia.Domain.Entities;
using OTTracker_Avalonia.AppServices.Interfaces.Repositories;
using OTTracker_Avalonia.AppServices.Interfaces.Services;
using OTTracker_Avalonia.AppServices.Services;
using OTTracker_Avalonia.AppServices.ViewModels;

namespace OTTracker_Avalonia.ViewModels;

public sealed partial class HistoryViewModel : ViewModelBase
{
    private readonly IOtEntryRepository _entries;
    private readonly ISettingsService _settings;
    private readonly AppEvents _events;
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IServiceProvider _services;

    private OtPeriod _settingsPeriod = OtPeriod.FromDate(DateTime.Today, 16, 15);
    private bool _periodInitialized;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthText))]
    private DateTime _selectedMonth = DateTime.Today;

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthHoursText))]
    private decimal _monthHours;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthEarningsText))]
    private decimal _monthEarnings;

    [ObservableProperty]
    private bool _isDeleteConfirmationVisible;

    [ObservableProperty]
    private EntryDisplay? _deletingEntry;

    public HistoryViewModel(
        IOtEntryRepository entries,
        ISettingsService settings,
        AppEvents events,
        MainWindowViewModel mainWindowViewModel,
        IServiceProvider services)
    {
        _entries = entries;
        _settings = settings;
        _events = events;
        _mainWindowViewModel = mainWindowViewModel;
        _services = services;

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        PreviousMonthCommand = new AsyncRelayCommand(PreviousMonthAsync);
        NextMonthCommand = new AsyncRelayCommand(NextMonthAsync);
        EditCommand = new AsyncRelayCommand<EntryDisplay>(EditAsync);
        DeleteCommand = new RelayCommand<EntryDisplay>(ShowDeleteConfirmation);
        ConfirmDeleteCommand = new AsyncRelayCommand(ConfirmDeleteAsync);
        CancelDeleteCommand = new RelayCommand(CancelDelete);

        _events.EntriesChanged += async (_, _) => await LoadAsync();
        _events.SettingsChanged += async (_, _) => await LoadAsync();
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand PreviousMonthCommand { get; }

    public IAsyncRelayCommand NextMonthCommand { get; }

    public IAsyncRelayCommand<EntryDisplay> EditCommand { get; }

    public IRelayCommand<EntryDisplay> DeleteCommand { get; }

    public IAsyncRelayCommand ConfirmDeleteCommand { get; }

    public IRelayCommand CancelDeleteCommand { get; }

    public ObservableCollection<CalendarDay> CalendarDays { get; } = [];

    public ObservableCollection<EntryDisplay> MonthEntries { get; } = [];

    public string MonthText
    {
        get
        {
            var period = GetOtPeriod(SelectedMonth);
            return period.DisplayText;
        }
    }

    public string MonthHoursText => $"{MonthHours:0.##} hrs";

    public string MonthEarningsText => $"฿{MonthEarnings:N0}";

    public async Task LoadAsync()
    {
        IsBusy = true;
        var settings = await _settings.GetAsync();
        _settingsPeriod = OtPeriod.FromDate(DateTime.Today, settings.PeriodStartDay, settings.PeriodEndDay);
        
        if (!_periodInitialized)
        {
            SelectedMonth = _settingsPeriod.Start;
            SelectedDate = _settingsPeriod.Start;
            _periodInitialized = true;
        }
        OnPropertyChanged(nameof(MonthText));

        var period = GetOtPeriod(SelectedMonth);
        var monthEntries = await _entries.GetPeriodAsync(period.Start, period.End);
        
        MonthHours = monthEntries.Sum(e => e.NetHours);
        MonthEarnings = monthEntries.Sum(e => e.EstimatedEarnings);

        MonthEntries.Clear();
        foreach (var display in monthEntries
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.StartTime)
            .Select(e => new EntryDisplay(e)))
        {
            MonthEntries.Add(display);
        }

        CalendarDays.Clear();
        var blanks = (int)period.Start.DayOfWeek;
        for (var i = 0; i < blanks; i++)
        {
            CalendarDays.Add(new CalendarDay());
        }

        var entryDates = monthEntries.Select(e => e.EntryDate.Date).ToHashSet();
        for (var date = period.Start; date <= period.End; date = date.AddDays(1))
        {
            CalendarDays.Add(new CalendarDay
            {
                Date = date,
                HasEntries = entryDates.Contains(date),
                IsSelected = date == SelectedDate.Date,
                IsToday = date == DateTime.Today
            });
        }
        IsBusy = false;
    }

    private async Task PreviousMonthAsync()
    {
        SelectedMonth = SelectedMonth.AddMonths(-1);
        SelectedDate = GetOtPeriod(SelectedMonth).Start;
        await LoadAsync();
    }

    private async Task NextMonthAsync()
    {
        SelectedMonth = SelectedMonth.AddMonths(1);
        SelectedDate = GetOtPeriod(SelectedMonth).Start;
        await LoadAsync();
    }

    private async Task EditAsync(EntryDisplay? display)
    {
        if (display is not null)
        {
            var logViewModel = _services.GetRequiredService<LogEntryViewModel>();
            await logViewModel.InitializeWithEntryIdAsync(display.Entry.Id);
            _mainWindowViewModel.NavigateTo(logViewModel);
        }
    }

    private void ShowDeleteConfirmation(EntryDisplay? display)
    {
        if (display is not null)
        {
            DeletingEntry = display;
            IsDeleteConfirmationVisible = true;
        }
    }

    private async Task ConfirmDeleteAsync()
    {
        if (DeletingEntry is not null)
        {
            await _entries.DeleteAsync(DeletingEntry.Entry);
            _events.NotifyEntriesChanged();
            IsDeleteConfirmationVisible = false;
            DeletingEntry = null;
            await LoadAsync();
        }
    }

    private void CancelDelete()
    {
        IsDeleteConfirmationVisible = false;
        DeletingEntry = null;
    }

    private OtPeriod GetOtPeriod(DateTime date)
    {
        var offset = ((date.Year - _settingsPeriod.Start.Year) * 12) + date.Month - _settingsPeriod.Start.Month;
        return _settingsPeriod.AddMonths(offset);
    }
}
