using AgroTrack.Models;
using AgroTrack.Utilities;

namespace AgroTrack.Services.Interfaces;

public interface ICommodityService
{
    Task<PagedResult<Commodity>> GetCommoditiesPagedAsync(string? searchTerm, bool? isActiveFilter, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Commodity>> GetActiveCommoditiesAsync(CancellationToken cancellationToken = default);
    Task<Result<int>> CreateCommodityAsync(Commodity commodity, CancellationToken cancellationToken = default);
    Task<Result> UpdateCommodityAsync(Commodity commodity, CancellationToken cancellationToken = default);
    Task<Result> SetCommodityActiveStatusAsync(int commodityId, bool isActive, CancellationToken cancellationToken = default);
    Task<Result> DeleteCommodityAsync(int commodityId, CancellationToken cancellationToken = default);
}
