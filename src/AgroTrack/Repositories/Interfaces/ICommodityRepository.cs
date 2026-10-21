using AgroTrack.Models;
using AgroTrack.Utilities;

namespace AgroTrack.Repositories.Interfaces;

public interface ICommodityRepository
{
    Task<Commodity?> GetByIdAsync(int commodityId, CancellationToken cancellationToken = default);
    Task<Commodity?> GetByCodeAsync(string commodityCode, CancellationToken cancellationToken = default);
    Task<PagedResult<Commodity>> GetPagedAsync(string? searchTerm, bool? isActiveFilter, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Commodity>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Commodity commodity, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Commodity commodity, CancellationToken cancellationToken = default);
    Task<bool> SetActiveStatusAsync(int commodityId, bool isActive, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int commodityId, CancellationToken cancellationToken = default);
    Task<int> GetPlantingsCountAsync(int commodityId, CancellationToken cancellationToken = default);
}
