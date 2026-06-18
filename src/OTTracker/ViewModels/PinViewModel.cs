using CommunityToolkit.Mvvm.Input;
using OTTracker.Services;

namespace OTTracker.ViewModels;

public sealed class PinViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IBiometricService _biometricService;
    private string _enteredPin = string.Empty;

    public PinViewModel(IAuthService authService, IBiometricService biometricService)
    {
        _authService = authService;
        _biometricService = biometricService;
        PressCommand = new AsyncRelayCommand<string>(PressAsync);
        BackspaceCommand = new RelayCommand(Backspace);
        UnlockBiometricCommand = new AsyncRelayCommand(UnlockBiometricAsync);
    }

    public IAsyncRelayCommand<string> PressCommand { get; }

    public IRelayCommand BackspaceCommand { get; }

    public IAsyncRelayCommand UnlockBiometricCommand { get; }

    public Func<Task>? Unlocked { get; set; }

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
        if (await _biometricService.AuthenticateAsync() && Unlocked is not null)
        {
            await Unlocked();
        }
    }

    private void RefreshDots()
    {
        OnPropertyChanged(nameof(Dot1));
        OnPropertyChanged(nameof(Dot2));
        OnPropertyChanged(nameof(Dot3));
        OnPropertyChanged(nameof(Dot4));
    }
}
