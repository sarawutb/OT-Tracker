using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using Supabase.Postgrest;

namespace OTTracker.Infrastructure.Repositories;

public sealed class OtEntryRepository(Supabase.Client client) : IOtEntryRepository
{
    private readonly Supabase.Client _client = client;

    public async Task<IReadOnlyList<OtEntry>> GetAllAsync()
    {
        var response = await _client.From<OtEntry>()
            .Order("entry_date", Constants.Ordering.Descending)
            .Order("start_time", Constants.Ordering.Descending)
            .Get();
        return response.Models;
    }

    public async Task<IReadOnlyList<OtEntry>> GetMonthAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return await GetPeriodAsync(start, end);
    }

    public async Task<IReadOnlyList<OtEntry>> GetPeriodAsync(DateTime start, DateTime end)
    {
        var periodStart = start.Date;
        var periodEnd = end.Date;
        
        var response = await _client.From<OtEntry>()
            .Filter("entry_date", Constants.Operator.GreaterThanOrEqual, periodStart.ToString("yyyy-MM-dd"))
            .Filter("entry_date", Constants.Operator.LessThanOrEqual, periodEnd.ToString("yyyy-MM-dd"))
            .Order("entry_date", Constants.Ordering.Descending)
            .Order("start_time", Constants.Ordering.Descending)
            .Get();
            
        return response.Models;
    }

    public async Task<IReadOnlyList<OtEntry>> GetRecentAsync(int count)
    {
        var response = await _client.From<OtEntry>()
            .Order("entry_date", Constants.Ordering.Descending)
            .Order("start_time", Constants.Ordering.Descending)
            .Limit(count)
            .Get();
            
        return response.Models;
    }

    public async Task<OtEntry?> GetByIdAsync(int id)
    {
        var response = await _client.From<OtEntry>()
            .Filter("id", Constants.Operator.Equals, id)
            .Get();
        return response.Models.FirstOrDefault();
    }

    public async Task SaveAsync(OtEntry entry)
    {
        var currentUser = _client.Auth.CurrentUser;
        if (currentUser is not null)
        {
            entry.UserId = currentUser.Id;
        }
        
        entry.ReviseDate = DateTime.Now;

        if (entry.Id == 0)
        {
            entry.CreateDate = DateTime.Now;
            await _client.From<OtEntry>().Insert(entry);
        }
        else
        {
            await _client.From<OtEntry>().Update(entry);
        }
    }

    public async Task DeleteAsync(OtEntry entry)
    {
        await _client.From<OtEntry>().Delete(entry);
    }

    public async Task ClearAsync()
    {
        var currentUser = _client.Auth.CurrentUser;
        if (currentUser is null)
        {
            return;
        }
        
        await _client.From<OtEntry>()
            .Filter("user_id", Constants.Operator.Equals, currentUser.Id)
            .Delete();
    }
}
