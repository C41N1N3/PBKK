using System.Data;
using AgroTrack.Data;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Utilities;
using Microsoft.Data.SqlClient;

namespace AgroTrack.Repositories.Implementations;

public class CommodityRepository : ICommodityRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CommodityRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Commodity?> GetByIdAsync(int commodityId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT c.CommodityId, c.CommodityCode, c.CommodityName, c.ResultUnit, c.EstimatedGrowthDays, 
                   c.Description, c.IsActive, c.CreatedAt, c.UpdatedAt,
                   (SELECT COUNT(*) FROM dbo.Plantings p WHERE p.CommodityId = c.CommodityId) AS TotalPlantingsCount
            FROM dbo.Commodities c
            WHERE c.CommodityId = @CommodityId;";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@CommodityId", SqlDbType.Int) { Value = commodityId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapCommodity(reader);
        }

        return null;
    }

    public async Task<Commodity?> GetByCodeAsync(string commodityCode, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT c.CommodityId, c.CommodityCode, c.CommodityName, c.ResultUnit, c.EstimatedGrowthDays, 
                   c.Description, c.IsActive, c.CreatedAt, c.UpdatedAt,
                   (SELECT COUNT(*) FROM dbo.Plantings p WHERE p.CommodityId = c.CommodityId) AS TotalPlantingsCount
            FROM dbo.Commodities c
            WHERE LOWER(c.CommodityCode) = LOWER(@CommodityCode);";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@CommodityCode", SqlDbType.NVarChar, 30) { Value = commodityCode.Trim() });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapCommodity(reader);
        }

        return null;
    }

    public async Task<PagedResult<Commodity>> GetPagedAsync(
        string? searchTerm, 
        bool? isActiveFilter, 
        int pageIndex, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var whereClauses = new List<string>();
        var parameters = new List<SqlParameter>();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            whereClauses.Add("(c.CommodityCode LIKE @Search OR c.CommodityName LIKE @Search)");
            parameters.Add(new SqlParameter("@Search", SqlDbType.NVarChar, 100) { Value = $"%{searchTerm.Trim()}%" });
        }

        if (isActiveFilter.HasValue)
        {
            whereClauses.Add("c.IsActive = @IsActive");
            parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = isActiveFilter.Value });
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : string.Empty;
        var offset = (Math.Max(1, pageIndex) - 1) * pageSize;

        var sql = $@"
            SELECT c.CommodityId, c.CommodityCode, c.CommodityName, c.ResultUnit, c.EstimatedGrowthDays, 
                   c.Description, c.IsActive, c.CreatedAt, c.UpdatedAt,
                   (SELECT COUNT(*) FROM dbo.Plantings p WHERE p.CommodityId = c.CommodityId) AS TotalPlantingsCount,
                   COUNT(*) OVER() AS TotalCount
            FROM dbo.Commodities c
            {whereSql}
            ORDER BY c.CommodityName ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        parameters.Add(new SqlParameter("@Offset", SqlDbType.Int) { Value = offset });
        parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = pageSize });

        var items = new List<Commodity>();
        var totalCount = 0;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters.ToArray());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapCommodity(reader));
            totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
        }

        return new PagedResult<Commodity>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<IReadOnlyList<Commodity>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT c.CommodityId, c.CommodityCode, c.CommodityName, c.ResultUnit, c.EstimatedGrowthDays, 
                   c.Description, c.IsActive, c.CreatedAt, c.UpdatedAt,
                   (SELECT COUNT(*) FROM dbo.Plantings p WHERE p.CommodityId = c.CommodityId) AS TotalPlantingsCount
            FROM dbo.Commodities c
            WHERE c.IsActive = 1
            ORDER BY c.CommodityName ASC;";

        var commodities = new List<Commodity>();
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            commodities.Add(MapCommodity(reader));
        }

        return commodities.AsReadOnly();
    }

    public async Task<int> CreateAsync(Commodity commodity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO dbo.Commodities (CommodityCode, CommodityName, ResultUnit, EstimatedGrowthDays, Description, IsActive, CreatedAt)
            OUTPUT INSERTED.CommodityId
            VALUES (@CommodityCode, @CommodityName, @ResultUnit, @EstimatedGrowthDays, @Description, @IsActive, SYSUTCDATETIME());";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add(new SqlParameter("@CommodityCode", SqlDbType.NVarChar, 30) { Value = commodity.CommodityCode.Trim() });
        command.Parameters.Add(new SqlParameter("@CommodityName", SqlDbType.NVarChar, 100) { Value = commodity.CommodityName.Trim() });
        command.Parameters.Add(new SqlParameter("@ResultUnit", SqlDbType.NVarChar, 20) { Value = commodity.ResultUnit.Trim() });
        command.Parameters.Add(new SqlParameter("@EstimatedGrowthDays", SqlDbType.Int) { Value = commodity.EstimatedGrowthDays });
        command.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar, 500) { Value = (object?)commodity.Description?.Trim() ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = commodity.IsActive });

        var newId = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
        commodity.CommodityId = newId;
        return newId;
    }

    public async Task<bool> UpdateAsync(Commodity commodity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE dbo.Commodities
            SET CommodityCode = @CommodityCode,
                CommodityName = @CommodityName,
                ResultUnit = @ResultUnit,
                EstimatedGrowthDays = @EstimatedGrowthDays,
                Description = @Description,
                IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME()
            WHERE CommodityId = @CommodityId;";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add(new SqlParameter("@CommodityId", SqlDbType.Int) { Value = commodity.CommodityId });
        command.Parameters.Add(new SqlParameter("@CommodityCode", SqlDbType.NVarChar, 30) { Value = commodity.CommodityCode.Trim() });
        command.Parameters.Add(new SqlParameter("@CommodityName", SqlDbType.NVarChar, 100) { Value = commodity.CommodityName.Trim() });
        command.Parameters.Add(new SqlParameter("@ResultUnit", SqlDbType.NVarChar, 20) { Value = commodity.ResultUnit.Trim() });
        command.Parameters.Add(new SqlParameter("@EstimatedGrowthDays", SqlDbType.Int) { Value = commodity.EstimatedGrowthDays });
        command.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar, 500) { Value = (object?)commodity.Description?.Trim() ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = commodity.IsActive });

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> SetActiveStatusAsync(int commodityId, bool isActive, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE dbo.Commodities
            SET IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME()
            WHERE CommodityId = @CommodityId;";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add(new SqlParameter("@CommodityId", SqlDbType.Int) { Value = commodityId });
        command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = isActive });

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int commodityId, CancellationToken cancellationToken = default)
    {
        const string sql = @"DELETE FROM dbo.Commodities WHERE CommodityId = @CommodityId;";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@CommodityId", SqlDbType.Int) { Value = commodityId });

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<int> GetPlantingsCountAsync(int commodityId, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT COUNT(*) FROM dbo.Plantings WHERE CommodityId = @CommodityId;";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@CommodityId", SqlDbType.Int) { Value = commodityId });

        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
    }

    private static Commodity MapCommodity(SqlDataReader reader)
    {
        return new Commodity
        {
            CommodityId = reader.GetInt32(reader.GetOrdinal("CommodityId")),
            CommodityCode = reader.GetString(reader.GetOrdinal("CommodityCode")),
            CommodityName = reader.GetString(reader.GetOrdinal("CommodityName")),
            ResultUnit = reader.GetString(reader.GetOrdinal("ResultUnit")),
            EstimatedGrowthDays = reader.GetInt32(reader.GetOrdinal("EstimatedGrowthDays")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            TotalPlantingsCount = reader.GetInt32(reader.GetOrdinal("TotalPlantingsCount"))
        };
    }
}
