using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using OTTracker.Infrastructure.Repositories;
using OTTracker.Infrastructure.Services;
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
        var userId = GetSignedInUserId();
        var settings = await localSettings.GetAsync();
        settings.UserId = userId;
        return await EnableSupabaseAsync(settings);
    }

    public async Task<int> EnableSupabaseAsync(AppSettings settings)
    {
        var userId = GetSignedInUserId();
        settings.UserId = userId;

        await localSettings.SaveAsync(settings);

        var loadedSettings = await supabaseSettings.GetAsync();
        var loadedEntries = await supabaseEntries.GetAllAsync();
        await ReplaceLocalSnapshotAsync(loadedSettings, loadedEntries);
        await modeService.SetUseSupabaseAsync(true);
        return loadedEntries.Count;
    }

    public async Task<int> DisableSupabaseAsync()
    {
        GetSignedInUserId();

        var settings = await supabaseSettings.GetAsync();
        var entries = await supabaseEntries.GetAllAsync();
        await ReplaceLocalSnapshotAsync(settings, entries);
        await modeService.SetUseSupabaseAsync(false);
        return entries.Count;
    }

    public async Task<int> SyncToSupabaseAsync()
    {
        var userId = GetSignedInUserId();

        var settings = await localSettings.GetAsync();
        settings.UserId = userId;
        return await SyncToSupabaseAsync(settings);
    }

    private async Task<int> SyncToSupabaseAsync(AppSettings settings)
    {
        var userId = GetSignedInUserId();
        settings.UserId = userId;
        await supabaseSettings.SaveSyncedSettingsAsync(settings);

        var localData = await localEntries.GetAllAsync();
        await supabaseEntries.ClearAsync(userId);
        foreach (var entry in localData)
        {
            var remoteEntry = CloneForSupabase(entry);
            remoteEntry.UserId = userId;
            await supabaseEntries.SaveAsync(remoteEntry);
        }

        return localData.Count;
    }

    private string GetSignedInUserId()
    {
        var userId = clientProvider.Client.Auth.CurrentUser?.Id;
        if (!Guid.TryParse(userId, out var parsed))
        {
            throw new InvalidOperationException("Sign in to Supabase before enabling sync.");
        }

        return parsed.ToString();
    }

    private async Task ReplaceLocalSnapshotAsync(
        AppSettings settings,
        IReadOnlyList<OtEntry> entries)
    {
        var deviceSettings = await localSettings.GetAsync();
        settings.PinLockEnabled = deviceSettings.PinLockEnabled;
        settings.BiometricUnlockEnabled = deviceSettings.BiometricUnlockEnabled;
        await localSettings.SaveAsync(settings);
        await localEntries.ClearAsync();

        foreach (var entry in entries)
        {
            await localEntries.SaveAsync(CloneForLocal(entry));
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
