using AgroTrack.DTOs;
using AgroTrack.Models;

namespace AgroTrack.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TargetVsActualDto>> GetTargetVsActualAsync(int count = 6, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlyHarvestDto>> GetMonthlyHarvestTrendsAsync(int monthsCount = 6, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryCostDto>> GetCostByCategoryDistributionAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommodityDistributionDto>> GetCommodityDistributionAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UpcomingHarvestDto>> GetUpcomingHarvestsAsync(int count = 5, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FarmingActivity>> GetRecentActivitiesAsync(int count = 5, CancellationToken cancellationToken = default);
}
