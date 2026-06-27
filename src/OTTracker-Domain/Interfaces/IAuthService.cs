using System.Threading.Tasks;

namespace OTTracker.Domain.Interfaces;

public interface IAuthService
{
    Task<bool> HasPinAsync();

    Task SetPinAsync(string pin);

    Task<bool> VerifyPinAsync(string pin);

    Task ClearPinAsync();

    Task<bool> IsPinLockEnabledAsync();

    Task SetPinLockEnabledAsync(bool enabled);
}
