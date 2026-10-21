using AgroTrack.Security;
using AgroTrack.Services.Interfaces;

namespace AgroTrack.Services.Implementations;

public class SessionService : ISessionService
{
    private CurrentUser? _currentUser;

    public CurrentUser? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null;

    public event EventHandler? SessionChanged;

    public void SetSession(CurrentUser user)
    {
        _currentUser = user ?? throw new ArgumentNullException(nameof(user));
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearSession()
    {
        _currentUser = null;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
