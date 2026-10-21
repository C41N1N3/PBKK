namespace AgroTrack.Enums;

public enum AppTheme
{
    Light,
    Dark
}

public enum LandStatus
{
    Active,
    Resting,
    Maintenance,
    Inactive
}

public static class LandStatusExtensions
{
    public static string ToDisplayString(this LandStatus status) => status switch
    {
        LandStatus.Active => "Aktif",
        LandStatus.Resting => "Bera / Istirahat",
        LandStatus.Maintenance => "Perawatan",
        LandStatus.Inactive => "Nonaktif",
        _ => status.ToString()
    };
}
