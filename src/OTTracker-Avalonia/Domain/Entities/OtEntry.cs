using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using OTTracker_Avalonia.Domain.Enums;

namespace OTTracker_Avalonia.Domain.Entities;

[Table("ot_entries")]
public sealed class OtEntry : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("entry_date")]
    public DateTime EntryDate { get; set; } = DateTime.Today;

    [Column("day_type")]
    public int DayTypeIndex { get; set; }

    [Column("start_time")]
    public string StartTimeString { get; set; } = "17:00:00";

    [Column("end_time")]
    public string EndTimeString { get; set; } = "21:00:00";

    [Column("break_minutes")]
    public int BreakMinutes { get; set; } = 30;

    [Column("note")]
    public string Note { get; set; } = string.Empty;

    [Column("net_hours")]
    public decimal NetHours { get; set; }

    [Column("hourly_rate")]
    public decimal HourlyRate { get; set; }

    [Column("multiplier")]
    public decimal Multiplier { get; set; }

    [Column("estimated_earnings")]
    public decimal EstimatedEarnings { get; set; }

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.Now;

    [Column("revise_date")]
    public DateTime ReviseDate { get; set; } = DateTime.Now;

    // Helper property to map TimeSpan StartTime to StartTimeString
    [Newtonsoft.Json.JsonIgnore]
    public TimeSpan StartTime
    {
        get => TimeSpan.TryParse(StartTimeString, out var ts) ? ts : new TimeSpan(17, 0, 0);
        set => StartTimeString = value.ToString(@"hh\:mm\:ss");
    }

    // Helper property to map TimeSpan EndTime to EndTimeString
    [Newtonsoft.Json.JsonIgnore]
    public TimeSpan EndTime
    {
        get => TimeSpan.TryParse(EndTimeString, out var ts) ? ts : new TimeSpan(21, 0, 0);
        set => EndTimeString = value.ToString(@"hh\:mm\:ss");
    }

    // Helper property for DayType enum
    [Newtonsoft.Json.JsonIgnore]
    public DayType DayType
    {
        get => (DayType)DayTypeIndex;
        set => DayTypeIndex = (int)value;
    }

    [Newtonsoft.Json.JsonIgnore]
    public string DayLabel => DayType switch
    {
        DayType.Weekend => "Weekend",
        DayType.Holiday => "Holiday",
        _ => "Regular"
    };
}
