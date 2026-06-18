using OTTracker.Data;
using OTTracker.Models;

namespace OTTracker.Services;

public sealed class SettingsService(AppDatabase database) : ISettingsService
{
    public async Task<AppSettings> GetAsync()
    {
        var connection = await database.GetConnectionAsync();
        var settings = await connection.Table<AppSettings>().FirstOrDefaultAsync(s => s.Id == 1);
        if (settings is not null)
        {
            return settings;
        }

        settings = new AppSettings();
        await connection.InsertAsync(settings);
        return settings;
    }

    public async Task SaveAsync(AppSettings settings)
    {
        settings.Id = 1;
        settings.ReviseDate = DateTime.Now;
        var connection = await database.GetConnectionAsync();
        var existing = await connection.Table<AppSettings>().FirstOrDefaultAsync(s => s.Id == 1);
        if (existing is null)
        {
            await connection.InsertAsync(settings);
        }
        else
        {
            await connection.UpdateAsync(settings);
        }
    }
}
