using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using OTTracker.Models;
using OTTracker.Services;

namespace OTTracker.ViewModels;

public sealed class DashboardViewModel : BaseViewModel
{
    private readonly IOtEntryRepository _entries;
    private string _monthText = DateTime.Today.ToString("MMMM yyyy");
    private decimal _totalHours;
    private decimal _estimatedEarnings;
    private decimal _thisWeekHours;
    private int _thisWeekEntries;
    private bool _maskEarnings;

    public DashboardViewModel(IOtEntryRepository entries, ISettingsService settings, AppEvents events)
    {
        _entries = entries;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        GoLogTodayCommand = new AsyncRelayCommand(GoLogTodayAsync);
        GoHistoryCommand = new AsyncRelayCommand(GoHistoryAsync);
        events.EntriesChanged += async (_, _) => await LoadAsync();
        events.SettingsChanged += async (_, _) => await LoadAsync();
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand GoLogTodayCommand { get; }

    public IAsyncRelayCommand GoHistoryCommand { get; }

    public ObservableCollection<EntryDisplay> RecentEntries { get; } = [];

    public ObservableCollection<WeeklyDaySummary> WeeklySummaries { get; } = [];

    public string MonthText
    {
        get => _monthText;
        set => SetProperty(ref _monthText, value);
    }

    public decimal TotalHours
    {
        get => _totalHours;
        set
        {
            if (SetProperty(ref _totalHours, value))
            {
                OnPropertyChanged(nameof(TotalHoursText));
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

    public decimal ThisWeekHours
    {
        get => _thisWeekHours;
        set => SetProperty(ref _thisWeekHours, value);
    }

    public int ThisWeekEntries
    {
        get => _thisWeekEntries;
        set => SetProperty(ref _thisWeekEntries, value);
    }

    public bool MaskEarnings
    {
        get => _maskEarnings;
        set
        {
            if (SetProperty(ref _maskEarnings, value))
            {
                OnPropertyChanged(nameof(EarningsText));
            }
        }
    }

    public string EarningsText => MaskEarnings ? "฿ *,***" : $"฿ {EstimatedEarnings:N2}";

    public string TotalHoursText => $"{TotalHours:0.##}";

    public async Task LoadAsync()
    {
        var today = DateTime.Today;
        MonthText = today.ToString("MMMM yyyy");
        var month = await _entries.GetMonthAsync(today.Year, today.Month);
        TotalHours = month.Sum(e => e.NetHours);
        EstimatedEarnings = month.Sum(e => e.EstimatedEarnings);

        var weekStart = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
        var weekEnd = weekStart.AddDays(7);
        var thisWeek = month.Where(e => e.EntryDate >= weekStart && e.EntryDate < weekEnd).ToList();
        ThisWeekHours = thisWeek.Sum(e => e.NetHours);
        ThisWeekEntries = thisWeek.Select(e => e.EntryDate.Date).Distinct().Count();

        RecentEntries.Clear();
        foreach (var entry in (await _entries.GetRecentAsync(3)).Select(e => new EntryDisplay(e)))
        {
            RecentEntries.Add(entry);
        }

        WeeklySummaries.Clear();
        for (var i = 0; i < 7; i++)
        {
            var day = weekStart.AddDays(i);
            var hours = thisWeek.Where(e => e.EntryDate.Date == day.Date).Sum(e => e.NetHours);
            WeeklySummaries.Add(new WeeklyDaySummary(day.ToString("ddd")[0].ToString(), hours, day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday));
        }
    }

    private static async Task GoLogTodayAsync()
    {
        await Shell.Current.GoToAsync("//Log");
    }

    private static async Task GoHistoryAsync()
    {
        await Shell.Current.GoToAsync("//History");
    }
}
