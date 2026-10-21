using AgroTrack.Enums;

namespace AgroTrack.Models;

public class Planting
{
    public int PlantingId { get; set; }
    public string PlantingCode { get; set; } = string.Empty;
    public int LandId { get; set; }
    public int CommodityId { get; set; }
    public string SeasonName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime ExpectedHarvestDate { get; set; } = DateTime.Today.AddDays(90);
    public DateTime? CompletionDate { get; set; }
    public decimal PlantedAreaHectares { get; set; }
    public decimal TargetYield { get; set; }
    public PlantingStatus Status { get; set; } = PlantingStatus.Planned;
    public string? Notes { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Joined Fields
    public string LandName { get; set; } = string.Empty;
    public string LandCode { get; set; } = string.Empty;
    public decimal LandTotalAreaHectares { get; set; }
    public string CommodityName { get; set; } = string.Empty;
    public string ResultUnit { get; set; } = "kg";
    public string CreatorName { get; set; } = string.Empty;

    // Aggregated Metrics (from Harvests, OperationalCosts, and MaterialUsages)
    public decimal TotalActualYield { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalOperationalCost { get; set; }
    public decimal TotalMaterialCost { get; set; }
    public decimal TotalCost => TotalOperationalCost + TotalMaterialCost;
    public decimal NetProfit => TotalRevenue - TotalCost;

    public decimal? TargetAchievementPercentage =>
        TargetYield > 0 ? Math.Round((TotalActualYield / TargetYield) * 100m, 1) : null;

    public string StatusDisplay => Status.ToDisplayString();
    public string PeriodDisplay => $"{StartDate:dd MMM yyyy} - {ExpectedHarvestDate:dd MMM yyyy}";
    public string PlantedAreaDisplay => $"{PlantedAreaHectares:N2} ha";
    public string TargetYieldDisplay => $"{TargetYield:N0} {ResultUnit}";
    public string ActualYieldDisplay => $"{TotalActualYield:N0} {ResultUnit}";
}
