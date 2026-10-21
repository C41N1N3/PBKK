using Microsoft.Data.SqlClient;

namespace AgroTrack.Data;

public interface IDbConnectionFactory
{
    string ConnectionString { get; }
    SqlConnection CreateConnection();
    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
