using System.Data;
using AgroTrack.Data;
using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Utilities;
using Microsoft.Data.SqlClient;

namespace AgroTrack.Repositories.Implementations;

public class ActivityRepository : IActivityRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ActivityRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<FarmingActivity?> GetByIdAsync(int activityId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT a.ActivityId, a.PlantingId, a.ActivityType, a.ActivityDate, a.Description, 
                   a.OfficerId, a.DurationHours, a.Status, a.CreatedAt, a.UpdatedAt,
                   p.PlantingCode, p.SeasonName, c.CommodityName, l.LandName, u.FullName AS OfficerName
            FROM dbo.FarmingActivities a
            INNER JOIN dbo.Plantings p ON a.PlantingId = p.PlantingId
            INNER JOIN dbo.Commodities c ON p.CommodityId = c.CommodityId
            INNER JOIN dbo.Lands l ON p.LandId = l.LandId
            INNER JOIN dbo.Users u ON a.OfficerId = u.UserId
            WHERE a.ActivityId = @ActivityId;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ActivityId", activityId);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapActivity(reader);
        }

        return null;
    }

    public async Task<PagedResult<FarmingActivity>> GetPagedAsync(
        int? plantingIdFilter,
        ActivityType? typeFilter,
        ActivityStatus? statusFilter,
        int? officerIdFilter,
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
            whereClauses.Add("a.PlantingId = @PlantingId");
            parameters.Add(new SqlParameter("@PlantingId", plantingIdFilter.Value));
        }

        if (typeFilter.HasValue)
        {
            whereClauses.Add("a.ActivityType = @ActivityType");
            parameters.Add(new SqlParameter("@ActivityType", typeFilter.Value.ToString()));
        }

        if (statusFilter.HasValue)
        {
            whereClauses.Add("a.Status = @Status");
            parameters.Add(new SqlParameter("@Status", statusFilter.Value.ToString()));
        }

        if (officerIdFilter.HasValue && officerIdFilter.Value > 0)
        {
            whereClauses.Add("a.OfficerId = @OfficerId");
            parameters.Add(new SqlParameter("@OfficerId", officerIdFilter.Value));
        }

        if (startDate.HasValue)
        {
            whereClauses.Add("a.ActivityDate >= @StartDate");
            parameters.Add(new SqlParameter("@StartDate", startDate.Value.Date));
        }

        if (endDate.HasValue)
        {
            whereClauses.Add("a.ActivityDate <= @EndDate");
            parameters.Add(new SqlParameter("@EndDate", endDate.Value.Date));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            whereClauses.Add("(a.Description LIKE @Search OR p.PlantingCode LIKE @Search OR u.FullName LIKE @Search)");
            parameters.Add(new SqlParameter("@Search", $"%{searchTerm.Trim()}%"));
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var countSql = $@"
            SELECT COUNT(*) 
            FROM dbo.FarmingActivities a
            INNER JOIN dbo.Plantings p ON a.PlantingId = p.PlantingId
            INNER JOIN dbo.Users u ON a.OfficerId = u.UserId
            {whereSql};";

        var dataSql = $@"
            SELECT a.ActivityId, a.PlantingId, a.ActivityType, a.ActivityDate, a.Description, 
                   a.OfficerId, a.DurationHours, a.Status, a.CreatedAt, a.UpdatedAt,
                   p.PlantingCode, p.SeasonName, c.CommodityName, l.LandName, u.FullName AS OfficerName
            FROM dbo.FarmingActivities a
            INNER JOIN dbo.Plantings p ON a.PlantingId = p.PlantingId
            INNER JOIN dbo.Commodities c ON p.CommodityId = c.CommodityId
            INNER JOIN dbo.Lands l ON p.LandId = l.LandId
            INNER JOIN dbo.Users u ON a.OfficerId = u.UserId
            {whereSql}
            ORDER BY a.ActivityDate DESC, a.ActivityId DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // Count Total
        int totalItems;
        using (var countCmd = new SqlCommand(countSql, conn))
        {
            foreach (var p in parameters)
                countCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
            totalItems = (int)(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0);
        }

        // Data
        var items = new List<FarmingActivity>();
        using (var dataCmd = new SqlCommand(dataSql, conn))
        {
            foreach (var p in parameters)
                dataCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));

            dataCmd.Parameters.AddWithValue("@Offset", (pageIndex - 1) * pageSize);
            dataCmd.Parameters.AddWithValue("@PageSize", pageSize);

            using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(MapActivity(reader));
            }
        }

        return new PagedResult<FarmingActivity>(items, totalItems, pageIndex, pageSize);
    }

    public async Task<IReadOnlyList<FarmingActivity>> GetByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT a.ActivityId, a.PlantingId, a.ActivityType, a.ActivityDate, a.Description, 
                   a.OfficerId, a.DurationHours, a.Status, a.CreatedAt, a.UpdatedAt,
                   p.PlantingCode, p.SeasonName, c.CommodityName, l.LandName, u.FullName AS OfficerName
            FROM dbo.FarmingActivities a
            INNER JOIN dbo.Plantings p ON a.PlantingId = p.PlantingId
            INNER JOIN dbo.Commodities c ON p.CommodityId = c.CommodityId
            INNER JOIN dbo.Lands l ON p.LandId = l.LandId
            INNER JOIN dbo.Users u ON a.OfficerId = u.UserId
            WHERE a.PlantingId = @PlantingId
            ORDER BY a.ActivityDate DESC, a.ActivityId DESC;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PlantingId", plantingId);

        var list = new List<FarmingActivity>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapActivity(reader));
        }

        return list;
    }

    public async Task<IReadOnlyList<FarmingActivity>> GetRecentActivitiesAsync(int count, CancellationToken cancellationToken = default)
    {
        var sql = $@"
            SELECT TOP (@Count) a.ActivityId, a.PlantingId, a.ActivityType, a.ActivityDate, a.Description, 
                   a.OfficerId, a.DurationHours, a.Status, a.CreatedAt, a.UpdatedAt,
                   p.PlantingCode, p.SeasonName, c.CommodityName, l.LandName, u.FullName AS OfficerName
            FROM dbo.FarmingActivities a
            INNER JOIN dbo.Plantings p ON a.PlantingId = p.PlantingId
            INNER JOIN dbo.Commodities c ON p.CommodityId = c.CommodityId
            INNER JOIN dbo.Lands l ON p.LandId = l.LandId
            INNER JOIN dbo.Users u ON a.OfficerId = u.UserId
            ORDER BY a.ActivityDate DESC, a.ActivityId DESC;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Count", count);

        var list = new List<FarmingActivity>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapActivity(reader));
        }

        return list;
    }

    public async Task<int> CreateAsync(FarmingActivity activity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO dbo.FarmingActivities 
                (PlantingId, ActivityType, ActivityDate, Description, OfficerId, DurationHours, Status, CreatedAt)
            OUTPUT INSERTED.ActivityId
            VALUES 
                (@PlantingId, @ActivityType, @ActivityDate, @Description, @OfficerId, @DurationHours, @Status, SYSUTCDATETIME());";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("@PlantingId", activity.PlantingId);
        cmd.Parameters.AddWithValue("@ActivityType", activity.ActivityType.ToString());
        cmd.Parameters.AddWithValue("@ActivityDate", activity.ActivityDate.Date);
        cmd.Parameters.AddWithValue("@Description", activity.Description.Trim());
        cmd.Parameters.AddWithValue("@OfficerId", activity.OfficerId);
        cmd.Parameters.AddWithValue("@DurationHours", (object?)activity.DurationHours ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", activity.Status.ToString());

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return (int)(result ?? 0);
    }

    public async Task<bool> UpdateAsync(FarmingActivity activity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE dbo.FarmingActivities
            SET PlantingId = @PlantingId,
                ActivityType = @ActivityType,
                ActivityDate = @ActivityDate,
                Description = @Description,
                OfficerId = @OfficerId,
                DurationHours = @DurationHours,
                Status = @Status,
                UpdatedAt = SYSUTCDATETIME()
            WHERE ActivityId = @ActivityId;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("@ActivityId", activity.ActivityId);
        cmd.Parameters.AddWithValue("@PlantingId", activity.PlantingId);
        cmd.Parameters.AddWithValue("@ActivityType", activity.ActivityType.ToString());
        cmd.Parameters.AddWithValue("@ActivityDate", activity.ActivityDate.Date);
        cmd.Parameters.AddWithValue("@Description", activity.Description.Trim());
        cmd.Parameters.AddWithValue("@OfficerId", activity.OfficerId);
        cmd.Parameters.AddWithValue("@DurationHours", (object?)activity.DurationHours ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", activity.Status.ToString());

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int activityId, CancellationToken cancellationToken = default)
    {
        const string sql = @"DELETE FROM dbo.FarmingActivities WHERE ActivityId = @ActivityId;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ActivityId", activityId);

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static FarmingActivity MapActivity(SqlDataReader reader)
    {
        return new FarmingActivity
        {
            ActivityId = reader.GetInt32(reader.GetOrdinal("ActivityId")),
            PlantingId = reader.GetInt32(reader.GetOrdinal("PlantingId")),
            ActivityType = Enum.TryParse<ActivityType>(reader.GetString(reader.GetOrdinal("ActivityType")), out var type) ? type : ActivityType.Other,
            ActivityDate = reader.GetDateTime(reader.GetOrdinal("ActivityDate")),
            Description = reader.GetString(reader.GetOrdinal("Description")),
            OfficerId = reader.GetInt32(reader.GetOrdinal("OfficerId")),
            DurationHours = reader.IsDBNull(reader.GetOrdinal("DurationHours")) ? null : reader.GetDecimal(reader.GetOrdinal("DurationHours")),
            Status = Enum.TryParse<ActivityStatus>(reader.GetString(reader.GetOrdinal("Status")), out var status) ? status : ActivityStatus.Scheduled,
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),

            PlantingCode = reader.GetString(reader.GetOrdinal("PlantingCode")),
            SeasonName = reader.GetString(reader.GetOrdinal("SeasonName")),
            CommodityName = reader.GetString(reader.GetOrdinal("CommodityName")),
            LandName = reader.GetString(reader.GetOrdinal("LandName")),
            OfficerName = reader.GetString(reader.GetOrdinal("OfficerName"))
        };
    }
}
