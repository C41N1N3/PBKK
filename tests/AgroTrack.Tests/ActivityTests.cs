using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Security;
using AgroTrack.Services.Implementations;
using AgroTrack.Services.Interfaces;
using AgroTrack.Utilities;
using Xunit;

namespace AgroTrack.Tests;

public class MockActivityRepository : IActivityRepository
{
    private readonly List<FarmingActivity> _activities = new();

    public Task<FarmingActivity?> GetByIdAsync(int activityId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_activities.FirstOrDefault(a => a.ActivityId == activityId));

    public Task<PagedResult<FarmingActivity>> GetPagedAsync(
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
        var list = _activities.ToList();
        return Task.FromResult(new PagedResult<FarmingActivity>(list, list.Count, pageIndex, pageSize));
    }

    public Task<IReadOnlyList<FarmingActivity>> GetByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<FarmingActivity>>(_activities.Where(a => a.PlantingId == plantingId).ToList().AsReadOnly());

    public Task<IReadOnlyList<FarmingActivity>> GetRecentActivitiesAsync(int count, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<FarmingActivity>>(_activities.Take(count).ToList().AsReadOnly());

    public Task<int> CreateAsync(FarmingActivity activity, CancellationToken cancellationToken = default)
    {
        activity.ActivityId = _activities.Count + 1;
        _activities.Add(activity);
        return Task.FromResult(activity.ActivityId);
    }

    public Task<bool> UpdateAsync(FarmingActivity activity, CancellationToken cancellationToken = default)
    {
        var existing = _activities.FirstOrDefault(a => a.ActivityId == activity.ActivityId);
        if (existing == null) return Task.FromResult(false);
        existing.Description = activity.Description;
        existing.Status = activity.Status;
        existing.DurationHours = activity.DurationHours;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int activityId, CancellationToken cancellationToken = default)
    {
        var existing = _activities.FirstOrDefault(a => a.ActivityId == activityId);
        if (existing == null) return Task.FromResult(false);
        _activities.Remove(existing);
        return Task.FromResult(true);
    }
}

public class ActivityTests
{
    private readonly MockActivityRepository _activityRepo = new();
    private readonly MockPlantingRepository _plantingRepo = new();
    private readonly SessionService _sessionService = new();
    private readonly ActivityService _activityService;

    public ActivityTests()
    {
        _sessionService.SetSession(new CurrentUser(1, "admin", "Admin Pertanian", UserRole.Administrator));

        // Seed an Active planting (starts 2026-01-01)
        _plantingRepo.CreateAsync(new Planting
        {
            PlantingId = 1,
            PlantingCode = "PL-ACTIVE",
            Status = PlantingStatus.Active,
            StartDate = new DateTime(2026, 1, 1),
            ExpectedHarvestDate = new DateTime(2026, 4, 15)
        }).Wait();

        // Seed a Completed planting
        _plantingRepo.CreateAsync(new Planting
        {
            PlantingId = 2,
            PlantingCode = "PL-COMPLETED",
            Status = PlantingStatus.Completed,
            StartDate = new DateTime(2025, 1, 1),
            ExpectedHarvestDate = new DateTime(2025, 4, 15)
        }).Wait();

        // Seed a Cancelled planting
        _plantingRepo.CreateAsync(new Planting
        {
            PlantingId = 3,
            PlantingCode = "PL-CANCELLED",
            Status = PlantingStatus.Cancelled,
            StartDate = new DateTime(2026, 1, 1),
            ExpectedHarvestDate = new DateTime(2026, 4, 15)
        }).Wait();

        _activityService = new ActivityService(_activityRepo, _plantingRepo, _sessionService);
    }

    [Fact]
    public async Task CreateActivity_ValidData_Succeeds()
    {
        var activity = new FarmingActivity
        {
            PlantingId = 1,
            ActivityType = ActivityType.Watering,
            ActivityDate = new DateTime(2026, 1, 10),
            DurationHours = 2.5m,
            Description = "Penyiraman pagi menggunakan sprinkle.",
            Status = ActivityStatus.Completed
        };

        var result = await _activityService.CreateActivityAsync(activity);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value > 0);
    }

