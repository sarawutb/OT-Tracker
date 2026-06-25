using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OTTracker_Avalonia.Domain.Entities;
using OTTracker_Avalonia.Domain.Enums;
using OTTracker_Avalonia.AppServices.Interfaces.Repositories;
using OTTracker_Avalonia.AppServices.Interfaces.Services;
using OTTracker_Avalonia.AppServices.Services;
using OTTracker_Avalonia.AppServices.ViewModels;

namespace OTTracker_Avalonia.ViewModels;

public sealed partial class DashboardViewModel : ViewModelBase
{
    private readonly IOtEntryRepository _entries;
    private readonly ISettingsService _settings;
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IServiceProvider _services;
    private readonly SemaphoreSlim _loadGate = new(1, 1);

    [ObservableProperty]
    private string _greetingText = GetGreeting(DateTime.Now);

    [ObservableProperty]
    private string _monthText = OtPeriod.FromDate(DateTime.Today, 16, 15).DisplayText;

    [ObservableProperty]
    private string _userName = "Username";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalHoursText))]
    private decimal _totalHours;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EarningsText))]
    private decimal _estimatedEarnings;

    [ObservableProperty]
    private decimal _thisWeekHours;

    [ObservableProperty]
    private int _thisWeekEntries;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EarningsText))]
    private bool _maskEarnings = true;
    private bool _suppressMaskSave;

    public DashboardViewModel(
        IOtEntryRepository entries,
        ISettingsService settings,
        AppEvents events,
        MainWindowViewModel mainWindowViewModel,
        IServiceProvider services)
    {
        _entries = entries;
        _settings = settings;
        _mainWindowViewModel = mainWindowViewModel;
        _services = services;

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

    public string EarningsText => MaskEarnings ? "฿ *,***" : $"฿ {EstimatedEarnings:N2}";

    public string TotalHoursText => $"{TotalHours:0.##}";

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
            MonthText = period.DisplayText;
            var month = await _entries.GetPeriodAsync(period.Start, period.End);
            
            TotalHours = month.Sum(e => e.NetHours);
            EstimatedEarnings = month.Sum(e => e.EstimatedEarnings);

            var weekStart = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
            var weekEnd = weekStart.AddDays(7);
            var thisWeek = month.Where(e => e.EntryDate >= weekStart && e.EntryDate < weekEnd).ToList();
            
            ThisWeekHours = thisWeek.Sum(e => e.NetHours);
            ThisWeekEntries = thisWeek.Select(e => e.EntryDate.Date).Distinct().Count();

            RecentEntries.Clear();
            var recent = await _entries.GetRecentAsync(5);
            foreach (var entry in recent.Select(e => new EntryDisplay(e) { MaskEarnings = MaskEarnings }))
            {
                RecentEntries.Add(entry);
            }

            WeeklySummaries.Clear();
            for (var i = 0; i < 7; i++)
            {
                var day = weekStart.AddDays(i);
                var hours = thisWeek.Where(e => e.EntryDate.Date == day.Date).Sum(e => e.NetHours);
                string dayLabel = day.DayOfWeek == DayOfWeek.Sunday
                            ? "Sun"
                            : day.ToString("ddd");
                WeeklySummaries.Add(new WeeklyDaySummary(dayLabel, hours, day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday));
            }
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private Task GoLogTodayAsync()
    {
        var logViewModel = _services.GetRequiredService<LogEntryViewModel>();
        _mainWindowViewModel.NavigateTo(logViewModel);
        return Task.CompletedTask;
    }

    private Task GoHistoryAsync()
    {
        var historyViewModel = _services.GetRequiredService<HistoryViewModel>();
        _mainWindowViewModel.NavigateTo(historyViewModel);
        return Task.CompletedTask;
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
