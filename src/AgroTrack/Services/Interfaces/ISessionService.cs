using AgroTrack.Security;

namespace AgroTrack.Services.Interfaces;

public interface ISessionService
{
    CurrentUser? CurrentUser { get; }
    bool IsAuthenticated { get; }
    event EventHandler? SessionChanged;

    void SetSession(CurrentUser user);
    void ClearSession();
}
