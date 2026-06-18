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
        await EnsureSettingsColumnsAsync(_connection);
        return _connection;
    }

    private static async Task EnsureSettingsColumnsAsync(SQLiteAsyncConnection connection)
    {
        var columns = await connection.QueryAsync<TableInfo>("PRAGMA table_info(AppSettings)");
        var existing = columns.Select(column => column.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!existing.Contains(nameof(AppSettings.DefaultStartTime)))
        {
            await connection.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN DefaultStartTime INTEGER NOT NULL DEFAULT 612000000000");
        }

        if (!existing.Contains(nameof(AppSettings.DefaultEndTime)))
        {
            await connection.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN DefaultEndTime INTEGER NOT NULL DEFAULT 756000000000");
        }

        if (!existing.Contains(nameof(AppSettings.DefaultBreakMinutes)))
        {
            await connection.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN DefaultBreakMinutes INTEGER NOT NULL DEFAULT 30");
        }
    }

    private sealed class TableInfo
    {
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }
}
