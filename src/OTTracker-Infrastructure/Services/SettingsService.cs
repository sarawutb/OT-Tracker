using System;
using System.Linq;
using System.Threading.Tasks;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using Supabase.Postgrest;

namespace OTTracker.Infrastructure.Services;

public sealed class SettingsService(Supabase.Client client) : ISettingsService
{
    private readonly Supabase.Client _client = client;

    public async Task<AppSettings> GetAsync()
    {
        if (!SupabaseAuthUser.TryGetUserId(_client, out var userId))
        {
            return new AppSettings();
        }

        var response = await _client.From<AppSettings>()
            .Filter("user_id", Constants.Operator.Equals, userId)
            .Get();

        var settings = response.Models.FirstOrDefault();
        if (settings is not null)
        {
            return settings;
        }

        // If it doesn't exist yet, we create it
        settings = new AppSettings
        {
            UserId = settings.UserId,
            UserName = _client.Auth.CurrentUser?.Email?.Split('@').FirstOrDefault() ?? "Username"
        };
        await _client.From<AppSettings>().Insert(settings);
        return settings;
    }

    public async Task SaveAsync(AppSettings settings)
    {
        settings.UserId = SupabaseAuthUser.GetRequiredUserId(_client, settings.UserId);
        settings.ReviseDate = DateTime.Now;

        await _client.From<AppSettings>().Upsert(settings);
    }
}
