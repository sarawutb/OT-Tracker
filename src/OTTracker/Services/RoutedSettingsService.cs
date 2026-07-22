using System.Threading.Tasks;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using SupabaseSettingsService = OTTracker.Infrastructure.Services.SettingsService;

namespace OTTracker.Services;

public sealed class RoutedSettingsService(
    IDataSourceModeService modeService,
    LocalSettingsService localSettings,
    SupabaseSettingsService supabaseSettings) : ISettingsService
{
    public async Task<AppSettings> GetAsync()
    {
        if (modeService.UseSupabase)
        {
            try
            {
                return await supabaseSettings.GetAsync();
            }
            catch
            {
                return await localSettings.GetAsync();
            }
        }

        return await localSettings.GetAsync();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        // Always persist to local SQLite storage as a reliable fallback cache
        await localSettings.SaveAsync(settings);

        if (modeService.UseSupabase)
        {
            try
            {
                await supabaseSettings.SaveSyncedSettingsAsync(settings);
            }
            catch
            {
                // Local save succeeded even if Supabase sync is temporarily offline
            }
        }
    }
}
