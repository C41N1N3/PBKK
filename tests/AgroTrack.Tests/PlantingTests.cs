using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Security;
using AgroTrack.Services.Implementations;
using AgroTrack.Services.Interfaces;
using AgroTrack.Utilities;
using Xunit;

namespace AgroTrack.Tests;

public class MockPlantingRepository : IPlantingRepository
{
    private readonly List<Planting> _plantings = new();

    public Task<Planting?> GetByIdAsync(int plantingId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_plantings.FirstOrDefault(p => p.PlantingId == plantingId));

    public Task<Planting?> GetByCodeAsync(string plantingCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_plantings.FirstOrDefault(p => string.Equals(p.PlantingCode, plantingCode, StringComparison.OrdinalIgnoreCase)));

    public Task<PagedResult<Planting>> GetPagedAsync(
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
        var items = _plantings.AsEnumerable();
        if (statusFilter.HasValue) items = items.Where(p => p.Status == statusFilter.Value);
        if (landIdFilter.HasValue) items = items.Where(p => p.LandId == landIdFilter.Value);
        var list = items.ToList();
        return Task.FromResult(new PagedResult<Planting>(list, list.Count, pageIndex, pageSize));
    }

    public Task<IReadOnlyList<Planting>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Planting>>(_plantings.Where(p => p.Status == PlantingStatus.Active).ToList().AsReadOnly());

    public Task<decimal> GetActiveOverlappingAreaAsync(int landId, DateTime startDate, DateTime expectedHarvestDate, int? excludePlantingId, CancellationToken cancellationToken = default)
    {
        var overlappingArea = _plantings
            .Where(p => p.LandId == landId && p.PlantingId != excludePlantingId && p.Status != PlantingStatus.Cancelled && p.Status != PlantingStatus.Completed)
            .Where(p => p.StartDate <= expectedHarvestDate && p.ExpectedHarvestDate >= startDate)
            .Sum(p => p.PlantedAreaHectares);
        return Task.FromResult(overlappingArea);
    }

    public Task<int> CreateAsync(Planting planting, CancellationToken cancellationToken = default)
    {
        planting.PlantingId = _plantings.Count + 1;
        _plantings.Add(planting);
        return Task.FromResult(planting.PlantingId);
    }

    public Task<bool> UpdateAsync(Planting planting, CancellationToken cancellationToken = default)
    {
        var existing = _plantings.FirstOrDefault(p => p.PlantingId == planting.PlantingId);
        if (existing == null) return Task.FromResult(false);
        existing.SeasonName = planting.SeasonName;
        existing.PlantedAreaHectares = planting.PlantedAreaHectares;
        existing.TargetYield = planting.TargetYield;
        return Task.FromResult(true);
    }

    public Task<bool> UpdateStatusAsync(int plantingId, PlantingStatus newStatus, DateTime? completionDate, CancellationToken cancellationToken = default)
    {
        var existing = _plantings.FirstOrDefault(p => p.PlantingId == plantingId);
        if (existing == null) return Task.FromResult(false);
        existing.Status = newStatus;
        existing.CompletionDate = completionDate;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int plantingId, CancellationToken cancellationToken = default)
    {
        var existing = _plantings.FirstOrDefault(p => p.PlantingId == plantingId);
        if (existing == null) return Task.FromResult(false);
        _plantings.Remove(existing);
        return Task.FromResult(true);
    }
}

public class PlantingTests
{
    private readonly MockLandRepository _landRepo = new();
    private readonly MockCommodityRepository _commodityRepo = new();
    private readonly MockPlantingRepository _plantingRepo = new();
    private readonly SessionService _sessionService = new();
    private readonly PlantingService _plantingService;

    public PlantingTests()
    {
        _sessionService.SetSession(new CurrentUser(1, "admin", "Admin Pertanian", UserRole.Administrator));

        // Seed Land A (5.0 ha) and Land B (2.0 ha)
        _landRepo.CreateAsync(new Land { LandId = 1, LandCode = "L-01", LandName = "Lahan A", AreaHectares = 5.0m, Status = LandStatus.Active }).Wait();
        _landRepo.CreateAsync(new Land { LandId = 2, LandCode = "L-02", LandName = "Lahan B", AreaHectares = 2.0m, Status = LandStatus.Active }).Wait();

        // Seed Commodity Padi
        _commodityRepo.CreateAsync(new Commodity { CommodityId = 1, CommodityCode = "KMD-PADI", CommodityName = "Padi IR64", ResultUnit = "kg", IsActive = true }).Wait();

        _plantingService = new PlantingService(_plantingRepo, _landRepo, _commodityRepo, _sessionService);
    }

