using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Security;
using AgroTrack.Services.Interfaces;
using AgroTrack.Utilities;

namespace AgroTrack.Services.Implementations;

public class MaterialService : IMaterialService
{
    private readonly IMaterialRepository _materialRepository;
    private readonly IActivityRepository _activityRepository;
    private readonly ISessionService _sessionService;

    public MaterialService(
        IMaterialRepository materialRepository,
        IActivityRepository activityRepository,
        ISessionService sessionService)
    {
        _materialRepository = materialRepository;
        _activityRepository = activityRepository;
        _sessionService = sessionService;
    }

    public async Task<PagedResult<AgriculturalMaterial>> GetMaterialsPagedAsync(
        string? searchTerm,
        MaterialCategory? categoryFilter,
        bool? isLowStockFilter,
        bool? isActiveFilter,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _materialRepository.GetPagedAsync(
            searchTerm, categoryFilter, isLowStockFilter, isActiveFilter, pageIndex, pageSize, cancellationToken);
    }

    public async Task<AgriculturalMaterial?> GetMaterialByIdAsync(int materialId, CancellationToken cancellationToken = default)
    {
        return await _materialRepository.GetByIdAsync(materialId, cancellationToken);
    }

    public async Task<IReadOnlyList<AgriculturalMaterial>> GetAllActiveMaterialsAsync(CancellationToken cancellationToken = default)
    {
        return await _materialRepository.GetAllActiveAsync(cancellationToken);
    }

