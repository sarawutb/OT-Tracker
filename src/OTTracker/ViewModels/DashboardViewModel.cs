using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using OTTracker.Infrastructure.Services;
using OTTracker.Services;

namespace OTTracker.ViewModels;

public sealed partial class DashboardViewModel : BaseViewModel
{
    private readonly IOtEntryRepository _entries;
    private readonly ISettingsService _settings;
    private readonly LocalSettingsService _localSettings;
    private readonly IUpdateService _updateService;
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
    private bool maskEarnings = Microsoft.Maui.Storage.Preferences.Default.Get("mask_earnings", true);
    private bool _suppressMaskSave;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private decimal regularHours;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private double regularPercentage;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private decimal weekendHours;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private double weekendPercentage;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private decimal holidayHours;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private double holidayPercentage;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private decimal averageHoursPerEntry;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private decimal maxHoursSingleEntry;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string regularRatioText = "0%";

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private decimal projectedHours;

    public DashboardViewModel(
        IOtEntryRepository entries,
        ISettingsService settings,
        LocalSettingsService localSettings,
        IUpdateService updateService,
        AppEvents events)
    {
        _entries = entries;
        _settings = settings;
        _localSettings = localSettings;
        _updateService = updateService;
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

    public ObservableCollection<WeeklyDayDisplayModel> WeeklySummaries { get; } = [];

    public ObservableCollection<MonthlyTrendSummary> MonthlyTrendSummaries { get; } = [];

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
            var deviceSettings = await _localSettings.GetAsync();
            GreetingText = GetGreeting(DateTime.Now);
            UserName = string.IsNullOrWhiteSpace(settings.UserName) ? "Username" : settings.UserName.Trim();
            _suppressMaskSave = true;
            MaskEarnings = deviceSettings.MaskEarnings;
            Microsoft.Maui.Storage.Preferences.Default.Set("mask_earnings", deviceSettings.MaskEarnings);
            _suppressMaskSave = false;
            _ = _updateService.CheckAndPromptUpdateAsync();

            var today = DateTime.Today;
            var period = OtPeriod.FromDate(today, settings.PeriodStartDay, settings.PeriodEndDay);
            var weekStart = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
            var weekEnd = weekStart.AddDays(7);

            MonthText = period.DisplayText;
            var monthTask = _entries.GetPeriodAsync(period.Start, period.End);
            var weekTask = _entries.GetPeriodAsync(weekStart, weekEnd.AddDays(-1));
            
            // Query 6 months of data for trend chart (based on custom cycle settings)
            var sixMonthsAgoStart = period.AddMonths(-5).Start;
            var trendTask = _entries.GetPeriodAsync(sixMonthsAgoStart, period.End);

            await Task.WhenAll(monthTask, weekTask, trendTask);

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

            // 1. Scaled Weekly Summaries
            WeeklySummaries.Clear();
            var weeklyDays = new List<(string DayLabel, decimal Hours, bool IsWeekend)>();
            for (var i = 0; i < 7; i++)
            {
                var day = weekStart.AddDays(i);
                var hours = thisWeek.Where(e => e.EntryDate.Date == day.Date).Sum(e => e.NetHours);
                string dayTH = day.DayOfWeek == DayOfWeek.Sunday
                            ? "อา."
                            : day.ToString("ddd");
                weeklyDays.Add((dayTH, hours, day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday));
            }
            var maxWeeklyHours = weeklyDays.Max(d => d.Hours);
            double weeklyScaleFactor = maxWeeklyHours > 0 ? 80.0 / (double)maxWeeklyHours : 0.0;
            foreach (var w in weeklyDays)
            {
                double barHeight = (double)w.Hours * weeklyScaleFactor;
                if (w.Hours > 0 && barHeight < 4) barHeight = 4; // Ensure visible
                WeeklySummaries.Add(new WeeklyDayDisplayModel(w.DayLabel, w.Hours, w.IsWeekend, barHeight));
            }

            // 2. Day Type Distribution
            var regHours = month.Where(e => e.DayType == Domain.Enums.DayType.Regular).Sum(e => e.NetHours);
            var wkHours = month.Where(e => e.DayType == Domain.Enums.DayType.Weekend).Sum(e => e.NetHours);
            var holHours = month.Where(e => e.DayType == Domain.Enums.DayType.Holiday).Sum(e => e.NetHours);

            RegularHours = regHours;
            WeekendHours = wkHours;
            HolidayHours = holHours;

            decimal totalPeriodHours = regHours + wkHours + holHours;
            if (totalPeriodHours > 0)
            {
                RegularPercentage = (double)(regHours / totalPeriodHours);
                WeekendPercentage = (double)(wkHours / totalPeriodHours);
                HolidayPercentage = (double)(holHours / totalPeriodHours);
            }
            else
            {
                RegularPercentage = 0;
                WeekendPercentage = 0;
                HolidayPercentage = 0;
            }

            // 3. 6-Month Trend Chart
            var trendEntries = await trendTask;
            MonthlyTrendSummaries.Clear();
            var monthlyGroups = new List<(string MonthName, decimal Hours, decimal Earnings, bool IsCurrentMonth)>();
            for (var i = 5; i >= 0; i--)
            {
                var targetPeriod = period.AddMonths(-i);
                var periodEntries = trendEntries.Where(e => e.EntryDate.Date >= targetPeriod.Start && e.EntryDate.Date <= targetPeriod.End).ToList();
                decimal hours = periodEntries.Sum(e => e.NetHours);
                decimal earnings = periodEntries.Sum(e => e.EstimatedEarnings);

                string monthName = targetPeriod.Start.ToString("MMM");
                bool isCurrentMonth = i == 0;
                monthlyGroups.Add((monthName, hours, earnings, isCurrentMonth));
            }

            var maxMonthlyHours = monthlyGroups.Max(m => m.Hours);
            double monthlyScaleFactor = maxMonthlyHours > 0 ? 100.0 / (double)maxMonthlyHours : 0.0;
            foreach (var m in monthlyGroups)
            {
                double barHeight = (double)m.Hours * monthlyScaleFactor;
                if (m.Hours > 0 && barHeight < 5) barHeight = 5; // Ensure visible
                MonthlyTrendSummaries.Add(new MonthlyTrendSummary(m.MonthName, m.Hours, m.Earnings, barHeight, m.IsCurrentMonth));
            }

            // 4. Key Metrics
            AverageHoursPerEntry = month.Count > 0 ? TotalHours / month.Count : 0m;
            MaxHoursSingleEntry = month.Count > 0 ? month.Max(e => e.NetHours) : 0m;
            RegularRatioText = TotalHours > 0 ? $"{regHours / TotalHours:P0}" : "0%";

            var totalDaysInPeriod = (period.End - period.Start).Days + 1;
            var elapsedDays = (today - period.Start).Days + 1;
            if (elapsedDays < 1) elapsedDays = 1;
            if (elapsedDays > totalDaysInPeriod) elapsedDays = totalDaysInPeriod;
            ProjectedHours = (TotalHours / elapsedDays) * totalDaysInPeriod;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private static async Task GoLogTodayAsync()
    {
        await Shell.Current.GoToAsync("Log");
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
            Microsoft.Maui.Storage.Preferences.Default.Set("mask_earnings", maskEarnings);
            var settings = await _localSettings.GetAsync();
            settings.MaskEarnings = maskEarnings;
            await _localSettings.SaveAsync(settings);
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

