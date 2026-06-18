using OTTracker.Models;

namespace OTTracker.Services;

public interface ISettingsService
{
    Task<AppSettings> GetAsync();

    Task SaveAsync(AppSettings settings);
}
