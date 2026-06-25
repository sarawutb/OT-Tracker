using System.Threading.Tasks;

namespace OTTracker_Avalonia.AppServices.Interfaces.Services;

public interface ISupabaseSessionService
{
    Task SaveSessionAsync(string accessToken, string refreshToken);
    Task<(string AccessToken, string RefreshToken)?> LoadSessionAsync();
    Task ClearSessionAsync();
}
