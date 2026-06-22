using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using OTTracker.Models;
using OTTracker.Services;

namespace OTTracker.ViewModels;

public sealed class DashboardViewModel : BaseViewModel
{
    private readonly IOtEntryRepository _entries;
    private readonly ISettingsService _settings;
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private string _monthText = DateTime.Today.ToString("MMMM yyyy");
    private decimal _totalHours;
    private decimal _estimatedEarnings;
    private decimal _thisWeekHours;
    private int _thisWeekEntries;
    private bool _maskEarnings;
    private bool _suppressMaskSave;

    public DashboardViewModel(IOtEntryRepository entries, ISettingsService settings, AppEvents events)
    {
        _entries = entries;
        _settings = settings;
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
                ApplyRecentEntriesMask();
                if (!_suppressMaskSave)
                {
                    _ = SaveMaskEarningsAsync(value);
                }
            }
        }
    }

    public string EarningsText => MaskEarnings ? "\u0E3F *,***" : $"\u0E3F {EstimatedEarnings:N2}";

    public string TotalHoursText => $"{TotalHours:0.##}";

    public async Task LoadAsync()
    {
        await _loadGate.WaitAsync();
        try
        {
            var settings = await _settings.GetAsync();
            _suppressMaskSave = true;
            MaskEarnings = settings.MaskEarnings;
            _suppressMaskSave = false;

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
            foreach (var entry in (await _entries.GetRecentAsync(3)).Select(e => new EntryDisplay(e) { MaskEarnings = MaskEarnings }))
            {
                RecentEntries.Add(entry);
            }

            WeeklySummaries.Clear();
            for (var i = 0; i < 7; i++)
            {
                var day = weekStart.AddDays(i);
                var hours = thisWeek.Where(e => e.EntryDate.Date == day.Date).Sum(e => e.NetHours);
                string dayTH = day.DayOfWeek == DayOfWeek.Sunday
                            ? "อา."
                            : day.ToString("ddd");
                WeeklySummaries.Add(new WeeklyDaySummary(dayTH, hours, day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday));
            }
        }
        finally
        {
            _loadGate.Release();
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

    private async Task SaveMaskEarningsAsync(bool maskEarnings)
    {
        try
        {
            var settings = await _settings.GetAsync();
            settings.MaskEarnings = maskEarnings;
            await _settings.SaveAsync(settings);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to save earnings privacy: {ex.Message}";
        }
    }

    private void ApplyRecentEntriesMask()
    {
        foreach (var entry in RecentEntries)
        {
            entry.MaskEarnings = MaskEarnings;
        }
    }
}

