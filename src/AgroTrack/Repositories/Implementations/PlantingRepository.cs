using System.Data;
using AgroTrack.Data;
using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Utilities;
using Microsoft.Data.SqlClient;

namespace AgroTrack.Repositories.Implementations;

public class PlantingRepository : IPlantingRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PlantingRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string BaseSelectSql = @"
        SELECT p.PlantingId, p.PlantingCode, p.LandId, p.CommodityId, p.SeasonName,
               p.StartDate, p.ExpectedHarvestDate, p.CompletionDate, p.PlantedAreaHectares,
               p.TargetYield, p.Status, p.Notes, p.CreatedBy, p.CreatedAt, p.UpdatedAt,
               l.LandName, l.LandCode, l.AreaHectares AS LandTotalAreaHectares,
               c.CommodityName, c.ResultUnit,
               u.FullName AS CreatorName,
               ISNULL(h.TotalActualYield, 0) AS TotalActualYield,
               ISNULL(h.TotalRevenue, 0) AS TotalRevenue,
               ISNULL(o.TotalOperationalCost, 0) AS TotalOperationalCost,
               ISNULL(m.TotalMaterialCost, 0) AS TotalMaterialCost
        FROM dbo.Plantings p
        INNER JOIN dbo.Lands l ON p.LandId = l.LandId
        INNER JOIN dbo.Commodities c ON p.CommodityId = c.CommodityId
        INNER JOIN dbo.Users u ON p.CreatedBy = u.UserId
        LEFT JOIN (
            SELECT PlantingId, SUM(YieldAmount) AS TotalActualYield, SUM(TotalRevenue) AS TotalRevenue
            FROM dbo.Harvests
            GROUP BY PlantingId
        ) h ON p.PlantingId = h.PlantingId
        LEFT JOIN (
            SELECT PlantingId, SUM(Amount) AS TotalOperationalCost
            FROM dbo.OperationalCosts
            GROUP BY PlantingId
        ) o ON p.PlantingId = o.PlantingId
        LEFT JOIN (
            SELECT fa.PlantingId, SUM(mu.Subtotal) AS TotalMaterialCost
            FROM dbo.MaterialUsages mu
            INNER JOIN dbo.FarmingActivities fa ON mu.ActivityId = fa.ActivityId
            GROUP BY fa.PlantingId
        ) m ON p.PlantingId = m.PlantingId";

    public async Task<Planting?> GetByIdAsync(int plantingId, CancellationToken cancellationToken = default)
    {
        var sql = $"{BaseSelectSql} WHERE p.PlantingId = @PlantingId;";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@PlantingId", SqlDbType.Int) { Value = plantingId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapPlanting(reader);
        }

        return null;
    }

    public async Task<Planting?> GetByCodeAsync(string plantingCode, CancellationToken cancellationToken = default)
    {
        var sql = $"{BaseSelectSql} WHERE LOWER(p.PlantingCode) = LOWER(@PlantingCode);";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@PlantingCode", SqlDbType.NVarChar, 30) { Value = plantingCode.Trim() });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapPlanting(reader);
        }

        return null;
    }

    public async Task<PagedResult<Planting>> GetPagedAsync(
        string? searchTerm, 
        PlantingStatus? statusFilter, 
        int? landIdFilter, 
        int? commodityIdFilter, 
        DateTime? startDateFrom, 
        DateTime? startDateTo, 
        int pageIndex, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var whereClauses = new List<string>();
        var parameters = new List<SqlParameter>();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            whereClauses.Add("(p.PlantingCode LIKE @Search OR p.SeasonName LIKE @Search OR l.LandName LIKE @Search OR c.CommodityName LIKE @Search)");
            parameters.Add(new SqlParameter("@Search", SqlDbType.NVarChar, 100) { Value = $"%{searchTerm.Trim()}%" });
        }

        if (statusFilter.HasValue)
        {
            whereClauses.Add("p.Status = @Status");
            parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = statusFilter.Value.ToString() });
        }

        if (landIdFilter.HasValue && landIdFilter > 0)
        {
            whereClauses.Add("p.LandId = @LandId");
            parameters.Add(new SqlParameter("@LandId", SqlDbType.Int) { Value = landIdFilter.Value });
        }

        if (commodityIdFilter.HasValue && commodityIdFilter > 0)
        {
            whereClauses.Add("p.CommodityId = @CommodityId");
            parameters.Add(new SqlParameter("@CommodityId", SqlDbType.Int) { Value = commodityIdFilter.Value });
        }

        if (startDateFrom.HasValue)
        {
            whereClauses.Add("p.StartDate >= @StartDateFrom");
            parameters.Add(new SqlParameter("@StartDateFrom", SqlDbType.Date) { Value = startDateFrom.Value });
        }

        if (startDateTo.HasValue)
        {
            whereClauses.Add("p.StartDate <= @StartDateTo");
            parameters.Add(new SqlParameter("@StartDateTo", SqlDbType.Date) { Value = startDateTo.Value });
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : string.Empty;
        var offset = (Math.Max(1, pageIndex) - 1) * pageSize;

        var sql = $@"
            SELECT p.PlantingId, p.PlantingCode, p.LandId, p.CommodityId, p.SeasonName,
                   p.StartDate, p.ExpectedHarvestDate, p.CompletionDate, p.PlantedAreaHectares,
                   p.TargetYield, p.Status, p.Notes, p.CreatedBy, p.CreatedAt, p.UpdatedAt,
                   l.LandName, l.LandCode, l.AreaHectares AS LandTotalAreaHectares,
                   c.CommodityName, c.ResultUnit,
                   u.FullName AS CreatorName,
                   ISNULL(h.TotalActualYield, 0) AS TotalActualYield,
                   ISNULL(h.TotalRevenue, 0) AS TotalRevenue,
                   ISNULL(o.TotalOperationalCost, 0) AS TotalOperationalCost,
                   ISNULL(m.TotalMaterialCost, 0) AS TotalMaterialCost,
                   COUNT(*) OVER() AS TotalCount
            FROM dbo.Plantings p
            INNER JOIN dbo.Lands l ON p.LandId = l.LandId
            INNER JOIN dbo.Commodities c ON p.CommodityId = c.CommodityId
            INNER JOIN dbo.Users u ON p.CreatedBy = u.UserId
            LEFT JOIN (
                SELECT PlantingId, SUM(YieldAmount) AS TotalActualYield, SUM(TotalRevenue) AS TotalRevenue
                FROM dbo.Harvests
                GROUP BY PlantingId
            ) h ON p.PlantingId = h.PlantingId
            LEFT JOIN (
                SELECT PlantingId, SUM(Amount) AS TotalOperationalCost
                FROM dbo.OperationalCosts
                GROUP BY PlantingId
            ) o ON p.PlantingId = o.PlantingId
            LEFT JOIN (
                SELECT fa.PlantingId, SUM(mu.Subtotal) AS TotalMaterialCost
                FROM dbo.MaterialUsages mu
                INNER JOIN dbo.FarmingActivities fa ON mu.ActivityId = fa.ActivityId
                GROUP BY fa.PlantingId
            ) m ON p.PlantingId = m.PlantingId
            {whereSql}
            ORDER BY p.StartDate DESC, p.PlantingId DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        parameters.Add(new SqlParameter("@Offset", SqlDbType.Int) { Value = offset });
        parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = pageSize });

        var items = new List<Planting>();
        var totalCount = 0;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters.ToArray());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapPlanting(reader));
            totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
        }

        return new PagedResult<Planting>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<IReadOnlyList<Planting>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var sql = $"{BaseSelectSql} WHERE p.Status IN ('Planned', 'Active', 'ReadyToHarvest') ORDER BY p.StartDate ASC;";

        var plantings = new List<Planting>();
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            plantings.Add(MapPlanting(reader));
        }

        return plantings.AsReadOnly();
    }

    public async Task<decimal> GetActiveOverlappingAreaAsync(
        int landId, 
        DateTime startDate, 
        DateTime expectedHarvestDate, 
        int? excludePlantingId, 
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT ISNULL(SUM(PlantedAreaHectares), 0)
            FROM dbo.Plantings
            WHERE LandId = @LandId
              AND Status IN ('Planned', 'Active', 'ReadyToHarvest')
              AND (@ExcludePlantingId IS NULL OR PlantingId <> @ExcludePlantingId)
              AND (StartDate <= @ExpectedHarvestDate AND ExpectedHarvestDate >= @StartDate);";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add(new SqlParameter("@LandId", SqlDbType.Int) { Value = landId });
        command.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.Date) { Value = startDate.Date });
        command.Parameters.Add(new SqlParameter("@ExpectedHarvestDate", SqlDbType.Date) { Value = expectedHarvestDate.Date });
        command.Parameters.Add(new SqlParameter("@ExcludePlantingId", SqlDbType.Int) { Value = (object?)excludePlantingId ?? DBNull.Value });

        return (decimal)(await command.ExecuteScalarAsync(cancellationToken) ?? 0m);
    }

    public async Task<int> CreateAsync(Planting planting, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO dbo.Plantings (
                PlantingCode, LandId, CommodityId, SeasonName, StartDate, 
                ExpectedHarvestDate, CompletionDate, PlantedAreaHectares, TargetYield, 
                Status, Notes, CreatedBy, CreatedAt
            )
            OUTPUT INSERTED.PlantingId
            VALUES (
                @PlantingCode, @LandId, @CommodityId, @SeasonName, @StartDate, 
                @ExpectedHarvestDate, @CompletionDate, @PlantedAreaHectares, @TargetYield, 
                @Status, @Notes, @CreatedBy, SYSUTCDATETIME()
            );";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add(new SqlParameter("@PlantingCode", SqlDbType.NVarChar, 30) { Value = planting.PlantingCode.Trim() });
        command.Parameters.Add(new SqlParameter("@LandId", SqlDbType.Int) { Value = planting.LandId });
        command.Parameters.Add(new SqlParameter("@CommodityId", SqlDbType.Int) { Value = planting.CommodityId });
        command.Parameters.Add(new SqlParameter("@SeasonName", SqlDbType.NVarChar, 100) { Value = planting.SeasonName.Trim() });
        command.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.Date) { Value = planting.StartDate.Date });
        command.Parameters.Add(new SqlParameter("@ExpectedHarvestDate", SqlDbType.Date) { Value = planting.ExpectedHarvestDate.Date });
        command.Parameters.Add(new SqlParameter("@CompletionDate", SqlDbType.Date) { Value = (object?)planting.CompletionDate?.Date ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@PlantedAreaHectares", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = planting.PlantedAreaHectares });
        command.Parameters.Add(new SqlParameter("@TargetYield", SqlDbType.Decimal) { Precision = 12, Scale = 2, Value = planting.TargetYield });
        command.Parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = planting.Status.ToString() });
        command.Parameters.Add(new SqlParameter("@Notes", SqlDbType.NVarChar, 1000) { Value = (object?)planting.Notes?.Trim() ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.Int) { Value = planting.CreatedBy });

        var newId = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
        planting.PlantingId = newId;
        return newId;
    }

    public async Task<bool> UpdateAsync(Planting planting, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE dbo.Plantings
            SET PlantingCode = @PlantingCode,
                LandId = @LandId,
                CommodityId = @CommodityId,
                SeasonName = @SeasonName,
                StartDate = @StartDate,
                ExpectedHarvestDate = @ExpectedHarvestDate,
                CompletionDate = @CompletionDate,
                PlantedAreaHectares = @PlantedAreaHectares,
                TargetYield = @TargetYield,
                Status = @Status,
                Notes = @Notes,
                UpdatedAt = SYSUTCDATETIME()
            WHERE PlantingId = @PlantingId;";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add(new SqlParameter("@PlantingId", SqlDbType.Int) { Value = planting.PlantingId });
        command.Parameters.Add(new SqlParameter("@PlantingCode", SqlDbType.NVarChar, 30) { Value = planting.PlantingCode.Trim() });
        command.Parameters.Add(new SqlParameter("@LandId", SqlDbType.Int) { Value = planting.LandId });
        command.Parameters.Add(new SqlParameter("@CommodityId", SqlDbType.Int) { Value = planting.CommodityId });
        command.Parameters.Add(new SqlParameter("@SeasonName", SqlDbType.NVarChar, 100) { Value = planting.SeasonName.Trim() });
        command.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.Date) { Value = planting.StartDate.Date });
        command.Parameters.Add(new SqlParameter("@ExpectedHarvestDate", SqlDbType.Date) { Value = planting.ExpectedHarvestDate.Date });
        command.Parameters.Add(new SqlParameter("@CompletionDate", SqlDbType.Date) { Value = (object?)planting.CompletionDate?.Date ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@PlantedAreaHectares", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = planting.PlantedAreaHectares });
        command.Parameters.Add(new SqlParameter("@TargetYield", SqlDbType.Decimal) { Precision = 12, Scale = 2, Value = planting.TargetYield });
        command.Parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = planting.Status.ToString() });
        command.Parameters.Add(new SqlParameter("@Notes", SqlDbType.NVarChar, 1000) { Value = (object?)planting.Notes?.Trim() ?? DBNull.Value });

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateStatusAsync(int plantingId, PlantingStatus newStatus, DateTime? completionDate, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE dbo.Plantings
            SET Status = @Status,
                CompletionDate = @CompletionDate,
                UpdatedAt = SYSUTCDATETIME()
            WHERE PlantingId = @PlantingId;";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add(new SqlParameter("@PlantingId", SqlDbType.Int) { Value = plantingId });
        command.Parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 30) { Value = newStatus.ToString() });
        command.Parameters.Add(new SqlParameter("@CompletionDate", SqlDbType.Date) { Value = (object?)completionDate?.Date ?? DBNull.Value });

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int plantingId, CancellationToken cancellationToken = default)
    {
        const string sql = @"DELETE FROM dbo.Plantings WHERE PlantingId = @PlantingId;";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@PlantingId", SqlDbType.Int) { Value = plantingId });

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static Planting MapPlanting(SqlDataReader reader)
    {
        var statusStr = reader.GetString(reader.GetOrdinal("Status"));
        _ = Enum.TryParse<PlantingStatus>(statusStr, true, out var status);

        return new Planting
        {
            PlantingId = reader.GetInt32(reader.GetOrdinal("PlantingId")),
            PlantingCode = reader.GetString(reader.GetOrdinal("PlantingCode")),
            LandId = reader.GetInt32(reader.GetOrdinal("LandId")),
            CommodityId = reader.GetInt32(reader.GetOrdinal("CommodityId")),
            SeasonName = reader.GetString(reader.GetOrdinal("SeasonName")),
            StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
            ExpectedHarvestDate = reader.GetDateTime(reader.GetOrdinal("ExpectedHarvestDate")),
            CompletionDate = reader.IsDBNull(reader.GetOrdinal("CompletionDate")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletionDate")),
            PlantedAreaHectares = reader.GetDecimal(reader.GetOrdinal("PlantedAreaHectares")),
            TargetYield = reader.GetDecimal(reader.GetOrdinal("TargetYield")),
            Status = status,
            Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
            CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),

            LandName = reader.GetString(reader.GetOrdinal("LandName")),
            LandCode = reader.GetString(reader.GetOrdinal("LandCode")),
            LandTotalAreaHectares = reader.GetDecimal(reader.GetOrdinal("LandTotalAreaHectares")),
            CommodityName = reader.GetString(reader.GetOrdinal("CommodityName")),
            ResultUnit = reader.GetString(reader.GetOrdinal("ResultUnit")),
            CreatorName = reader.GetString(reader.GetOrdinal("CreatorName")),

            TotalActualYield = reader.GetDecimal(reader.GetOrdinal("TotalActualYield")),
            TotalRevenue = reader.GetDecimal(reader.GetOrdinal("TotalRevenue")),
            TotalOperationalCost = reader.GetDecimal(reader.GetOrdinal("TotalOperationalCost")),
            TotalMaterialCost = reader.GetDecimal(reader.GetOrdinal("TotalMaterialCost"))
        };
    }
}
