using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Services.Interfaces;
using AgroTrack.Utilities;

namespace AgroTrack.Services.Implementations;

public class CommodityService : ICommodityService
{
    private readonly ICommodityRepository _commodityRepository;
    private readonly ISessionService _sessionService;

    public CommodityService(ICommodityRepository commodityRepository, ISessionService sessionService)
    {
        _commodityRepository = commodityRepository;
        _sessionService = sessionService;
    }

    public async Task<PagedResult<Commodity>> GetCommoditiesPagedAsync(
        string? searchTerm, 
        bool? isActiveFilter, 
        int pageIndex, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        return await _commodityRepository.GetPagedAsync(searchTerm, isActiveFilter, pageIndex, pageSize, cancellationToken);
    }

    public async Task<IReadOnlyList<Commodity>> GetActiveCommoditiesAsync(CancellationToken cancellationToken = default)
    {
        return await _commodityRepository.GetAllActiveAsync(cancellationToken);
    }

    public async Task<Result<int>> CreateCommodityAsync(Commodity commodity, CancellationToken cancellationToken = default)
    {
        if (!_sessionService.CurrentUser?.CanManageMasterData ?? true)
            return Result<int>.Failure("Akun Anda tidak memiliki izin untuk menambah komoditas.");

        if (string.IsNullOrWhiteSpace(commodity.CommodityCode))
            return Result<int>.Failure("Kode komoditas wajib diisi.");

        if (string.IsNullOrWhiteSpace(commodity.CommodityName))
            return Result<int>.Failure("Nama komoditas wajib diisi.");

        if (string.IsNullOrWhiteSpace(commodity.ResultUnit))
            return Result<int>.Failure("Satuan hasil panen wajib diisi.");

        if (commodity.EstimatedGrowthDays < 0)
            return Result<int>.Failure("Estimasi hari tumbuh tidak boleh bernilai negatif.");

        var existing = await _commodityRepository.GetByCodeAsync(commodity.CommodityCode, cancellationToken);
        if (existing != null)
            return Result<int>.Failure($"Kode komoditas '{commodity.CommodityCode}' sudah digunakan.");

        var id = await _commodityRepository.CreateAsync(commodity, cancellationToken);
        return Result<int>.Success(id);
    }

    public async Task<Result> UpdateCommodityAsync(Commodity commodity, CancellationToken cancellationToken = default)
    {
        if (!_sessionService.CurrentUser?.CanManageMasterData ?? true)
            return Result.Failure("Akun Anda tidak memiliki izin untuk mengubah komoditas.");

        if (string.IsNullOrWhiteSpace(commodity.CommodityCode))
            return Result.Failure("Kode komoditas wajib diisi.");

        if (string.IsNullOrWhiteSpace(commodity.CommodityName))
            return Result.Failure("Nama komoditas wajib diisi.");

        if (string.IsNullOrWhiteSpace(commodity.ResultUnit))
            return Result.Failure("Satuan hasil panen wajib diisi.");

        if (commodity.EstimatedGrowthDays < 0)
            return Result.Failure("Estimasi hari tumbuh tidak boleh bernilai negatif.");

        var existing = await _commodityRepository.GetByCodeAsync(commodity.CommodityCode, cancellationToken);
        if (existing != null && existing.CommodityId != commodity.CommodityId)
            return Result.Failure($"Kode komoditas '{commodity.CommodityCode}' sudah digunakan oleh komoditas lain.");

        var success = await _commodityRepository.UpdateAsync(commodity, cancellationToken);
        return success ? Result.Success() : Result.Failure("Gagal memperbarui data komoditas.");
    }

    public async Task<Result> SetCommodityActiveStatusAsync(int commodityId, bool isActive, CancellationToken cancellationToken = default)
    {
        if (!_sessionService.CurrentUser?.CanManageMasterData ?? true)
            return Result.Failure("Akun Anda tidak memiliki izin untuk mengubah status komoditas.");

        var success = await _commodityRepository.SetActiveStatusAsync(commodityId, isActive, cancellationToken);
        return success ? Result.Success() : Result.Failure("Gagal memperbarui status komoditas.");
    }

    public async Task<Result> DeleteCommodityAsync(int commodityId, CancellationToken cancellationToken = default)
    {
        if (!_sessionService.CurrentUser?.CanManageMasterData ?? true)
            return Result.Failure("Akun Anda tidak memiliki izin untuk menghapus komoditas.");

        var plantingsCount = await _commodityRepository.GetPlantingsCountAsync(commodityId, cancellationToken);
        if (plantingsCount > 0)
        {
            return Result.Failure($"Komoditas ini tidak dapat dihapus karena telah terhubung dengan {plantingsCount} penanaman. Silakan nonaktifkan komoditas ini.");
        }

        try
        {
            var success = await _commodityRepository.DeleteAsync(commodityId, cancellationToken);
            return success ? Result.Success() : Result.Failure("Data komoditas tidak ditemukan.");
        }
        catch
        {
            return Result.Failure("Komoditas tidak dapat dihapus karena memiliki keterkaitan data transaksi. Silakan ubah status menjadi Nonaktif.");
        }
    }
}
