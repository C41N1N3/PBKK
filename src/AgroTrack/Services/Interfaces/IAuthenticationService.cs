using AgroTrack.Security;
using AgroTrack.Utilities;

namespace AgroTrack.Services.Interfaces;

public interface IAuthenticationService
{
    Task<Result<CurrentUser>> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    void Logout();
}
