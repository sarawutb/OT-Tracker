namespace OTTracker.Services;

public sealed class BiometricService : IBiometricService
{
    public Task<bool> IsAvailableAsync()
    {
        var platform = DeviceInfo.Current.Platform;
        return Task.FromResult(platform == DevicePlatform.iOS || platform == DevicePlatform.Android);
    }

    public async Task<bool> AuthenticateAsync()
    {
        await Task.Delay(250);
        return await IsAvailableAsync();
    }
}
