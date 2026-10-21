namespace AgroTrack.Enums;

public enum UserRole
{
    Administrator,
    Manager,
    FieldOfficer
}

public static class UserRoleExtensions
{
    public static string ToDisplayString(this UserRole role) => role switch
    {
        UserRole.Administrator => "Administrator",
        UserRole.Manager => "Manajer",
        UserRole.FieldOfficer => "Petugas Lapangan",
        _ => role.ToString()
    };
}
