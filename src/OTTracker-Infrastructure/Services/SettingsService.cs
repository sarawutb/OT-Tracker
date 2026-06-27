using System;
using System.Linq;
using System.Threading.Tasks;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Interfaces;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Supabase.Postgrest;

namespace OTTracker.Infrastructure.Services;

public sealed class SettingsService(ISupabaseClientProvider clientProvider) : ISettingsService
{
    private Supabase.Client Client => clientProvider.Client;

    public async Task<AppSettings> GetAsync()
    {
        var client = Client;
        if (!SupabaseAuthUser.TryGetUserId(client, out var userId))
        {
            return new AppSettings();
        }

        var response = await client.From<AppSettings>()
            .Filter("user_id", Constants.Operator.Equals, userId)
            .Get();

        var settings = response.Models.FirstOrDefault();
        if (settings is not null)
        {
            return settings;
        }

        return new AppSettings
        {
            UserId = userId,
            UserName = client.Auth.CurrentUser?.Email?.Split('@').FirstOrDefault() ?? "Username"
        };
    }

    public async Task SaveAsync(AppSettings settings)
    {
        var client = Client;
        settings.UserId = SupabaseAuthUser.GetRequiredUserId(client, settings.UserId);
        settings.ReviseDate = DateTime.Now;

        await client.From<AppSettings>().Upsert(settings);
    }

    public async Task SaveSyncedSettingsAsync(AppSettings settings)
    {
        var client = Client;
        settings.UserId = SupabaseAuthUser.GetRequiredUserId(client, settings.UserId);
        settings.ReviseDate = DateTime.Now;

        await client.From<SyncedAppSettings>().Upsert(SyncedAppSettings.FromDomain(settings));
    }

    [Table("app_settings")]
    private sealed class SyncedAppSettings : BaseModel
    {
        [PrimaryKey("user_id", false)]
        public string UserId { get; init; } = string.Empty;

        [Column("base_monthly_salary")]
        public decimal BaseMonthlySalary { get; init; }

        [Column("working_days_per_month")]
        public int WorkingDaysPerMonth { get; init; }

        [Column("hours_per_day")]
        public decimal HoursPerDay { get; init; }

        [Column("default_start_time")]
        public string DefaultStartTimeString { get; init; } = string.Empty;

        [Column("default_end_time")]
        public string DefaultEndTimeString { get; init; } = string.Empty;

        [Column("default_break_minutes")]
        public int DefaultBreakMinutes { get; init; }

        [Column("period_start_day")]
        public int PeriodStartDay { get; init; }

        [Column("period_end_day")]
        public int PeriodEndDay { get; init; }

        [Column("regular_multiplier")]
        public decimal RegularMultiplier { get; init; }

        [Column("weekend_multiplier")]
        public decimal WeekendMultiplier { get; init; }

        [Column("holiday_multiplier")]
        public decimal HolidayMultiplier { get; init; }

        [Column("mask_earnings")]
        public bool MaskEarnings { get; init; }

        [Column("currency_code")]
        public string CurrencyCode { get; init; } = "THB";

        [Column("user_name")]
        public string UserName { get; init; } = "Username";

        [Column("revise_date")]
        [Newtonsoft.Json.JsonConverter(typeof(InvariantDateTimeConverter))]
        public DateTime ReviseDate { get; init; }

        public static SyncedAppSettings FromDomain(AppSettings settings) => new()
        {
            UserId = settings.UserId,
            BaseMonthlySalary = settings.BaseMonthlySalary,
            WorkingDaysPerMonth = settings.WorkingDaysPerMonth,
            HoursPerDay = settings.HoursPerDay,
            DefaultStartTimeString = settings.DefaultStartTimeString,
            DefaultEndTimeString = settings.DefaultEndTimeString,
            DefaultBreakMinutes = settings.DefaultBreakMinutes,
            PeriodStartDay = settings.PeriodStartDay,
            PeriodEndDay = settings.PeriodEndDay,
            RegularMultiplier = settings.RegularMultiplier,
            WeekendMultiplier = settings.WeekendMultiplier,
            HolidayMultiplier = settings.HolidayMultiplier,
            MaskEarnings = settings.MaskEarnings,
            CurrencyCode = settings.CurrencyCode,
            UserName = settings.UserName,
            ReviseDate = settings.ReviseDate
        };
    }
}
