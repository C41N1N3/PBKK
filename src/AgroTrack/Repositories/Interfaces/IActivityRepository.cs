using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Utilities;

namespace AgroTrack.Repositories.Interfaces;

public interface IActivityRepository
{
    Task<FarmingActivity?> GetByIdAsync(int activityId, CancellationToken cancellationToken = default);
    Task<PagedResult<FarmingActivity>> GetPagedAsync(
        int? plantingIdFilter,
        ActivityType? typeFilter,
        ActivityStatus? statusFilter,
        int? officerIdFilter,
        DateTime? startDate,
        DateTime? endDate,
        string? searchTerm,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FarmingActivity>> GetByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FarmingActivity>> GetRecentActivitiesAsync(int count, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(FarmingActivity activity, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(FarmingActivity activity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int activityId, CancellationToken cancellationToken = default);
}
