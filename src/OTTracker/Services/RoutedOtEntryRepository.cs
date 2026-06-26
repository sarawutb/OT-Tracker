using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using OTTracker.Infrastructure.Repositories;

namespace OTTracker.Services;

public sealed class RoutedOtEntryRepository(
    IDataSourceModeService modeService,
    LocalOtEntryRepository localRepository,
    OtEntryRepository supabaseRepository) : IOtEntryRepository
{
    private IOtEntryRepository Current => modeService.UseSupabase ? supabaseRepository : localRepository;

    public Task<IReadOnlyList<OtEntry>> GetAllAsync() => Current.GetAllAsync();

    public Task<IReadOnlyList<OtEntry>> GetMonthAsync(int year, int month) => Current.GetMonthAsync(year, month);

    public Task<IReadOnlyList<OtEntry>> GetPeriodAsync(DateTime start, DateTime end) => Current.GetPeriodAsync(start, end);

    public Task<IReadOnlyList<OtEntry>> GetRecentAsync(int count) => Current.GetRecentAsync(count);

    public Task<OtEntry?> GetByIdAsync(int id) => Current.GetByIdAsync(id);

    public Task SaveAsync(OtEntry entry) => Current.SaveAsync(entry);

    public Task DeleteAsync(OtEntry entry) => Current.DeleteAsync(entry);

    public Task ClearAsync() => Current.ClearAsync();
}
