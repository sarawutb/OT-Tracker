using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OTTracker.Domain.Entities;

namespace OTTracker.Domain.Interfaces;

public interface IOtEntryRepository
{
    Task<IReadOnlyList<OtEntry>> GetAllAsync();

    Task<IReadOnlyList<OtEntry>> GetMonthAsync(int year, int month);

    Task<IReadOnlyList<OtEntry>> GetPeriodAsync(DateTime start, DateTime end);

    Task<IReadOnlyList<OtEntry>> GetRecentAsync(int count);

    Task<OtEntry?> GetByIdAsync(int id);

    Task SaveAsync(OtEntry entry);

    Task DeleteAsync(OtEntry entry);

    Task ClearAsync();
}
