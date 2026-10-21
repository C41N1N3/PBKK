namespace AgroTrack.Models;

public class Commodity
{
    public int CommodityId { get; set; }
    public string CommodityCode { get; set; } = string.Empty;
    public string CommodityName { get; set; } = string.Empty;
    public string ResultUnit { get; set; } = "kg";
    public int EstimatedGrowthDays { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Aggregate property from SQL join
    public int TotalPlantingsCount { get; set; }

    public string StatusDisplay => IsActive ? "Aktif" : "Nonaktif";
    public string GrowthDaysDisplay => $"{EstimatedGrowthDays} hari";
}
