using CommunityToolkit.Mvvm.Input;
using OTTracker.Models;
using OTTracker.Services;

namespace OTTracker.ViewModels;

public sealed class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly IOtCalculationService _calculator;
    private readonly IAuthService _auth;
    private readonly IOtEntryRepository _entries;
    private readonly ICsvExportService _csv;
    private readonly AppEvents _events;
    private decimal _baseMonthlySalary = 30000m;
    private int _workingDaysPerMonth = 30;
    private decimal _hoursPerDay = 8m;
    private TimeSpan _defaultStartTime = new(17, 0, 0);
    private TimeSpan _defaultEndTime = new(21, 0, 0);
    private int _defaultBreakMinutes = 30;
    private decimal _regularMultiplier = 1.5m;
    private decimal _weekendMultiplier = 2m;
    private decimal _holidayMultiplier = 3m;
    private bool _pinLockEnabled;
    private bool _biometricUnlockEnabled;

    public SettingsViewModel(ISettingsService settingsService, IOtCalculationService calculator, IAuthService auth, IOtEntryRepository entries, ICsvExportService csv, AppEvents events)
    {
        _settingsService = settingsService;
        _calculator = calculator;
        _auth = auth;
        _entries = entries;
        _csv = csv;
        _events = events;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ChangePinCommand = new AsyncRelayCommand(ChangePinAsync);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        ClearDataCommand = new AsyncRelayCommand(ClearDataAsync);
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public IAsyncRelayCommand ChangePinCommand { get; }

    public IAsyncRelayCommand ExportCommand { get; }

    public IAsyncRelayCommand ClearDataCommand { get; }

    public decimal BaseMonthlySalary
    {
        get => _baseMonthlySalary;
        set
        {
            if (SetProperty(ref _baseMonthlySalary, value))
            {
                RefreshCalculated();
            }
        }
    }

    public int WorkingDaysPerMonth
    {
        get => _workingDaysPerMonth;
        set
        {
            if (SetProperty(ref _workingDaysPerMonth, value))
            {
                RefreshCalculated();
            }
        }
    }

    public decimal HoursPerDay
    {
        get => _hoursPerDay;
        set
        {
            if (SetProperty(ref _hoursPerDay, value))
            {
                RefreshCalculated();
            }
        }
    }

    public TimeSpan DefaultStartTime
    {
        get => _defaultStartTime;
        set => SetProperty(ref _defaultStartTime, value);
    }

    public TimeSpan DefaultEndTime
    {
        get => _defaultEndTime;
        set => SetProperty(ref _defaultEndTime, value);
    }

    public int DefaultBreakMinutes
    {
        get => _defaultBreakMinutes;
        set => SetProperty(ref _defaultBreakMinutes, value);
    }

    public decimal RegularMultiplier
    {
        get => _regularMultiplier;
        set => SetProperty(ref _regularMultiplier, value);
    }

    public decimal WeekendMultiplier
    {
        get => _weekendMultiplier;
        set => SetProperty(ref _weekendMultiplier, value);
    }

    public decimal HolidayMultiplier
    {
        get => _holidayMultiplier;
        set => SetProperty(ref _holidayMultiplier, value);
    }

    public bool PinLockEnabled
    {
        get => _pinLockEnabled;
        set => SetProperty(ref _pinLockEnabled, value);
    }

    public bool BiometricUnlockEnabled
    {
        get => _biometricUnlockEnabled;
        set => SetProperty(ref _biometricUnlockEnabled, value);
    }

    public string HourlyRateText => $"฿{_calculator.GetHourlyRate(ToSettings()):N2} / hr";

    public string FormulaText => $"{BaseMonthlySalary:N0} / ({WorkingDaysPerMonth} x {HoursPerDay:0.##}) = {_calculator.GetHourlyRate(ToSettings()):N2}";

    public async Task LoadAsync()
    {
        var settings = await _settingsService.GetAsync();
        BaseMonthlySalary = settings.BaseMonthlySalary;
        WorkingDaysPerMonth = settings.WorkingDaysPerMonth;
        HoursPerDay = settings.HoursPerDay;
        DefaultStartTime = settings.DefaultStartTime;
        DefaultEndTime = settings.DefaultEndTime;
        DefaultBreakMinutes = settings.DefaultBreakMinutes;
        RegularMultiplier = settings.RegularMultiplier;
        WeekendMultiplier = settings.WeekendMultiplier;
        HolidayMultiplier = settings.HolidayMultiplier;
        PinLockEnabled = settings.PinLockEnabled;
        BiometricUnlockEnabled = settings.BiometricUnlockEnabled;
        RefreshCalculated();
    }

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
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

        if (DefaultEndTime <= DefaultStartTime)
        {
            ErrorMessage = "Default end time must be later than default start time.";
            return;
        }

        await _settingsService.SaveAsync(ToSettings());
        _events.NotifySettingsChanged();
        await Shell.Current.DisplayAlert("Settings saved", "Your OT settings are updated.", "OK");
    }

    private async Task ChangePinAsync()
    {
        var pin = await Shell.Current.DisplayPromptAsync("Change PIN", "Enter a new 4-digit PIN", "Save", "Cancel", "1234", 4, Keyboard.Numeric);
        if (pin is null)
        {
            return;
        }

        if (pin.Length != 4 || pin.Any(c => !char.IsDigit(c)))
        {
            await Shell.Current.DisplayAlert("Invalid PIN", "PIN must be exactly 4 digits.", "OK");
            return;
        }

        await _auth.SetPinAsync(pin);
        PinLockEnabled = true;
        await _settingsService.SaveAsync(ToSettings());
        _events.NotifySettingsChanged();
        await Shell.Current.DisplayAlert("PIN updated", "PIN lock is ready.", "OK");
    }

    private async Task ExportAsync()
    {
        var all = await _entries.GetAllAsync();
        if (all.Count == 0)
        {
            await Shell.Current.DisplayAlert("Nothing to export", "Add OT entries before exporting.", "OK");
            return;
        }

        await _csv.ExportAsync(all);
    }

    private async Task ClearDataAsync()
    {
        var confirm = await Shell.Current.DisplayAlert("Clear all data", "Delete every OT entry from this device?", "Clear", "Cancel");
        if (!confirm)
        {
            return;
        }

        await _entries.ClearAsync();
        _events.NotifyEntriesChanged();
    }

    private AppSettings ToSettings() => new()
    {
        BaseMonthlySalary = BaseMonthlySalary,
        WorkingDaysPerMonth = WorkingDaysPerMonth,
        HoursPerDay = HoursPerDay,
        DefaultStartTime = DefaultStartTime,
        DefaultEndTime = DefaultEndTime,
        DefaultBreakMinutes = DefaultBreakMinutes,
        RegularMultiplier = RegularMultiplier,
        WeekendMultiplier = WeekendMultiplier,
        HolidayMultiplier = HolidayMultiplier,
        PinLockEnabled = PinLockEnabled,
        BiometricUnlockEnabled = BiometricUnlockEnabled
    };

    private void RefreshCalculated()
    {
        OnPropertyChanged(nameof(HourlyRateText));
        OnPropertyChanged(nameof(FormulaText));
    }
}
