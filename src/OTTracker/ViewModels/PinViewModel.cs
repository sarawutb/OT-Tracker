using CommunityToolkit.Mvvm.Input;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using OTTracker.Services;

namespace OTTracker.ViewModels;

public sealed partial class PinViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IBiometricService _biometricService;
    private readonly LocalSettingsService _localSettings;
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(BiometricHint))]
    private bool isBiometricVisible;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private AppSettings appSetting = new();

    public PinViewModel(
        IAuthService authService,
        IBiometricService biometricService,
        LocalSettingsService localSettings)
    {
        _authService = authService;
        _biometricService = biometricService;
        _localSettings = localSettings;
        PressCommand = new AsyncRelayCommand<string>(PressAsync);
        BackspaceCommand = new RelayCommand(Backspace);
        UnlockBiometricCommand = new AsyncRelayCommand(UnlockBiometricAsync);
    }

    public IAsyncRelayCommand<string> PressCommand { get; }

    public IRelayCommand BackspaceCommand { get; }

    public IAsyncRelayCommand UnlockBiometricCommand { get; }

    public Func<Task>? Unlocked { get; set; }

    public string BiometricHint => IsBiometricVisible ? "Use Face ID or fingerprint" : "Enter your PIN to unlock";

    public string Dot1 => EnteredPin.Length >= 1 ? "●" : "○";

    public string Dot2 => EnteredPin.Length >= 2 ? "●" : "○";

    public string Dot3 => EnteredPin.Length >= 3 ? "●" : "○";

    public string Dot4 => EnteredPin.Length >= 4 ? "●" : "○";
    public VisualElement? AnimeDot1 { get; set; }

    public VisualElement? AnimeDot2 { get; set; }

    public VisualElement? AnimeDot3 { get; set; }

    public VisualElement? AnimeDot4 { get; set; }

    private string _enteredPin = string.Empty;
    public string EnteredPin
    {
        get => _enteredPin;
        set
        {
            _enteredPin = value;
            _ = SetEnteredPinAnime();
        }
    }

    private async Task SetEnteredPinAnime()
    {
        if (EnteredPin.Length == 1)
        {
            await DotAnimatedAsync(AnimeDot1);
        }
        else if (EnteredPin.Length == 2)
        {
            await DotAnimatedAsync(AnimeDot2);
        }
        else if (EnteredPin.Length == 3)
        {
            await DotAnimatedAsync(AnimeDot3);
        }
        else if (EnteredPin.Length == 4)
        {
            await DotAnimatedAsync(AnimeDot4);
        }
    }

    private static async Task DotAnimatedAsync(VisualElement? dot)
    {
        if (dot == null) return; 
        await dot.TranslateTo(0, 3, 50);
        await dot.TranslateTo(0, 0, 100);
    }

    public async Task PressAsync(string? digit)
    {
        if (EnteredPin.Length >= 4 || string.IsNullOrWhiteSpace(digit))
        {
            return;
        }

        ErrorMessage = string.Empty;
        EnteredPin += digit;
        RefreshDots();

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
        RefreshDots();
    }

    private void Backspace()
    {
        if (EnteredPin.Length == 0)
        {
            return;
        }

        EnteredPin = EnteredPin[..^1];
        RefreshDots();
    }

    private async Task UnlockBiometricAsync()
    {
        if (!IsBiometricVisible)
        {
            await App.Current.MainPage.DisplayAlert("Fingerprint alert", "Fingerprint unlock is not available. Use your PIN.", "OK");
            return;
        }

        if (await _biometricService.AuthenticateAsync())
        {
            if (Unlocked is not null)
            {
                await Unlocked();
            }
            return;
        }
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
        AppSetting = await _localSettings.GetAsync();
        var hasPin = await _authService.HasPinAsync();
        var canOfferBiometric = AppSetting.PinLockEnabled && AppSetting.BiometricUnlockEnabled && hasPin;
        IsBiometricVisible = canOfferBiometric && await _biometricService.IsAvailableAsync();
        if (AppSetting.BiometricUnlockEnabled)
            await UnlockBiometricAsync();
    }
}

