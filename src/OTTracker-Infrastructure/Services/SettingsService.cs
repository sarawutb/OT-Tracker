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
        var currentUser = _client.Auth.CurrentUser;
        if (currentUser is null)
        {
            return new AppSettings();
        }

        var response = await _client.From<AppSettings>()
            .Filter("user_id", Constants.Operator.Equals, currentUser.Id)
            .Get();

        var settings = response.Models.FirstOrDefault();
        if (settings is not null)
        {
            return settings;
        }

        // If it doesn't exist yet, we create it
        settings = new AppSettings
        {
            UserId = currentUser.Id,
            UserName = currentUser.Email?.Split('@').FirstOrDefault() ?? "Username"
        };
        await _client.From<AppSettings>().Insert(settings);
        return settings;
    }

    public async Task SaveAsync(AppSettings settings)
    {
        var currentUser = _client.Auth.CurrentUser;
        if (currentUser is null)
        {
            return;
        }

        settings.UserId = currentUser.Id;
        settings.ReviseDate = DateTime.Now;

        await _client.From<AppSettings>().Upsert(settings);
    }
}
