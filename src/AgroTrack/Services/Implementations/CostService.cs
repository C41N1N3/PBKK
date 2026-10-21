using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Security;
using AgroTrack.Services.Interfaces;
using AgroTrack.Utilities;

namespace AgroTrack.Services.Implementations;

public class CostService : ICostService
{
    private readonly ICostRepository _costRepository;
    private readonly IPlantingRepository _plantingRepository;
    private readonly ISessionService _sessionService;

    public CostService(
        ICostRepository costRepository,
        IPlantingRepository plantingRepository,
        ISessionService sessionService)
    {
        _costRepository = costRepository;
        _plantingRepository = plantingRepository;
        _sessionService = sessionService;
    }

    public async Task<PagedResult<OperationalCost>> GetCostsPagedAsync(
        int? plantingIdFilter,
        CostCategory? categoryFilter,
        DateTime? startDate,
        DateTime? endDate,
        string? searchTerm,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _costRepository.GetPagedAsync(
            plantingIdFilter,
            categoryFilter,
            startDate,
            endDate,
            searchTerm,
            pageIndex,
            pageSize,
            cancellationToken);
    }

    public async Task<OperationalCost?> GetCostByIdAsync(int costId, CancellationToken cancellationToken = default)
    {
        return await _costRepository.GetByIdAsync(costId, cancellationToken);
    }

    public async Task<IReadOnlyList<OperationalCost>> GetCostsByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default)
    {
        return await _costRepository.GetByPlantingIdAsync(plantingId, cancellationToken);
    }

    public async Task<decimal> GetTotalCostByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default)
    {
        return await _costRepository.GetTotalCostByPlantingIdAsync(plantingId, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<CostCategory, decimal>> GetCostsByCategoryAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        return await _costRepository.GetCostsByCategoryAsync(startDate, endDate, cancellationToken);
    }

    public async Task<Result<int>> CreateCostAsync(OperationalCost cost, CancellationToken cancellationToken = default)
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser == null)
            return Result<int>.Failure("Sesi pengguna tidak valid. Silakan login kembali.");

        if (cost.PlantingId <= 0)
            return Result<int>.Failure("Penanaman wajib dipilih.");

        if (cost.Amount <= 0)
            return Result<int>.Failure("Jumlah biaya operasional harus lebih dari nol rupiah.");

        if (string.IsNullOrWhiteSpace(cost.Description))
            return Result<int>.Failure("Deskripsi biaya operasional wajib diisi.");

        var planting = await _plantingRepository.GetByIdAsync(cost.PlantingId, cancellationToken);
        if (planting == null)
            return Result<int>.Failure("Data penanaman yang dipilih tidak ditemukan.");

        if (planting.Status == PlantingStatus.Completed)
            return Result<int>.Failure("Penanaman berstatus Selesai tidak dapat menerima pencatatan biaya baru.");

        if (planting.Status == PlantingStatus.Cancelled)
            return Result<int>.Failure("Penanaman berstatus Dibatalkan tidak dapat menerima pencatatan biaya baru.");

        cost.CreatedBy = currentUser.UserId;
        cost.Description = cost.Description.Trim();

        var newId = await _costRepository.CreateAsync(cost, cancellationToken);
        return Result<int>.Success(newId);
    }

    public async Task<Result> UpdateCostAsync(OperationalCost cost, CancellationToken cancellationToken = default)
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser == null)
            return Result.Failure("Sesi pengguna tidak valid. Silakan login kembali.");

        var existing = await _costRepository.GetByIdAsync(cost.CostId, cancellationToken);
        if (existing == null)
            return Result.Failure("Data biaya yang akan diubah tidak ditemukan.");

        if (currentUser.Role == UserRole.FieldOfficer && existing.CreatedBy != currentUser.UserId)
            return Result.Failure("Akun Anda hanya diizinkan mengubah biaya yang Anda catat sendiri.");

        if (cost.Amount <= 0)
            return Result.Failure("Jumlah biaya operasional harus lebih dari nol rupiah.");

        if (string.IsNullOrWhiteSpace(cost.Description))
            return Result.Failure("Deskripsi biaya operasional wajib diisi.");

        var planting = await _plantingRepository.GetByIdAsync(cost.PlantingId, cancellationToken);
        if (planting == null)
            return Result.Failure("Data penanaman tidak ditemukan.");

        if (planting.Status == PlantingStatus.Cancelled)
            return Result.Failure("Penanaman telah dibatalkan. Biaya tidak dapat diubah.");

        cost.Description = cost.Description.Trim();

        var success = await _costRepository.UpdateAsync(cost, cancellationToken);
        return success ? Result.Success() : Result.Failure("Gagal memperbarui biaya operasional di database.");
    }

    public async Task<Result> DeleteCostAsync(int costId, CancellationToken cancellationToken = default)
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser == null)
            return Result.Failure("Sesi pengguna tidak valid. Silakan login kembali.");

        if (currentUser.Role == UserRole.FieldOfficer)
            return Result.Failure("Akun Anda tidak memiliki izin untuk menghapus data biaya operasional.");

        var existing = await _costRepository.GetByIdAsync(costId, cancellationToken);
        if (existing == null)
            return Result.Failure("Data biaya yang akan dihapus tidak ditemukan.");

        var success = await _costRepository.DeleteAsync(costId, cancellationToken);
        return success ? Result.Success() : Result.Failure("Gagal menghapus biaya operasional dari database.");
    }
}
