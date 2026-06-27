using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;

namespace OTTracker_Avalonia.ViewModels;

public sealed partial class PinViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private AppSettings _appSetting = new();

    [ObservableProperty]
    private string _versionDisplay = string.Empty;

    private string _enteredPin = string.Empty;
    public string EnteredPin
    {
        get => _enteredPin;
        set
        {
            if (SetProperty(ref _enteredPin, value))
            {
                RefreshDots();
            }
        }
    }

    public PinViewModel(IAuthService authService, ISettingsService settingsService)
    {
        _authService = authService;
        _settingsService = settingsService;
        PressCommand = new AsyncRelayCommand<string>(PressAsync);
        BackspaceCommand = new RelayCommand(Backspace);
        VersionDisplay = $"v{AppVersion}";
    }

    public IAsyncRelayCommand<string> PressCommand { get; }

    public IRelayCommand BackspaceCommand { get; }

    public Func<Task>? Unlocked { get; set; }

    public string BiometricHint => "Enter your PIN to unlock";

    public string Dot1 => EnteredPin.Length >= 1 ? "●" : "○";

    public string Dot2 => EnteredPin.Length >= 2 ? "●" : "○";

    public string Dot3 => EnteredPin.Length >= 3 ? "●" : "○";

    public string Dot4 => EnteredPin.Length >= 4 ? "●" : "○";

    public async Task PressAsync(string? digit)
    {
        if (EnteredPin.Length >= 4 || string.IsNullOrWhiteSpace(digit))
        {
            return;
        }

        ErrorMessage = string.Empty;
        EnteredPin += digit;

        if (EnteredPin.Length != 4)
        {
            return;
        }

        if (await _authService.VerifyPinAsync(EnteredPin))
        {
            if (Unlocked is not null)
            {
                await Unlocked();
            }
            return;
        }

        ErrorMessage = "Incorrect PIN - try again";
        await Task.Delay(550);
        EnteredPin = string.Empty;
    }

    private void Backspace()
    {
        if (EnteredPin.Length == 0)
        {
            return;
        }

        EnteredPin = EnteredPin[..^1];
    }

    private void RefreshDots()
    {
        OnPropertyChanged(nameof(Dot1));
        OnPropertyChanged(nameof(Dot2));
        OnPropertyChanged(nameof(Dot3));
        OnPropertyChanged(nameof(Dot4));
    }

    public async Task LoadAsync()
    {
        AppSetting = await _settingsService.GetAsync();
        EnteredPin = string.Empty;
        ErrorMessage = string.Empty;
    }
}
