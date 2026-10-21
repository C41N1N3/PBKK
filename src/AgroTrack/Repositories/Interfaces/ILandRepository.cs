using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Utilities;

namespace AgroTrack.Repositories.Interfaces;

public interface ILandRepository
{
    Task<Land?> GetByIdAsync(int landId, CancellationToken cancellationToken = default);
    Task<Land?> GetByCodeAsync(string landCode, CancellationToken cancellationToken = default);
    Task<PagedResult<Land>> GetPagedAsync(string? searchTerm, LandStatus? statusFilter, string? soilTypeFilter, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Land>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctSoilTypesAsync(CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Land land, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Land land, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int landId, CancellationToken cancellationToken = default);
    Task<int> GetActivePlantingsCountAsync(int landId, CancellationToken cancellationToken = default);
}
