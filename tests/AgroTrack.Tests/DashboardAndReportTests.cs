using AgroTrack.DTOs;
using AgroTrack.Enums;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Services.Implementations;
using Xunit;

namespace AgroTrack.Tests;

public class MockDashboardRepository : IDashboardRepository
{
    public Task<DashboardSummaryDto> GetSummaryAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new DashboardSummaryDto
        {
            TotalActiveLands = 5,
            TotalLandAreaHectares = 22.5m,
            ActivePlantingsCount = 4,
            TodayActivitiesCount = 3,
            LowStockMaterialsCount = 2,
            TotalHarvestYield = 18500m,
            TotalOperationalCost = 28_000_000m,
            TotalRevenue = 45_000_000m
        });
    }

    public Task<IReadOnlyList<TargetVsActualDto>> GetTargetVsActualAsync(int count, CancellationToken cancellationToken = default)
    {
        var list = new List<TargetVsActualDto>
        {
            new TargetVsActualDto
            {
                PlantingId = 1,
                PlantingCode = "PL-2026-001",
                TargetYield = 10000m,
                ActualYield = 9200m
            },
            new TargetVsActualDto
            {
                PlantingId = 2,
                PlantingCode = "PL-2026-002",
                TargetYield = 0m, // zero target test
                ActualYield = 500m
            }
        };
        return Task.FromResult<IReadOnlyList<TargetVsActualDto>>(list.AsReadOnly());
    }

    public Task<IReadOnlyList<MonthlyHarvestDto>> GetMonthlyHarvestTrendsAsync(int monthsCount, CancellationToken cancellationToken = default)
    {
        var list = new List<MonthlyHarvestDto>
        {
            new MonthlyHarvestDto { MonthYear = "2026-03", TotalYield = 8000m, TotalRevenue = 20_000_000m },
            new MonthlyHarvestDto { MonthYear = "2026-04", TotalYield = 12000m, TotalRevenue = 30_000_000m }
        };
        return Task.FromResult<IReadOnlyList<MonthlyHarvestDto>>(list.AsReadOnly());
    }

    public Task<IReadOnlyList<CategoryCostDto>> GetCostByCategoryDistributionAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var list = new List<CategoryCostDto>
        {
            new CategoryCostDto { Category = CostCategory.Labor, TotalAmount = 15_000_000m, PercentageOfTotal = 60m },
            new CategoryCostDto { Category = CostCategory.Fuel, TotalAmount = 10_000_000m, PercentageOfTotal = 40m }
        };
        return Task.FromResult<IReadOnlyList<CategoryCostDto>>(list.AsReadOnly());
    }

    public Task<IReadOnlyList<CommodityDistributionDto>> GetCommodityDistributionAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<CommodityDistributionDto>
        {
            new CommodityDistributionDto { CommodityName = "Padi", PlantingsCount = 2, TotalAreaHectares = 10m, PercentageOfTotal = 50m }
        };
        return Task.FromResult<IReadOnlyList<CommodityDistributionDto>>(list.AsReadOnly());
    }

    public Task<IReadOnlyList<UpcomingHarvestDto>> GetUpcomingHarvestsAsync(int count, CancellationToken cancellationToken = default)
    {
        var list = new List<UpcomingHarvestDto>
        {
            new UpcomingHarvestDto
            {
                PlantingId = 1,
                PlantingCode = "PL-2026-001",
                ExpectedHarvestDate = DateTime.Today.AddDays(7)
            }
        };
        return Task.FromResult<IReadOnlyList<UpcomingHarvestDto>>(list.AsReadOnly());
    }
}

public class MockReportRepository : IReportRepository
{
    public Task<IReadOnlyList<ProfitLossReportDto>> GetProfitLossReportAsync(
        DateTime? startDateFrom,
        DateTime? startDateTo,
        int? landId,
        int? commodityId,
        PlantingStatus? status,
        CancellationToken cancellationToken = default)
    {
        var list = new List<ProfitLossReportDto>
        {
            new ProfitLossReportDto
            {
                PlantingId = 1,
                PlantingCode = "PL-001",
                SeasonName = "Musim A",
                TargetYield = 10000m,
                ActualYield = 9500m,
                TotalOperationalCost = 5_000_000m,
                TotalMaterialCost = 3_000_000m,
                TotalRevenue = 15_000_000m
            },
            new ProfitLossReportDto
            {
                PlantingId = 2,
                PlantingCode = "PL-002",
                SeasonName = "Musim B",
                TargetYield = 0m, // Zero Target test
                ActualYield = 1000m,
                TotalOperationalCost = 4_000_000m,
                TotalMaterialCost = 2_000_000m,
                TotalRevenue = 3_000_000m // Loss scenario
            }
        };
        return Task.FromResult<IReadOnlyList<ProfitLossReportDto>>(list.AsReadOnly());
    }
}

public class DashboardAndReportTests
{
    [Fact]
    public void DashboardSummary_EstimatedProfit_IsCalculatedCorrectly()
    {
        var summary = new DashboardSummaryDto
        {
            TotalRevenue = 50_000_000m,
            TotalOperationalCost = 32_000_000m
        };

        Assert.Equal(18_000_000m, summary.EstimatedNetProfit);
    }

    [Fact]
    public void TargetVsActualDto_TargetPercentage_HandlesZeroTargetSafely()
    {
        var normalItem = new TargetVsActualDto
        {
            TargetYield = 10000m,
            ActualYield = 8500m
        };
        Assert.Equal(85.0m, normalItem.TargetPercentage);

        var zeroItem = new TargetVsActualDto
        {
            TargetYield = 0m,
            ActualYield = 500m
        };
        Assert.Equal(0m, zeroItem.TargetPercentage);
        Assert.Equal("-", zeroItem.PercentageDisplay);
    }

    [Fact]
    public void ProfitLossReport_FinancialMetrics_AreAccurate()
    {
        var profitable = new ProfitLossReportDto
        {
            TargetYield = 10000m,
            ActualYield = 9500m,
            TotalMaterialCost = 3_000_000m,
            TotalOperationalCost = 5_000_000m,
            TotalRevenue = 15_000_000m
        };

        Assert.Equal(8_000_000m, profitable.TotalCost);
        Assert.Equal(7_000_000m, profitable.NetProfit);
        Assert.True(profitable.IsProfit);
        Assert.Equal(95.0m, profitable.TargetAchievementPercentage);

        var lossMaking = new ProfitLossReportDto
        {
            TargetYield = 0m,
            ActualYield = 1000m,
            TotalMaterialCost = 2_000_000m,
            TotalOperationalCost = 4_000_000m,
            TotalRevenue = 3_000_000m
        };

        Assert.Equal(6_000_000m, lossMaking.TotalCost);
        Assert.Equal(-3_000_000m, lossMaking.NetProfit);
        Assert.False(lossMaking.IsProfit);
        Assert.Null(lossMaking.TargetAchievementPercentage); // division by zero guarded
    }
}
