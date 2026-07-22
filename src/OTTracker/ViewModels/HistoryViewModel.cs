using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using OTTracker.Infrastructure.Services;

namespace OTTracker.ViewModels;

public sealed partial class HistoryViewModel : BaseViewModel
{
    private readonly IOtEntryRepository _entries;
    private readonly ISettingsService _settings;
    private readonly AppEvents _events;
    private OtPeriod _settingsPeriod = OtPeriod.FromDate(DateTime.Today, 16, 15);
    private bool _periodInitialized;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(MonthText))]
    private DateTime selectedMonth = DateTime.Today;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private DateTime selectedDate = DateTime.Today;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(MonthHoursText))]
    private decimal monthHours;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(MonthEarningsText))]
    private decimal monthEarnings;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool isCalendarView = true;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool hasSelectedDayEntries;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string selectedDayText = string.Empty;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private int currentPagePosition = 1;

    private bool _isUpdatingPosition;

    public HistoryViewModel(IOtEntryRepository entries, ISettingsService settings, AppEvents events)
    {
        _entries = entries;
        _settings = settings;
        _events = events;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        PreviousMonthCommand = new AsyncRelayCommand(PreviousMonthAsync);
        NextMonthCommand = new AsyncRelayCommand(NextMonthAsync);
        EditCommand = new AsyncRelayCommand<EntryDisplay>(EditAsync);
        DeleteCommand = new AsyncRelayCommand<EntryDisplay>(DeleteAsync);
        ClickDayCommand = new AsyncRelayCommand<CalendarDay>(ClickDayAsync);
        LogSelectedDayCommand = new AsyncRelayCommand(LogSelectedDayAsync);
        _events.EntriesChanged += async (_, _) => await LoadAsync();
        _events.SettingsChanged += async (_, _) => await LoadAsync();
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand PreviousMonthCommand { get; }

    public IAsyncRelayCommand NextMonthCommand { get; }

    public IAsyncRelayCommand<EntryDisplay> EditCommand { get; }

    public IAsyncRelayCommand<EntryDisplay> DeleteCommand { get; }

    public IAsyncRelayCommand<CalendarDay> ClickDayCommand { get; }

    public IAsyncRelayCommand LogSelectedDayCommand { get; }

    public ObservableCollection<CalendarDay> CalendarDays { get; } = [];

    public ObservableCollection<MonthPageModel> MonthPages { get; } = [];

    public ObservableCollection<EntryDisplay> MonthEntries { get; } = [];

    public ObservableCollection<EntryDisplay> SelectedDayEntries { get; } = [];

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

        var prevPeriod = GetOtPeriod(SelectedMonth.AddMonths(-1));
        var currentPeriod = GetOtPeriod(SelectedMonth);
        var nextPeriod = GetOtPeriod(SelectedMonth.AddMonths(1));

        var prevPage = await BuildMonthPageAsync(SelectedMonth.AddMonths(-1), prevPeriod);
        var currentPage = await BuildMonthPageAsync(SelectedMonth, currentPeriod);
        var nextPage = await BuildMonthPageAsync(SelectedMonth.AddMonths(1), nextPeriod);

        _isUpdatingPosition = true;
        MonthPages.Clear();
        MonthPages.Add(prevPage);
        MonthPages.Add(currentPage);
        MonthPages.Add(nextPage);
        CurrentPagePosition = 1;
        _isUpdatingPosition = false;

        var monthEntries = await _entries.GetPeriodAsync(currentPeriod.Start, currentPeriod.End);
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

        UpdateSelectedDayEntries();
        IsBusy = false;
    }

    private async Task<MonthPageModel> BuildMonthPageAsync(DateTime monthDate, OtPeriod period)
    {
        var entries = await _entries.GetPeriodAsync(period.Start, period.End);
        var entryGroups = entries.GroupBy(e => e.EntryDate.Date)
                                 .ToDictionary(g => g.Key, g => g.ToList());

        var days = new List<CalendarDay>();
        var blanks = (int)period.Start.DayOfWeek;
        for (var i = 0; i < blanks; i++)
        {
            days.Add(new CalendarDay());
        }

        for (var date = period.Start; date <= period.End; date = date.AddDays(1))
        {
            var hasEntries = entryGroups.TryGetValue(date.Date, out var dayEntries);
            var totalHours = hasEntries ? dayEntries.Sum(e => e.NetHours) : 0m;
            var dominantDayType = hasEntries ? dayEntries.First().DayType : (OTTracker.Domain.Enums.DayType?)null;

            days.Add(new CalendarDay
            {
                Date = date,
                HasEntries = hasEntries,
                TotalHours = totalHours,
                DayType = dominantDayType,
                IsSelected = date.Date == SelectedDate.Date,
                IsToday = date.Date == DateTime.Today
            });
        }

        return new MonthPageModel(monthDate, period, days);
    }

    partial void OnCurrentPagePositionChanged(int value)
    {
        if (_isUpdatingPosition) return;

        if (value == 0)
        {
            _ = PreviousMonthAsync();
        }
        else if (value == 2)
        {
            _ = NextMonthAsync();
        }
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

    private static async Task EditAsync(EntryDisplay? display)
    {
        if (display is not null)
        {
            await Shell.Current.GoToAsync($"Log?id={display.Entry.Id}");
        }
    }

    private async Task ClickDayAsync(CalendarDay? day)
    {
        if (day is null || day.Date is null)
        {
            return;
        }

        SelectedDate = day.Date.Value.Date;
        UpdateSelectedDayEntries();
        RefreshCalendarSelection();
        await Task.CompletedTask;
    }

    private void UpdateSelectedDayEntries()
    {
        SelectedDayEntries.Clear();
        var dayEntries = MonthEntries.Where(e => e.Entry.EntryDate.Date == SelectedDate.Date).ToList();
        foreach (var entry in dayEntries)
        {
            SelectedDayEntries.Add(entry);
        }
        HasSelectedDayEntries = SelectedDayEntries.Count > 0;
        SelectedDayText = SelectedDate.ToString("d MMM yyyy");
    }

    private void RefreshCalendarSelection()
    {
        if (MonthPages.Count > 1)
        {
            var currentMonthPage = MonthPages[1];
            var updatedDays = currentMonthPage.CalendarDays.Select(d => d.IsBlank ? d : new CalendarDay
            {
                Date = d.Date,
                HasEntries = d.HasEntries,
                TotalHours = d.TotalHours,
                DayType = d.DayType,
                IsSelected = d.Date.Value.Date == SelectedDate.Date,
                IsToday = d.IsToday
            }).ToList();

            currentMonthPage.CalendarDays.Clear();
            foreach (var d in updatedDays)
            {
                currentMonthPage.CalendarDays.Add(d);
            }
        }
    }

    private async Task LogSelectedDayAsync()
    {
        await Shell.Current.GoToAsync($"Log?date={SelectedDate:yyyy-MM-dd}");
    }

    private async Task DeleteAsync(EntryDisplay? display)
    {
        if (display is null)
        {
            return;
        }

        var confirm = await Shell.Current.DisplayAlert("Delete entry", "Remove this OT entry?", "Delete", "Cancel");
        if (!confirm)
        {
            return;
        }

        await _entries.DeleteAsync(display.Entry);
        _events.NotifyEntriesChanged();
        await LoadAsync();
    }

    private OtPeriod GetOtPeriod(DateTime date)
    {
        var offset = ((date.Year - _settingsPeriod.Start.Year) * 12) + date.Month - _settingsPeriod.Start.Month;
        return _settingsPeriod.AddMonths(offset);
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ShowCalendarView() => IsCalendarView = true;

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ShowListView() => IsCalendarView = false;
}
