namespace AgroTrack.Enums;

public enum ActivityStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}

public static class ActivityStatusExtensions
{
    public static string ToDisplayString(this ActivityStatus status) => status switch
    {
        ActivityStatus.Scheduled => "Dijadwalkan",
        ActivityStatus.InProgress => "Sedang Berjalan",
        ActivityStatus.Completed => "Selesai",
        ActivityStatus.Cancelled => "Dibatalkan",
        _ => status.ToString()
    };
}
