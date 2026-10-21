using System.Data;
using AgroTrack.Data;
using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Utilities;
using Microsoft.Data.SqlClient;

namespace AgroTrack.Repositories.Implementations;

public class HarvestRepository : IHarvestRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public HarvestRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Harvest?> GetByIdAsync(int harvestId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT h.HarvestId, h.PlantingId, h.HarvestDate, h.YieldAmount, h.Quality, 
                   h.SellingPricePerUnit, h.TotalRevenue, h.Notes, h.CreatedBy, h.CreatedAt, h.UpdatedAt,
                   p.PlantingCode, p.SeasonName, cmd.CommodityName, cmd.ResultUnit, l.LandName, u.FullName AS CreatorName
            FROM dbo.Harvests h
            INNER JOIN dbo.Plantings p ON h.PlantingId = p.PlantingId
            INNER JOIN dbo.Commodities cmd ON p.CommodityId = cmd.CommodityId
            INNER JOIN dbo.Lands l ON p.LandId = l.LandId
            INNER JOIN dbo.Users u ON h.CreatedBy = u.UserId
            WHERE h.HarvestId = @HarvestId;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@HarvestId", harvestId);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapHarvest(reader);
        }

        return null;
    }

    public async Task<PagedResult<Harvest>> GetPagedAsync(
        int? plantingIdFilter,
        HarvestQuality? qualityFilter,
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
            whereClauses.Add("h.PlantingId = @PlantingId");
            parameters.Add(new SqlParameter("@PlantingId", plantingIdFilter.Value));
        }

        if (qualityFilter.HasValue)
        {
            whereClauses.Add("h.Quality = @Quality");
            parameters.Add(new SqlParameter("@Quality", qualityFilter.Value.ToString()));
        }

        if (startDate.HasValue)
        {
            whereClauses.Add("h.HarvestDate >= @StartDate");
            parameters.Add(new SqlParameter("@StartDate", startDate.Value.Date));
        }

        if (endDate.HasValue)
        {
            whereClauses.Add("h.HarvestDate <= @EndDate");
            parameters.Add(new SqlParameter("@EndDate", endDate.Value.Date));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            whereClauses.Add("(h.Notes LIKE @Search OR p.PlantingCode LIKE @Search OR cmd.CommodityName LIKE @Search)");
            parameters.Add(new SqlParameter("@Search", $"%{searchTerm.Trim()}%"));
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var countSql = $@"
            SELECT COUNT(*) 
            FROM dbo.Harvests h
            INNER JOIN dbo.Plantings p ON h.PlantingId = p.PlantingId
            INNER JOIN dbo.Commodities cmd ON p.CommodityId = cmd.CommodityId
            INNER JOIN dbo.Users u ON h.CreatedBy = u.UserId
            {whereSql};";

        var dataSql = $@"
            SELECT h.HarvestId, h.PlantingId, h.HarvestDate, h.YieldAmount, h.Quality, 
                   h.SellingPricePerUnit, h.TotalRevenue, h.Notes, h.CreatedBy, h.CreatedAt, h.UpdatedAt,
                   p.PlantingCode, p.SeasonName, cmd.CommodityName, cmd.ResultUnit, l.LandName, u.FullName AS CreatorName
            FROM dbo.Harvests h
            INNER JOIN dbo.Plantings p ON h.PlantingId = p.PlantingId
            INNER JOIN dbo.Commodities cmd ON p.CommodityId = cmd.CommodityId
            INNER JOIN dbo.Lands l ON p.LandId = l.LandId
            INNER JOIN dbo.Users u ON h.CreatedBy = u.UserId
            {whereSql}
            ORDER BY h.HarvestDate DESC, h.HarvestId DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        int totalCount;
        using (var countCmd = new SqlCommand(countSql, conn))
        {
            foreach (var p in parameters)
                countCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
            totalCount = (int)(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0);
        }

        var items = new List<Harvest>();
        using (var dataCmd = new SqlCommand(dataSql, conn))
        {
            foreach (var p in parameters)
                dataCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));

            dataCmd.Parameters.AddWithValue("@Offset", (pageIndex - 1) * pageSize);
            dataCmd.Parameters.AddWithValue("@PageSize", pageSize);

            using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(MapHarvest(reader));
            }
        }

        return new PagedResult<Harvest>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<IReadOnlyList<Harvest>> GetByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT h.HarvestId, h.PlantingId, h.HarvestDate, h.YieldAmount, h.Quality, 
                   h.SellingPricePerUnit, h.TotalRevenue, h.Notes, h.CreatedBy, h.CreatedAt, h.UpdatedAt,
                   p.PlantingCode, p.SeasonName, cmd.CommodityName, cmd.ResultUnit, l.LandName, u.FullName AS CreatorName
            FROM dbo.Harvests h
            INNER JOIN dbo.Plantings p ON h.PlantingId = p.PlantingId
            INNER JOIN dbo.Commodities cmd ON p.CommodityId = cmd.CommodityId
            INNER JOIN dbo.Lands l ON p.LandId = l.LandId
            INNER JOIN dbo.Users u ON h.CreatedBy = u.UserId
            WHERE h.PlantingId = @PlantingId
            ORDER BY h.HarvestDate DESC, h.HarvestId DESC;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PlantingId", plantingId);

        var list = new List<Harvest>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapHarvest(reader));
        }

        return list;
    }

    public async Task<decimal> GetTotalYieldByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ISNULL(SUM(YieldAmount), 0) FROM dbo.Harvests WHERE PlantingId = @PlantingId;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PlantingId", plantingId);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
    }

    public async Task<decimal> GetTotalRevenueByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ISNULL(SUM(TotalRevenue), 0) FROM dbo.Harvests WHERE PlantingId = @PlantingId;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PlantingId", plantingId);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
    }

    public async Task<IReadOnlyList<(string MonthYear, decimal TotalYield, decimal TotalRevenue)>> GetMonthlyHarvestSummaryAsync(int monthsCount, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT FORMAT(HarvestDate, 'yyyy-MM') AS MonthYear,
                   SUM(YieldAmount) AS TotalYield,
                   SUM(TotalRevenue) AS TotalRevenue
            FROM dbo.Harvests
            WHERE HarvestDate >= DATEADD(MONTH, -@MonthsCount, SYSUTCDATETIME())
            GROUP BY FORMAT(HarvestDate, 'yyyy-MM')
            ORDER BY MonthYear ASC;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MonthsCount", monthsCount);

        var result = new List<(string MonthYear, decimal TotalYield, decimal TotalRevenue)>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var my = reader.GetString(0);
            var yield = reader.GetDecimal(1);
            var rev = reader.GetDecimal(2);
            result.Add((my, yield, rev));
        }

        return result;
    }

    public async Task<int> CreateAsync(Harvest harvest, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO dbo.Harvests 
                (PlantingId, HarvestDate, YieldAmount, Quality, SellingPricePerUnit, TotalRevenue, Notes, CreatedBy, CreatedAt)
            OUTPUT INSERTED.HarvestId
            VALUES 
                (@PlantingId, @HarvestDate, @YieldAmount, @Quality, @SellingPricePerUnit, @TotalRevenue, @Notes, @CreatedBy, SYSUTCDATETIME());";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("@PlantingId", harvest.PlantingId);
        cmd.Parameters.AddWithValue("@HarvestDate", harvest.HarvestDate.Date);
        cmd.Parameters.AddWithValue("@YieldAmount", harvest.YieldAmount);
        cmd.Parameters.AddWithValue("@Quality", harvest.Quality.ToString());
        cmd.Parameters.AddWithValue("@SellingPricePerUnit", harvest.SellingPricePerUnit);
        cmd.Parameters.AddWithValue("@TotalRevenue", harvest.YieldAmount * harvest.SellingPricePerUnit);
        cmd.Parameters.AddWithValue("@Notes", (object?)harvest.Notes?.Trim() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedBy", harvest.CreatedBy);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return (int)(result ?? 0);
    }

    public async Task<bool> UpdateAsync(Harvest harvest, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE dbo.Harvests
            SET PlantingId = @PlantingId,
                HarvestDate = @HarvestDate,
                YieldAmount = @YieldAmount,
                Quality = @Quality,
                SellingPricePerUnit = @SellingPricePerUnit,
                TotalRevenue = @TotalRevenue,
                Notes = @Notes,
                UpdatedAt = SYSUTCDATETIME()
            WHERE HarvestId = @HarvestId;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("@HarvestId", harvest.HarvestId);
        cmd.Parameters.AddWithValue("@PlantingId", harvest.PlantingId);
        cmd.Parameters.AddWithValue("@HarvestDate", harvest.HarvestDate.Date);
        cmd.Parameters.AddWithValue("@YieldAmount", harvest.YieldAmount);
        cmd.Parameters.AddWithValue("@Quality", harvest.Quality.ToString());
        cmd.Parameters.AddWithValue("@SellingPricePerUnit", harvest.SellingPricePerUnit);
        cmd.Parameters.AddWithValue("@TotalRevenue", harvest.YieldAmount * harvest.SellingPricePerUnit);
        cmd.Parameters.AddWithValue("@Notes", (object?)harvest.Notes?.Trim() ?? DBNull.Value);

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int harvestId, CancellationToken cancellationToken = default)
    {
        const string sql = @"DELETE FROM dbo.Harvests WHERE HarvestId = @HarvestId;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@HarvestId", harvestId);

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static Harvest MapHarvest(SqlDataReader reader)
    {
        return new Harvest
        {
            HarvestId = reader.GetInt32(reader.GetOrdinal("HarvestId")),
            PlantingId = reader.GetInt32(reader.GetOrdinal("PlantingId")),
            HarvestDate = reader.GetDateTime(reader.GetOrdinal("HarvestDate")),
            YieldAmount = reader.GetDecimal(reader.GetOrdinal("YieldAmount")),
            Quality = Enum.TryParse<HarvestQuality>(reader.GetString(reader.GetOrdinal("Quality")), out var q) ? q : HarvestQuality.Standard,
            SellingPricePerUnit = reader.GetDecimal(reader.GetOrdinal("SellingPricePerUnit")),
            Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
            CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),

            PlantingCode = reader.GetString(reader.GetOrdinal("PlantingCode")),
            SeasonName = reader.GetString(reader.GetOrdinal("SeasonName")),
            CommodityName = reader.GetString(reader.GetOrdinal("CommodityName")),
            ResultUnit = reader.GetString(reader.GetOrdinal("ResultUnit")),
            LandName = reader.GetString(reader.GetOrdinal("LandName")),
            CreatorName = reader.GetString(reader.GetOrdinal("CreatorName"))
        };
    }
}
