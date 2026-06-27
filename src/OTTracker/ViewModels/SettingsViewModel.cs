using CommunityToolkit.Mvvm.Input;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using OTTracker.Infrastructure.Services;
using OTTracker.Services;

namespace OTTracker.ViewModels;

public sealed partial class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly LocalSettingsService _localSettings;
    private readonly IOtCalculationService _calculator;
    private readonly IAuthService _auth;
    private readonly IOtEntryRepository _entries;
    private readonly ICsvExportService _csv;
    private readonly AppEvents _events;
    private readonly ISupabaseConfigService _configService;
    private readonly ISupabaseClientProvider _clientProvider;
    private readonly IDataSourceModeService _modeService;
    private readonly IDataSyncService _syncService;
    private bool _maskEarnings;
    private string _userId = string.Empty;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(SupabaseStatusText))]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(ShowSupabaseCredentials))]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(ShowSupabaseAction))]
    private bool useSupabase;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(SupabaseActionText))]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(SupabaseActionColor))]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(SupabaseStatusText))]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(ShowSupabaseSwitch))]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(ShowSupabaseCredentials))]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(ShowSupabaseAction))]
    private bool isSupabaseConnected;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string supabaseUrl = string.Empty;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string supabaseAnonKey = string.Empty;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string userName = "Username";

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(HourlyRateText))]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(FormulaText))]
    private decimal baseMonthlySalary = 10000m;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(HourlyRateText))]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(FormulaText))]
    private int workingDaysPerMonth = 30;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(HourlyRateText))]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(FormulaText))]
    private decimal hoursPerDay = 8m;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private TimeSpan defaultStartTime = new(17, 0, 0);

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private TimeSpan defaultEndTime = new(21, 0, 0);

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private int defaultBreakMinutes = 30;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private int periodStartDay = 16;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private int periodEndDay = 15;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private decimal regularMultiplier = 1.5m;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private decimal weekendMultiplier = 2m;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private decimal holidayMultiplier = 3m;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool pinLockEnabled;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool biometricUnlockEnabled;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private AppSettings appSetting = new();

    public SettingsViewModel(
        ISettingsService settingsService,
        LocalSettingsService localSettings,
        IOtCalculationService calculator,
        IAuthService auth,
        IOtEntryRepository entries,
        ICsvExportService csv,
        AppEvents events,
        ISupabaseConfigService configService,
        ISupabaseClientProvider clientProvider,
        IDataSourceModeService modeService,
        IDataSyncService syncService)
    {
        IsBusy = true;
        _settingsService = settingsService;
        _localSettings = localSettings;
        _calculator = calculator;
        _auth = auth;
        _entries = entries;
        _csv = csv;
        _events = events;
        _configService = configService;
        _clientProvider = clientProvider;
        _modeService = modeService;
        _syncService = syncService;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        SaveSupabaseConfigCommand = new AsyncRelayCommand(SaveSupabaseConfigAsync);
        ApplyDataSourceCommand = new AsyncRelayCommand(ApplyDataSourceAsync);
        ChangePinCommand = new AsyncRelayCommand(ChangePinAsync);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        ImportCommand = new AsyncRelayCommand(ImportAsync);
        ClearDataCommand = new AsyncRelayCommand(ClearDataAsync);
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public IAsyncRelayCommand SaveSupabaseConfigCommand { get; }

    public IAsyncRelayCommand ApplyDataSourceCommand { get; }

    public IAsyncRelayCommand ChangePinCommand { get; }

    public IAsyncRelayCommand ExportCommand { get; }

    public IAsyncRelayCommand ImportCommand { get; }

    public IAsyncRelayCommand ClearDataCommand { get; }

    public string HourlyRateText => $"฿{_calculator.GetHourlyRate(ToSettings()):N2} / hr";

    public string FormulaText => $"{BaseMonthlySalary:N0} / ({WorkingDaysPerMonth} x {HoursPerDay:0.##}) = {_calculator.GetHourlyRate(ToSettings()):N2}";

    public string SupabaseActionText => IsSupabaseConnected ? "Disconnect" : "Connect";

    public Color SupabaseActionColor => IsSupabaseConnected ? GetColor("Red") : GetColor("GreenMid");

    public string SupabaseStatusText => IsSupabaseConnected
        ? "Connected to Supabase"
        : UseSupabase
            ? "Enter your Supabase connection details"
            : "Using local SQLite data";

    public bool ShowSupabaseSwitch => !IsSupabaseConnected;

    public bool ShowSupabaseCredentials => !IsSupabaseConnected && UseSupabase;

    public bool ShowSupabaseAction => IsSupabaseConnected || UseSupabase;

    private static Color GetColor(string resourceKey)
    {
        return Application.Current?.Resources.TryGetValue(resourceKey, out var value) == true && value is Color color
            ? color
            : Colors.Transparent;
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var credentials = _configService.GetCredentials();
        IsSupabaseConnected = _modeService.UseSupabase;
        UseSupabase = IsSupabaseConnected;
        SupabaseUrl = credentials.Url;
        SupabaseAnonKey = credentials.AnonKey;

        try
        {
            AppSetting = await _settingsService.GetAsync();
        }
        catch (Exception ex)
        {
            AppSetting = new AppSettings();
            ErrorMessage = $"Supabase connection failed: {ex.Message}";
        }

        var deviceSettings = await _localSettings.GetAsync();

        UserName = string.IsNullOrWhiteSpace(AppSetting.UserName) ? "Username" : AppSetting.UserName.Trim();
        _userId = AppSetting.UserId;
        BaseMonthlySalary = AppSetting.BaseMonthlySalary;
        WorkingDaysPerMonth = AppSetting.WorkingDaysPerMonth;
        HoursPerDay = AppSetting.HoursPerDay;
        DefaultStartTime = AppSetting.DefaultStartTime;
        DefaultEndTime = AppSetting.DefaultEndTime;
        DefaultBreakMinutes = AppSetting.DefaultBreakMinutes;
        PeriodStartDay = AppSetting.PeriodStartDay;
        PeriodEndDay = AppSetting.PeriodEndDay;
        RegularMultiplier = AppSetting.RegularMultiplier;
        WeekendMultiplier = AppSetting.WeekendMultiplier;
        HolidayMultiplier = AppSetting.HolidayMultiplier;
        PinLockEnabled = deviceSettings.PinLockEnabled;
        BiometricUnlockEnabled = deviceSettings.BiometricUnlockEnabled;
        _maskEarnings = AppSetting.MaskEarnings;
        RefreshCalculated();
        IsBusy = false;
    }

    private async Task SaveSupabaseConfigAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(SupabaseUrl) || string.IsNullOrWhiteSpace(SupabaseAnonKey))
        {
            ErrorMessage = "Supabase URL and anon key are required.";
            return;
        }

        IsBusy = true;
        try
        {
            await _configService.SaveCredentialsAsync(SupabaseUrl.Trim(), SupabaseAnonKey.Trim());
            _clientProvider.RecreateClient(SupabaseUrl.Trim(), SupabaseAnonKey.Trim());
            await CurrentPage?.DisplayAlert("Connection saved", "Supabase connection settings are updated.", "OK");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save Supabase connection: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ApplyDataSourceAsync()
    {
        ErrorMessage = string.Empty;

        try
        {
            var settings = ToSettings();
            if (!_modeService.UseSupabase)
            {
                if (string.IsNullOrWhiteSpace(SupabaseUrl) || string.IsNullOrWhiteSpace(SupabaseAnonKey))
                {
                    ErrorMessage = "Supabase URL and anon key are required.";
                    return;
                }

                _clientProvider.RecreateClient(SupabaseUrl.Trim(), SupabaseAnonKey.Trim());

                var email = await CurrentPage?.DisplayPromptAsync(
                    "Activate Supabase",
                    "Enter your Supabase email",
                    "Continue",
                    "Cancel",
                    "email@example.com",
                    keyboard: Keyboard.Email);

                if (string.IsNullOrWhiteSpace(email))
                {
                    return;
                }

                var password = await CurrentPage?.DisplayPromptAsync(
                    "Activate Supabase",
                    "Enter your Supabase password",
                    "Activate",
                    "Cancel",
                    keyboard: Keyboard.Password);

                if (string.IsNullOrWhiteSpace(password))
                {
                    return;
                }

                await _clientProvider.Client.Auth.SignIn(email.Trim(), password);
                var userId = _clientProvider.Client.Auth.CurrentUser?.Id;
                if (!Guid.TryParse(userId, out var parsedUserId))
                {
                    throw new InvalidOperationException("Supabase sign-in did not return a valid user id.");
                }

                _userId = parsedUserId.ToString();
                settings.UserId = _userId;
                var loaded = await _syncService.EnableSupabaseAsync(settings);
                await _configService.SaveCredentialsAsync(SupabaseUrl.Trim(), SupabaseAnonKey.Trim());
                await LoadAsync();
                _events.NotifySettingsChanged();
                _events.NotifyEntriesChanged();
                await CurrentPage?.DisplayAlert("Supabase enabled", $"{loaded} OT record(s) loaded.", "OK");
                return;
            }

            var confirmDisable = await CurrentPage?.DisplayAlert(
                "Disconnect Supabase?",
                "Supabase data will be copied to local SQLite before switching.",
                "Disconnect",
                "Cancel");
            if (!confirmDisable)
            {
                return;
            }

            var copied = await _syncService.DisableSupabaseAsync();
            try
            {
                await _clientProvider.Client.Auth.SignOut();
            }
            catch
            {
                // SQLite is already active and contains the latest Supabase snapshot.
            }

            SupabaseUrl = string.Empty;
            SupabaseAnonKey = string.Empty;
            await _configService.SaveCredentialsAsync(string.Empty, string.Empty);
            await LoadAsync();
            _events.NotifySettingsChanged();
            _events.NotifyEntriesChanged();
            await CurrentPage?.DisplayAlert("SQLite enabled", $"{copied} Supabase OT record(s) copied to SQLite.", "OK");
        }
        catch (Exception ex)
        {
            IsSupabaseConnected = _modeService.UseSupabase;
            UseSupabase = IsSupabaseConnected || UseSupabase;
            ErrorMessage = ex.Message;
            await CurrentPage?.DisplayAlert("Error Message", ErrorMessage, "OK");
        }
    }

    private async Task SaveAsync()
    {
        try
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

            if (BiometricUnlockEnabled && (!PinLockEnabled || !await _auth.HasPinAsync()))
            {
                ErrorMessage = "Set a 4-digit PIN before enabling fingerprint unlock.";
                return;
            }

            await _settingsService.SaveAsync(ToSettings());
            await SaveDeviceSecurityAsync();
            _events.NotifySettingsChanged();
            await CurrentPage?.DisplayAlert("Settings saved", "Your OT settings are updated.", "OK");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await CurrentPage?.DisplayAlert("Error Message", ErrorMessage, "OK");
        }
    }

    private async Task ChangePinAsync()
    {
        var pin = await CurrentPage?.DisplayPromptAsync("Change PIN", "Enter a new 4-digit PIN", "Save", "Cancel", "1234", 4, Keyboard.Numeric);
        if (pin is null)
        {
            if (await _auth.IsPinLockEnabledAsync() == false)
                PinLockEnabled = false;
            return;
        }

        if (pin.Length != 4 || pin.Any(c => !char.IsDigit(c)))
        {
            await CurrentPage?.DisplayAlert("Invalid PIN", "PIN must be exactly 4 digits.", "OK");
            return;
        }

        await _auth.SetPinAsync(pin);
        PinLockEnabled = true;
        await SaveDeviceSecurityAsync();
        _events.NotifySettingsChanged();
        await CurrentPage?.DisplayAlert("PIN updated", "PIN lock is ready.", "OK");
    }

    private async Task ExportAsync()
    {
        var all = await _entries.GetAllAsync();
        if (all.Count == 0)
        {
            await CurrentPage?.DisplayAlert("Nothing to export", "Add OT entries before exporting.", "OK");
            return;
        }

        await _csv.ExportAsync(all);
    }

    private async Task ImportAsync()
    {
        var confirm = await CurrentPage?.DisplayAlert("Import CSV", "Import OT records from a CSV file and add them to this device?", "Import", "Cancel");
        if (!confirm)
        {
            return;
        }

        try
        {
            var imported = await _csv.ImportAsync();
            if (imported is null)
            {
                return;
            }

            if (imported.Count == 0)
            {
                await CurrentPage?.DisplayAlert("No records imported", "No CSV records were imported.", "OK");
                return;
            }

            foreach (var entry in imported)
            {
                await _entries.SaveAsync(entry);
            }

            _events.NotifyEntriesChanged();
            await CurrentPage?.DisplayAlert("Import complete", $"{imported.Count} OT record(s) imported.", "OK");
        }
        catch (Exception ex)
        {
            await CurrentPage?.DisplayAlert("Import failed", ex.Message, "OK");
        }
    }

    private async Task ClearDataAsync()
    {
        var confirm = await CurrentPage?.DisplayAlert("Clear all data", "Delete every OT entry from this device?", "Clear", "Cancel");
        if (!confirm)
        {
            return;
        }

        await _entries.ClearAsync();
        _events.NotifyEntriesChanged();
    }

    private AppSettings ToSettings() => new()
    {
        UserId = _userId,
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
        BiometricUnlockEnabled = BiometricUnlockEnabled,
        MaskEarnings = _maskEarnings
    };

    private async Task SaveDeviceSecurityAsync()
    {
        var settings = await _localSettings.GetAsync();
        settings.PinLockEnabled = PinLockEnabled;
        settings.BiometricUnlockEnabled = BiometricUnlockEnabled;
        await _auth.SetPinLockEnabledAsync(PinLockEnabled);
        await _localSettings.SaveAsync(settings);
    }

    private void RefreshCalculated()
    {
        OnPropertyChanged(nameof(HourlyRateText));
        OnPropertyChanged(nameof(FormulaText));
    }

    partial void OnPinLockEnabledChanged(bool value)
    {
        if (!value)
        {
            BiometricUnlockEnabled = false;
        }
    }

    public async Task CheckPinLock()
    {
        bool _isPinLockEnabled = await _auth.IsPinLockEnabledAsync();
        if (PinLockEnabled && !_isPinLockEnabled)
            await ChangePinAsync();
        else
        {
            if (_isPinLockEnabled && !PinLockEnabled)
            {
                await _auth.ClearPinAsync();
                await SaveDeviceSecurityAsync();
                await CurrentPage?.DisplayAlert("PIN updated", "PIN lock is closed.", "OK");
            }
        }
    }
}