    public async Task<Result<int>> CreateMaterialAsync(AgriculturalMaterial material, CancellationToken cancellationToken = default)
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser == null)
            return Result<int>.Failure("Sesi pengguna tidak valid. Silakan login kembali.");

        if (currentUser.Role == UserRole.FieldOfficer)
            return Result<int>.Failure("Akun Anda tidak memiliki izin untuk menambah master bahan pertanian.");

        if (string.IsNullOrWhiteSpace(material.MaterialCode))
            return Result<int>.Failure("Kode bahan pertanian wajib diisi.");

        if (string.IsNullOrWhiteSpace(material.MaterialName))
            return Result<int>.Failure("Nama bahan pertanian wajib diisi.");

        if (string.IsNullOrWhiteSpace(material.Unit))
            return Result<int>.Failure("Satuan bahan wajib diisi.");

        if (material.Stock < 0)
            return Result<int>.Failure("Stok awal tidak boleh bernilai negatif.");

        if (material.MinimumStock < 0)
            return Result<int>.Failure("Batas minimum stok tidak boleh bernilai negatif.");

        if (material.UnitPrice < 0)
            return Result<int>.Failure("Harga satuan tidak boleh bernilai negatif.");

        var existing = await _materialRepository.GetByCodeAsync(material.MaterialCode, cancellationToken);
        if (existing != null)
            return Result<int>.Failure($"Kode bahan '{material.MaterialCode}' sudah digunakan.");

        material.MaterialCode = material.MaterialCode.Trim();
        material.MaterialName = material.MaterialName.Trim();
        material.Unit = material.Unit.Trim();

        var newId = await _materialRepository.CreateAsync(material, cancellationToken);
        return Result<int>.Success(newId);
    }

    public async Task<Result> UpdateMaterialAsync(AgriculturalMaterial material, CancellationToken cancellationToken = default)
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser == null)
            return Result.Failure("Sesi pengguna tidak valid. Silakan login kembali.");

        if (currentUser.Role == UserRole.FieldOfficer)
            return Result.Failure("Akun Anda tidak memiliki izin untuk mengubah data bahan pertanian.");

        if (string.IsNullOrWhiteSpace(material.MaterialName))
            return Result.Failure("Nama bahan pertanian wajib diisi.");

        if (string.IsNullOrWhiteSpace(material.Unit))
            return Result.Failure("Satuan bahan wajib diisi.");

        if (material.Stock < 0)
            return Result.Failure("Stok tidak boleh bernilai negatif.");

        if (material.MinimumStock < 0)
            return Result.Failure("Batas minimum stok tidak boleh bernilai negatif.");

        if (material.UnitPrice < 0)
            return Result.Failure("Harga satuan tidak boleh bernilai negatif.");

        material.MaterialName = material.MaterialName.Trim();
        material.Unit = material.Unit.Trim();

        var success = await _materialRepository.UpdateAsync(material, cancellationToken);
        return success ? Result.Success() : Result.Failure("Gagal memperbarui data bahan di database.");
    }

    public async Task<Result> DeleteMaterialAsync(int materialId, CancellationToken cancellationToken = default)
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser == null)
            return Result.Failure("Sesi pengguna tidak valid. Silakan login kembali.");

        if (currentUser.Role != UserRole.Administrator)
            return Result.Failure("Hanya Administrator yang memiliki izin untuk menghapus data bahan.");

        var success = await _materialRepository.DeleteAsync(materialId, cancellationToken);
        return success ? Result.Success() : Result.Failure("Gagal menghapus data bahan.");
    }

    public async Task<Result> RecordMaterialUsageAsync(MaterialUsage usage, CancellationToken cancellationToken = default)
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser == null)
            return Result.Failure("Sesi pengguna tidak valid. Silakan login kembali.");

        if (usage.ActivityId <= 0)
            return Result.Failure("Aktivitas lapangan terkait wajib dipilih.");

        if (usage.MaterialId <= 0)
            return Result.Failure("Bahan pertanian wajib dipilih.");

        if (usage.Quantity <= 0)
            return Result.Failure("Jumlah penggunaan bahan harus lebih dari nol.");

        // Check Activity and Planting status
        var activity = await _activityRepository.GetByIdAsync(usage.ActivityId, cancellationToken);
        if (activity == null)
            return Result.Failure("Aktivitas yang dipilih tidak ditemukan.");

        // Check material stock
        var material = await _materialRepository.GetByIdAsync(usage.MaterialId, cancellationToken);
        if (material == null)
            return Result.Failure("Bahan pertanian tidak ditemukan.");

        if (!material.IsActive)
            return Result.Failure($"Bahan '{material.MaterialName}' berstatus nonaktif dan tidak dapat digunakan.");

        if (material.Stock < usage.Quantity)
        {
            return Result.Failure(
                $"Stok bahan '{material.MaterialName}' tidak mencukupi! Stok saat ini: {material.Stock:N2} {material.Unit}, dibutuhkan: {usage.Quantity:N2} {material.Unit}.");
        }

        usage.CreatedBy = currentUser.UserId;
        usage.PriceAtUsage = material.UnitPrice;

        var success = await _materialRepository.RecordUsageWithStockDeductionAsync(usage, cancellationToken);
        return success
            ? Result.Success()
            : Result.Failure("Gagal mencatat penggunaan bahan. Terjadi masalah pada transaksi database atau stok tidak mencukupi.");
    }

    public async Task<Result> CancelMaterialUsageAsync(int materialUsageId, CancellationToken cancellationToken = default)
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser == null)
            return Result.Failure("Sesi pengguna tidak valid. Silakan login kembali.");

        var success = await _materialRepository.RevertUsageWithStockRestorationAsync(materialUsageId, cancellationToken);
        return success
            ? Result.Success()
            : Result.Failure("Gagal membatalkan transaksi penggunaan bahan.");
    }

    public async Task<IReadOnlyList<MaterialUsage>> GetUsagesByActivityIdAsync(int activityId, CancellationToken cancellationToken = default)
    {
        return await _materialRepository.GetUsagesByActivityIdAsync(activityId, cancellationToken);
    }

    public async Task<PagedResult<MaterialUsage>> GetUsagesPagedAsync(int? materialId, int? plantingId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _materialRepository.GetUsagesPagedAsync(materialId, plantingId, pageIndex, pageSize, cancellationToken);
    }
}
