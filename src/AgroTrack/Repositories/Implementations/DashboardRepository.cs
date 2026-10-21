using System.Data;
using AgroTrack.Data;
using AgroTrack.DTOs;
using AgroTrack.Enums;
using AgroTrack.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace AgroTrack.Repositories.Implementations;

public class DashboardRepository : IDashboardRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DashboardRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var summary = new DashboardSummaryDto();

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // 1. Lands Count and Total Area
        const string landsSql = @"
            SELECT COUNT(*), ISNULL(SUM(AreaHectares), 0) 
            FROM dbo.Lands 
            WHERE Status = 'Active';";

        using (var cmd = new SqlCommand(landsSql, conn))
        using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                summary.TotalActiveLands = reader.GetInt32(0);
                summary.TotalLandAreaHectares = reader.GetDecimal(1);
            }
        }

        // 2. Active Plantings Count
        const string plantingsSql = @"
            SELECT COUNT(*) 
            FROM dbo.Plantings 
            WHERE Status = 'Active';";

        using (var cmd = new SqlCommand(plantingsSql, conn))
        {
            summary.ActivePlantingsCount = (int)(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0);
        }

        // 3. Today's Activities Count
        const string activitiesSql = @"
            SELECT COUNT(*) 
            FROM dbo.FarmingActivities 
            WHERE ActivityDate = CAST(SYSUTCDATETIME() AS DATE);";

        using (var cmd = new SqlCommand(activitiesSql, conn))
        {
            summary.TodayActivitiesCount = (int)(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0);
        }

        // 4. Low Stock Materials Count
        const string materialsSql = @"
            SELECT COUNT(*) 
            FROM dbo.AgriculturalMaterials 
            WHERE Stock <= MinimumStock AND IsActive = 1;";

        using (var cmd = new SqlCommand(materialsSql, conn))
        {
            summary.LowStockMaterialsCount = (int)(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0);
        }

        // 5. Total Harvest Yield & Revenue in selected period
        var harvestWhere = "";
        if (startDate.HasValue && endDate.HasValue)
            harvestWhere = "WHERE HarvestDate >= @StartDate AND HarvestDate <= @EndDate";
        else if (startDate.HasValue)
            harvestWhere = "WHERE HarvestDate >= @StartDate";
        else if (endDate.HasValue)
            harvestWhere = "WHERE HarvestDate <= @EndDate";

        var harvestSql = $@"
            SELECT ISNULL(SUM(YieldAmount), 0), ISNULL(SUM(TotalRevenue), 0)
            FROM dbo.Harvests
            {harvestWhere};";

        using (var cmd = new SqlCommand(harvestSql, conn))
        {
            if (startDate.HasValue) cmd.Parameters.AddWithValue("@StartDate", startDate.Value.Date);
            if (endDate.HasValue) cmd.Parameters.AddWithValue("@EndDate", endDate.Value.Date);

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                summary.TotalHarvestYield = reader.GetDecimal(0);
                summary.TotalRevenue = reader.GetDecimal(1);
            }
        }

        // 6. Total Operational Cost in selected period
        var costWhere = "";
        if (startDate.HasValue && endDate.HasValue)
            costWhere = "WHERE CostDate >= @StartDate AND CostDate <= @EndDate";
        else if (startDate.HasValue)
            costWhere = "WHERE CostDate >= @StartDate";
        else if (endDate.HasValue)
            costWhere = "WHERE CostDate <= @EndDate";

        var costSql = $@"
            SELECT ISNULL(SUM(Amount), 0)
            FROM dbo.OperationalCosts
            {costWhere};";

        using (var cmd = new SqlCommand(costSql, conn))
        {
            if (startDate.HasValue) cmd.Parameters.AddWithValue("@StartDate", startDate.Value.Date);
            if (endDate.HasValue) cmd.Parameters.AddWithValue("@EndDate", endDate.Value.Date);

            var costResult = await cmd.ExecuteScalarAsync(cancellationToken);
            summary.TotalOperationalCost = costResult != null && costResult != DBNull.Value ? Convert.ToDecimal(costResult) : 0m;
        }

        return summary;
    }

    public async Task<IReadOnlyList<TargetVsActualDto>> GetTargetVsActualAsync(int count, CancellationToken cancellationToken = default)
    {
        var sql = $@"
            SELECT TOP (@Count) p.PlantingId, p.PlantingCode, p.SeasonName, c.CommodityName, c.ResultUnit,
                   p.TargetYield, ISNULL(SUM(h.YieldAmount), 0) AS ActualYield
            FROM dbo.Plantings p
            INNER JOIN dbo.Commodities c ON p.CommodityId = c.CommodityId
            LEFT JOIN dbo.Harvests h ON p.PlantingId = h.PlantingId
            WHERE p.Status IN ('Active', 'ReadyToHarvest', 'Completed')
            GROUP BY p.PlantingId, p.PlantingCode, p.SeasonName, c.CommodityName, c.ResultUnit, p.TargetYield
            ORDER BY p.PlantingId DESC;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Count", count);

        var list = new List<TargetVsActualDto>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new TargetVsActualDto
            {
                PlantingId = reader.GetInt32(0),
                PlantingCode = reader.GetString(1),
                SeasonName = reader.GetString(2),
                CommodityName = reader.GetString(3),
                ResultUnit = reader.GetString(4),
                TargetYield = reader.GetDecimal(5),
                ActualYield = reader.GetDecimal(6)
            });
        }

        return list;
    }

    public async Task<IReadOnlyList<MonthlyHarvestDto>> GetMonthlyHarvestTrendsAsync(int monthsCount, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT FORMAT(HarvestDate, 'yyyy-MM') AS MonthYear,
                   FORMAT(HarvestDate, 'MMM yyyy') AS MonthLabel,
                   ISNULL(SUM(YieldAmount), 0) AS TotalYield,
                   ISNULL(SUM(TotalRevenue), 0) AS TotalRevenue
            FROM dbo.Harvests
            WHERE HarvestDate >= DATEADD(MONTH, -@MonthsCount, SYSUTCDATETIME())
            GROUP BY FORMAT(HarvestDate, 'yyyy-MM'), FORMAT(HarvestDate, 'MMM yyyy')
            ORDER BY MonthYear ASC;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MonthsCount", monthsCount);

        var list = new List<MonthlyHarvestDto>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new MonthlyHarvestDto
            {
                MonthYear = reader.GetString(0),
                MonthLabel = reader.GetString(1),
                TotalYield = reader.GetDecimal(2),
                TotalRevenue = reader.GetDecimal(3)
            });
        }

        if (list.Any())
        {
            var maxYield = list.Max(m => m.TotalYield);
            if (maxYield > 0)
            {
                foreach (var item in list)
                {
                    item.BarHeightPercentage = Math.Max(10, (double)(item.TotalYield / maxYield) * 100);
                }
            }
        }

        return list;
    }

    public async Task<IReadOnlyList<CategoryCostDto>> GetCostByCategoryDistributionAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var whereSql = "";
        if (startDate.HasValue && endDate.HasValue)
            whereSql = "WHERE CostDate >= @StartDate AND CostDate <= @EndDate";
        else if (startDate.HasValue)
            whereSql = "WHERE CostDate >= @StartDate";
        else if (endDate.HasValue)
            whereSql = "WHERE CostDate <= @EndDate";

        var sql = $@"
            SELECT Category, SUM(Amount) AS TotalAmount
            FROM dbo.OperationalCosts
            {whereSql}
            GROUP BY Category
            ORDER BY TotalAmount DESC;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        if (startDate.HasValue) cmd.Parameters.AddWithValue("@StartDate", startDate.Value.Date);
        if (endDate.HasValue) cmd.Parameters.AddWithValue("@EndDate", endDate.Value.Date);

        var list = new List<CategoryCostDto>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var catStr = reader.GetString(0);
            var amount = reader.GetDecimal(1);
            if (Enum.TryParse<CostCategory>(catStr, out var cat))
            {
                list.Add(new CategoryCostDto
                {
                    Category = cat,
                    TotalAmount = amount
                });
            }
        }

        var total = list.Sum(c => c.TotalAmount);
        if (total > 0)
        {
            foreach (var item in list)
            {
                item.PercentageOfTotal = Math.Round((item.TotalAmount / total) * 100m, 1);
            }
        }

        return list;
    }

    public async Task<IReadOnlyList<CommodityDistributionDto>> GetCommodityDistributionAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT c.CommodityName, COUNT(p.PlantingId) AS PlantingsCount, ISNULL(SUM(p.PlantedAreaHectares), 0) AS TotalArea
            FROM dbo.Commodities c
            LEFT JOIN dbo.Plantings p ON c.CommodityId = p.CommodityId AND p.Status NOT IN ('Cancelled')
            GROUP BY c.CommodityName
            HAVING COUNT(p.PlantingId) > 0
            ORDER BY TotalArea DESC;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);

        var list = new List<CommodityDistributionDto>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new CommodityDistributionDto
            {
                CommodityName = reader.GetString(0),
                PlantingsCount = reader.GetInt32(1),
                TotalAreaHectares = reader.GetDecimal(2)
            });
        }

        var totalArea = list.Sum(c => c.TotalAreaHectares);
        if (totalArea > 0)
        {
            foreach (var item in list)
            {
                item.PercentageOfTotal = Math.Round((item.TotalAreaHectares / totalArea) * 100m, 1);
            }
        }

        return list;
    }

    public async Task<IReadOnlyList<UpcomingHarvestDto>> GetUpcomingHarvestsAsync(int count, CancellationToken cancellationToken = default)
    {
        var sql = $@"
            SELECT TOP (@Count) p.PlantingId, p.PlantingCode, p.SeasonName, c.CommodityName, l.LandName, p.ExpectedHarvestDate
            FROM dbo.Plantings p
            INNER JOIN dbo.Commodities c ON p.CommodityId = c.CommodityId
            INNER JOIN dbo.Lands l ON p.LandId = l.LandId
            WHERE p.Status IN ('Active', 'ReadyToHarvest')
            ORDER BY p.ExpectedHarvestDate ASC;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Count", count);

        var list = new List<UpcomingHarvestDto>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new UpcomingHarvestDto
            {
                PlantingId = reader.GetInt32(0),
                PlantingCode = reader.GetString(1),
                SeasonName = reader.GetString(2),
                CommodityName = reader.GetString(3),
                LandName = reader.GetString(4),
                ExpectedHarvestDate = reader.GetDateTime(5)
            });
        }

        return list;
    }
}
