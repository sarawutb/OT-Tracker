using OTTracker.Data;
using OTTracker.Models;

namespace OTTracker.Services;

public sealed class SettingsService(AppDatabase database) : ISettingsService
{
    private const string BiometricUnlockKey = "ot_tracker_biometric_unlock_enabled";

    public async Task<AppSettings> GetAsync()
    {
        var connection = await database.GetConnectionAsync();
        var settings = await connection.Table<AppSettings>().FirstOrDefaultAsync(s => s.Id == 1);
        if (settings is not null)
        {
            settings.BiometricUnlockEnabled = await GetBiometricUnlockEnabledAsync();
            return settings;
        }

        settings = new AppSettings
        {
            BiometricUnlockEnabled = await GetBiometricUnlockEnabledAsync()
        };
        await connection.InsertAsync(settings);
        return settings;
    }

    public async Task SaveAsync(AppSettings settings)
    {
        await SecureStorage.Default.SetAsync(BiometricUnlockKey, settings.BiometricUnlockEnabled ? "true" : "false");

        settings.Id = 1;
        settings.ReviseDate = DateTime.Now;

        var settingsForDatabase = new AppSettings
        {
            Id = settings.Id,
            BaseMonthlySalary = settings.BaseMonthlySalary,
            WorkingDaysPerMonth = settings.WorkingDaysPerMonth,
            HoursPerDay = settings.HoursPerDay,
            DefaultStartTime = settings.DefaultStartTime,
            DefaultEndTime = settings.DefaultEndTime,
            DefaultBreakMinutes = settings.DefaultBreakMinutes,
            PeriodStartDay = settings.PeriodStartDay,
            PeriodEndDay = settings.PeriodEndDay,
            PeriodStartDate = settings.PeriodStartDate.Date,
            PeriodEndDate = settings.PeriodEndDate.Date,
            RegularMultiplier = settings.RegularMultiplier,
            WeekendMultiplier = settings.WeekendMultiplier,
            HolidayMultiplier = settings.HolidayMultiplier,
            PinLockEnabled = settings.PinLockEnabled,
            BiometricUnlockEnabled = false,
            MaskEarnings = settings.MaskEarnings,
            CurrencyCode = settings.CurrencyCode,
            UserName = settings.UserName,
            ReviseDate = settings.ReviseDate
        };

        var connection = await database.GetConnectionAsync();
        var existing = await connection.Table<AppSettings>().FirstOrDefaultAsync(s => s.Id == 1);
        if (existing is null)
        {
            await connection.InsertAsync(settingsForDatabase);
        }
        else
        {
            await connection.UpdateAsync(settingsForDatabase);
        }
    }

    private static async Task<bool> GetBiometricUnlockEnabledAsync()
    {
        return string.Equals(await SecureStorage.Default.GetAsync(BiometricUnlockKey), "true", StringComparison.OrdinalIgnoreCase);
    }
}
