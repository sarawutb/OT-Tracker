using SQLite;

namespace OTTracker.Data;

public sealed class LocalAppDatabase
{
    private SQLiteAsyncConnection? _connection;

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
        {
            return _connection;
        }

        var path = Path.Combine(FileSystem.AppDataDirectory, "ottracker-local.db3");
        _connection = new SQLiteAsyncConnection(path);
        await _connection.CreateTableAsync<LocalOtEntryRecord>();
        await _connection.CreateTableAsync<LocalAppSettingsRecord>();
        return _connection;
    }
}
