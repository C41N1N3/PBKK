using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Security;
using AgroTrack.Services.Implementations;
using Xunit;

namespace AgroTrack.Tests;

public class MockUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public MockUserRepository()
    {
        var hasher = new PasswordHasher();
        _users.Add(new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = hasher.HashPassword("AdminPassword@123"),
            FullName = "Administrator Sistem",
            Role = UserRole.Administrator,
            IsActive = true
        });
        _users.Add(new User
        {
            UserId = 2,
            Username = "manager",
            PasswordHash = hasher.HashPassword("ManagerPassword@123"),
            FullName = "Manajer Kebun",
            Role = UserRole.Manager,
            IsActive = true
        });
        _users.Add(new User
        {
            UserId = 3,
            Username = "petugas",
            PasswordHash = hasher.HashPassword("PetugasPassword@123"),
            FullName = "Petugas Lapangan",
            Role = UserRole.FieldOfficer,
            IsActive = true
        });
        _users.Add(new User
        {
            UserId = 4,
            Username = "nonaktif",
            PasswordHash = hasher.HashPassword("UserPassword@123"),
            FullName = "User Nonaktif",
            Role = UserRole.FieldOfficer,
            IsActive = false
        });
    }

    public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.UserId == userId));

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<User>>(_users.AsReadOnly());

    public Task<int> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        user.UserId = _users.Count + 1;
        _users.Add(user);
        return Task.FromResult(user.UserId);
    }

    public Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var existing = _users.FirstOrDefault(u => u.UserId == user.UserId);
        if (existing == null) return Task.FromResult(false);
        existing.FullName = user.FullName;
        existing.Role = user.Role;
        existing.IsActive = user.IsActive;
        return Task.FromResult(true);
    }

    public Task<bool> SetActiveStatusAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var existing = _users.FirstOrDefault(u => u.UserId == userId);
        if (existing == null) return Task.FromResult(false);
        existing.IsActive = isActive;
        return Task.FromResult(true);
    }

    public Task<bool> UpdatePasswordAsync(int userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        var existing = _users.FirstOrDefault(u => u.UserId == userId);
        if (existing == null) return Task.FromResult(false);
        existing.PasswordHash = passwordHash;
        return Task.FromResult(true);
    }
}

public class AuthenticationTests
{
    private readonly MockUserRepository _userRepo = new();
    private readonly PasswordHasher _hasher = new();
    private readonly SessionService _sessionService = new();

    private AuthenticationService CreateAuthService() =>
        new(_userRepo, _hasher, _sessionService);

    [Fact]
    public async Task LoginAsync_ValidCredentials_SetsSessionAndReturnsSuccess()
    {
        var authService = CreateAuthService();

        var result = await authService.LoginAsync("admin", "AdminPassword@123");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("admin", result.Value.Username);
        Assert.Equal(UserRole.Administrator, result.Value.Role);
        Assert.True(_sessionService.IsAuthenticated);
        Assert.Equal(result.Value, _sessionService.CurrentUser);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_FailsWithGenericMessage()
    {
        var authService = CreateAuthService();

        var result = await authService.LoginAsync("admin", "SalahPassword123");

        Assert.False(result.IsSuccess);
        Assert.Equal("Nama pengguna atau kata sandi tidak valid.", result.ErrorMessage);
        Assert.False(_sessionService.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsync_NonExistentUsername_FailsWithGenericMessage()
    {
        var authService = CreateAuthService();

        var result = await authService.LoginAsync("tidak_ada_user", "AnyPassword123");

        Assert.False(result.IsSuccess);
        Assert.Equal("Nama pengguna atau kata sandi tidak valid.", result.ErrorMessage);
        Assert.False(_sessionService.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsync_InactiveAccount_FailsWithDeactivatedMessage()
    {
        var authService = CreateAuthService();

        var result = await authService.LoginAsync("nonaktif", "UserPassword@123");

        Assert.False(result.IsSuccess);
        Assert.Contains("dinonaktifkan", result.ErrorMessage);
        Assert.False(_sessionService.IsAuthenticated);
    }

    [Fact]
    public void Logout_ClearsSessionSuccessfully()
    {
        var authService = CreateAuthService();
        _sessionService.SetSession(new CurrentUser(1, "admin", "Admin", UserRole.Administrator));
        Assert.True(_sessionService.IsAuthenticated);

        authService.Logout();

        Assert.False(_sessionService.IsAuthenticated);
        Assert.Null(_sessionService.CurrentUser);
    }

    [Theory]
    [InlineData(UserRole.Administrator, true, false, false, true, true)]
    [InlineData(UserRole.Manager, false, true, false, true, true)]
    [InlineData(UserRole.FieldOfficer, false, false, true, false, false)]
    public void RolePermissions_EvaluatesCorrectly(
        UserRole role, bool expectedAdmin, bool expectedManager, bool expectedFieldOfficer,
        bool expectedCanManageMaster, bool expectedCanViewReports)
    {
        var user = new CurrentUser(1, "test", "Test User", role);

        Assert.Equal(expectedAdmin, user.IsAdmin);
        Assert.Equal(expectedManager, user.IsManager);
        Assert.Equal(expectedFieldOfficer, user.IsFieldOfficer);
        Assert.Equal(expectedCanManageMaster, user.CanManageMasterData);
        Assert.Equal(expectedCanViewReports, user.CanViewReports);
    }
}
