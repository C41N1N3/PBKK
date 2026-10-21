using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Utilities;

namespace AgroTrack.Services.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<Result<User>> CreateUserAsync(string username, string fullName, UserRole role, string password, CancellationToken cancellationToken = default);
    Task<Result> UpdateUserAsync(int userId, string fullName, UserRole role, CancellationToken cancellationToken = default);
    Task<Result> ToggleUserStatusAsync(int userId, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(int userId, string newPassword, CancellationToken cancellationToken = default);
}
