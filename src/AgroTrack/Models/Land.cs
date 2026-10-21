using AgroTrack.Enums;

namespace AgroTrack.Models;

public class Land
{
    public int LandId { get; set; }
    public string LandCode { get; set; } = string.Empty;
    public string LandName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal AreaHectares { get; set; }
    public string SoilType { get; set; } = string.Empty;
    public LandStatus Status { get; set; } = LandStatus.Active;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Aggregate property from SQL join
    public int ActivePlantingsCount { get; set; }

    public string StatusDisplay => Status.ToDisplayString();
    public string AreaDisplay => $"{AreaHectares:N2} ha";
}
