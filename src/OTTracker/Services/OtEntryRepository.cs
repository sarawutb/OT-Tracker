using OTTracker.Data;
using OTTracker.Models;

namespace OTTracker.Services;

public sealed class OtEntryRepository(AppDatabase database) : IOtEntryRepository
{
    public async Task<IReadOnlyList<OtEntry>> GetAllAsync()
    {
        var connection = await database.GetConnectionAsync();
        return await connection.Table<OtEntry>().OrderByDescending(e => e.EntryDate).ThenByDescending(e => e.StartTime).ToListAsync();
    }

    public async Task<IReadOnlyList<OtEntry>> GetMonthAsync(int year, int month)
    {
        var connection = await database.GetConnectionAsync();
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        return await GetPeriodAsync(start, end.AddDays(-1));
    }

    public async Task<IReadOnlyList<OtEntry>> GetPeriodAsync(DateTime start, DateTime end)
    {
        var connection = await database.GetConnectionAsync();
        var periodStart = start.Date;
        var periodEndExclusive = end.Date.AddDays(1);
        return await connection.Table<OtEntry>()
            .Where(e => e.EntryDate >= periodStart && e.EntryDate < periodEndExclusive)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.StartTime)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<OtEntry>> GetRecentAsync(int count)
    {
        var connection = await database.GetConnectionAsync();
        return await connection.Table<OtEntry>()
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.StartTime)
            .Take(count)
            .ToListAsync();
    }

    public async Task<OtEntry?> GetByIdAsync(int id)
    {
        var connection = await database.GetConnectionAsync();
        return await connection.Table<OtEntry>().FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task SaveAsync(OtEntry entry)
    {
        var connection = await database.GetConnectionAsync();
        if (entry.Id == 0)
        {
            entry.CreateDate = DateTime.Now;
            await connection.InsertAsync(entry);
        }
        else
        {
            await connection.UpdateAsync(entry);
        }
    }

    public async Task DeleteAsync(OtEntry entry)
    {
        var connection = await database.GetConnectionAsync();
        await connection.DeleteAsync(entry);
    }

    public async Task ClearAsync()
    {
        var connection = await database.GetConnectionAsync();
        await connection.DeleteAllAsync<OtEntry>();
    }
}
