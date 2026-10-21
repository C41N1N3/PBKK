using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Utilities;

namespace AgroTrack.Services.Interfaces;

public interface ILandService
{
    Task<PagedResult<Land>> GetLandsPagedAsync(string? searchTerm, LandStatus? statusFilter, string? soilTypeFilter, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Land>> GetActiveLandsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetSoilTypesAsync(CancellationToken cancellationToken = default);
    Task<Result<int>> CreateLandAsync(Land land, CancellationToken cancellationToken = default);
    Task<Result> UpdateLandAsync(Land land, CancellationToken cancellationToken = default);
    Task<Result> DeleteLandAsync(int landId, CancellationToken cancellationToken = default);
}
