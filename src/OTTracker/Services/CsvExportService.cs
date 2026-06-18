using System.Globalization;
using System.Text;
using OTTracker.Models;

namespace OTTracker.Services;

public sealed class CsvExportService : ICsvExportService
{
    public async Task<string> ExportAsync(IEnumerable<OtEntry> entries)
    {
        var path = Path.Combine(FileSystem.CacheDirectory, $"ot-records-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        var builder = new StringBuilder();
        builder.AppendLine("Date,Day type,Start time,End time,Break minutes,Net OT hours,Hourly rate,Multiplier,Estimated earnings,Note");

        foreach (var entry in entries.OrderBy(e => e.EntryDate).ThenBy(e => e.StartTime))
        {
            builder.AppendLine(string.Join(",", new[]
            {
                Escape(entry.EntryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                Escape(entry.DayLabel),
                Escape(entry.StartTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture)),
                Escape(entry.EndTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture)),
                entry.BreakMinutes.ToString(CultureInfo.InvariantCulture),
                entry.NetHours.ToString("0.##", CultureInfo.InvariantCulture),
                entry.HourlyRate.ToString("0.00", CultureInfo.InvariantCulture),
                entry.Multiplier.ToString("0.##", CultureInfo.InvariantCulture),
                entry.EstimatedEarnings.ToString("0.00", CultureInfo.InvariantCulture),
                Escape(entry.Note)
            }));
        }

        await File.WriteAllTextAsync(path, builder.ToString(), Encoding.UTF8);
        await Share.Default.RequestAsync(new ShareFileRequest("Export OT records", new ShareFile(path)));
        return path;
    }

    private static string Escape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
