using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Security;
using AgroTrack.Services.Interfaces;
using AgroTrack.Utilities;

namespace AgroTrack.Services.Implementations;

public class ActivityService : IActivityService
{
    private readonly IActivityRepository _activityRepository;
    private readonly IPlantingRepository _plantingRepository;
    private readonly ISessionService _sessionService;

    public ActivityService(
        IActivityRepository activityRepository,
        IPlantingRepository plantingRepository,
        ISessionService sessionService)
    {
        _activityRepository = activityRepository;
        _plantingRepository = plantingRepository;
        _sessionService = sessionService;
    }

    public async Task<PagedResult<FarmingActivity>> GetActivitiesPagedAsync(
        int? plantingIdFilter,
        ActivityType? typeFilter,
        ActivityStatus? statusFilter,
        int? officerIdFilter,
        DateTime? startDate,
        DateTime? endDate,
        string? searchTerm,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _activityRepository.GetPagedAsync(
            plantingIdFilter,
            typeFilter,
            statusFilter,
            officerIdFilter,
            startDate,
            endDate,
            searchTerm,
            pageIndex,
            pageSize,
            cancellationToken);
    }

    public async Task<FarmingActivity?> GetActivityByIdAsync(int activityId, CancellationToken cancellationToken = default)
    {
        return await _activityRepository.GetByIdAsync(activityId, cancellationToken);
    }

    public async Task<IReadOnlyList<FarmingActivity>> GetActivitiesByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default)
    {
        return await _activityRepository.GetByPlantingIdAsync(plantingId, cancellationToken);
    }

    public async Task<IReadOnlyList<FarmingActivity>> GetRecentActivitiesAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _activityRepository.GetRecentActivitiesAsync(count, cancellationToken);
    }

    public async Task<Result<int>> CreateActivityAsync(FarmingActivity activity, CancellationToken cancellationToken = default)
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser == null)
            return Result<int>.Failure("Sesi pengguna tidak valid. Silakan login kembali.");

        if (string.IsNullOrWhiteSpace(activity.Description))
            return Result<int>.Failure("Deskripsi aktivitas wajib diisi.");

        if (activity.PlantingId <= 0)
            return Result<int>.Failure("Penanaman wajib dipilih.");

        if (activity.DurationHours.HasValue && activity.DurationHours.Value < 0)
            return Result<int>.Failure("Durasi aktivitas tidak boleh bernilai negatif.");

        // Check Planting Status
        var planting = await _plantingRepository.GetByIdAsync(activity.PlantingId, cancellationToken);
        if (planting == null)
            return Result<int>.Failure("Data penanaman yang dipilih tidak ditemukan.");

        if (planting.Status == PlantingStatus.Completed)
            return Result<int>.Failure("Penanaman berstatus Selesai tidak dapat menerima aktivitas baru.");

        if (planting.Status == PlantingStatus.Cancelled)
            return Result<int>.Failure("Penanaman berstatus Dibatalkan tidak dapat menerima aktivitas baru.");

        if (activity.ActivityDate.Date < planting.StartDate.Date)
            return Result<int>.Failure($"Tanggal aktivitas ({activity.ActivityDate:dd/MM/yyyy}) tidak boleh mendahului tanggal mulai penanaman ({planting.StartDate:dd/MM/yyyy}).");

        // Set OfficerId to current user if not specified or if current user is FieldOfficer
        if (currentUser.Role == UserRole.FieldOfficer || activity.OfficerId <= 0)
        {
            activity.OfficerId = currentUser.UserId;
        }

        activity.Description = activity.Description.Trim();

        var newId = await _activityRepository.CreateAsync(activity, cancellationToken);
        return Result<int>.Success(newId);
    }

    public async Task<Result> UpdateActivityAsync(FarmingActivity activity, CancellationToken cancellationToken = default)
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser == null)
            return Result.Failure("Sesi pengguna tidak valid. Silakan login kembali.");

        var existing = await _activityRepository.GetByIdAsync(activity.ActivityId, cancellationToken);
        if (existing == null)
            return Result.Failure("Aktivitas yang akan diubah tidak ditemukan.");

        // Role authorization check: FieldOfficer can only edit own activities
        if (currentUser.Role == UserRole.FieldOfficer && existing.OfficerId != currentUser.UserId)
            return Result.Failure("Akun Anda hanya diizinkan mengubah aktivitas yang Anda buat sendiri.");

        if (string.IsNullOrWhiteSpace(activity.Description))
            return Result.Failure("Deskripsi aktivitas wajib diisi.");

        if (activity.DurationHours.HasValue && activity.DurationHours.Value < 0)
            return Result.Failure("Durasi aktivitas tidak boleh bernilai negatif.");

        // Check Planting Status
        var planting = await _plantingRepository.GetByIdAsync(activity.PlantingId, cancellationToken);
        if (planting == null)
            return Result.Failure("Data penanaman tidak ditemukan.");

        if (planting.Status == PlantingStatus.Completed && existing.Status == ActivityStatus.Completed)
            return Result.Failure("Penanaman telah selesai. Aktivitas tidak dapat diubah lagi.");

        if (planting.Status == PlantingStatus.Cancelled)
            return Result.Failure("Penanaman telah dibatalkan. Aktivitas tidak dapat diubah.");

        activity.Description = activity.Description.Trim();

        var success = await _activityRepository.UpdateAsync(activity, cancellationToken);
        return success ? Result.Success() : Result.Failure("Gagal memperbarui aktivitas di database.");
    }

    public async Task<Result> DeleteActivityAsync(int activityId, CancellationToken cancellationToken = default)
    {
        var currentUser = _sessionService.CurrentUser;
        if (currentUser == null)
            return Result.Failure("Sesi pengguna tidak valid. Silakan login kembali.");

        if (currentUser.Role == UserRole.FieldOfficer)
            return Result.Failure("Akun Anda tidak memiliki izin untuk menghapus data aktivitas.");

        var existing = await _activityRepository.GetByIdAsync(activityId, cancellationToken);
        if (existing == null)
            return Result.Failure("Aktivitas yang akan dihapus tidak ditemukan.");

        var success = await _activityRepository.DeleteAsync(activityId, cancellationToken);
        return success ? Result.Success() : Result.Failure("Gagal menghapus aktivitas dari database.");
    }
}
