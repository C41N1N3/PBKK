namespace AgroTrack.Enums;

public enum CostCategory
{
    Labor,
    ToolRental,
    Transportation,
    Irrigation,
    Maintenance,
    Fuel,
    Other
}

public static class CostCategoryExtensions
{
    public static string ToDisplayString(this CostCategory category) => category switch
    {
        CostCategory.Labor => "Tenaga kerja",
        CostCategory.ToolRental => "Sewa alat",
        CostCategory.Transportation => "Transportasi",
        CostCategory.Irrigation => "Irigasi",
        CostCategory.Maintenance => "Perawatan",
        CostCategory.Fuel => "Bahan bakar",
        CostCategory.Other => "Lainnya",
        _ => category.ToString()
    };
}
