using System.Globalization;
using AgroTrack.Enums;

namespace AgroTrack.DTOs;

public class DashboardSummaryDto
{
    public int TotalActiveLands { get; set; }
    public decimal TotalLandAreaHectares { get; set; }
    public int ActivePlantingsCount { get; set; }
    public int TodayActivitiesCount { get; set; }
    public int LowStockMaterialsCount { get; set; }

    public decimal TotalHarvestYield { get; set; }
    public decimal TotalOperationalCost { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal EstimatedNetProfit => TotalRevenue - TotalOperationalCost;

    // Display Formatted Properties
    public string TotalLandAreaDisplay => $"{TotalLandAreaHectares:N2} ha";
    public string TotalHarvestYieldDisplay => $"{TotalHarvestYield:N0} kg";
    public string TotalOperationalCostDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", TotalOperationalCost);
    public string TotalRevenueDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", TotalRevenue);
    public string EstimatedNetProfitDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", EstimatedNetProfit);
}

public class TargetVsActualDto
{
    public int PlantingId { get; set; }
    public string PlantingCode { get; set; } = string.Empty;
    public string SeasonName { get; set; } = string.Empty;
    public string CommodityName { get; set; } = string.Empty;
    public string ResultUnit { get; set; } = "kg";
    public decimal TargetYield { get; set; }
    public decimal ActualYield { get; set; }

    public decimal TargetPercentage =>
        TargetYield > 0 ? Math.Min(100m, Math.Round((ActualYield / TargetYield) * 100m, 1)) : 0m;

    public string TargetDisplay => $"{TargetYield:N0} {ResultUnit}";
    public string ActualDisplay => $"{ActualYield:N0} {ResultUnit}";
    public string PercentageDisplay => TargetYield > 0 ? $"{Math.Round((ActualYield / TargetYield) * 100m, 1)}%" : "-";
}

public class MonthlyHarvestDto
{
    public string MonthYear { get; set; } = string.Empty; // e.g. "2026-04"
    public string MonthLabel { get; set; } = string.Empty; // e.g. "Apr 2026"
    public decimal TotalYield { get; set; }
    public decimal TotalRevenue { get; set; }

    // Proportional bar height calculation relative to max month
    public double BarHeightPercentage { get; set; }

    public string YieldDisplay => $"{TotalYield:N0} kg";
    public string RevenueDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", TotalRevenue);
}

public class CategoryCostDto
{
    public CostCategory Category { get; set; }
    public string CategoryName => Category.ToDisplayString();
    public decimal TotalAmount { get; set; }
    public decimal PercentageOfTotal { get; set; }

    public string AmountDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", TotalAmount);
    public string PercentageDisplay => $"{PercentageOfTotal:N1}%";
}

public class CommodityDistributionDto
{
    public string CommodityName { get; set; } = string.Empty;
    public int PlantingsCount { get; set; }
    public decimal TotalAreaHectares { get; set; }
    public decimal PercentageOfTotal { get; set; }

    public string TotalAreaDisplay => $"{TotalAreaHectares:N2} ha";
    public string PercentageDisplay => $"{PercentageOfTotal:N1}%";
}

public class UpcomingHarvestDto
{
    public int PlantingId { get; set; }
    public string PlantingCode { get; set; } = string.Empty;
    public string SeasonName { get; set; } = string.Empty;
    public string CommodityName { get; set; } = string.Empty;
    public string LandName { get; set; } = string.Empty;
    public DateTime ExpectedHarvestDate { get; set; }
    public int DaysRemaining => (ExpectedHarvestDate.Date - DateTime.Today).Days;

    public string ExpectedDateDisplay => ExpectedHarvestDate.ToString("dd MMM yyyy");
    public string DaysRemainingDisplay => DaysRemaining switch
    {
        < 0 => $"Terlambat {Math.Abs(DaysRemaining)} hari",
        0 => "Hari ini!",
        _ => $"{DaysRemaining} hari lagi"
    };
}
