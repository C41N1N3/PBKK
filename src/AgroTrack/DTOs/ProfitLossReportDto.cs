using System.Globalization;
using AgroTrack.Enums;

namespace AgroTrack.DTOs;

public class ProfitLossReportDto
{
    public int PlantingId { get; set; }
    public string PlantingCode { get; set; } = string.Empty;
    public string SeasonName { get; set; } = string.Empty;
    public string LandName { get; set; } = string.Empty;
    public string CommodityName { get; set; } = string.Empty;
    public string ResultUnit { get; set; } = "kg";
    public PlantingStatus Status { get; set; }

    public decimal TargetYield { get; set; }
    public decimal ActualYield { get; set; }
    public decimal? TargetAchievementPercentage =>
        TargetYield > 0 ? Math.Round((ActualYield / TargetYield) * 100m, 1) : null;

    public decimal TotalMaterialCost { get; set; }
    public decimal TotalOperationalCost { get; set; }
    public decimal TotalCost => TotalMaterialCost + TotalOperationalCost;
    public decimal TotalRevenue { get; set; }
    public decimal NetProfit => TotalRevenue - TotalCost;

    public bool IsProfit => NetProfit >= 0;

    // Display Properties
    public string StatusDisplay => Status.ToDisplayString();
    public string TargetYieldDisplay => $"{TargetYield:N0} {ResultUnit}";
    public string ActualYieldDisplay => $"{ActualYield:N0} {ResultUnit}";
    public string TargetAchievementDisplay => TargetAchievementPercentage.HasValue ? $"{TargetAchievementPercentage.Value:N1}%" : "-";
    public string TotalMaterialCostDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", TotalMaterialCost);
    public string TotalOperationalCostDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", TotalOperationalCost);
    public string TotalCostDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", TotalCost);
    public string TotalRevenueDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", TotalRevenue);
    public string NetProfitDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", NetProfit);
}
