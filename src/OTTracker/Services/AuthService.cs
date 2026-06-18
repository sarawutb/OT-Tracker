using System.Security.Cryptography;
using System.Text;

namespace OTTracker.Services;

public sealed class AuthService : IAuthService
{
    private const string PinHashKey = "ot_tracker_pin_hash";

    public async Task<bool> HasPinAsync()
    {
        return !string.IsNullOrWhiteSpace(await SecureStorage.Default.GetAsync(PinHashKey));
    }

    public async Task SetPinAsync(string pin)
    {
        await SecureStorage.Default.SetAsync(PinHashKey, Hash(pin));
    }

    public async Task<bool> VerifyPinAsync(string pin)
    {
        var savedHash = await SecureStorage.Default.GetAsync(PinHashKey);
        return !string.IsNullOrWhiteSpace(savedHash) && savedHash == Hash(pin);
    }

    public Task ClearPinAsync()
    {
        SecureStorage.Default.Remove(PinHashKey);
        return Task.CompletedTask;
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
