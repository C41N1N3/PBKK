using AgroTrack.Repositories.Interfaces;
using AgroTrack.Security;
using AgroTrack.Services.Interfaces;
using AgroTrack.Utilities;
using Microsoft.Data.SqlClient;

namespace AgroTrack.Services.Implementations;

public class AuthenticationService : IAuthenticationService
{
    private const string GenericErrorMessage = "Nama pengguna atau kata sandi tidak valid.";
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISessionService _sessionService;

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ISessionService sessionService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _sessionService = sessionService;
    }

    public async Task<Result<CurrentUser>> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Result<CurrentUser>.Failure("Nama pengguna dan kata sandi wajib diisi.");
        }

        try
        {
            var trimmedUsername = username.Trim();
            var user = await _userRepository.GetByUsernameAsync(trimmedUsername, cancellationToken);

            // Generic error message prevents username enumeration attack
            if (user is null)
            {
                return Result<CurrentUser>.Failure(GenericErrorMessage);
            }

            if (!user.IsActive)
            {
                return Result<CurrentUser>.Failure("Akun Anda telah dinonaktifkan. Silakan hubungi Administrator.");
            }

            var isPasswordValid = _passwordHasher.VerifyPassword(password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return Result<CurrentUser>.Failure(GenericErrorMessage);
            }

            var currentUser = new CurrentUser(user.UserId, user.Username, user.FullName, user.Role);
            _sessionService.SetSession(currentUser);

            return Result<CurrentUser>.Success(currentUser);
        }
        catch (SqlException)
        {
            return Result<CurrentUser>.Failure("Gagal terhubung ke basis data. Pastikan layanan SQL Server aktif dan coba lagi.");
        }
        catch (Exception ex)
        {
            return Result<CurrentUser>.Failure($"Terjadi kesalahan saat proses masuk: {ex.Message}");
        }
    }

    public void Logout()
    {
        _sessionService.ClearSession();
    }
}
