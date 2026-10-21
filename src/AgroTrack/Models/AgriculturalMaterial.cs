using System.Globalization;
using AgroTrack.Enums;

namespace AgroTrack.Models;

public class AgriculturalMaterial
{
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public MaterialCategory Category { get; set; } = MaterialCategory.Fertilizer;
    public string Unit { get; set; } = "kg";
    public decimal Stock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Computed / Display Properties
    public bool IsLowStock => Stock <= MinimumStock;
    public string CategoryDisplay => Category.ToDisplayString();
    public string StockDisplay => $"{Stock:N2} {Unit}";
    public string MinimumStockDisplay => $"{MinimumStock:N2} {Unit}";
    public string UnitPriceDisplay => string.Format(CultureInfo.GetCultureInfo("id-ID"), "Rp {0:N0}/{1}", UnitPrice, Unit);
    public string StatusDisplay => IsActive ? "Aktif" : "Nonaktif";
}
