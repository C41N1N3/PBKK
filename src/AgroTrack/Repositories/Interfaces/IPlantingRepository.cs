using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Utilities;

namespace AgroTrack.Repositories.Interfaces;

public interface IPlantingRepository
{
    Task<Planting?> GetByIdAsync(int plantingId, CancellationToken cancellationToken = default);
    Task<Planting?> GetByCodeAsync(string plantingCode, CancellationToken cancellationToken = default);
    Task<PagedResult<Planting>> GetPagedAsync(
        string? searchTerm, 
        PlantingStatus? statusFilter, 
        int? landIdFilter, 
        int? commodityIdFilter, 
        DateTime? startDateFrom, 
        DateTime? startDateTo, 
        int pageIndex, 
        int pageSize, 
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Planting>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetActiveOverlappingAreaAsync(int landId, DateTime startDate, DateTime expectedHarvestDate, int? excludePlantingId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Planting planting, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Planting planting, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int plantingId, PlantingStatus newStatus, DateTime? completionDate, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int plantingId, CancellationToken cancellationToken = default);
}
