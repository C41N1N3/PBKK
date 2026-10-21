using AgroTrack.Enums;

namespace AgroTrack.Models;

public class FarmingActivity
{
    public int ActivityId { get; set; }
    public int PlantingId { get; set; }
    public ActivityType ActivityType { get; set; } = ActivityType.Inspection;
    public DateTime ActivityDate { get; set; } = DateTime.Today;
    public string Description { get; set; } = string.Empty;
    public int OfficerId { get; set; }
    public decimal? DurationHours { get; set; }
    public ActivityStatus Status { get; set; } = ActivityStatus.Scheduled;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Joined Fields for Display
    public string PlantingCode { get; set; } = string.Empty;
    public string SeasonName { get; set; } = string.Empty;
    public string CommodityName { get; set; } = string.Empty;
    public string LandName { get; set; } = string.Empty;
    public string OfficerName { get; set; } = string.Empty;

    // Computed / Display Properties
    public string ActivityTypeDisplay => ActivityType.ToDisplayString();
    public string StatusDisplay => Status.ToDisplayString();
    public string ActivityDateDisplay => ActivityDate.ToString("dd MMM yyyy");
    public string DurationDisplay => DurationHours.HasValue ? $"{DurationHours.Value:N1} Jam" : "-";
}
