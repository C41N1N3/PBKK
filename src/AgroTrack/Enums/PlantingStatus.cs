namespace AgroTrack.Enums;

public enum PlantingStatus
{
    Planned,
    Active,
    ReadyToHarvest,
    Completed,
    Cancelled
}

public static class PlantingStatusExtensions
{
    public static string ToDisplayString(this PlantingStatus status) => status switch
    {
        PlantingStatus.Planned => "Direncanakan",
        PlantingStatus.Active => "Aktif",
        PlantingStatus.ReadyToHarvest => "Siap Panen",
        PlantingStatus.Completed => "Selesai",
        PlantingStatus.Cancelled => "Dibatalkan",
        _ => status.ToString()
    };
}
