using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Utilities;

namespace AgroTrack.Services.Interfaces;

public interface IPlantingService
{
    Task<PagedResult<Planting>> GetPlantingsPagedAsync(
        string? searchTerm, 
        PlantingStatus? statusFilter, 
        int? landIdFilter, 
        int? commodityIdFilter, 
        DateTime? startDateFrom, 
        DateTime? startDateTo, 
        int pageIndex, 
        int pageSize, 
        CancellationToken cancellationToken = default);
    Task<Planting?> GetPlantingByIdAsync(int plantingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Planting>> GetActivePlantingsAsync(CancellationToken cancellationToken = default);
    Task<Result<int>> CreatePlantingAsync(Planting planting, CancellationToken cancellationToken = default);
    Task<Result> UpdatePlantingAsync(Planting planting, CancellationToken cancellationToken = default);
    Task<Result> ChangeStatusAsync(int plantingId, PlantingStatus newStatus, CancellationToken cancellationToken = default);
    Task<Result> DeletePlantingAsync(int plantingId, CancellationToken cancellationToken = default);
}
