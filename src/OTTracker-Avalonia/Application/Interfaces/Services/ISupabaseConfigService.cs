using System.Threading.Tasks;

namespace OTTracker_Avalonia.AppServices.Interfaces.Services;

public interface ISupabaseConfigService
{
    (string Url, string AnonKey) GetCredentials();
    Task SaveCredentialsAsync(string url, string anonKey);
}
