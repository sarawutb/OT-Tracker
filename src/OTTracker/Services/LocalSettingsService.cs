using OTTracker.Data;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;

namespace OTTracker.Services;

public sealed class LocalSettingsService(LocalAppDatabase database) : ISettingsService
{
    public async Task<AppSettings> GetAsync()
    {
        var connection = await database.GetConnectionAsync();
        var row = await connection.Table<LocalAppSettingsRecord>().FirstOrDefaultAsync(s => s.Id == 1);
        if (row is not null)
        {
            return row.ToDomain();
        }

        var settings = new AppSettings();
        await connection.InsertAsync(LocalAppSettingsRecord.FromDomain(settings));
        return settings;
    }

    public async Task SaveAsync(AppSettings settings)
    {
        var connection = await database.GetConnectionAsync();
        settings.ReviseDate = DateTime.Now;
        await connection.InsertOrReplaceAsync(LocalAppSettingsRecord.FromDomain(settings));
    }
}
