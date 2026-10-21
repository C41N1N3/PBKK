using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Security;
using AgroTrack.Services.Interfaces;
using AgroTrack.Utilities;

namespace AgroTrack.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISessionService _sessionService;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ISessionService sessionService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _sessionService = sessionService;
    }

    private string? CheckAdminPermission()
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser == null || currentUser.Role != UserRole.Administrator)
        {
            return "Hanya Administrator yang memiliki izin mengelola data pengguna.";
        }
        return null;
    }

    public async Task<IReadOnlyList<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var perm = CheckAdminPermission();
        if (perm != null)
        {
            return Array.Empty<User>();
        }

        return await _userRepository.GetAllAsync(cancellationToken);
    }

    public async Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var perm = CheckAdminPermission();
        if (perm != null) return null;

        return await _userRepository.GetByIdAsync(userId, cancellationToken);
    }

    public async Task<Result<User>> CreateUserAsync(
        string username,
        string fullName,
        UserRole role,
        string password,
        CancellationToken cancellationToken = default)
    {
        var perm = CheckAdminPermission();
        if (perm != null) return Result<User>.Failure(perm);

        var trimmedUsername = username?.Trim();
        var trimmedFullName = fullName?.Trim();

        if (string.IsNullOrWhiteSpace(trimmedUsername))
        {
            return Result<User>.Failure("Username wajib diisi.");
        }

        if (trimmedUsername.Length < 3 || trimmedUsername.Length > 50)
        {
            return Result<User>.Failure("Username harus memiliki panjang 3 hingga 50 karakter.");
        }

        if (string.IsNullOrWhiteSpace(trimmedFullName))
        {
            return Result<User>.Failure("Nama lengkap wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            return Result<User>.Failure("Password minimal terdiri dari 6 karakter.");
        }

        var existingUser = await _userRepository.GetByUsernameAsync(trimmedUsername, cancellationToken);
        if (existingUser != null)
        {
            return Result<User>.Failure($"Username '{trimmedUsername}' sudah digunakan oleh pengguna lain.");
        }

        var passwordHash = _passwordHasher.HashPassword(password);
        var newUser = new User
        {
            Username = trimmedUsername,
            FullName = trimmedFullName,
            Role = role,
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var newId = await _userRepository.CreateAsync(newUser, cancellationToken);
        newUser.UserId = newId;
        return Result<User>.Success(newUser);
    }

    public async Task<Result> UpdateUserAsync(
        int userId,
        string fullName,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var perm = CheckAdminPermission();
        if (perm != null) return Result.Failure(perm);

        var trimmedFullName = fullName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedFullName))
        {
            return Result.Failure("Nama lengkap wajib diisi.");
        }

        var existingUser = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (existingUser == null)
        {
            return Result.Failure("Pengguna tidak ditemukan.");
        }

        // If self, cannot demote self from Administrator
        if (_sessionService.CurrentUser?.UserId == userId && role != UserRole.Administrator)
        {
            return Result.Failure("Anda tidak dapat mengubah role akun Administrator Anda sendiri.");
        }

        existingUser.FullName = trimmedFullName;
        existingUser.Role = role;

        var success = await _userRepository.UpdateAsync(existingUser, cancellationToken);
        return success ? Result.Success() : Result.Failure("Gagal memperbarui data pengguna di database.");
    }

    public async Task<Result> ToggleUserStatusAsync(int userId, CancellationToken cancellationToken = default)
    {
        var perm = CheckAdminPermission();
        if (perm != null) return Result.Failure(perm);

        var existingUser = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (existingUser == null)
        {
            return Result.Failure("Pengguna tidak ditemukan.");
        }

        // Cannot deactivate self
        if (_sessionService.CurrentUser?.UserId == userId)
        {
            return Result.Failure("Anda tidak dapat menonaktifkan akun yang sedang aktif digunakan.");
        }

        var newStatus = !existingUser.IsActive;
        var success = await _userRepository.SetActiveStatusAsync(userId, newStatus, cancellationToken);
        return success ? Result.Success() : Result.Failure("Gagal mengubah status pengguna di database.");
    }

    public async Task<Result> ResetPasswordAsync(int userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var perm = CheckAdminPermission();
        if (perm != null) return Result.Failure(perm);

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            return Result.Failure("Password baru minimal terdiri dari 6 karakter.");
        }

        var existingUser = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (existingUser == null)
        {
            return Result.Failure("Pengguna tidak ditemukan.");
        }

        var newHash = _passwordHasher.HashPassword(newPassword);
        var success = await _userRepository.UpdatePasswordAsync(userId, newHash, cancellationToken);
        return success ? Result.Success() : Result.Failure("Gagal memperbarui kata sandi di database.");
    }
}
