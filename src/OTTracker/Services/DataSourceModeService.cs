namespace OTTracker.Services;

public sealed class DataSourceModeService : IDataSourceModeService
{
    private const string UseSupabaseKey = "ot_tracker_use_supabase";

    public bool UseSupabase => Preferences.Default.Get(UseSupabaseKey, false);

    public Task SetUseSupabaseAsync(bool enabled)
    {
        Preferences.Default.Set(UseSupabaseKey, enabled);
        return Task.CompletedTask;
    }
}
