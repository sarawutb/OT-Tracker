namespace OTTracker.Services;

public interface IDataSyncService
{
    Task<int> EnableSupabaseAsync();

    Task<int> DisableSupabaseAsync();

    Task<int> SyncToSupabaseAsync();
}