    [Fact]
    public async Task CreateActivity_CompletedPlanting_Fails()
    {
        var activity = new FarmingActivity
        {
            PlantingId = 2, // Completed planting!
            ActivityType = ActivityType.Weeding,
            ActivityDate = new DateTime(2026, 1, 10),
            Description = "Penyiangan rumput liar."
        };

        var result = await _activityService.CreateActivityAsync(activity);
        Assert.False(result.IsSuccess);
        Assert.Contains("Selesai tidak dapat menerima aktivitas baru", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateActivity_CancelledPlanting_Fails()
    {
        var activity = new FarmingActivity
        {
            PlantingId = 3, // Cancelled planting!
            ActivityType = ActivityType.Inspection,
            ActivityDate = new DateTime(2026, 1, 10),
            Description = "Pemeriksaan tanaman."
        };

        var result = await _activityService.CreateActivityAsync(activity);
        Assert.False(result.IsSuccess);
        Assert.Contains("Dibatalkan tidak dapat menerima aktivitas baru", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateActivity_DateBeforePlantingStart_Fails()
    {
        var activity = new FarmingActivity
        {
            PlantingId = 1, // StartDate is 2026-01-01
            ActivityType = ActivityType.LandPreparation,
            ActivityDate = new DateTime(2025, 12, 25), // Before start date!
            Description = "Persiapan lahan terlalu awal."
        };

        var result = await _activityService.CreateActivityAsync(activity);
        Assert.False(result.IsSuccess);
        Assert.Contains("tidak boleh mendahului tanggal mulai", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateActivity_NegativeDuration_Fails()
    {
        var activity = new FarmingActivity
        {
            PlantingId = 1,
            ActivityType = ActivityType.PestControl,
            ActivityDate = new DateTime(2026, 1, 15),
            DurationHours = -1.5m,
            Description = "Penyemprotan pestisida."
        };

        var result = await _activityService.CreateActivityAsync(activity);
        Assert.False(result.IsSuccess);
        Assert.Contains("tidak boleh bernilai negatif", result.ErrorMessage);
    }

    [Fact]
    public async Task FieldOfficer_CanOnlyEditOwnActivity()
    {
        // Activity created by user 2 (Officer A)
        var actId = await _activityRepo.CreateAsync(new FarmingActivity
        {
            ActivityId = 10,
            PlantingId = 1,
            OfficerId = 2,
            ActivityDate = new DateTime(2026, 1, 10),
            Description = "Catatan petugas A",
            Status = ActivityStatus.InProgress
        });

        // Current session is user 3 (Officer B)
        _sessionService.SetSession(new CurrentUser(3, "petugas_b", "Petugas B", UserRole.FieldOfficer));

        var updateAttempt = new FarmingActivity
        {
            ActivityId = actId,
            PlantingId = 1,
            Description = "Mencoba mengubah catatan petugas A"
        };

        var result = await _activityService.UpdateActivityAsync(updateAttempt);
        Assert.False(result.IsSuccess);
        Assert.Contains("hanya diizinkan mengubah aktivitas yang Anda buat sendiri", result.ErrorMessage);
    }

    [Fact]
    public async Task FieldOfficer_CannotDeleteActivity_Denied()
    {
        var actId = await _activityRepo.CreateAsync(new FarmingActivity
        {
            ActivityId = 11,
            PlantingId = 1,
            OfficerId = 3,
            ActivityDate = new DateTime(2026, 1, 10),
            Description = "Aktivitas"
        });

        _sessionService.SetSession(new CurrentUser(3, "petugas", "Petugas Lapangan", UserRole.FieldOfficer));

        var result = await _activityService.DeleteActivityAsync(actId);
        Assert.False(result.IsSuccess);
        Assert.Contains("tidak memiliki izin", result.ErrorMessage);
    }
}
