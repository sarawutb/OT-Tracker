namespace OTTracker.Services;

using OTTracker.Domain.Entities;

public interface IDataSyncService
{
    Task<int> EnableSupabaseAsync();

    Task<int> EnableSupabaseAsync(AppSettings settings);

    /// <summary>
    /// Switches to Supabase and replaces the user's Supabase data with this device's local data.
    /// Returns the number of entries uploaded.
    /// </summary>
    Task<int> EnableSupabaseWithLocalDataAsync(AppSettings settings);

    Task<int> DisableSupabaseAsync();

    Task<int> SyncToSupabaseAsync();
}
