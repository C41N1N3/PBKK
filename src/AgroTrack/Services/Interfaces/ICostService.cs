using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Utilities;

namespace AgroTrack.Services.Interfaces;

public interface ICostService
{
    Task<PagedResult<OperationalCost>> GetCostsPagedAsync(
        int? plantingIdFilter,
        CostCategory? categoryFilter,
        DateTime? startDate,
        DateTime? endDate,
        string? searchTerm,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OperationalCost?> GetCostByIdAsync(int costId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperationalCost>> GetCostsByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalCostByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<CostCategory, decimal>> GetCostsByCategoryAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    Task<Result<int>> CreateCostAsync(OperationalCost cost, CancellationToken cancellationToken = default);
    Task<Result> UpdateCostAsync(OperationalCost cost, CancellationToken cancellationToken = default);
    Task<Result> DeleteCostAsync(int costId, CancellationToken cancellationToken = default);
}
