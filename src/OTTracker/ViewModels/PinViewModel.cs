using CommunityToolkit.Mvvm.Input;
using OTTracker.Services;

namespace OTTracker.ViewModels;

public sealed class PinViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IBiometricService _biometricService;
    private readonly ISettingsService _settingsService;
    private string _enteredPin = string.Empty;
    private bool _isBiometricVisible;

    public PinViewModel(IAuthService authService, IBiometricService biometricService, ISettingsService settingsService)
    {
        _authService = authService;
        _biometricService = biometricService;
        _settingsService = settingsService;
        PressCommand = new AsyncRelayCommand<string>(PressAsync);
        BackspaceCommand = new RelayCommand(Backspace);
        UnlockBiometricCommand = new AsyncRelayCommand(UnlockBiometricAsync);
    }

    public IAsyncRelayCommand<string> PressCommand { get; }

    public IRelayCommand BackspaceCommand { get; }

    public IAsyncRelayCommand UnlockBiometricCommand { get; }

    public Func<Task>? Unlocked { get; set; }

    public bool IsBiometricVisible
    {
        get => _isBiometricVisible;
        private set
        {
            if (SetProperty(ref _isBiometricVisible, value))
            {
                OnPropertyChanged(nameof(BiometricHint));
            }
        }
    }

    public string BiometricHint => IsBiometricVisible ? "Use Face ID or fingerprint" : "Enter your PIN to unlock";

    public string Dot1 => _enteredPin.Length >= 1 ? "●" : "○";

    public string Dot2 => _enteredPin.Length >= 2 ? "●" : "○";

    public string Dot3 => _enteredPin.Length >= 3 ? "●" : "○";

    public string Dot4 => _enteredPin.Length >= 4 ? "●" : "○";

    private async Task PressAsync(string? digit)
    {
        if (_enteredPin.Length >= 4 || string.IsNullOrWhiteSpace(digit))
        {
            return;
        }

        ErrorMessage = string.Empty;
        _enteredPin += digit;
        RefreshDots();

        if (_enteredPin.Length != 4)
        {
            return;
        }

        if (await _authService.VerifyPinAsync(_enteredPin))
        {
            if (Unlocked is not null)
            {
                await Unlocked();
            }
            return;
        }

        ErrorMessage = "Incorrect PIN - try again";
        await Task.Delay(550);
        _enteredPin = string.Empty;
        RefreshDots();
    }

    private void Backspace()
    {
        if (_enteredPin.Length == 0)
        {
            return;
        }

        _enteredPin = _enteredPin[..^1];
        RefreshDots();
    }

    private async Task UnlockBiometricAsync()
    {
        ErrorMessage = string.Empty;

        var settings = await _settingsService.GetAsync();
        var hasPin = await _authService.HasPinAsync();
        var canOfferBiometric = settings.PinLockEnabled && settings.BiometricUnlockEnabled && hasPin;
        var biometricAvailable = canOfferBiometric && await _biometricService.IsAvailableAsync();

        IsBiometricVisible = biometricAvailable;
        if (canOfferBiometric && !biometricAvailable)
        {
            //ErrorMessage = _biometricService.LastError ?? "Fingerprint unlock is not available. Use your PIN.";
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
        string errorMessage = _biometricService.LastError ?? "Fingerprint unlock was cancelled. Use your PIN.";
        await App.Current.MainPage.DisplayAlert("Fingerprint alert", errorMessage, "OK");

    }

    private void RefreshDots()
    {
        OnPropertyChanged(nameof(Dot1));
        OnPropertyChanged(nameof(Dot2));
        OnPropertyChanged(nameof(Dot3));
        OnPropertyChanged(nameof(Dot4));
    }
}


