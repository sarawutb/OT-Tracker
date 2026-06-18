namespace OTTracker.Services;

public interface IBiometricService
{
    Task<bool> IsAvailableAsync();

    Task<bool> AuthenticateAsync();
}
