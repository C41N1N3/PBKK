using System.Data;
using System.Text;
using AgroTrack.Data;
using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Utilities;
using Microsoft.Data.SqlClient;

namespace AgroTrack.Repositories.Implementations;

public class LandRepository : ILandRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LandRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Land?> GetByIdAsync(int landId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT l.LandId, l.LandCode, l.LandName, l.Location, l.AreaHectares, l.SoilType, 
                   l.Status, l.Description, l.CreatedAt, l.UpdatedAt,
                   (SELECT COUNT(*) FROM dbo.Plantings p WHERE p.LandId = l.LandId AND p.Status IN ('Planned', 'Active', 'ReadyToHarvest')) AS ActivePlantingsCount
            FROM dbo.Lands l
            WHERE l.LandId = @LandId;";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@LandId", SqlDbType.Int) { Value = landId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapLand(reader);
        }

        return null;
    }

    public async Task<Land?> GetByCodeAsync(string landCode, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT l.LandId, l.LandCode, l.LandName, l.Location, l.AreaHectares, l.SoilType, 
                   l.Status, l.Description, l.CreatedAt, l.UpdatedAt,
                   (SELECT COUNT(*) FROM dbo.Plantings p WHERE p.LandId = l.LandId AND p.Status IN ('Planned', 'Active', 'ReadyToHarvest')) AS ActivePlantingsCount
            FROM dbo.Lands l
            WHERE LOWER(l.LandCode) = LOWER(@LandCode);";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@LandCode", SqlDbType.NVarChar, 30) { Value = landCode.Trim() });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapLand(reader);
        }

        return null;
    }

    public async Task<PagedResult<Land>> GetPagedAsync(
        string? searchTerm, 
        LandStatus? statusFilter, 
        string? soilTypeFilter, 
        int pageIndex, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var whereClauses = new List<string>();
        var parameters = new List<SqlParameter>();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            whereClauses.Add("(l.LandCode LIKE @Search OR l.LandName LIKE @Search OR l.Location LIKE @Search)");
            parameters.Add(new SqlParameter("@Search", SqlDbType.NVarChar, 100) { Value = $"%{searchTerm.Trim()}%" });
        }

        if (statusFilter.HasValue)
        {
            whereClauses.Add("l.Status = @Status");
            parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = statusFilter.Value.ToString() });
        }

        if (!string.IsNullOrWhiteSpace(soilTypeFilter) && soilTypeFilter != "Semua")
        {
            whereClauses.Add("l.SoilType = @SoilType");
            parameters.Add(new SqlParameter("@SoilType", SqlDbType.NVarChar, 50) { Value = soilTypeFilter.Trim() });
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : string.Empty;
        var offset = (Math.Max(1, pageIndex) - 1) * pageSize;

        var sql = $@"
            SELECT l.LandId, l.LandCode, l.LandName, l.Location, l.AreaHectares, l.SoilType, 
                   l.Status, l.Description, l.CreatedAt, l.UpdatedAt,
                   (SELECT COUNT(*) FROM dbo.Plantings p WHERE p.LandId = l.LandId AND p.Status IN ('Planned', 'Active', 'ReadyToHarvest')) AS ActivePlantingsCount,
                   COUNT(*) OVER() AS TotalCount
            FROM dbo.Lands l
            {whereSql}
            ORDER BY l.LandName ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        parameters.Add(new SqlParameter("@Offset", SqlDbType.Int) { Value = offset });
        parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = pageSize });

        var items = new List<Land>();
        var totalCount = 0;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters.ToArray());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapLand(reader));
            totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
        }

        return new PagedResult<Land>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<IReadOnlyList<Land>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT l.LandId, l.LandCode, l.LandName, l.Location, l.AreaHectares, l.SoilType, 
                   l.Status, l.Description, l.CreatedAt, l.UpdatedAt,
                   (SELECT COUNT(*) FROM dbo.Plantings p WHERE p.LandId = l.LandId AND p.Status IN ('Planned', 'Active', 'ReadyToHarvest')) AS ActivePlantingsCount
            FROM dbo.Lands l
            WHERE l.Status = 'Active'
            ORDER BY l.LandName ASC;";

        var lands = new List<Land>();
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            lands.Add(MapLand(reader));
        }

        return lands.AsReadOnly();
    }

    public async Task<IReadOnlyList<string>> GetDistinctSoilTypesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT DISTINCT SoilType 
            FROM dbo.Lands 
            WHERE SoilType IS NOT NULL AND SoilType <> '' 
            ORDER BY SoilType ASC;";

        var soils = new List<string>();
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            soils.Add(reader.GetString(0));
        }

        return soils.AsReadOnly();
    }

    public async Task<int> CreateAsync(Land land, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO dbo.Lands (LandCode, LandName, Location, AreaHectares, SoilType, Status, Description, CreatedAt)
            OUTPUT INSERTED.LandId
            VALUES (@LandCode, @LandName, @Location, @AreaHectares, @SoilType, @Status, @Description, SYSUTCDATETIME());";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add(new SqlParameter("@LandCode", SqlDbType.NVarChar, 30) { Value = land.LandCode.Trim() });
        command.Parameters.Add(new SqlParameter("@LandName", SqlDbType.NVarChar, 100) { Value = land.LandName.Trim() });
        command.Parameters.Add(new SqlParameter("@Location", SqlDbType.NVarChar, 200) { Value = land.Location.Trim() });
        command.Parameters.Add(new SqlParameter("@AreaHectares", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = land.AreaHectares });
        command.Parameters.Add(new SqlParameter("@SoilType", SqlDbType.NVarChar, 50) { Value = land.SoilType.Trim() });
        command.Parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = land.Status.ToString() });
        command.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar, 500) { Value = (object?)land.Description?.Trim() ?? DBNull.Value });

        var newId = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
        land.LandId = newId;
        return newId;
    }

    public async Task<bool> UpdateAsync(Land land, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE dbo.Lands
            SET LandCode = @LandCode,
                LandName = @LandName,
                Location = @Location,
                AreaHectares = @AreaHectares,
                SoilType = @SoilType,
                Status = @Status,
                Description = @Description,
                UpdatedAt = SYSUTCDATETIME()
            WHERE LandId = @LandId;";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add(new SqlParameter("@LandId", SqlDbType.Int) { Value = land.LandId });
        command.Parameters.Add(new SqlParameter("@LandCode", SqlDbType.NVarChar, 30) { Value = land.LandCode.Trim() });
        command.Parameters.Add(new SqlParameter("@LandName", SqlDbType.NVarChar, 100) { Value = land.LandName.Trim() });
        command.Parameters.Add(new SqlParameter("@Location", SqlDbType.NVarChar, 200) { Value = land.Location.Trim() });
        command.Parameters.Add(new SqlParameter("@AreaHectares", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = land.AreaHectares });
        command.Parameters.Add(new SqlParameter("@SoilType", SqlDbType.NVarChar, 50) { Value = land.SoilType.Trim() });
        command.Parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = land.Status.ToString() });
        command.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar, 500) { Value = (object?)land.Description?.Trim() ?? DBNull.Value });

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int landId, CancellationToken cancellationToken = default)
    {
        const string sql = @"DELETE FROM dbo.Lands WHERE LandId = @LandId;";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@LandId", SqlDbType.Int) { Value = landId });

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<int> GetActivePlantingsCountAsync(int landId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(*) 
            FROM dbo.Plantings 
            WHERE LandId = @LandId AND Status IN ('Planned', 'Active', 'ReadyToHarvest');";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@LandId", SqlDbType.Int) { Value = landId });

        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
    }

    private static Land MapLand(SqlDataReader reader)
    {
        var statusStr = reader.GetString(reader.GetOrdinal("Status"));
        _ = Enum.TryParse<LandStatus>(statusStr, true, out var status);

        return new Land
        {
            LandId = reader.GetInt32(reader.GetOrdinal("LandId")),
            LandCode = reader.GetString(reader.GetOrdinal("LandCode")),
            LandName = reader.GetString(reader.GetOrdinal("LandName")),
            Location = reader.GetString(reader.GetOrdinal("Location")),
            AreaHectares = reader.GetDecimal(reader.GetOrdinal("AreaHectares")),
            SoilType = reader.GetString(reader.GetOrdinal("SoilType")),
            Status = status,
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            ActivePlantingsCount = reader.GetInt32(reader.GetOrdinal("ActivePlantingsCount"))
        };
    }
}
