using OTTracker.Domain.Entities;
using SQLite;

namespace OTTracker.Data;

public sealed class LocalAppSettingsRecord
{
    [PrimaryKey]
    public int Id { get; set; } = 1;

    public string UserId { get; set; } = string.Empty;

    public double BaseMonthlySalary { get; set; } = 30000d;

    public int WorkingDaysPerMonth { get; set; } = 30;

    public double HoursPerDay { get; set; } = 8d;

    public string DefaultStartTimeString { get; set; } = "17:00:00";

    public string DefaultEndTimeString { get; set; } = "21:00:00";

    public int DefaultBreakMinutes { get; set; } = 30;

    public int PeriodStartDay { get; set; } = 16;

    public int PeriodEndDay { get; set; } = 15;

    public double RegularMultiplier { get; set; } = 1.5d;

    public double WeekendMultiplier { get; set; } = 2.0d;

    public double HolidayMultiplier { get; set; } = 3.0d;

    public bool PinLockEnabled { get; set; }

    public bool BiometricUnlockEnabled { get; set; }

    public bool MaskEarnings { get; set; }

    public string CurrencyCode { get; set; } = "THB";

    public string UserName { get; set; } = "Say HI";

    public DateTime ReviseDate { get; set; } = DateTime.Now;

    public static LocalAppSettingsRecord FromDomain(AppSettings settings) => new()
    {
        Id = 1,
        UserId = settings.UserId,
        BaseMonthlySalary = (double)settings.BaseMonthlySalary,
        WorkingDaysPerMonth = settings.WorkingDaysPerMonth,
        HoursPerDay = (double)settings.HoursPerDay,
        DefaultStartTimeString = settings.DefaultStartTimeString,
        DefaultEndTimeString = settings.DefaultEndTimeString,
        DefaultBreakMinutes = settings.DefaultBreakMinutes,
        PeriodStartDay = settings.PeriodStartDay,
        PeriodEndDay = settings.PeriodEndDay,
        RegularMultiplier = (double)settings.RegularMultiplier,
        WeekendMultiplier = (double)settings.WeekendMultiplier,
        HolidayMultiplier = (double)settings.HolidayMultiplier,
        PinLockEnabled = settings.PinLockEnabled,
        BiometricUnlockEnabled = settings.BiometricUnlockEnabled,
        MaskEarnings = settings.MaskEarnings,
        CurrencyCode = settings.CurrencyCode,
        UserName = settings.UserName,
        ReviseDate = settings.ReviseDate
    };

    public AppSettings ToDomain() => new()
    {
        UserId = UserId,
        BaseMonthlySalary = (decimal)BaseMonthlySalary,
        WorkingDaysPerMonth = WorkingDaysPerMonth,
        HoursPerDay = (decimal)HoursPerDay,
        DefaultStartTimeString = DefaultStartTimeString,
        DefaultEndTimeString = DefaultEndTimeString,
        DefaultBreakMinutes = DefaultBreakMinutes,
        PeriodStartDay = PeriodStartDay,
        PeriodEndDay = PeriodEndDay,
        RegularMultiplier = (decimal)RegularMultiplier,
        WeekendMultiplier = (decimal)WeekendMultiplier,
        HolidayMultiplier = (decimal)HolidayMultiplier,
        PinLockEnabled = PinLockEnabled,
        BiometricUnlockEnabled = BiometricUnlockEnabled,
        MaskEarnings = MaskEarnings,
        CurrencyCode = CurrencyCode,
        UserName = UserName,
        ReviseDate = ReviseDate
    };
}
