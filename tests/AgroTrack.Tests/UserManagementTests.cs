using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Security;
using AgroTrack.Services.Implementations;
using AgroTrack.Services.Interfaces;
using Xunit;

namespace AgroTrack.Tests;

public class UserManagementTests
{
    private class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = new();

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = Users.FirstOrDefault(u => u.UserId == userId);
            return Task.FromResult(user);
        }

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            var user = Users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(user);
        }

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(Users.AsReadOnly());
        }

        public Task<int> CreateAsync(User user, CancellationToken cancellationToken = default)
        {
            user.UserId = Users.Count + 1;
            Users.Add(user);
            return Task.FromResult(user.UserId);
        }

        public Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            var existing = Users.FirstOrDefault(u => u.UserId == user.UserId);
            if (existing != null)
            {
                existing.FullName = user.FullName;
                existing.Role = user.Role;
                existing.IsActive = user.IsActive;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> SetActiveStatusAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
        {
            var existing = Users.FirstOrDefault(u => u.UserId == userId);
            if (existing != null)
            {
                existing.IsActive = isActive;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> UpdatePasswordAsync(int userId, string passwordHash, CancellationToken cancellationToken = default)
        {
            var existing = Users.FirstOrDefault(u => u.UserId == userId);
            if (existing != null)
            {
                existing.PasswordHash = passwordHash;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }

    private class FakeSessionService : ISessionService
    {
        public CurrentUser? CurrentUser { get; set; }
        public bool IsAuthenticated => CurrentUser != null;
        public event EventHandler? SessionChanged;

        public void SetSession(CurrentUser user)
        {
            CurrentUser = user;
            SessionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ClearSession()
        {
            CurrentUser = null;
            SessionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private readonly FakeUserRepository _userRepo;
    private readonly PasswordHasher _passwordHasher;
    private readonly FakeSessionService _sessionService;
    private readonly UserService _userService;

    public UserManagementTests()
    {
        _userRepo = new FakeUserRepository();
        _passwordHasher = new PasswordHasher();
        _sessionService = new FakeSessionService();
        _userService = new UserService(_userRepo, _passwordHasher, _sessionService);

        // Seed initial admin user
        var admin = new User
        {
            UserId = 1,
            Username = "admin",
            FullName = "Administrator AgroTrack",
            Role = UserRole.Administrator,
            PasswordHash = _passwordHasher.HashPassword("Admin123!"),
            IsActive = true
        };
        _userRepo.Users.Add(admin);
        _sessionService.CurrentUser = new CurrentUser(1, "admin", "Administrator AgroTrack", UserRole.Administrator);
    }

    [Fact]
    public async Task CreateUser_NonAdmin_ShouldFailWithPermissionError()
    {
        // Arrange
        _sessionService.CurrentUser = new CurrentUser(2, "petugas1", "Petugas Lapangan", UserRole.FieldOfficer);

        // Act
        var result = await _userService.CreateUserAsync("budi", "Budi Santoso", UserRole.FieldOfficer, "Password123");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Hanya Administrator", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateUser_DuplicateUsername_ShouldFail()
    {
        // Act
        var result = await _userService.CreateUserAsync("ADMIN", "Admin Baru", UserRole.Administrator, "Password123");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("sudah digunakan", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateUser_ShortPassword_ShouldFail()
    {
        // Act
        var result = await _userService.CreateUserAsync("userbaru", "User Baru", UserRole.Manager, "123");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("minimal terdiri dari 6 karakter", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateUser_ValidInputs_ShouldSucceedWithHashedPassword()
    {
        // Act
        var result = await _userService.CreateUserAsync("manajer1", "Manajer Kebun", UserRole.Manager, "Rahasia123");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("manajer1", result.Value.Username);
        Assert.NotEqual("Rahasia123", result.Value.PasswordHash);
        Assert.True(_passwordHasher.VerifyPassword("Rahasia123", result.Value.PasswordHash));
    }

    [Fact]
    public async Task ToggleUserStatus_DeactivateSelf_ShouldFail()
    {
        // Act (Admin trying to deactivate Admin self)
        var result = await _userService.ToggleUserStatusAsync(1);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("tidak dapat menonaktifkan akun yang sedang aktif digunakan", result.ErrorMessage);
    }

    [Fact]
    public async Task ToggleUserStatus_OtherUser_ShouldToggleSuccessfully()
    {
        // Arrange
        var otherUser = new User
        {
            UserId = 2,
            Username = "staf1",
            FullName = "Staf Satu",
            Role = UserRole.FieldOfficer,
            IsActive = true
        };
        _userRepo.Users.Add(otherUser);

        // Act
        var result = await _userService.ToggleUserStatusAsync(2);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(otherUser.IsActive);
    }

    [Fact]
    public async Task ResetPassword_ValidPassword_ShouldUpdateHash()
    {
        // Arrange
        var otherUser = new User
        {
            UserId = 2,
            Username = "staf1",
            FullName = "Staf Satu",
            Role = UserRole.FieldOfficer,
            PasswordHash = _passwordHasher.HashPassword("OldPass123"),
            IsActive = true
        };
        _userRepo.Users.Add(otherUser);

        // Act
        var result = await _userService.ResetPasswordAsync(2, "NewSecurePass2026");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(_passwordHasher.VerifyPassword("NewSecurePass2026", otherUser.PasswordHash));
        Assert.False(_passwordHasher.VerifyPassword("OldPass123", otherUser.PasswordHash));
    }
}
