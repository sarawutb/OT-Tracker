using System.Threading.Tasks;

namespace OTTracker_Avalonia.AppServices.Interfaces.Services;

public interface IAuthService
{
    Task<bool> HasPinAsync();

    Task SetPinAsync(string pin);

    Task<bool> VerifyPinAsync(string pin);

    Task ClearPinAsync();
}
