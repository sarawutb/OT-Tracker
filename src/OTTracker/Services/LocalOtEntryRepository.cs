using OTTracker.Data;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;

namespace OTTracker.Services;

public sealed class LocalOtEntryRepository(LocalAppDatabase database) : IOtEntryRepository
{
    public async Task<IReadOnlyList<OtEntry>> GetAllAsync()
    {
        var connection = await database.GetConnectionAsync();
        var rows = await connection.Table<LocalOtEntryRecord>().ToListAsync();
        return rows
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.StartTimeString)
            .Select(e => e.ToDomain())
            .ToList();
    }

    public async Task<IReadOnlyList<OtEntry>> GetMonthAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return await GetPeriodAsync(start, end);
    }

    public async Task<IReadOnlyList<OtEntry>> GetPeriodAsync(DateTime start, DateTime end)
    {
        var connection = await database.GetConnectionAsync();
        var rows = await connection.Table<LocalOtEntryRecord>()
            .Where(e => e.EntryDate >= start.Date && e.EntryDate <= end.Date)
            .ToListAsync();
        return rows
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.StartTimeString)
            .Select(e => e.ToDomain())
            .ToList();
    }

    public async Task<IReadOnlyList<OtEntry>> GetRecentAsync(int count)
    {
        var all = await GetAllAsync();
        return all.Take(count).ToList();
    }

    public async Task<OtEntry?> GetByIdAsync(int id)
    {
        var connection = await database.GetConnectionAsync();
        var row = await connection.Table<LocalOtEntryRecord>()
            .FirstOrDefaultAsync(e => e.LocalId == id);
        return row?.ToDomain();
    }

    public async Task SaveAsync(OtEntry entry)
    {
        var connection = await database.GetConnectionAsync();
        entry.ReviseDate = DateTime.Now;

        if (entry.Id == 0)
        {
            entry.CreateDate = DateTime.Now;
            var record = LocalOtEntryRecord.FromDomain(entry);
            await connection.InsertAsync(record);
            entry.Id = record.LocalId;
            return;
        }

        await connection.InsertOrReplaceAsync(LocalOtEntryRecord.FromDomain(entry));
    }

    public async Task DeleteAsync(OtEntry entry)
    {
        var connection = await database.GetConnectionAsync();
        await connection.DeleteAsync<LocalOtEntryRecord>(entry.Id);
    }

    public async Task ClearAsync()
    {
        var connection = await database.GetConnectionAsync();
        await connection.DeleteAllAsync<LocalOtEntryRecord>();
    }
}
