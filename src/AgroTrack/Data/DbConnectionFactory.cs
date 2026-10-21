using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AgroTrack.Data;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration? configuration = null)
    {
        var envConn = Environment.GetEnvironmentVariable("AGROTRACK_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(envConn))
        {
            _connectionString = envConn;
            return;
        }

        var configConn = configuration?.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(configConn))
        {
            _connectionString = configConn;
            return;
        }

        throw new InvalidOperationException("Connection string 'DefaultConnection' tidak ditemukan. Harap konfigurasikan connection string di appsettings.json, appsettings.local.json, atau environment variable 'AGROTRACK_CONNECTION_STRING'.");
    }

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = !string.IsNullOrWhiteSpace(connectionString)
            ? connectionString
            : throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));
    }

    public string ConnectionString => _connectionString;

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
