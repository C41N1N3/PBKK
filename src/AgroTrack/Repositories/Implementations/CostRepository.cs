using System.Data;
using AgroTrack.Data;
using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Utilities;
using Microsoft.Data.SqlClient;

namespace AgroTrack.Repositories.Implementations;

public class CostRepository : ICostRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CostRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<OperationalCost?> GetByIdAsync(int costId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT c.CostId, c.PlantingId, c.CostDate, c.Category, c.Description, 
                   c.Amount, c.CreatedBy, c.CreatedAt, c.UpdatedAt,
                   p.PlantingCode, p.SeasonName, cmd.CommodityName, l.LandName, u.FullName AS CreatorName
            FROM dbo.OperationalCosts c
            INNER JOIN dbo.Plantings p ON c.PlantingId = p.PlantingId
            INNER JOIN dbo.Commodities cmd ON p.CommodityId = cmd.CommodityId
            INNER JOIN dbo.Lands l ON p.LandId = l.LandId
            INNER JOIN dbo.Users u ON c.CreatedBy = u.UserId
            WHERE c.CostId = @CostId;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@CostId", costId);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapCost(reader);
        }

        return null;
    }

    public async Task<PagedResult<OperationalCost>> GetPagedAsync(
        int? plantingIdFilter,
        CostCategory? categoryFilter,
        DateTime? startDate,
        DateTime? endDate,
        string? searchTerm,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var whereClauses = new List<string>();
        var parameters = new List<SqlParameter>();

        if (plantingIdFilter.HasValue && plantingIdFilter.Value > 0)
        {
            whereClauses.Add("c.PlantingId = @PlantingId");
            parameters.Add(new SqlParameter("@PlantingId", plantingIdFilter.Value));
        }

        if (categoryFilter.HasValue)
        {
            whereClauses.Add("c.Category = @Category");
            parameters.Add(new SqlParameter("@Category", categoryFilter.Value.ToString()));
        }

        if (startDate.HasValue)
        {
            whereClauses.Add("c.CostDate >= @StartDate");
            parameters.Add(new SqlParameter("@StartDate", startDate.Value.Date));
        }

        if (endDate.HasValue)
        {
            whereClauses.Add("c.CostDate <= @EndDate");
            parameters.Add(new SqlParameter("@EndDate", endDate.Value.Date));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            whereClauses.Add("(c.Description LIKE @Search OR p.PlantingCode LIKE @Search OR u.FullName LIKE @Search)");
            parameters.Add(new SqlParameter("@Search", $"%{searchTerm.Trim()}%"));
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var countSql = $@"
            SELECT COUNT(*) 
            FROM dbo.OperationalCosts c
            INNER JOIN dbo.Plantings p ON c.PlantingId = p.PlantingId
            INNER JOIN dbo.Users u ON c.CreatedBy = u.UserId
            {whereSql};";

        var dataSql = $@"
            SELECT c.CostId, c.PlantingId, c.CostDate, c.Category, c.Description, 
                   c.Amount, c.CreatedBy, c.CreatedAt, c.UpdatedAt,
                   p.PlantingCode, p.SeasonName, cmd.CommodityName, l.LandName, u.FullName AS CreatorName
            FROM dbo.OperationalCosts c
            INNER JOIN dbo.Plantings p ON c.PlantingId = p.PlantingId
            INNER JOIN dbo.Commodities cmd ON p.CommodityId = cmd.CommodityId
            INNER JOIN dbo.Lands l ON p.LandId = l.LandId
            INNER JOIN dbo.Users u ON c.CreatedBy = u.UserId
            {whereSql}
            ORDER BY c.CostDate DESC, c.CostId DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        int totalCount;
        using (var countCmd = new SqlCommand(countSql, conn))
        {
            foreach (var p in parameters)
                countCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
            totalCount = (int)(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0);
        }

        var items = new List<OperationalCost>();
        using (var dataCmd = new SqlCommand(dataSql, conn))
        {
            foreach (var p in parameters)
                dataCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));

            dataCmd.Parameters.AddWithValue("@Offset", (pageIndex - 1) * pageSize);
            dataCmd.Parameters.AddWithValue("@PageSize", pageSize);

            using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(MapCost(reader));
            }
        }

        return new PagedResult<OperationalCost>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<IReadOnlyList<OperationalCost>> GetByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT c.CostId, c.PlantingId, c.CostDate, c.Category, c.Description, 
                   c.Amount, c.CreatedBy, c.CreatedAt, c.UpdatedAt,
                   p.PlantingCode, p.SeasonName, cmd.CommodityName, l.LandName, u.FullName AS CreatorName
            FROM dbo.OperationalCosts c
            INNER JOIN dbo.Plantings p ON c.PlantingId = p.PlantingId
            INNER JOIN dbo.Commodities cmd ON p.CommodityId = cmd.CommodityId
            INNER JOIN dbo.Lands l ON p.LandId = l.LandId
            INNER JOIN dbo.Users u ON c.CreatedBy = u.UserId
            WHERE c.PlantingId = @PlantingId
            ORDER BY c.CostDate DESC, c.CostId DESC;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PlantingId", plantingId);

        var list = new List<OperationalCost>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapCost(reader));
        }

        return list;
    }

    public async Task<decimal> GetTotalCostByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ISNULL(SUM(Amount), 0) FROM dbo.OperationalCosts WHERE PlantingId = @PlantingId;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PlantingId", plantingId);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
    }

    public async Task<IReadOnlyDictionary<CostCategory, decimal>> GetCostsByCategoryAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var whereClauses = new List<string>();
        var parameters = new List<SqlParameter>();

        if (startDate.HasValue)
        {
            whereClauses.Add("CostDate >= @StartDate");
            parameters.Add(new SqlParameter("@StartDate", startDate.Value.Date));
        }

        if (endDate.HasValue)
        {
            whereClauses.Add("CostDate <= @EndDate");
            parameters.Add(new SqlParameter("@EndDate", endDate.Value.Date));
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var sql = $@"
            SELECT Category, SUM(Amount) AS TotalAmount
            FROM dbo.OperationalCosts
            {whereSql}
            GROUP BY Category;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        foreach (var p in parameters)
            cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));

        var dict = new Dictionary<CostCategory, decimal>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var catStr = reader.GetString(0);
            var amount = reader.GetDecimal(1);
            if (Enum.TryParse<CostCategory>(catStr, out var cat))
            {
                dict[cat] = amount;
            }
        }

        return dict;
    }

    public async Task<int> CreateAsync(OperationalCost cost, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO dbo.OperationalCosts 
                (PlantingId, CostDate, Category, Description, Amount, CreatedBy, CreatedAt)
            OUTPUT INSERTED.CostId
            VALUES 
                (@PlantingId, @CostDate, @Category, @Description, @Amount, @CreatedBy, SYSUTCDATETIME());";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("@PlantingId", cost.PlantingId);
        cmd.Parameters.AddWithValue("@CostDate", cost.CostDate.Date);
        cmd.Parameters.AddWithValue("@Category", cost.Category.ToString());
        cmd.Parameters.AddWithValue("@Description", cost.Description.Trim());
        cmd.Parameters.AddWithValue("@Amount", cost.Amount);
        cmd.Parameters.AddWithValue("@CreatedBy", cost.CreatedBy);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return (int)(result ?? 0);
    }

    public async Task<bool> UpdateAsync(OperationalCost cost, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE dbo.OperationalCosts
            SET PlantingId = @PlantingId,
                CostDate = @CostDate,
                Category = @Category,
                Description = @Description,
                Amount = @Amount,
                UpdatedAt = SYSUTCDATETIME()
            WHERE CostId = @CostId;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("@CostId", cost.CostId);
        cmd.Parameters.AddWithValue("@PlantingId", cost.PlantingId);
        cmd.Parameters.AddWithValue("@CostDate", cost.CostDate.Date);
        cmd.Parameters.AddWithValue("@Category", cost.Category.ToString());
        cmd.Parameters.AddWithValue("@Description", cost.Description.Trim());
        cmd.Parameters.AddWithValue("@Amount", cost.Amount);

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int costId, CancellationToken cancellationToken = default)
    {
        const string sql = @"DELETE FROM dbo.OperationalCosts WHERE CostId = @CostId;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@CostId", costId);

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static OperationalCost MapCost(SqlDataReader reader)
    {
        return new OperationalCost
        {
            CostId = reader.GetInt32(reader.GetOrdinal("CostId")),
            PlantingId = reader.GetInt32(reader.GetOrdinal("PlantingId")),
            CostDate = reader.GetDateTime(reader.GetOrdinal("CostDate")),
            Category = Enum.TryParse<CostCategory>(reader.GetString(reader.GetOrdinal("Category")), out var cat) ? cat : CostCategory.Other,
            Description = reader.GetString(reader.GetOrdinal("Description")),
            Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
            CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),

            PlantingCode = reader.GetString(reader.GetOrdinal("PlantingCode")),
            SeasonName = reader.GetString(reader.GetOrdinal("SeasonName")),
            CommodityName = reader.GetString(reader.GetOrdinal("CommodityName")),
            LandName = reader.GetString(reader.GetOrdinal("LandName")),
            CreatorName = reader.GetString(reader.GetOrdinal("CreatorName"))
        };
    }
}
