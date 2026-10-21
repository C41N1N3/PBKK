using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Utilities;

namespace AgroTrack.Repositories.Interfaces;

public interface IHarvestRepository
{
    Task<Harvest?> GetByIdAsync(int harvestId, CancellationToken cancellationToken = default);
    Task<PagedResult<Harvest>> GetPagedAsync(
        int? plantingIdFilter,
        HarvestQuality? qualityFilter,
        DateTime? startDate,
        DateTime? endDate,
        string? searchTerm,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Harvest>> GetByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalYieldByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalRevenueByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(string MonthYear, decimal TotalYield, decimal TotalRevenue)>> GetMonthlyHarvestSummaryAsync(int monthsCount, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Harvest harvest, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Harvest harvest, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int harvestId, CancellationToken cancellationToken = default);
}
