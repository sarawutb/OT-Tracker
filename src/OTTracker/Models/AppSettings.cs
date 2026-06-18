using SQLite;

namespace OTTracker.Models;

public sealed class AppSettings
{
    [PrimaryKey]
    public int Id { get; set; } = 1;

    public decimal BaseMonthlySalary { get; set; } = 30000m;

    public int WorkingDaysPerMonth { get; set; } = 30;

    public decimal HoursPerDay { get; set; } = 8m;

    public decimal RegularMultiplier { get; set; } = 1.5m;

    public decimal WeekendMultiplier { get; set; } = 2.0m;

    public decimal HolidayMultiplier { get; set; } = 3.0m;

    public bool PinLockEnabled { get; set; }

    public bool BiometricUnlockEnabled { get; set; }

    public string CurrencyCode { get; set; } = "THB";

    public DateTime ReviseDate { get; set; } = DateTime.Now;
}
