using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Services.Interfaces;
using AgroTrack.Utilities;

namespace AgroTrack.Services.Implementations;

public class PlantingService : IPlantingService
{
    private readonly IPlantingRepository _plantingRepository;
    private readonly ILandRepository _landRepository;
    private readonly ICommodityRepository _commodityRepository;
    private readonly ISessionService _sessionService;

    public PlantingService(
        IPlantingRepository plantingRepository,
        ILandRepository landRepository,
        ICommodityRepository commodityRepository,
        ISessionService sessionService)
    {
        _plantingRepository = plantingRepository;
        _landRepository = landRepository;
        _commodityRepository = commodityRepository;
        _sessionService = sessionService;
    }

    public async Task<PagedResult<Planting>> GetPlantingsPagedAsync(
        string? searchTerm, 
        PlantingStatus? statusFilter, 
        int? landIdFilter, 
        int? commodityIdFilter, 
        DateTime? startDateFrom, 
        DateTime? startDateTo, 
        int pageIndex, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        return await _plantingRepository.GetPagedAsync(
            searchTerm, statusFilter, landIdFilter, commodityIdFilter, 
            startDateFrom, startDateTo, pageIndex, pageSize, cancellationToken);
    }

    public async Task<Planting?> GetPlantingByIdAsync(int plantingId, CancellationToken cancellationToken = default)
    {
        return await _plantingRepository.GetByIdAsync(plantingId, cancellationToken);
    }

    public async Task<IReadOnlyList<Planting>> GetActivePlantingsAsync(CancellationToken cancellationToken = default)
    {
        return await _plantingRepository.GetAllActiveAsync(cancellationToken);
    }

    public async Task<Result<int>> CreatePlantingAsync(Planting planting, CancellationToken cancellationToken = default)
    {
        if (!_sessionService.CurrentUser?.CanManageMasterData ?? true)
            return Result<int>.Failure("Akun Anda tidak memiliki izin untuk membuat musim tanam baru.");

        // Validation
        if (string.IsNullOrWhiteSpace(planting.PlantingCode))
            return Result<int>.Failure("Kode penanaman wajib diisi.");

        if (string.IsNullOrWhiteSpace(planting.SeasonName))
            return Result<int>.Failure("Nama musim tanam wajib diisi.");

        if (planting.LandId <= 0)
            return Result<int>.Failure("Lahan pertanian wajib dipilih.");

        if (planting.CommodityId <= 0)
            return Result<int>.Failure("Komoditas pertanian wajib dipilih.");

        if (planting.PlantedAreaHectares <= 0)
            return Result<int>.Failure("Luas tanam harus lebih dari nol hektare.");

        if (planting.TargetYield < 0)
            return Result<int>.Failure("Target hasil panen tidak boleh bernilai negatif.");

        if (planting.ExpectedHarvestDate <= planting.StartDate)
            return Result<int>.Failure("Tanggal target panen harus setelah tanggal mulai.");

        // Check Unique Planting Code
        var existing = await _plantingRepository.GetByCodeAsync(planting.PlantingCode, cancellationToken);
        if (existing != null)
            return Result<int>.Failure($"Kode penanaman '{planting.PlantingCode}' sudah digunakan.");

        // Validate Land Capacity
        var land = await _landRepository.GetByIdAsync(planting.LandId, cancellationToken);
        if (land == null)
            return Result<int>.Failure("Lahan yang dipilih tidak ditemukan.");

        if (planting.PlantedAreaHectares > land.AreaHectares)
            return Result<int>.Failure($"Luas tanam ({planting.PlantedAreaHectares:N2} ha) melebihi luas fisik lahan ({land.AreaHectares:N2} ha).");

        // Validate Overlapping Active Plantings on the Same Land
        var overlappingArea = await _plantingRepository.GetActiveOverlappingAreaAsync(
            planting.LandId, planting.StartDate, planting.ExpectedHarvestDate, null, cancellationToken);

        var remainingCapacity = land.AreaHectares - overlappingArea;
        if (planting.PlantedAreaHectares > remainingCapacity)
        {
            return Result<int>.Failure(
                $"Kapasitas lahan tidak mencukupi untuk periode ini. Sisa luas yang tersedia pada rentang waktu ini adalah {Math.Max(0, remainingCapacity):N2} ha (Luas fisik: {land.AreaHectares:N2} ha, Terpakai aktif: {overlappingArea:N2} ha).");
        }

        // Set Creator
        planting.CreatedBy = _sessionService.CurrentUser!.UserId;

        var newId = await _plantingRepository.CreateAsync(planting, cancellationToken);
        return Result<int>.Success(newId);
    }

