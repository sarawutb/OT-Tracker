using OTTracker.Models;

namespace OTTracker.Services;

public sealed class OtCalculationService : IOtCalculationService
{
    public decimal GetHourlyRate(AppSettings settings)
    {
        if (settings.BaseMonthlySalary <= 0 || settings.WorkingDaysPerMonth <= 0 || settings.HoursPerDay <= 0)
        {
            return 0;
        }

        return Math.Round(settings.BaseMonthlySalary / (settings.WorkingDaysPerMonth * settings.HoursPerDay), 2);
    }

    public decimal GetMultiplier(AppSettings settings, DayType dayType) => dayType switch
    {
        DayType.Weekend => settings.WeekendMultiplier,
        DayType.Holiday => settings.HolidayMultiplier,
        _ => settings.RegularMultiplier
    };

    public decimal GetNetHours(TimeSpan startTime, TimeSpan endTime, int breakMinutes)
    {
        var minutes = (decimal)(endTime - startTime).TotalMinutes - breakMinutes;
        return Math.Round(Math.Max(0, minutes / 60m), 2);
    }

    public decimal GetEstimatedEarnings(decimal netHours, decimal hourlyRate, decimal multiplier)
    {
        return Math.Round(netHours * hourlyRate * multiplier, 2);
    }

    public void ApplyCalculation(OtEntry entry, AppSettings settings)
    {
        entry.HourlyRate = GetHourlyRate(settings);
        entry.Multiplier = GetMultiplier(settings, entry.DayType);
        entry.NetHours = GetNetHours(entry.StartTime, entry.EndTime, entry.BreakMinutes);
        entry.EstimatedEarnings = GetEstimatedEarnings(entry.NetHours, entry.HourlyRate, entry.Multiplier);
        entry.ReviseDate = DateTime.Now;
    }
}
