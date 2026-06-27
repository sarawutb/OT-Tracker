namespace OTTracker.Services;

using OTTracker.Domain.Entities;

public interface IDataSyncService
{
    Task<int> EnableSupabaseAsync();

    Task<int> EnableSupabaseAsync(AppSettings settings);

    Task<int> DisableSupabaseAsync();

    Task<int> SyncToSupabaseAsync();
}
