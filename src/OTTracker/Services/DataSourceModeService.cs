namespace OTTracker.Services;

public sealed class DataSourceModeService : IDataSourceModeService
{
    private const string UseSupabaseKey = "ot_tracker_use_supabase";
    private const string PendingSupabaseSyncKey = "ot_tracker_pending_supabase_sync";

    public bool UseSupabase => Preferences.Default.Get(UseSupabaseKey, false);

    public bool PendingSupabaseSync => Preferences.Default.Get(PendingSupabaseSyncKey, false);

    public Task SetUseSupabaseAsync(bool enabled)
    {
        Preferences.Default.Set(UseSupabaseKey, enabled);
        return Task.CompletedTask;
    }

    public Task SetPendingSupabaseSyncAsync(bool pending)
    {
        Preferences.Default.Set(PendingSupabaseSyncKey, pending);
        return Task.CompletedTask;
    }
}
