using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using OTTracker.Infrastructure.Repositories;
using SupabaseSettingsService = OTTracker.Infrastructure.Services.SettingsService;

namespace OTTracker.Services;

public sealed class DataSyncService(
    IDataSourceModeService modeService,
    ISupabaseClientProvider clientProvider,
    LocalOtEntryRepository localEntries,
    LocalSettingsService localSettings,
    OtEntryRepository supabaseEntries,
    SupabaseSettingsService supabaseSettings) : IDataSyncService
{
    public async Task<int> EnableSupabaseAsync()
    {
        EnsureSignedIn();
        var count = await SyncToSupabaseAsync();
        await modeService.SetUseSupabaseAsync(true);
        await modeService.SetPendingSupabaseSyncAsync(false);
        return count;
    }

    public async Task<int> DisableSupabaseAsync()
    {
        if (clientProvider.Client.Auth.CurrentUser is not null)
        {
            var settings = await supabaseSettings.GetAsync();
            await localSettings.SaveAsync(settings);

            var remoteEntries = await supabaseEntries.GetAllAsync();
            await localEntries.ClearAsync();
            foreach (var entry in remoteEntries)
            {
                await localEntries.SaveAsync(CloneForLocal(entry));
            }

            await modeService.SetUseSupabaseAsync(false);
            await modeService.SetPendingSupabaseSyncAsync(false);
            return remoteEntries.Count;
        }

        await modeService.SetUseSupabaseAsync(false);
        await modeService.SetPendingSupabaseSyncAsync(false);
        return 0;
    }

    public async Task<int> SyncToSupabaseAsync()
    {
        EnsureSignedIn();

        var settings = await localSettings.GetAsync();
        await supabaseSettings.SaveAsync(settings);

        var local = await localEntries.GetAllAsync();
        await supabaseEntries.ClearAsync();
        foreach (var entry in local.OrderBy(e => e.EntryDate).ThenBy(e => e.StartTime))
        {
            await supabaseEntries.SaveAsync(CloneForSupabase(entry));
        }

        return local.Count;
    }

    private void EnsureSignedIn()
    {
        if (clientProvider.Client.Auth.CurrentUser is null)
        {
            throw new InvalidOperationException("Sign in to Supabase before enabling sync.");
        }
    }

    private static OtEntry CloneForSupabase(OtEntry entry) => Clone(entry, id: 0);

    private static OtEntry CloneForLocal(OtEntry entry) => Clone(entry, id: 0);

    private static OtEntry Clone(OtEntry entry, int id) => new()
    {
        Id = id,
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
