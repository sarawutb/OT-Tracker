using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using OTTracker.Infrastructure.Services;

namespace OTTracker.ViewModels;

public sealed partial class DashboardViewModel : BaseViewModel
{
    private readonly IOtEntryRepository _entries;
    private readonly ISettingsService _settings;
    private readonly SemaphoreSlim _loadGate = new(1, 1);

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string greetingText = GetGreeting(DateTime.Now);

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string monthText = OtPeriod.FromDate(DateTime.Today, 16, 15).DisplayText;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string userName = "Username";

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(TotalHoursText))]
    private decimal totalHours;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(EarningsText))]
    private decimal estimatedEarnings;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private decimal thisWeekHours;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private int thisWeekEntries;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool isRefreshing;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(EarningsText))]
    private bool maskEarnings = true;
    private bool _suppressMaskSave;

    public DashboardViewModel(IOtEntryRepository entries, ISettingsService settings, AppEvents events)
    {
        _entries = entries;
        _settings = settings;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        GoLogTodayCommand = new AsyncRelayCommand(GoLogTodayAsync);
        GoHistoryCommand = new AsyncRelayCommand(GoHistoryAsync);
        events.EntriesChanged += async (_, _) => await LoadAsync();
        events.SettingsChanged += async (_, _) => await LoadAsync();
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand RefreshCommand { get; }

    public IAsyncRelayCommand GoLogTodayCommand { get; }

    public IAsyncRelayCommand GoHistoryCommand { get; }

    public ObservableCollection<EntryDisplay> RecentEntries { get; } = [];

    public ObservableCollection<WeeklyDaySummary> WeeklySummaries { get; } = [];

    public string EarningsText => MaskEarnings ? "\u0E3F *,***" : $"\u0E3F {EstimatedEarnings:N2}";

    public string TotalHoursText => $"{TotalHours:0.##}";

    public async Task RefreshAsync()
    {
        try
        {
            await LoadAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public async Task LoadAsync()
    {
        await _loadGate.WaitAsync();
        try
        {
            var settings = await _settings.GetAsync();
            GreetingText = GetGreeting(DateTime.Now);
            UserName = string.IsNullOrWhiteSpace(settings.UserName) ? "Username" : settings.UserName.Trim();
            _suppressMaskSave = true;
            MaskEarnings = settings.MaskEarnings;
            _suppressMaskSave = false;

            var today = DateTime.Today;
            var period = OtPeriod.FromDate(today, settings.PeriodStartDay, settings.PeriodEndDay);
            var weekStart = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
            var weekEnd = weekStart.AddDays(7);

            MonthText = period.DisplayText;
            var monthTask = _entries.GetPeriodAsync(period.Start, period.End);
            var weekTask = _entries.GetPeriodAsync(weekStart, weekEnd.AddDays(-1));
            await Task.WhenAll(monthTask, weekTask);

            var month = await monthTask;
            TotalHours = month.Sum(e => e.NetHours);
            EstimatedEarnings = month.Sum(e => e.EstimatedEarnings);

            var thisWeek = (await weekTask)
                .Where(e => e.EntryDate.Date >= weekStart && e.EntryDate.Date < weekEnd)
                .ToList();
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

    private static string GetGreeting(DateTime dateTime) => GetGreeting(dateTime.TimeOfDay);

    private static string GetGreeting(TimeSpan time)
    {
        if (time >= new TimeSpan(6, 0, 0) && time < new TimeSpan(12, 0, 0))
        {
            return "Good Morning";
        }

        if (time >= new TimeSpan(12, 0, 0) && time < new TimeSpan(17, 0, 0))
        {
            return "Good Afternoon";
        }

        return "Good Evening";
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

    partial void OnMaskEarningsChanged(bool value)
    {
        ApplyRecentEntriesMask();
        if (!_suppressMaskSave)
        {
            _ = SaveMaskEarningsAsync(value);
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

