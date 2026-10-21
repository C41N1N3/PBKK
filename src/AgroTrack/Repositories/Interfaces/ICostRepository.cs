using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Utilities;

namespace AgroTrack.Repositories.Interfaces;

public interface ICostRepository
{
    Task<OperationalCost?> GetByIdAsync(int costId, CancellationToken cancellationToken = default);
    Task<PagedResult<OperationalCost>> GetPagedAsync(
        int? plantingIdFilter,
        CostCategory? categoryFilter,
        DateTime? startDate,
        DateTime? endDate,
        string? searchTerm,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperationalCost>> GetByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalCostByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<CostCategory, decimal>> GetCostsByCategoryAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(OperationalCost cost, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(OperationalCost cost, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int costId, CancellationToken cancellationToken = default);
}
