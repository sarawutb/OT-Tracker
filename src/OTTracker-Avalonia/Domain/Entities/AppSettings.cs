using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace OTTracker_Avalonia.Domain.Entities;

[Table("app_settings")]
public sealed class AppSettings : BaseModel
{
    [PrimaryKey("user_id", false)]
    public string UserId { get; set; } = string.Empty;

    [Column("base_monthly_salary")]
    public decimal BaseMonthlySalary { get; set; } = 30000m;

    [Column("working_days_per_month")]
    public int WorkingDaysPerMonth { get; set; } = 30;

    [Column("hours_per_day")]
    public decimal HoursPerDay { get; set; } = 8m;

    [Column("default_start_time")]
    public string DefaultStartTimeString { get; set; } = "17:00:00";

    [Column("default_end_time")]
    public string DefaultEndTimeString { get; set; } = "21:00:00";

    [Column("default_break_minutes")]
    public int DefaultBreakMinutes { get; set; } = 30;

    [Column("period_start_day")]
    public int PeriodStartDay { get; set; } = 16;

    [Column("period_end_day")]
    public int PeriodEndDay { get; set; } = 15;

    [Column("regular_multiplier")]
    public decimal RegularMultiplier { get; set; } = 1.5m;

    [Column("weekend_multiplier")]
    public decimal WeekendMultiplier { get; set; } = 2.0m;

    [Column("holiday_multiplier")]
    public decimal HolidayMultiplier { get; set; } = 3.0m;

    [Column("pin_lock_enabled")]
    public bool PinLockEnabled { get; set; }

    [Column("biometric_unlock_enabled")]
    public bool BiometricUnlockEnabled { get; set; }

    [Column("mask_earnings")]
    public bool MaskEarnings { get; set; }

    [Column("currency_code")]
    public string CurrencyCode { get; set; } = "THB";

    [Column("user_name")]
    public string UserName { get; set; } = "Say HI";

    [Column("revise_date")]
    public DateTime ReviseDate { get; set; } = DateTime.Now;

    // Helper property to map TimeSpan DefaultStartTime to DefaultStartTimeString
    [Newtonsoft.Json.JsonIgnore]
    public TimeSpan DefaultStartTime
    {
        get => TimeSpan.TryParse(DefaultStartTimeString, out var ts) ? ts : new TimeSpan(17, 0, 0);
        set => DefaultStartTimeString = value.ToString(@"hh\:mm\:ss");
    }

    // Helper property to map TimeSpan DefaultEndTime to DefaultEndTimeString
    [Newtonsoft.Json.JsonIgnore]
    public TimeSpan DefaultEndTime
    {
        get => TimeSpan.TryParse(DefaultEndTimeString, out var ts) ? ts : new TimeSpan(21, 0, 0);
        set => DefaultEndTimeString = value.ToString(@"hh\:mm\:ss");
    }
}
