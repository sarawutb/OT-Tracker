namespace OTTracker.Services;

public interface IDataSourceModeService
{
    bool UseSupabase { get; }

    bool PendingSupabaseSync { get; }

    Task SetUseSupabaseAsync(bool enabled);

    Task SetPendingSupabaseSyncAsync(bool pending);
}
