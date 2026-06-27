namespace OTTracker.Services;

public interface IDataSourceModeService
{
    bool UseSupabase { get; }

    Task SetUseSupabaseAsync(bool enabled);
}
