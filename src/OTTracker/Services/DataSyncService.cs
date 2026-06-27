using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using OTTracker.Infrastructure.Repositories;
using OTTracker.Infrastructure.Services;
using SupabaseSettingsService = OTTracker.Infrastructure.Services.SettingsService;

namespace OTTracker.Services;

public sealed class DataSyncService(
    IDataSourceModeService modeService,
    ISupabaseClientProvider clientProvider,
    ISupabaseConfigService supabaseConfigService,
    ISettingsService settingsService,
    LocalOtEntryRepository localEntries,
    LocalSettingsService localSettings,
    OtEntryRepository supabaseEntries,
    SupabaseSettingsService supabaseSettings) : IDataSyncService
{
    public async Task<int> EnableSupabaseAsync()
    {
        EnsureSignedIn();
        var settings = await localSettings.GetAsync();
        var count = await SyncToSupabaseAsync(settings);
        await modeService.SetUseSupabaseAsync(true);
        return count;
    }

    public async Task<int> EnableSupabaseAsync(AppSettings settings)
    {
        EnsureSignedIn();
        var userId = settings.UserId;
        settings = await supabaseSettings.GetAsync();
        settings.UserId = userId;
        await settingsService.SaveAsync(settings);
        var count = await SyncToSupabaseAsync(settings);
        await modeService.SetUseSupabaseAsync(true);
        return count;
    }

    public async Task<int> DisableSupabaseAsync()
    {
        await modeService.SetUseSupabaseAsync(false);
        return 0;
    }

    public async Task<int> SyncToSupabaseAsync()
    {
        EnsureSignedIn();

        var settings = await localSettings.GetAsync();
        return await SyncToSupabaseAsync(settings);
    }

    private async Task<int> SyncToSupabaseAsync(AppSettings settings)
    {
        var data = await supabaseEntries.GetAllAsync();
        return data.Count;
    }

    private void EnsureSignedIn()
    {
        if (clientProvider?.Client?.Auth.CurrentUser is null)
        {
            throw new InvalidOperationException("Sign in to Supabase before enabling sync.");
        }
    }

    private static OtEntry CloneForSupabase(OtEntry entry) => Clone(entry, id: 0);

    private static OtEntry CloneForLocal(OtEntry entry) => Clone(entry, id: 0);

    private static OtEntry Clone(OtEntry entry, int id) => new()
    {
        Id = id,
        UserId = entry.UserId,
        EntryDate = entry.EntryDate.Date,
        DayTypeIndex = entry.DayTypeIndex,
        StartTimeString = entry.StartTimeString,
        EndTimeString = entry.EndTimeString,
        BreakMinutes = entry.BreakMinutes,
        Note = entry.Note,
        NetHours = entry.NetHours,
        HourlyRate = entry.HourlyRate,
        Multiplier = entry.Multiplier,
        EstimatedEarnings = entry.EstimatedEarnings,
        CreateDate = entry.CreateDate,
        ReviseDate = entry.ReviseDate
    };
}
