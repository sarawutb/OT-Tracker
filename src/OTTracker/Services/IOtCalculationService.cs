using OTTracker.Models;

namespace OTTracker.Services;

public interface IOtCalculationService
{
    decimal GetHourlyRate(AppSettings settings);

    decimal GetMultiplier(AppSettings settings, DayType dayType);

    decimal GetNetHours(TimeSpan startTime, TimeSpan endTime, int breakMinutes);

    decimal GetEstimatedEarnings(decimal netHours, decimal hourlyRate, decimal multiplier);

    void ApplyCalculation(OtEntry entry, AppSettings settings);
}
