using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Utilities;

namespace AgroTrack.Services.Interfaces;

public interface IActivityService
{
    Task<PagedResult<FarmingActivity>> GetActivitiesPagedAsync(
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

    Task<FarmingActivity?> GetActivityByIdAsync(int activityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FarmingActivity>> GetActivitiesByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FarmingActivity>> GetRecentActivitiesAsync(int count, CancellationToken cancellationToken = default);
    Task<Result<int>> CreateActivityAsync(FarmingActivity activity, CancellationToken cancellationToken = default);
    Task<Result> UpdateActivityAsync(FarmingActivity activity, CancellationToken cancellationToken = default);
    Task<Result> DeleteActivityAsync(int activityId, CancellationToken cancellationToken = default);
}
