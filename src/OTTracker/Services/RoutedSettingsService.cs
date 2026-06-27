using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using SupabaseSettingsService = OTTracker.Infrastructure.Services.SettingsService;

namespace OTTracker.Services;

public sealed class RoutedSettingsService(
    IDataSourceModeService modeService,
    LocalSettingsService localSettings,
    SupabaseSettingsService supabaseSettings) : ISettingsService
{
    private ISettingsService Current => modeService.UseSupabase ? supabaseSettings : localSettings;

    public Task<AppSettings> GetAsync() => Current.GetAsync();

    public async Task SaveAsync(AppSettings settings)
    {
        if (!modeService.UseSupabase)
        {
            await localSettings.SaveAsync(settings);
            return;
        }

        await supabaseSettings.SaveSyncedSettingsAsync(settings);
    }
}
