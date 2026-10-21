using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Services.Interfaces;
using AgroTrack.Utilities;

namespace AgroTrack.Services.Implementations;

public class LandService : ILandService
{
    private readonly ILandRepository _landRepository;
    private readonly ISessionService _sessionService;

    public LandService(ILandRepository landRepository, ISessionService sessionService)
    {
        _landRepository = landRepository;
        _sessionService = sessionService;
    }

    public async Task<PagedResult<Land>> GetLandsPagedAsync(
        string? searchTerm, 
        LandStatus? statusFilter, 
        string? soilTypeFilter, 
        int pageIndex, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        return await _landRepository.GetPagedAsync(searchTerm, statusFilter, soilTypeFilter, pageIndex, pageSize, cancellationToken);
    }

    public async Task<IReadOnlyList<Land>> GetActiveLandsAsync(CancellationToken cancellationToken = default)
    {
        return await _landRepository.GetAllActiveAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetSoilTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _landRepository.GetDistinctSoilTypesAsync(cancellationToken);
    }

    public async Task<Result<int>> CreateLandAsync(Land land, CancellationToken cancellationToken = default)
    {
        if (!_sessionService.CurrentUser?.CanManageMasterData ?? true)
            return Result<int>.Failure("Akun Anda tidak memiliki izin untuk menambah data lahan.");

        if (string.IsNullOrWhiteSpace(land.LandCode))
            return Result<int>.Failure("Kode lahan wajib diisi.");

        if (string.IsNullOrWhiteSpace(land.LandName))
            return Result<int>.Failure("Nama lahan wajib diisi.");

        if (string.IsNullOrWhiteSpace(land.Location))
            return Result<int>.Failure("Lokasi lahan wajib diisi.");

        if (land.AreaHectares <= 0)
            return Result<int>.Failure("Luas lahan harus lebih dari nol.");

        if (string.IsNullOrWhiteSpace(land.SoilType))
            return Result<int>.Failure("Jenis tanah wajib diisi.");

        // Check uniqueness
        var existing = await _landRepository.GetByCodeAsync(land.LandCode, cancellationToken);
        if (existing != null)
            return Result<int>.Failure($"Kode lahan '{land.LandCode}' sudah digunakan.");

        var id = await _landRepository.CreateAsync(land, cancellationToken);
        return Result<int>.Success(id);
    }

    public async Task<Result> UpdateLandAsync(Land land, CancellationToken cancellationToken = default)
    {
        if (!_sessionService.CurrentUser?.CanManageMasterData ?? true)
            return Result.Failure("Akun Anda tidak memiliki izin untuk mengubah data lahan.");

        if (string.IsNullOrWhiteSpace(land.LandCode))
            return Result.Failure("Kode lahan wajib diisi.");

        if (string.IsNullOrWhiteSpace(land.LandName))
            return Result.Failure("Nama lahan wajib diisi.");

        if (string.IsNullOrWhiteSpace(land.Location))
            return Result.Failure("Lokasi lahan wajib diisi.");

        if (land.AreaHectares <= 0)
            return Result.Failure("Luas lahan harus lebih dari nol.");

        if (string.IsNullOrWhiteSpace(land.SoilType))
            return Result.Failure("Jenis tanah wajib diisi.");

        // Check uniqueness with other lands
        var existing = await _landRepository.GetByCodeAsync(land.LandCode, cancellationToken);
        if (existing != null && existing.LandId != land.LandId)
            return Result.Failure($"Kode lahan '{land.LandCode}' sudah digunakan oleh lahan lain.");

        var success = await _landRepository.UpdateAsync(land, cancellationToken);
        return success ? Result.Success() : Result.Failure("Gagal memperbarui data lahan.");
    }

    public async Task<Result> DeleteLandAsync(int landId, CancellationToken cancellationToken = default)
    {
        if (!_sessionService.CurrentUser?.CanManageMasterData ?? true)
            return Result.Failure("Akun Anda tidak memiliki izin untuk menghapus data lahan.");

        var plantingsCount = await _landRepository.GetActivePlantingsCountAsync(landId, cancellationToken);
        if (plantingsCount > 0)
        {
            return Result.Failure($"Lahan tidak dapat dihapus karena masih digunakan dalam {plantingsCount} penanaman aktif. Anda dapat mengubah statusnya menjadi 'Nonaktif'.");
        }

        try
        {
            var success = await _landRepository.DeleteAsync(landId, cancellationToken);
            return success ? Result.Success() : Result.Failure("Data lahan tidak ditemukan.");
        }
        catch
        {
            return Result.Failure("Lahan tidak dapat dihapus karena terdapat riwayat transaksi penanaman sebelumnya. Silakan ubah status lahan menjadi 'Nonaktif'.");
        }
    }
}
