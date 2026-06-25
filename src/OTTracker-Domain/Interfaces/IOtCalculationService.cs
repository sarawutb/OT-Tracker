using System;
using OTTracker.Domain.Entities;
using OTTracker.Domain.Enums;

namespace OTTracker.Domain.Interfaces;

public interface IOtCalculationService
{
    decimal GetHourlyRate(AppSettings settings);

    decimal GetMultiplier(AppSettings settings, DayType dayType);

    decimal GetNetHours(TimeSpan startTime, TimeSpan endTime, int breakMinutes);

    decimal GetEstimatedEarnings(decimal netHours, decimal hourlyRate, decimal multiplier);

    void ApplyCalculation(OtEntry entry, AppSettings settings);
}
