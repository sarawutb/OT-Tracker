using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using OTTracker.Infrastructure.Services;
using Supabase.Postgrest;

namespace OTTracker.Infrastructure.Repositories;

public sealed class OtEntryRepository(ISupabaseClientProvider clientProvider) : IOtEntryRepository
{
    private Supabase.Client Client => clientProvider.Client;

    public async Task<IReadOnlyList<OtEntry>> GetAllAsync()
    {
        var response = await Client.From<OtEntry>()
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

        var response = await Client.From<OtEntry>()
            .Filter("entry_date", Constants.Operator.GreaterThanOrEqual, periodStart.ToString("yyyy-MM-dd", new CultureInfo("en-US")))
            .Filter("entry_date", Constants.Operator.LessThanOrEqual, periodEnd.ToString("yyyy-MM-dd", new CultureInfo("en-US")))
            .Order("entry_date", Constants.Ordering.Descending)
            .Order("start_time", Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<IReadOnlyList<OtEntry>> GetRecentAsync(int count)
    {
        var response = await Client.From<OtEntry>()
            .Order("entry_date", Constants.Ordering.Descending)
            .Order("start_time", Constants.Ordering.Descending)
            .Limit(count)
            .Get();

        return response.Models;
    }

    public async Task<OtEntry?> GetByIdAsync(int id)
    {
        var response = await Client.From<OtEntry>()
            .Filter("id", Constants.Operator.Equals, id)
            .Get();
        return response.Models.FirstOrDefault();
    }

    public async Task SaveAsync(OtEntry entry)
    {
        var client = Client;
        entry.UserId = SupabaseAuthUser.GetRequiredUserId(client, entry.UserId);
        entry.ReviseDate = DateTime.Now;
        if (entry.Id == 0)
        {
            entry.CreateDate = DateTime.Now;
            await client.From<OtEntry>().Insert(entry);
        }
        else
        {
            await client.From<OtEntry>().Update(entry);
        }
    }

    public async Task DeleteAsync(OtEntry entry)
    {
        await Client.From<OtEntry>().Delete(entry);
    }

    public async Task ClearAsync()
    {
        var client = Client;
        if (!SupabaseAuthUser.TryGetUserId(client, out var userId))
        {
            return;
        }

        await ClearAsync(userId);
    }

    public async Task ClearAsync(string userId)
    {
        var client = Client;
        userId = SupabaseAuthUser.GetRequiredUserId(client, userId);

        await client.From<OtEntry>()
            .Filter("user_id", Constants.Operator.Equals, userId)
            .Delete();
    }
}
