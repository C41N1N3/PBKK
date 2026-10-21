using AgroTrack.DTOs;

namespace AgroTrack.Repositories.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardSummaryDto> GetSummaryAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TargetVsActualDto>> GetTargetVsActualAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlyHarvestDto>> GetMonthlyHarvestTrendsAsync(int monthsCount, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryCostDto>> GetCostByCategoryDistributionAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommodityDistributionDto>> GetCommodityDistributionAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UpcomingHarvestDto>> GetUpcomingHarvestsAsync(int count, CancellationToken cancellationToken = default);
}