    public async Task<Result> UpdatePlantingAsync(Planting planting, CancellationToken cancellationToken = default)
    {
        if (!_sessionService.CurrentUser?.CanManageMasterData ?? true)
            return Result.Failure("Akun Anda tidak memiliki izin untuk mengubah data penanaman.");

        if (string.IsNullOrWhiteSpace(planting.PlantingCode))
            return Result.Failure("Kode penanaman wajib diisi.");

        if (string.IsNullOrWhiteSpace(planting.SeasonName))
            return Result.Failure("Nama musim tanam wajib diisi.");

        if (planting.LandId <= 0)
            return Result.Failure("Lahan pertanian wajib dipilih.");

        if (planting.CommodityId <= 0)
            return Result.Failure("Komoditas pertanian wajib dipilih.");

        if (planting.PlantedAreaHectares <= 0)
            return Result.Failure("Luas tanam harus lebih dari nol hektare.");

        if (planting.TargetYield < 0)
            return Result.Failure("Target hasil panen tidak boleh bernilai negatif.");

        if (planting.ExpectedHarvestDate <= planting.StartDate)
            return Result.Failure("Tanggal target panen harus setelah tanggal mulai.");

        if (planting.CompletionDate.HasValue && planting.CompletionDate.Value < planting.StartDate)
            return Result.Failure("Tanggal penyelesaian tidak boleh mendahului tanggal mulai tanam.");

        // Check Unique Planting Code against other records
        var existing = await _plantingRepository.GetByCodeAsync(planting.PlantingCode, cancellationToken);
        if (existing != null && existing.PlantingId != planting.PlantingId)
            return Result.Failure($"Kode penanaman '{planting.PlantingCode}' sudah digunakan oleh penanaman lain.");

        // Validate Land Capacity
        var land = await _landRepository.GetByIdAsync(planting.LandId, cancellationToken);
        if (land == null)
            return Result.Failure("Lahan yang dipilih tidak ditemukan.");

        if (planting.PlantedAreaHectares > land.AreaHectares)
            return Result.Failure($"Luas tanam ({planting.PlantedAreaHectares:N2} ha) melebihi luas fisik lahan ({land.AreaHectares:N2} ha).");

        // Validate Overlapping Active Plantings (excluding current plantingId)
        var overlappingArea = await _plantingRepository.GetActiveOverlappingAreaAsync(
            planting.LandId, planting.StartDate, planting.ExpectedHarvestDate, planting.PlantingId, cancellationToken);

        var remainingCapacity = land.AreaHectares - overlappingArea;
        if (planting.PlantedAreaHectares > remainingCapacity)
        {
            return Result.Failure(
                $"Kapasitas lahan tidak mencukupi untuk periode ini. Sisa luas tersedia adalah {Math.Max(0, remainingCapacity):N2} ha.");
        }

        var success = await _plantingRepository.UpdateAsync(planting, cancellationToken);
        return success ? Result.Success() : Result.Failure("Gagal memperbarui data penanaman.");
    }

    public async Task<Result> ChangeStatusAsync(int plantingId, PlantingStatus newStatus, CancellationToken cancellationToken = default)
    {
        if (!_sessionService.CurrentUser?.CanManageMasterData ?? true)
            return Result.Failure("Akun Anda tidak memiliki izin untuk mengubah status penanaman.");

        var planting = await _plantingRepository.GetByIdAsync(plantingId, cancellationToken);
        if (planting == null)
            return Result.Failure("Data penanaman tidak ditemukan.");

        DateTime? completionDate = newStatus == PlantingStatus.Completed ? DateTime.Today : null;
        var success = await _plantingRepository.UpdateStatusAsync(plantingId, newStatus, completionDate, cancellationToken);
        return success ? Result.Success() : Result.Failure("Gagal memperbarui status penanaman.");
    }

    public async Task<Result> DeletePlantingAsync(int plantingId, CancellationToken cancellationToken = default)
    {
        if (!_sessionService.CurrentUser?.CanManageMasterData ?? true)
            return Result.Failure("Akun Anda tidak memiliki izin untuk menghapus data penanaman.");

        try
        {
            var success = await _plantingRepository.DeleteAsync(plantingId, cancellationToken);
            return success ? Result.Success() : Result.Failure("Data penanaman tidak ditemukan.");
        }
        catch
        {
            return Result.Failure("Penanaman tidak dapat dihapus karena sudah memiliki catatan aktivitas, biaya, atau panen. Anda dapat mengubah statusnya menjadi 'Dibatalkan'.");
        }
    }
}
