using OTTracker.Models;
using SQLite;

namespace OTTracker.Data;

public sealed class AppDatabase
{
    private SQLiteAsyncConnection? _connection;

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
        {
            return _connection;
        }

        var path = Path.Combine(FileSystem.AppDataDirectory, "ot_tracker.db3");
        _connection = new SQLiteAsyncConnection(path);
        await _connection.CreateTableAsync<OtEntry>();
        await _connection.CreateTableAsync<AppSettings>();
        return _connection;
    }
}
