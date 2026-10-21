using System.Globalization;

namespace AgroTrack.Models;

public class MaterialUsage
{
    public int MaterialUsageId { get; set; }
    public int ActivityId { get; set; }
    public int MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public decimal PriceAtUsage { get; set; }
    public decimal Subtotal => Quantity * PriceAtUsage;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedBy { get; set; }

    // Joined Fields for Display
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = "kg";
    public string PlantingCode { get; set; } = string.Empty;
    public string SeasonName { get; set; } = string.Empty;
    public string ActivityDescription { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;

    // Display Properties
    public string QuantityDisplay => $"{Quantity:N2} {Unit}";
    public string PriceAtUsageDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}/{1}", PriceAtUsage, Unit);
    public string SubtotalDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}", Subtotal);
    public string DateDisplay => CreatedAt.ToString("dd MMM yyyy HH:mm");
}
