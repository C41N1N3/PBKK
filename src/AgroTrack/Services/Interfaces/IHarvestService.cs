using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Utilities;

namespace AgroTrack.Services.Interfaces;

public interface IHarvestService
{
    Task<PagedResult<Harvest>> GetHarvestsPagedAsync(
        int? plantingIdFilter,
        HarvestQuality? qualityFilter,
        DateTime? startDate,
        DateTime? endDate,
        string? searchTerm,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Harvest?> GetHarvestByIdAsync(int harvestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Harvest>> GetHarvestsByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalYieldByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalRevenueByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(string MonthYear, decimal TotalYield, decimal TotalRevenue)>> GetMonthlyHarvestSummaryAsync(int monthsCount, CancellationToken cancellationToken = default);
    Task<Result<int>> CreateHarvestAsync(Harvest harvest, CancellationToken cancellationToken = default);
    Task<Result> UpdateHarvestAsync(Harvest harvest, CancellationToken cancellationToken = default);
    Task<Result> DeleteHarvestAsync(int harvestId, CancellationToken cancellationToken = default);
}
