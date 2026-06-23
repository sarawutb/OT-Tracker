using CommunityToolkit.Mvvm.Input;
using OTTracker.Models;
using OTTracker.Services;

namespace OTTracker.ViewModels;

public sealed partial class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly IOtCalculationService _calculator;
    private readonly IAuthService _auth;
    private readonly IOtEntryRepository _entries;
    private readonly ICsvExportService _csv;
    private readonly AppEvents _events;
    private bool _maskEarnings;

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

    public SettingsViewModel(ISettingsService settingsService, IOtCalculationService calculator, IAuthService auth, IOtEntryRepository entries, ICsvExportService csv, AppEvents events)
    {
        IsBusy = true;
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
        ImportCommand = new AsyncRelayCommand(ImportAsync);
        ClearDataCommand = new AsyncRelayCommand(ClearDataAsync);
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public IAsyncRelayCommand ChangePinCommand { get; }

    public IAsyncRelayCommand ExportCommand { get; }

    public IAsyncRelayCommand ImportCommand { get; }

    public IAsyncRelayCommand ClearDataCommand { get; }

    public string HourlyRateText => $"฿{_calculator.GetHourlyRate(ToSettings()):N2} / hr";

    public string FormulaText => $"{BaseMonthlySalary:N0} / ({WorkingDaysPerMonth} x {HoursPerDay:0.##}) = {_calculator.GetHourlyRate(ToSettings()):N2}";

    public async Task LoadAsync()
    {
        var settings = await _settingsService.GetAsync();
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
        BiometricUnlockEnabled = settings.BiometricUnlockEnabled;
        _maskEarnings = settings.MaskEarnings;
        RefreshCalculated();
        IsBusy = false;
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

        if (BiometricUnlockEnabled && (!PinLockEnabled || !await _auth.HasPinAsync()))
        {
            ErrorMessage = "Set a 4-digit PIN before enabling fingerprint unlock.";
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

    private async Task ImportAsync()
    {
        var confirm = await Shell.Current.DisplayAlert("Import CSV", "Import OT records from a CSV file and add them to this device?", "Import", "Cancel");
        if (!confirm)
        {
            return;
        }

        try
        {
            var imported = await _csv.ImportAsync();
            if (imported.Count == 0)
            {
                await Shell.Current.DisplayAlert("No records imported", "No CSV records were imported.", "OK");
                return;
            }

            foreach (var entry in imported)
            {
                await _entries.SaveAsync(entry);
            }

            _events.NotifyEntriesChanged();
            await Shell.Current.DisplayAlert("Import complete", $"{imported.Count} OT record(s) imported.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Import failed", ex.Message, "OK");
        }
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
}
