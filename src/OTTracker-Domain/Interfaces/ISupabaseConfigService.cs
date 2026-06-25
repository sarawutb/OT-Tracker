using System.Threading.Tasks;

namespace OTTracker.Domain.Interfaces;

public interface ISupabaseConfigService
{
    (string Url, string AnonKey) GetCredentials();
    Task SaveCredentialsAsync(string url, string anonKey);
}
