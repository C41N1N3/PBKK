using AgroTrack.DTOs;
using AgroTrack.Enums;

namespace AgroTrack.Services.Interfaces;

public interface IReportService
{
    Task<IReadOnlyList<ProfitLossReportDto>> GetProfitLossReportAsync(
        DateTime? startDateFrom,
        DateTime? startDateTo,
        int? landId,
        int? commodityId,
        PlantingStatus? status,
        CancellationToken cancellationToken = default);
}
