using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OTTracker_Avalonia.Domain.Entities;
using OTTracker_Avalonia.AppServices.Interfaces.Repositories;
using OTTracker_Avalonia.AppServices.Interfaces.Services;
using OTTracker_Avalonia.AppServices.Services;

namespace OTTracker_Avalonia.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IOtCalculationService _calculator;
    private readonly IAuthService _auth;
    private readonly IOtEntryRepository _entries;
    private readonly ICsvExportService _csv;
    private readonly AppEvents _events;
    private readonly ISupabaseConfigService _configService;

    [ObservableProperty]
    private string _supabaseUrl = string.Empty;

    [ObservableProperty]
    private string _supabaseAnonKey = string.Empty;

    private bool _maskEarnings;

    [ObservableProperty]
    private string _userName = "Username";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HourlyRateText))]
    [NotifyPropertyChangedFor(nameof(FormulaText))]
    private decimal _baseMonthlySalary = 30000m;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HourlyRateText))]
    [NotifyPropertyChangedFor(nameof(FormulaText))]
    private int _workingDaysPerMonth = 30;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HourlyRateText))]
    [NotifyPropertyChangedFor(nameof(FormulaText))]
    private decimal _hoursPerDay = 8m;

    [ObservableProperty]
    private TimeSpan _defaultStartTime = new(17, 0, 0);

    [ObservableProperty]
    private TimeSpan _defaultEndTime = new(21, 0, 0);

    [ObservableProperty]
    private int _defaultBreakMinutes = 30;

    [ObservableProperty]
    private int _periodStartDay = 16;

    [ObservableProperty]
    private int _periodEndDay = 15;

    [ObservableProperty]
    private decimal _regularMultiplier = 1.5m;

    [ObservableProperty]
    private decimal _weekendMultiplier = 2m;

    [ObservableProperty]
    private decimal _holidayMultiplier = 3m;

    [ObservableProperty]
    private bool _pinLockEnabled;

    // Overlay modal properties
    [ObservableProperty]
    private bool _isChangePinVisible;

    [ObservableProperty]
    private string _newPinInput = string.Empty;

    [ObservableProperty]
    private bool _isAlertVisible;

    [ObservableProperty]
    private string _alertTitle = string.Empty;

    [ObservableProperty]
    private string _alertMessage = string.Empty;

    [ObservableProperty]
    private bool _isConfirmVisible;

    [ObservableProperty]
    private string _confirmTitle = string.Empty;

    [ObservableProperty]
    private string _confirmMessage = string.Empty;

    private Func<Task>? _confirmCallback;

    public SettingsViewModel(
        ISettingsService settingsService,
        IOtCalculationService calculator,
        IAuthService auth,
        IOtEntryRepository entries,
        ICsvExportService csv,
        AppEvents events,
        ISupabaseConfigService configService)
    {
        _settingsService = settingsService;
        _calculator = calculator;
        _auth = auth;
        _entries = entries;
        _csv = csv;
        _events = events;
        _configService = configService;

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ChangePinCommand = new RelayCommand(ShowChangePin);
        ConfirmPinChangeCommand = new AsyncRelayCommand(ConfirmPinChangeAsync);
        CancelPinChangeCommand = new RelayCommand(CancelPinChange);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        ImportCommand = new RelayCommand(ShowImportConfirmation);
        ClearDataCommand = new RelayCommand(ShowClearConfirmation);
        
        CloseAlertCommand = new RelayCommand(CloseAlert);
        ConfirmActionCommand = new AsyncRelayCommand(ConfirmActionAsync);
        CancelActionCommand = new RelayCommand(CancelAction);

        IsBusy = true;
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public IRelayCommand ChangePinCommand { get; }
    
    public IAsyncRelayCommand ConfirmPinChangeCommand { get; }

    public IRelayCommand CancelPinChangeCommand { get; }

    public IAsyncRelayCommand ExportCommand { get; }

    public IRelayCommand ImportCommand { get; }

    public IRelayCommand ClearDataCommand { get; }

    public IRelayCommand CloseAlertCommand { get; }

    public IAsyncRelayCommand ConfirmActionCommand { get; }

    public IRelayCommand CancelActionCommand { get; }

    public string HourlyRateText => $"฿{_calculator.GetHourlyRate(ToSettings()):N2} / hr";

    public string FormulaText => $"{BaseMonthlySalary:N0} / ({WorkingDaysPerMonth} x {HoursPerDay:0.##}) = {_calculator.GetHourlyRate(ToSettings()):N2}";

    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var creds = _configService.GetCredentials();
        SupabaseUrl = creds.Url;
        SupabaseAnonKey = creds.AnonKey;

        AppSettings settings;
        try
        {
            settings = await _settingsService.GetAsync();
        }
        catch (Exception ex)
        {
            settings = new AppSettings();
            ErrorMessage = "Supabase connection failed. Please check your credentials.";
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex}");
        }

        UserName = string.IsNullOrWhiteSpace(settings.UserName) ? "Username" : settings.UserName.Trim();
        BaseMonthlySalary = settings.BaseMonthlySalary;
        WorkingDaysPerMonth = settings.WorkingDaysPerMonth;
        HoursPerDay = settings.HoursPerDay;
        DefaultStartTime = settings.DefaultStartTime;
        DefaultEndTime = settings.DefaultEndTime;
        DefaultBreakMinutes = settings.DefaultBreakMinutes;
        PeriodStartDay = settings.PeriodStartDay;
        PeriodEndDay = settings.PeriodEndDay;
        RegularMultiplier = settings.RegularMultiplier;
        WeekendMultiplier = settings.WeekendMultiplier;
        HolidayMultiplier = settings.HolidayMultiplier;
        PinLockEnabled = settings.PinLockEnabled;
        _maskEarnings = settings.MaskEarnings;
        RefreshCalculated();
        IsBusy = false;
    }

    [RelayCommand]
    private async Task SaveSupabaseConfigAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            await _configService.SaveCredentialsAsync(SupabaseUrl, SupabaseAnonKey);
            
            // Recreate client in provider
            var clientProvider = App.Services?.GetRequiredService<ISupabaseClientProvider>();
            clientProvider?.RecreateClient(SupabaseUrl, SupabaseAnonKey);

            await LoadAsync();
            ShowAlert("Connection Saved", "Supabase credentials updated and verified successfully.");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save config: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        var clientProvider = App.Services?.GetRequiredService<ISupabaseClientProvider>();
        if (clientProvider != null)
        {
            try
            {
                await clientProvider.Client.Auth.SignOut();
            }
            catch
            {
                // Ignore sign out errors
            }
        }

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            var mainViewModel = App.Services?.GetRequiredService<MainWindowViewModel>();
            var loginViewModel = App.Services?.GetRequiredService<LoginViewModel>();
            if (mainViewModel != null && loginViewModel != null)
            {
                mainViewModel.NavigateTo(loginViewModel);
            }
        });
    }

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        UserName = string.IsNullOrWhiteSpace(UserName) ? "Username" : UserName.Trim();

        if (BaseMonthlySalary <= 0 || WorkingDaysPerMonth <= 0 || HoursPerDay <= 0 ||
            RegularMultiplier <= 0 || WeekendMultiplier <= 0 || HolidayMultiplier <= 0)
        {
            ErrorMessage = "Salary, work time, and multipliers must be greater than 0.";
            return;
        }

        if (DefaultBreakMinutes < 0)
        {
            ErrorMessage = "Default break minutes must be 0 or greater.";
            return;
        }

        if (PeriodStartDay is < 1 or > 31 || PeriodEndDay is < 1 or > 31)
        {
            ErrorMessage = "OT period start and end days must be between 1 and 31.";
            return;
        }

        if (DefaultEndTime <= DefaultStartTime)
        {
            ErrorMessage = "Default end time must be later than default start time.";
            return;
        }

        if (PinLockEnabled && !await _auth.HasPinAsync())
        {
            ErrorMessage = "Set a 4-digit PIN before enabling lock.";
            return;
        }

        await _settingsService.SaveAsync(ToSettings());
        _events.NotifySettingsChanged();
        ShowAlert("Settings saved", "Your OT settings are updated.");
    }

    private void ShowChangePin()
    {
        NewPinInput = string.Empty;
        IsChangePinVisible = true;
    }

    private async Task ConfirmPinChangeAsync()
    {
        ErrorMessage = string.Empty;
        if (NewPinInput.Length != 4 || NewPinInput.Any(c => !char.IsDigit(c)))
        {
            ShowAlert("Invalid PIN", "PIN must be exactly 4 digits.");
            return;
        }

        await _auth.SetPinAsync(NewPinInput);
        PinLockEnabled = true;
        await _settingsService.SaveAsync(ToSettings());
        _events.NotifySettingsChanged();
        IsChangePinVisible = false;
        ShowAlert("PIN updated", "PIN lock is ready.");
    }

    private void CancelPinChange()
    {
        IsChangePinVisible = false;
    }

    private async Task ExportAsync()
    {
        var all = await _entries.GetAllAsync();
        if (all.Count == 0)
        {
            ShowAlert("Nothing to export", "Add OT entries before exporting.");
            return;
        }

        var success = await _csv.ExportAsync(all);
        if (success)
        {
            ShowAlert("Export complete", "Your OT records have been exported successfully.");
        }
    }

    private void ShowImportConfirmation()
    {
        _confirmCallback = ConfirmImportAsync;
        ShowConfirm("Import CSV", "Import OT records from a CSV file and add them to this device?");
    }

    private async Task ConfirmImportAsync()
    {
        try
        {
            var imported = await _csv.ImportAsync();
            if (imported is null)
            {
                return;
            }

            if (imported.Count == 0)
            {
                ShowAlert("No records imported", "No CSV records were found or imported.");
                return;
            }

            foreach (var entry in imported)
            {
                await _entries.SaveAsync(entry);
            }

            _events.NotifyEntriesChanged();
            ShowAlert("Import complete", $"{imported.Count} OT record(s) imported.");
        }
        catch (Exception ex)
        {
            ShowAlert("Import failed", ex.Message);
        }
    }

    private void ShowClearConfirmation()
    {
        _confirmCallback = ConfirmClearAsync;
        ShowConfirm("Clear all data", "Delete every OT entry from this device? This action cannot be undone.");
    }

    private async Task ConfirmClearAsync()
    {
        await _entries.ClearAsync();
        _events.NotifyEntriesChanged();
        ShowAlert("Data cleared", "All OT records have been deleted.");
    }

    private AppSettings ToSettings() => new()
    {
        UserName = string.IsNullOrWhiteSpace(UserName) ? "Username" : UserName.Trim(),
        BaseMonthlySalary = BaseMonthlySalary,
        WorkingDaysPerMonth = WorkingDaysPerMonth,
        HoursPerDay = HoursPerDay,
        DefaultStartTime = DefaultStartTime,
        DefaultEndTime = DefaultEndTime,
        DefaultBreakMinutes = DefaultBreakMinutes,
        PeriodStartDay = PeriodStartDay,
        PeriodEndDay = PeriodEndDay,
        RegularMultiplier = RegularMultiplier,
        WeekendMultiplier = WeekendMultiplier,
        HolidayMultiplier = HolidayMultiplier,
        PinLockEnabled = PinLockEnabled,
        BiometricUnlockEnabled = false,
        MaskEarnings = _maskEarnings
    };

    private void RefreshCalculated()
    {
        OnPropertyChanged(nameof(HourlyRateText));
        OnPropertyChanged(nameof(FormulaText));
    }

    private void ShowAlert(string title, string message)
    {
        AlertTitle = title;
        AlertMessage = message;
        IsAlertVisible = true;
    }

    private void CloseAlert()
    {
        IsAlertVisible = false;
    }

    private void ShowConfirm(string title, string message)
    {
        ConfirmTitle = title;
        ConfirmMessage = message;
        IsConfirmVisible = true;
    }

    private async Task ConfirmActionAsync()
    {
        IsConfirmVisible = false;
        if (_confirmCallback is not null)
        {
            await _confirmCallback();
            _confirmCallback = null;
        }
    }

    private void CancelAction()
    {
        IsConfirmVisible = false;
        _confirmCallback = null;
    }
}