    [Fact]
    public async Task CreatePlanting_WithValidData_Succeeds()
    {
        var planting = new Planting
        {
            PlantingCode = "PL-2026-001",
            LandId = 1,
            CommodityId = 1,
            SeasonName = "Musim Rendeng 2026",
            StartDate = new DateTime(2026, 1, 1),
            ExpectedHarvestDate = new DateTime(2026, 4, 15),
            PlantedAreaHectares = 3.5m,
            TargetYield = 15000m,
            Status = PlantingStatus.Planned
        };

        var result = await _plantingService.CreatePlantingAsync(planting);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value > 0);
    }

    [Fact]
    public async Task CreatePlanting_HarvestBeforeStart_Fails()
    {
        var planting = new Planting
        {
            PlantingCode = "PL-INVALID-DATE",
            LandId = 1,
            CommodityId = 1,
            SeasonName = "Musim Uji",
            StartDate = new DateTime(2026, 5, 1),
            ExpectedHarvestDate = new DateTime(2026, 4, 1), // Invalid
            PlantedAreaHectares = 2.0m,
            TargetYield = 5000m
        };

        var result = await _plantingService.CreatePlantingAsync(planting);
        Assert.False(result.IsSuccess);
        Assert.Contains("setelah tanggal mulai", result.ErrorMessage);
    }

    [Fact]
    public async Task CreatePlanting_ExceedingLandArea_Fails()
    {
        var planting = new Planting
        {
            PlantingCode = "PL-EXCEED-LAND",
            LandId = 2, // Land B area is 2.0
            CommodityId = 1,
            SeasonName = "Musim Rendeng",
            StartDate = new DateTime(2026, 1, 1),
            ExpectedHarvestDate = new DateTime(2026, 4, 1),
            PlantedAreaHectares = 2.5m, // 2.5 > 2.0
            TargetYield = 10000m
        };

        var result = await _plantingService.CreatePlantingAsync(planting);
        Assert.False(result.IsSuccess);
        Assert.Contains("melebihi luas fisik", result.ErrorMessage);
    }

    [Fact]
    public async Task CreatePlanting_OverlappingExceedsCapacity_Fails()
    {
        // First planting uses 3.0 hectares on Land 1 (total 5.0)
        var p1 = new Planting
        {
            PlantingCode = "PL-ACTIVE-01",
            LandId = 1,
            CommodityId = 1,
            SeasonName = "Musim A",
            StartDate = new DateTime(2026, 1, 1),
            ExpectedHarvestDate = new DateTime(2026, 6, 1),
            PlantedAreaHectares = 3.0m,
            TargetYield = 10000m,
            Status = PlantingStatus.Active
        };
        await _plantingService.CreatePlantingAsync(p1);

        // Second planting requests 2.5 hectares during overlapping period (3.0 + 2.5 = 5.5 > 5.0)
        var p2 = new Planting
        {
            PlantingCode = "PL-ACTIVE-02",
            LandId = 1,
            CommodityId = 1,
            SeasonName = "Musim B",
            StartDate = new DateTime(2026, 3, 1),
            ExpectedHarvestDate = new DateTime(2026, 7, 1),
            PlantedAreaHectares = 2.5m,
            TargetYield = 8000m,
            Status = PlantingStatus.Planned
        };

        var result = await _plantingService.CreatePlantingAsync(p2);
        Assert.False(result.IsSuccess);
        Assert.Contains("Kapasitas lahan tidak mencukupi", result.ErrorMessage);
    }

    [Fact]
    public void TargetPercentage_Calculations_HandledSafely()
    {
        var normalPlanting = new Planting
        {
            TargetYield = 10000m,
            TotalActualYield = 9500m
        };
        Assert.Equal(95.0m, normalPlanting.TargetAchievementPercentage);

        // Guard against TargetYield = 0 (division by zero)
        var zeroTargetPlanting = new Planting
        {
            TargetYield = 0m,
            TotalActualYield = 500m
        };
        Assert.Null(zeroTargetPlanting.TargetAchievementPercentage);
    }

    [Fact]
    public void FinancialCalculations_NetProfit_IsAccurate()
    {
        var planting = new Planting
        {
            TotalOperationalCost = 10_000_000m,
            TotalMaterialCost = 5_000_000m,
            TotalRevenue = 22_500_000m
        };

        Assert.Equal(7_500_000m, planting.NetProfit);
    }

    [Fact]
    public async Task FieldOfficer_CannotDeletePlanting_Denied()
    {
        _sessionService.SetSession(new CurrentUser(3, "petugas", "Petugas Lapangan", UserRole.FieldOfficer));
        var result = await _plantingService.DeletePlantingAsync(1);
        Assert.False(result.IsSuccess);
        Assert.Contains("tidak memiliki izin", result.ErrorMessage);
    }
}
