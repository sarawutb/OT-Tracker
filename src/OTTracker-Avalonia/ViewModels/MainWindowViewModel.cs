using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace OTTracker_Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private bool _isSidebarVisible;

    [ObservableProperty]
    private int _selectedMenuIndex = 0;

    public string? TitleDisplay => $"OT Tracker - v{AppVersion}";

    public MainWindowViewModel(IServiceProvider services)
    {
        _services = services;
        
        GoToDashboardCommand = new RelayCommand(GoToDashboard);
        GoToLogEntryCommand = new RelayCommand(GoToLogEntry);
        GoToHistoryCommand = new RelayCommand(GoToHistory);
        GoToSettingsCommand = new RelayCommand(GoToSettings);
    }

    public IRelayCommand GoToDashboardCommand { get; }
    public IRelayCommand GoToLogEntryCommand { get; }
    public IRelayCommand GoToHistoryCommand { get; }
    public IRelayCommand GoToSettingsCommand { get; }

    public void NavigateTo(ViewModelBase viewModel)
    {
        CurrentView = viewModel;
        IsSidebarVisible = viewModel is not PinViewModel && viewModel is not LoginViewModel;

        // Synchronize selected sidebar menu index
        SelectedMenuIndex = viewModel switch
        {
            DashboardViewModel => 0,
            LogEntryViewModel => 1,
            HistoryViewModel => 2,
            SettingsViewModel => 3,
            _ => SelectedMenuIndex
        };

        // Proactively call LoadAsync if the Viewmodel supports it
        if (viewModel is DashboardViewModel db) _ = db.LoadAsync();
        else if (viewModel is HistoryViewModel hist) _ = hist.LoadAsync();
        else if (viewModel is SettingsViewModel sett) _ = sett.LoadAsync();
        else if (viewModel is PinViewModel pin) _ = pin.LoadAsync();
    }

    private void GoToDashboard()
    {
        var vm = _services.GetRequiredService<DashboardViewModel>();
        NavigateTo(vm);
    }

    private void GoToLogEntry()
    {
        var vm = _services.GetRequiredService<LogEntryViewModel>();
        _ = vm.InitializeWithEntryIdAsync(0); // Create mode
        NavigateTo(vm);
    }

    private void GoToHistory()
    {
        var vm = _services.GetRequiredService<HistoryViewModel>();
        NavigateTo(vm);
    }

    private void GoToSettings()
    {
        var vm = _services.GetRequiredService<SettingsViewModel>();
        NavigateTo(vm);
    }
}
