namespace OTTracker.Models;

public sealed record MonthlySummary(decimal TotalHours, decimal EstimatedEarnings, int EntryCount);

public sealed record WeeklyDaySummary(string Day, decimal Hours, bool IsWeekend);
