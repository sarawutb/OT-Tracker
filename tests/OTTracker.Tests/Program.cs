var failures = new List<string>();

AssertEqual(125m, HourlyRate(30000m, 30, 8m), "Hourly rate default");
AssertEqual(3.5m, NetHours(new TimeSpan(17, 0, 0), new TimeSpan(21, 0, 0), 30), "Net hours default");
AssertEqual(656.25m, EstimatedEarnings(3.5m, 125m, 1.5m), "Regular earnings");
AssertEqual(1250m, EstimatedEarnings(5m, 125m, 2m), "Weekend earnings");
AssertEqual(1500m, EstimatedEarnings(4m, 125m, 3m), "Holiday earnings");
AssertEqual(0m, NetHours(new TimeSpan(18, 0, 0), new TimeSpan(17, 0, 0), 0), "Negative hours clamp");
AssertEqual(263.85m, EstimatedEarnings(1.5m, 117.27m, 1.5m), "Earnings truncate (263.8575)");
AssertEqual(170.45m, HourlyRate(30000m, 22, 8m), "Hourly rate truncate (170.4545)");
AssertEqual(148.80m, HourlyRate(25000m, 21, 8m), "Hourly rate truncate (148.8095)");

if (failures.Count > 0)
{
    Console.Error.WriteLine("OT Tracker calculation tests failed:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    return 1;
}

Console.WriteLine("OT Tracker calculation tests passed.");
return 0;

static decimal HourlyRate(decimal salary, int days, decimal hoursPerDay)
{
    return Truncate2(salary / (days * hoursPerDay));
}

static decimal NetHours(TimeSpan start, TimeSpan end, int breakMinutes)
{
    var minutes = (decimal)(end - start).TotalMinutes - breakMinutes;
    return Math.Round(Math.Max(0, minutes / 60m), 2);
}

static decimal EstimatedEarnings(decimal netHours, decimal hourlyRate, decimal multiplier)
{
    return Truncate2(netHours * hourlyRate * multiplier);
}

static decimal Truncate2(decimal value) => Math.Truncate(value * 100m) / 100m;

void AssertEqual(decimal expected, decimal actual, string name)
{
    if (expected != actual)
    {
        failures.Add($"{name}: expected {expected}, actual {actual}");
    }
}
