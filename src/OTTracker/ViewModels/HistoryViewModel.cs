using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using OTTracker.Models;
using OTTracker.Services;

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

    public HistoryViewModel(IOtEntryRepository entries, ISettingsService settings, AppEvents events)
    {
        _entries = entries;
        _settings = settings;
        _events = events;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        PreviousMonthCommand = new AsyncRelayCommand(PreviousMonthAsync);
        NextMonthCommand = new AsyncRelayCommand(NextMonthAsync);
        SelectDateCommand = new AsyncRelayCommand<CalendarDay>(SelectDateAsync);
        EditCommand = new AsyncRelayCommand<EntryDisplay>(EditAsync);
        DeleteCommand = new AsyncRelayCommand<EntryDisplay>(DeleteAsync);
        _events.EntriesChanged += async (_, _) => await LoadAsync();
        _events.SettingsChanged += async (_, _) => await LoadAsync();
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand PreviousMonthCommand { get; }

    public IAsyncRelayCommand NextMonthCommand { get; }

    public IAsyncRelayCommand<CalendarDay> SelectDateCommand { get; }

    public IAsyncRelayCommand<EntryDisplay> EditCommand { get; }

    public IAsyncRelayCommand<EntryDisplay> DeleteCommand { get; }

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

    private async Task SelectDateAsync(CalendarDay? day)
    {
        if (day?.Date is null)
        {
            return;
        }

        SelectedDate = day.Date.Value;
        await LoadAsync();
    }

    private static async Task EditAsync(EntryDisplay? display)
    {
        if (display is not null)
        {
            await Shell.Current.GoToAsync($"//Log?id={display.Entry.Id}");
        }
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
}
