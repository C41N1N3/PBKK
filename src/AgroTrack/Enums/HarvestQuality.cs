namespace AgroTrack.Enums;

public enum HarvestQuality
{
    GradeA,
    GradeB,
    GradeC,
    Standard
}

public static class HarvestQualityExtensions
{
    public static string ToDisplayString(this HarvestQuality quality) => quality switch
    {
        HarvestQuality.GradeA => "Grade A (Super)",
        HarvestQuality.GradeB => "Grade B (Baik)",
        HarvestQuality.GradeC => "Grade C (Cukup)",
        HarvestQuality.Standard => "Standar",
        _ => quality.ToString()
    };
}
