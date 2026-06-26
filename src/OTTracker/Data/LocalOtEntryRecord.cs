using OTTracker.Domain.Entities;
using SQLite;

namespace OTTracker.Data;

public sealed class LocalOtEntryRecord
{
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }

    public DateTime EntryDate { get; set; } = DateTime.Today;

    public int DayTypeIndex { get; set; }

    public string StartTimeString { get; set; } = "17:00:00";

    public string EndTimeString { get; set; } = "21:00:00";

    public int BreakMinutes { get; set; } = 30;

    public string Note { get; set; } = string.Empty;

    public double NetHours { get; set; }

    public double HourlyRate { get; set; }

    public double Multiplier { get; set; }

    public double EstimatedEarnings { get; set; }

    public DateTime CreateDate { get; set; } = DateTime.Now;

    public DateTime ReviseDate { get; set; } = DateTime.Now;

    public static LocalOtEntryRecord FromDomain(OtEntry entry) => new()
    {
        LocalId = entry.Id,
        EntryDate = entry.EntryDate.Date,
        DayTypeIndex = entry.DayTypeIndex,
        StartTimeString = entry.StartTimeString,
        EndTimeString = entry.EndTimeString,
        BreakMinutes = entry.BreakMinutes,
        Note = entry.Note,
        NetHours = (double)entry.NetHours,
        HourlyRate = (double)entry.HourlyRate,
        Multiplier = (double)entry.Multiplier,
        EstimatedEarnings = (double)entry.EstimatedEarnings,
        CreateDate = entry.CreateDate,
        ReviseDate = entry.ReviseDate
    };

    public OtEntry ToDomain() => new()
    {
        Id = LocalId,
        EntryDate = EntryDate.Date,
        DayTypeIndex = DayTypeIndex,
        StartTimeString = StartTimeString,
        EndTimeString = EndTimeString,
        BreakMinutes = BreakMinutes,
        Note = Note,
        NetHours = (decimal)NetHours,
        HourlyRate = (decimal)HourlyRate,
        Multiplier = (decimal)Multiplier,
        EstimatedEarnings = (decimal)EstimatedEarnings,
        CreateDate = CreateDate,
        ReviseDate = ReviseDate
    };
}
