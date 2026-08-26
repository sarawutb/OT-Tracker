using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using OTTracker.Infrastructure.Repositories;

namespace OTTracker.Services;

public sealed class RoutedOtEntryRepository(
    IDataSourceModeService modeService,
    LocalOtEntryRepository localRepository,
    OtEntryRepository supabaseRepository) : IOtEntryRepository
{
    public async Task<IReadOnlyList<OtEntry>> GetAllAsync()
    {
        if (modeService.UseSupabase)
        {
            try
            {
                return await supabaseRepository.GetAllAsync();
            }
            catch
            {
                return await localRepository.GetAllAsync();
            }
        }

        return await localRepository.GetAllAsync();
    }

    public async Task<IReadOnlyList<OtEntry>> GetMonthAsync(int year, int month)
    {
        if (modeService.UseSupabase)
        {
            try
            {
                return await supabaseRepository.GetMonthAsync(year, month);
            }
            catch
            {
                return await localRepository.GetMonthAsync(year, month);
            }
        }

        return await localRepository.GetMonthAsync(year, month);
    }

    public async Task<IReadOnlyList<OtEntry>> GetPeriodAsync(DateTime start, DateTime end)
    {
        if (modeService.UseSupabase)
        {
            try
            {
                return await supabaseRepository.GetPeriodAsync(start, end);
            }
            catch
            {
                return await localRepository.GetPeriodAsync(start, end);
            }
        }

        return await localRepository.GetPeriodAsync(start, end);
    }

    public async Task<IReadOnlyList<OtEntry>> GetRecentAsync(int count)
    {
        if (modeService.UseSupabase)
        {
            try
            {
                return await supabaseRepository.GetRecentAsync(count);
            }
            catch
            {
                return await localRepository.GetRecentAsync(count);
            }
        }

        return await localRepository.GetRecentAsync(count);
    }

    public async Task<OtEntry?> GetByIdAsync(int id)
    {
        if (modeService.UseSupabase)
        {
            try
            {
                return await supabaseRepository.GetByIdAsync(id);
            }
            catch
            {
                return await localRepository.GetByIdAsync(id);
            }
        }

        return await localRepository.GetByIdAsync(id);
    }

    public async Task SaveAsync(OtEntry entry)
    {
        await localRepository.SaveAsync(entry);

        if (modeService.UseSupabase)
        {
            try
            {
                await supabaseRepository.SaveAsync(entry);
            }
            catch
            {
                // Local save succeeded even if remote sync failed
            }
        }
    }

    public async Task DeleteAsync(OtEntry entry)
    {
        await localRepository.DeleteAsync(entry);

        if (modeService.UseSupabase)
        {
            try
            {
                await supabaseRepository.DeleteAsync(entry);
            }
            catch
            {
                // Local delete succeeded
            }
        }
    }

    public async Task ClearAsync()
    {
        await localRepository.ClearAsync();

        if (modeService.UseSupabase)
        {
            try
            {
                await supabaseRepository.ClearAsync();
            }
            catch
            {
                // Local clear succeeded
            }
        }
    }
}
