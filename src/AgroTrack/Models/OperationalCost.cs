using System.Globalization;
using AgroTrack.Enums;

namespace AgroTrack.Models;

public class OperationalCost
{
    public int CostId { get; set; }
    public int PlantingId { get; set; }
    public DateTime CostDate { get; set; } = DateTime.Today;
    public CostCategory Category { get; set; } = CostCategory.Labor;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Joined Fields for Display
    public string PlantingCode { get; set; } = string.Empty;
    public string SeasonName { get; set; } = string.Empty;
    public string CommodityName { get; set; } = string.Empty;
    public string LandName { get; set; } = string.Empty;
    public string CreatorName { get; set; } = string.Empty;

    // Display Formats
    public string CategoryDisplay => Category.ToDisplayString();
    public string CostDateDisplay => CostDate.ToString("dd MMM yyyy");
    public string AmountDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", Amount);
}
