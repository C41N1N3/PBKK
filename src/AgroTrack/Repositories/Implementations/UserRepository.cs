using System.Data;
using AgroTrack.Data;
using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace AgroTrack.Repositories.Implementations;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT UserId, Username, PasswordHash, FullName, Role, IsActive, CreatedAt, UpdatedAt
            FROM dbo.Users
            WHERE UserId = @UserId;";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapUser(reader);
        }

        return null;
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT UserId, Username, PasswordHash, FullName, Role, IsActive, CreatedAt, UpdatedAt
            FROM dbo.Users
            WHERE LOWER(Username) = LOWER(@Username);";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 50) { Value = username.Trim() });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapUser(reader);
        }

        return null;
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT UserId, Username, PasswordHash, FullName, Role, IsActive, CreatedAt, UpdatedAt
            FROM dbo.Users
            ORDER BY FullName ASC;";

        var users = new List<User>();
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(MapUser(reader));
        }

        return users.AsReadOnly();
    }

    public async Task<int> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO dbo.Users (Username, PasswordHash, FullName, Role, IsActive, CreatedAt)
            OUTPUT INSERTED.UserId
            VALUES (@Username, @PasswordHash, @FullName, @Role, @IsActive, SYSUTCDATETIME());";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 50) { Value = user.Username.Trim() });
        command.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 256) { Value = user.PasswordHash });
        command.Parameters.Add(new SqlParameter("@FullName", SqlDbType.NVarChar, 100) { Value = user.FullName.Trim() });
        command.Parameters.Add(new SqlParameter("@Role", SqlDbType.NVarChar, 30) { Value = user.Role.ToString() });
        command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = user.IsActive });

        var newId = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
        user.UserId = newId;
        return newId;
    }

    public async Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE dbo.Users
            SET FullName = @FullName,
                Role = @Role,
                IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME()
            WHERE UserId = @UserId;";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = user.UserId });
        command.Parameters.Add(new SqlParameter("@FullName", SqlDbType.NVarChar, 100) { Value = user.FullName.Trim() });
        command.Parameters.Add(new SqlParameter("@Role", SqlDbType.NVarChar, 30) { Value = user.Role.ToString() });
        command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = user.IsActive });

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> SetActiveStatusAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE dbo.Users
            SET IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME()
            WHERE UserId = @UserId;";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
        command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = isActive });

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> UpdatePasswordAsync(int userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE dbo.Users
            SET PasswordHash = @PasswordHash,
                UpdatedAt = SYSUTCDATETIME()
            WHERE UserId = @UserId;";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
        command.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 256) { Value = passwordHash });

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    private static User MapUser(SqlDataReader reader)
    {
        var roleStr = reader.GetString(reader.GetOrdinal("Role"));
        _ = Enum.TryParse<UserRole>(roleStr, true, out var role);

        return new User
        {
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            Role = role,
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        };
    }
}
