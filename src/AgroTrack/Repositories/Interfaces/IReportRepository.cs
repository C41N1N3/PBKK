using AgroTrack.DTOs;
using AgroTrack.Enums;

namespace AgroTrack.Repositories.Interfaces;

public interface IReportRepository
{
    Task<IReadOnlyList<ProfitLossReportDto>> GetProfitLossReportAsync(
        DateTime? startDateFrom,
        DateTime? startDateTo,
        int? landId,
        int? commodityId,
        PlantingStatus? status,
        CancellationToken cancellationToken = default);
}
