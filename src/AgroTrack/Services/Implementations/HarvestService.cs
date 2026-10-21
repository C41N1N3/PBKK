using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Security;
using AgroTrack.Services.Interfaces;
using AgroTrack.Utilities;

namespace AgroTrack.Services.Implementations;

public class HarvestService : IHarvestService
{
    private readonly IHarvestRepository _harvestRepository;
    private readonly IPlantingRepository _plantingRepository;
    private readonly ISessionService _sessionService;

    public HarvestService(
        IHarvestRepository harvestRepository,
        IPlantingRepository plantingRepository,
        ISessionService sessionService)
    {
        _harvestRepository = harvestRepository;
        _plantingRepository = plantingRepository;
        _sessionService = sessionService;
    }

    public async Task<PagedResult<Harvest>> GetHarvestsPagedAsync(
        int? plantingIdFilter,
        HarvestQuality? qualityFilter,
        DateTime? startDate,
        DateTime? endDate,
        string? searchTerm,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _harvestRepository.GetPagedAsync(
            plantingIdFilter,
            qualityFilter,
            startDate,
            endDate,
            searchTerm,
            pageIndex,
            pageSize,
            cancellationToken);
    }

    public async Task<Harvest?> GetHarvestByIdAsync(int harvestId, CancellationToken cancellationToken = default)
    {
        return await _harvestRepository.GetByIdAsync(harvestId, cancellationToken);
    }

    public async Task<IReadOnlyList<Harvest>> GetHarvestsByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default)
    {
        return await _harvestRepository.GetByPlantingIdAsync(plantingId, cancellationToken);
    }

    public async Task<decimal> GetTotalYieldByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default)
    {
        return await _harvestRepository.GetTotalYieldByPlantingIdAsync(plantingId, cancellationToken);
    }

    public async Task<decimal> GetTotalRevenueByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default)
    {
        return await _harvestRepository.GetTotalRevenueByPlantingIdAsync(plantingId, cancellationToken);
    }

    public async Task<IReadOnlyList<(string MonthYear, decimal TotalYield, decimal TotalRevenue)>> GetMonthlyHarvestSummaryAsync(int monthsCount, CancellationToken cancellationToken = default)
    {
        return await _harvestRepository.GetMonthlyHarvestSummaryAsync(monthsCount, cancellationToken);
    }

    public async Task<Result<int>> CreateHarvestAsync(Harvest harvest, CancellationToken cancellationToken = default)
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser == null)
            return Result<int>.Failure("Sesi pengguna tidak valid. Silakan login kembali.");

        if (harvest.PlantingId <= 0)
            return Result<int>.Failure("Penanaman wajib dipilih.");

        if (harvest.YieldAmount <= 0)
            return Result<int>.Failure("Jumlah hasil panen harus lebih dari nol.");

        if (harvest.SellingPricePerUnit < 0)
            return Result<int>.Failure("Harga jual per satuan tidak boleh bernilai negatif.");

        var planting = await _plantingRepository.GetByIdAsync(harvest.PlantingId, cancellationToken);
        if (planting == null)
            return Result<int>.Failure("Data penanaman yang dipilih tidak ditemukan.");

        if (planting.Status == PlantingStatus.Cancelled)
            return Result<int>.Failure("Penanaman berstatus Dibatalkan tidak dapat menerima pencatatan hasil panen.");

        if (harvest.HarvestDate.Date < planting.StartDate.Date)
            return Result<int>.Failure($"Tanggal panen ({harvest.HarvestDate:dd/MM/yyyy}) tidak boleh mendahului tanggal mulai penanaman ({planting.StartDate:dd/MM/yyyy}).");

        harvest.CreatedBy = currentUser.UserId;
        harvest.Notes = harvest.Notes?.Trim();

        var newId = await _harvestRepository.CreateAsync(harvest, cancellationToken);
        return Result<int>.Success(newId);
    }

    public async Task<Result> UpdateHarvestAsync(Harvest harvest, CancellationToken cancellationToken = default)
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser == null)
            return Result.Failure("Sesi pengguna tidak valid. Silakan login kembali.");

        var existing = await _harvestRepository.GetByIdAsync(harvest.HarvestId, cancellationToken);
        if (existing == null)
            return Result.Failure("Data hasil panen yang akan diubah tidak ditemukan.");

        if (currentUser.Role == UserRole.FieldOfficer && existing.CreatedBy != currentUser.UserId)
            return Result.Failure("Akun Anda hanya diizinkan mengubah hasil panen yang Anda catat sendiri.");

        if (harvest.YieldAmount <= 0)
            return Result.Failure("Jumlah hasil panen harus lebih dari nol.");

        if (harvest.SellingPricePerUnit < 0)
            return Result.Failure("Harga jual per satuan tidak boleh bernilai negatif.");

        var planting = await _plantingRepository.GetByIdAsync(harvest.PlantingId, cancellationToken);
        if (planting == null)
            return Result.Failure("Data penanaman tidak ditemukan.");

        if (planting.Status == PlantingStatus.Cancelled)
            return Result.Failure("Penanaman telah dibatalkan. Hasil panen tidak dapat diubah.");

        harvest.Notes = harvest.Notes?.Trim();

        var success = await _harvestRepository.UpdateAsync(harvest, cancellationToken);
        return success ? Result.Success() : Result.Failure("Gagal memperbarui hasil panen di database.");
    }

    public async Task<Result> DeleteHarvestAsync(int harvestId, CancellationToken cancellationToken = default)
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser == null)
            return Result.Failure("Sesi pengguna tidak valid. Silakan login kembali.");

        if (currentUser.Role == UserRole.FieldOfficer)
            return Result.Failure("Akun Anda tidak memiliki izin untuk menghapus data hasil panen.");

        var existing = await _harvestRepository.GetByIdAsync(harvestId, cancellationToken);
        if (existing == null)
            return Result.Failure("Data hasil panen yang akan dihapus tidak ditemukan.");

        var success = await _harvestRepository.DeleteAsync(harvestId, cancellationToken);
        return success ? Result.Success() : Result.Failure("Gagal menghapus hasil panen dari database.");
    }
}
