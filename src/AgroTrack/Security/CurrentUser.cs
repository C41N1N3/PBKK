using AgroTrack.Enums;

namespace AgroTrack.Security;

public record CurrentUser(int UserId, string Username, string FullName, UserRole Role)
{
    public bool IsAdmin => Role == UserRole.Administrator;
    public bool IsManager => Role == UserRole.Manager;
    public bool IsFieldOfficer => Role == UserRole.FieldOfficer;
    public bool CanManageUsers => IsAdmin;
    public bool CanManageMasterData => IsAdmin || IsManager;
    public bool CanViewReports => IsAdmin || IsManager;
    public bool CanManageCosts => IsAdmin || IsManager;
    public string RoleDisplayName => Role.ToDisplayString();
}
