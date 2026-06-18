using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using OTTracker.Models;
using OTTracker.Services;

namespace OTTracker.ViewModels;

public sealed class HistoryViewModel : BaseViewModel
{
    private readonly IOtEntryRepository _entries;
    private readonly AppEvents _events;
    private DateTime _selectedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _selectedDate = DateTime.Today;
    private decimal _monthHours;
    private decimal _monthEarnings;

    public HistoryViewModel(IOtEntryRepository entries, AppEvents events)
    {
        _entries = entries;
        _events = events;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        PreviousMonthCommand = new AsyncRelayCommand(PreviousMonthAsync);
        NextMonthCommand = new AsyncRelayCommand(NextMonthAsync);
        SelectDateCommand = new AsyncRelayCommand<CalendarDay>(SelectDateAsync);
        EditCommand = new AsyncRelayCommand<EntryDisplay>(EditAsync);
        DeleteCommand = new AsyncRelayCommand<EntryDisplay>(DeleteAsync);
        _events.EntriesChanged += async (_, _) => await LoadAsync();
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand PreviousMonthCommand { get; }

    public IAsyncRelayCommand NextMonthCommand { get; }

    public IAsyncRelayCommand<CalendarDay> SelectDateCommand { get; }

    public IAsyncRelayCommand<EntryDisplay> EditCommand { get; }

    public IAsyncRelayCommand<EntryDisplay> DeleteCommand { get; }

    public ObservableCollection<CalendarDay> CalendarDays { get; } = [];

    public ObservableCollection<EntryDisplay> MonthEntries { get; } = [];

    public DateTime SelectedMonth
    {
        get => _selectedMonth;
        set
        {
            if (SetProperty(ref _selectedMonth, value))
            {
                OnPropertyChanged(nameof(MonthText));
            }
        }
    }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set => SetProperty(ref _selectedDate, value);
    }

    public decimal MonthHours
    {
        get => _monthHours;
        set
        {
            if (SetProperty(ref _monthHours, value))
            {
                OnPropertyChanged(nameof(MonthHoursText));
            }
        }
    }

    public decimal MonthEarnings
    {
        get => _monthEarnings;
        set
        {
            if (SetProperty(ref _monthEarnings, value))
            {
                OnPropertyChanged(nameof(MonthEarningsText));
            }
        }
    }

    public string MonthText => SelectedMonth.ToString("MMMM yyyy");

    public string MonthHoursText => $"{MonthHours:0.##} hrs";

    public string MonthEarningsText => $"฿{MonthEarnings:N0}";

    public async Task LoadAsync()
    {
        var monthEntries = await _entries.GetMonthAsync(SelectedMonth.Year, SelectedMonth.Month);
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
        var blanks = (int)new DateTime(SelectedMonth.Year, SelectedMonth.Month, 1).DayOfWeek;
        for (var i = 0; i < blanks; i++)
        {
            CalendarDays.Add(new CalendarDay());
        }

        var daysInMonth = DateTime.DaysInMonth(SelectedMonth.Year, SelectedMonth.Month);
        var entryDates = monthEntries.Select(e => e.EntryDate.Date).ToHashSet();
        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(SelectedMonth.Year, SelectedMonth.Month, day);
            CalendarDays.Add(new CalendarDay
            {
                Date = date,
                HasEntries = entryDates.Contains(date),
                IsSelected = date == SelectedDate.Date,
                IsToday = date == DateTime.Today
            });
        }
    }

    private async Task PreviousMonthAsync()
    {
        SelectedMonth = SelectedMonth.AddMonths(-1);
        SelectedDate = SelectedMonth;
        await LoadAsync();
    }

    private async Task NextMonthAsync()
    {
        SelectedMonth = SelectedMonth.AddMonths(1);
        SelectedDate = SelectedMonth;
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
}
