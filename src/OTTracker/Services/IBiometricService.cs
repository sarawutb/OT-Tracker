namespace OTTracker.Services;

public interface IBiometricService
{
    string? LastError { get; }

    Task<bool> IsAvailableAsync();

    Task<bool> AuthenticateAsync();
}
