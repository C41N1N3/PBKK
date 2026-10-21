namespace AgroTrack.Enums;

public enum ActivityType
{
    LandPreparation,
    Planting,
    Watering,
    Fertilizing,
    PestControl,
    Weeding,
    Inspection,
    Harvesting,
    Other
}

public static class ActivityTypeExtensions
{
    public static string ToDisplayString(this ActivityType type) => type switch
    {
        ActivityType.LandPreparation => "Persiapan lahan",
        ActivityType.Planting => "Penanaman",
        ActivityType.Watering => "Penyiraman",
        ActivityType.Fertilizing => "Pemupukan",
        ActivityType.PestControl => "Pengendalian hama",
        ActivityType.Weeding => "Penyiangan",
        ActivityType.Inspection => "Pemeriksaan",
        ActivityType.Harvesting => "Panen",
        ActivityType.Other => "Lainnya",
        _ => type.ToString()
    };
}
