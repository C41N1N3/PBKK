using System.Data;
using AgroTrack.Data;
using AgroTrack.DTOs;
using AgroTrack.Enums;
using AgroTrack.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace AgroTrack.Repositories.Implementations;

public class ReportRepository : IReportRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ReportRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ProfitLossReportDto>> GetProfitLossReportAsync(
        DateTime? startDateFrom,
        DateTime? startDateTo,
        int? landId,
        int? commodityId,
        PlantingStatus? status,
        CancellationToken cancellationToken = default)
    {
        var whereClauses = new List<string>();
        var parameters = new List<SqlParameter>();

        if (startDateFrom.HasValue)
        {
            whereClauses.Add("p.StartDate >= @StartDateFrom");
            parameters.Add(new SqlParameter("@StartDateFrom", startDateFrom.Value.Date));
        }

        if (startDateTo.HasValue)
        {
            whereClauses.Add("p.StartDate <= @StartDateTo");
            parameters.Add(new SqlParameter("@StartDateTo", startDateTo.Value.Date));
        }

        if (landId.HasValue && landId.Value > 0)
        {
            whereClauses.Add("p.LandId = @LandId");
            parameters.Add(new SqlParameter("@LandId", landId.Value));
        }

        if (commodityId.HasValue && commodityId.Value > 0)
        {
            whereClauses.Add("p.CommodityId = @CommodityId");
            parameters.Add(new SqlParameter("@CommodityId", commodityId.Value));
        }

        if (status.HasValue)
        {
            whereClauses.Add("p.Status = @Status");
            parameters.Add(new SqlParameter("@Status", status.Value.ToString()));
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var sql = $@"
            SELECT p.PlantingId, p.PlantingCode, p.SeasonName, p.Status, p.TargetYield,
                   l.LandName, c.CommodityName, c.ResultUnit,
                   ISNULL(h.TotalYield, 0) AS ActualYield,
                   ISNULL(h.TotalRevenue, 0) AS TotalRevenue,
                   ISNULL(oc.TotalCost, 0) AS TotalOperationalCost,
                   ISNULL(mu.TotalMatCost, 0) AS TotalMaterialCost
            FROM dbo.Plantings p
            INNER JOIN dbo.Lands l ON p.LandId = l.LandId
            INNER JOIN dbo.Commodities c ON p.CommodityId = c.CommodityId
            LEFT JOIN (
                SELECT PlantingId, SUM(YieldAmount) AS TotalYield, SUM(TotalRevenue) AS TotalRevenue
                FROM dbo.Harvests
                GROUP BY PlantingId
            ) h ON p.PlantingId = h.PlantingId
            LEFT JOIN (
                SELECT PlantingId, SUM(Amount) AS TotalCost
                FROM dbo.OperationalCosts
                GROUP BY PlantingId
            ) oc ON p.PlantingId = oc.PlantingId
            LEFT JOIN (
                SELECT a.PlantingId, SUM(u.Subtotal) AS TotalMatCost
                FROM dbo.MaterialUsages u
                INNER JOIN dbo.FarmingActivities a ON u.ActivityId = a.ActivityId
                GROUP BY a.PlantingId
            ) mu ON p.PlantingId = mu.PlantingId
            {whereSql}
            ORDER BY p.StartDate DESC, p.PlantingId DESC;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        foreach (var p in parameters)
            cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));

        var list = new List<ProfitLossReportDto>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new ProfitLossReportDto
            {
                PlantingId = reader.GetInt32(reader.GetOrdinal("PlantingId")),
                PlantingCode = reader.GetString(reader.GetOrdinal("PlantingCode")),
                SeasonName = reader.GetString(reader.GetOrdinal("SeasonName")),
                Status = Enum.TryParse<PlantingStatus>(reader.GetString(reader.GetOrdinal("Status")), out var st) ? st : PlantingStatus.Planned,
                TargetYield = reader.GetDecimal(reader.GetOrdinal("TargetYield")),
                LandName = reader.GetString(reader.GetOrdinal("LandName")),
                CommodityName = reader.GetString(reader.GetOrdinal("CommodityName")),
                ResultUnit = reader.GetString(reader.GetOrdinal("ResultUnit")),
                ActualYield = reader.GetDecimal(reader.GetOrdinal("ActualYield")),
                TotalRevenue = reader.GetDecimal(reader.GetOrdinal("TotalRevenue")),
                TotalOperationalCost = reader.GetDecimal(reader.GetOrdinal("TotalOperationalCost")),
                TotalMaterialCost = reader.GetDecimal(reader.GetOrdinal("TotalMaterialCost"))
            });
        }

        return list;
    }
}
