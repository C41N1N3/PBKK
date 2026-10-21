using System.Globalization;
using AgroTrack.Enums;

namespace AgroTrack.Models;

public class Harvest
{
    public int HarvestId { get; set; }
    public int PlantingId { get; set; }
    public DateTime HarvestDate { get; set; } = DateTime.Today;
    public decimal YieldAmount { get; set; }
    public HarvestQuality Quality { get; set; } = HarvestQuality.GradeA;
    public decimal SellingPricePerUnit { get; set; }
    public decimal TotalRevenue => YieldAmount * SellingPricePerUnit;
    public string? Notes { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Joined Fields for Display
    public string PlantingCode { get; set; } = string.Empty;
    public string SeasonName { get; set; } = string.Empty;
    public string CommodityName { get; set; } = string.Empty;
    public string ResultUnit { get; set; } = "kg";
    public string LandName { get; set; } = string.Empty;
    public string CreatorName { get; set; } = string.Empty;

    // Display Formats
    public string QualityDisplay => Quality.ToDisplayString();
    public string HarvestDateDisplay => HarvestDate.ToString("dd MMM yyyy");
    public string YieldDisplay => $"{YieldAmount:N0} {ResultUnit}";
    public string PricePerUnitDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}/{1}", SellingPricePerUnit, ResultUnit);
    public string TotalRevenueDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", TotalRevenue);
}
