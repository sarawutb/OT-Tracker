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

    public async Task<IReadOnlyList<OtEntry>> ImportAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Import OT records CSV"
        });

        if (result is null)
        {
            return Array.Empty<OtEntry>();
        }

        await using var stream = await result.OpenReadAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        var content = await reader.ReadToEndAsync();
        var rows = ParseRows(content);
        if (rows.Count == 0)
        {
            return Array.Empty<OtEntry>();
        }

        var firstRow = rows[0];
        var startIndex = firstRow.Count > 0 && string.Equals(firstRow[0].Trim(), "Date", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        var entries = new List<OtEntry>();

        for (var i = startIndex; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Count == 0 || row.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var rowNumber = i + 1;
            if (row.Count < 10)
            {
                throw new InvalidDataException($"CSV row {rowNumber} has {row.Count} columns; expected 10.");
            }

            var now = DateTime.Now;
            entries.Add(new OtEntry
            {
                EntryDate = ParseDate(row[0], rowNumber),
                DayType = ParseDayType(row[1], rowNumber),
                StartTime = ParseTime(row[2], rowNumber, "start time"),
                EndTime = ParseTime(row[3], rowNumber, "end time"),
                BreakMinutes = ParseInt(row[4], rowNumber, "break minutes"),
                NetHours = ParseDecimal(row[5], rowNumber, "net OT hours"),
                HourlyRate = ParseDecimal(row[6], rowNumber, "hourly rate"),
                Multiplier = ParseDecimal(row[7], rowNumber, "multiplier"),
                EstimatedEarnings = ParseDecimal(row[8], rowNumber, "estimated earnings"),
                Note = row[9],
                CreateDate = now,
                ReviseDate = now
            });
        }

        return entries;
    }

    private static List<List<string>> ParseRows(string content)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < content.Length; i++)
        {
            var current = content[i];
            if (inQuotes)
            {
                if (current == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(current);
                }

                continue;
            }

            switch (current)
            {
                case '"':
                    inQuotes = true;
                    break;
                case ',':
                    AddField();
                    break;
                case '\r':
                    AddRow();
                    if (i + 1 < content.Length && content[i + 1] == '\n')
                    {
                        i++;
                    }
                    break;
                case '\n':
                    AddRow();
                    break;
                default:
                    field.Append(current);
                    break;
            }
        }

        if (inQuotes)
        {
            throw new InvalidDataException("CSV file has an unterminated quoted field.");
        }

        if (field.Length > 0 || row.Count > 0)
        {
            AddRow();
        }

        return rows;

        void AddField()
        {
            row.Add(field.ToString());
            field.Clear();
        }

        void AddRow()
        {
            AddField();
            rows.Add(row);
            row = new List<string>();
        }
    }

    private static DateTime ParseDate(string value, int rowNumber)
    {
        var trimmed = value.Trim();
        if (DateTime.TryParseExact(trimmed, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var exactDate) ||
            DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out exactDate) ||
            DateTime.TryParse(trimmed, CultureInfo.CurrentCulture, DateTimeStyles.None, out exactDate))
        {
            return exactDate.Date;
        }

        throw new InvalidDataException($"CSV row {rowNumber} has an invalid date.");
    }

    private static TimeSpan ParseTime(string value, int rowNumber, string columnName)
    {
        var trimmed = value.Trim();
        if (TimeSpan.TryParseExact(trimmed, @"hh\:mm", CultureInfo.InvariantCulture, out var exactTime) ||
            TimeSpan.TryParse(trimmed, CultureInfo.InvariantCulture, out exactTime) ||
            TimeSpan.TryParse(trimmed, CultureInfo.CurrentCulture, out exactTime))
        {
            return exactTime;
        }

        throw new InvalidDataException($"CSV row {rowNumber} has an invalid {columnName}.");
    }

    private static int ParseInt(string value, int rowNumber, string columnName)
    {
        if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new InvalidDataException($"CSV row {rowNumber} has an invalid {columnName}.");
    }

    private static decimal ParseDecimal(string value, int rowNumber, string columnName)
    {
        if (decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new InvalidDataException($"CSV row {rowNumber} has an invalid {columnName}.");
    }

    private static DayType ParseDayType(string value, int rowNumber)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric) && Enum.IsDefined(typeof(DayType), numeric))
        {
            return (DayType)numeric;
        }

        return normalized switch
        {
            "regular" or "regular workday" or "workday" => DayType.Regular,
            "weekend" => DayType.Weekend,
            "holiday" or "public holiday" => DayType.Holiday,
            _ => throw new InvalidDataException($"CSV row {rowNumber} has an invalid day type.")
        };
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
