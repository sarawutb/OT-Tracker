using OTTracker.Models;

namespace OTTracker.Services;

public interface IOtEntryRepository
{
    Task<IReadOnlyList<OtEntry>> GetAllAsync();

    Task<IReadOnlyList<OtEntry>> GetMonthAsync(int year, int month);

    Task<IReadOnlyList<OtEntry>> GetRecentAsync(int count);

    Task<OtEntry?> GetByIdAsync(int id);

    Task SaveAsync(OtEntry entry);

    Task DeleteAsync(OtEntry entry);

    Task ClearAsync();
}
