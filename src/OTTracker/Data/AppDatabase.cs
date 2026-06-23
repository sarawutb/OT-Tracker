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

        if (!existing.Contains(nameof(AppSettings.PeriodStartDay)))
        {
            await connection.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN PeriodStartDay INTEGER NOT NULL DEFAULT 16");
        }

        if (!existing.Contains(nameof(AppSettings.PeriodEndDay)))
        {
            await connection.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN PeriodEndDay INTEGER NOT NULL DEFAULT 15");
        }

        if (!existing.Contains(nameof(AppSettings.PeriodStartDate)))
        {
            var defaultStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 16).Ticks;
            await connection.ExecuteAsync($"ALTER TABLE AppSettings ADD COLUMN PeriodStartDate INTEGER NOT NULL DEFAULT {defaultStart}");
        }

        if (!existing.Contains(nameof(AppSettings.PeriodEndDate)))
        {
            var defaultEnd = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 16).AddMonths(1).AddDays(-1).Ticks;
            await connection.ExecuteAsync($"ALTER TABLE AppSettings ADD COLUMN PeriodEndDate INTEGER NOT NULL DEFAULT {defaultEnd}");
        }

        if (!existing.Contains(nameof(AppSettings.MaskEarnings)))
        {
            await connection.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN MaskEarnings INTEGER NOT NULL DEFAULT 0");
        }

        if (!existing.Contains(nameof(AppSettings.UserName)))
        {
            await connection.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN UserName TEXT DEFAULT 'Username'");
        }
    }

    private sealed class TableInfo
    {
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }
}
