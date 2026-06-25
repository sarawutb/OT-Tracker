using System.Threading.Tasks;

namespace OTTracker.Domain.Interfaces;

public interface ISupabaseSessionService
{
    Task SaveSessionAsync(string accessToken, string refreshToken);
    Task<(string AccessToken, string RefreshToken)?> LoadSessionAsync();
    Task ClearSessionAsync();
}
