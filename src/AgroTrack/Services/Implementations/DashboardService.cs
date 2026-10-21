using AgroTrack.DTOs;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Services.Interfaces;

namespace AgroTrack.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IActivityRepository _activityRepository;

    public DashboardService(
        IDashboardRepository dashboardRepository,
        IActivityRepository activityRepository)
    {
        _dashboardRepository = dashboardRepository;
        _activityRepository = activityRepository;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        return await _dashboardRepository.GetSummaryAsync(startDate, endDate, cancellationToken);
    }

    public async Task<IReadOnlyList<TargetVsActualDto>> GetTargetVsActualAsync(int count = 6, CancellationToken cancellationToken = default)
    {
        return await _dashboardRepository.GetTargetVsActualAsync(count, cancellationToken);
    }

    public async Task<IReadOnlyList<MonthlyHarvestDto>> GetMonthlyHarvestTrendsAsync(int monthsCount = 6, CancellationToken cancellationToken = default)
    {
        return await _dashboardRepository.GetMonthlyHarvestTrendsAsync(monthsCount, cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryCostDto>> GetCostByCategoryDistributionAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        return await _dashboardRepository.GetCostByCategoryDistributionAsync(startDate, endDate, cancellationToken);
    }

    public async Task<IReadOnlyList<CommodityDistributionDto>> GetCommodityDistributionAsync(CancellationToken cancellationToken = default)
    {
        return await _dashboardRepository.GetCommodityDistributionAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UpcomingHarvestDto>> GetUpcomingHarvestsAsync(int count = 5, CancellationToken cancellationToken = default)
    {
        return await _dashboardRepository.GetUpcomingHarvestsAsync(count, cancellationToken);
    }

    public async Task<IReadOnlyList<FarmingActivity>> GetRecentActivitiesAsync(int count = 5, CancellationToken cancellationToken = default)
    {
        return await _activityRepository.GetRecentActivitiesAsync(count, cancellationToken);
    }
}
