namespace AgroTrack.Enums;

public enum MaterialCategory
{
    Seed,
    Fertilizer,
    Pesticide,
    Herbicide,
    Fungicide,
    ConsumableTool,
    Other
}

public static class MaterialCategoryExtensions
{
    public static string ToDisplayString(this MaterialCategory category) => category switch
    {
        MaterialCategory.Seed => "Benih",
        MaterialCategory.Fertilizer => "Pupuk",
        MaterialCategory.Pesticide => "Pestisida",
        MaterialCategory.Herbicide => "Herbisida",
        MaterialCategory.Fungicide => "Fungisida",
        MaterialCategory.ConsumableTool => "Peralatan habis pakai",
        MaterialCategory.Other => "Lainnya",
        _ => category.ToString()
    };
}
