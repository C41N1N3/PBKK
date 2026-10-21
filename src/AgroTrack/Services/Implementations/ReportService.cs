using AgroTrack.DTOs;
using AgroTrack.Enums;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Services.Interfaces;

namespace AgroTrack.Services.Implementations;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<IReadOnlyList<ProfitLossReportDto>> GetProfitLossReportAsync(
        DateTime? startDateFrom,
        DateTime? startDateTo,
        int? landId,
        int? commodityId,
        PlantingStatus? status,
        CancellationToken cancellationToken = default)
    {
        return await _reportRepository.GetProfitLossReportAsync(
            startDateFrom,
            startDateTo,
            landId,
            commodityId,
            status,
            cancellationToken);
    }
}
