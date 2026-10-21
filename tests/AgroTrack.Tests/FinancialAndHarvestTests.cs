using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Security;
using AgroTrack.Services.Implementations;
using AgroTrack.Services.Interfaces;
using AgroTrack.Utilities;
using Xunit;

namespace AgroTrack.Tests;

public class MockCostRepository : ICostRepository
{
    private readonly List<OperationalCost> _costs = new();

    public Task<OperationalCost?> GetByIdAsync(int costId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_costs.FirstOrDefault(c => c.CostId == costId));

    public Task<PagedResult<OperationalCost>> GetPagedAsync(
        int? plantingIdFilter,
        CostCategory? categoryFilter,
        DateTime? startDate,
        DateTime? endDate,
        string? searchTerm,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var list = _costs.ToList();
        return Task.FromResult(new PagedResult<OperationalCost>(list, list.Count, pageIndex, pageSize));
    }

    public Task<IReadOnlyList<OperationalCost>> GetByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OperationalCost>>(_costs.Where(c => c.PlantingId == plantingId).ToList().AsReadOnly());

    public Task<decimal> GetTotalCostByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_costs.Where(c => c.PlantingId == plantingId).Sum(c => c.Amount));

    public Task<IReadOnlyDictionary<CostCategory, decimal>> GetCostsByCategoryAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var dict = _costs.GroupBy(c => c.Category).ToDictionary(g => g.Key, g => g.Sum(c => c.Amount));
        return Task.FromResult<IReadOnlyDictionary<CostCategory, decimal>>(dict);
    }

    public Task<int> CreateAsync(OperationalCost cost, CancellationToken cancellationToken = default)
    {
        cost.CostId = _costs.Count + 1;
        _costs.Add(cost);
        return Task.FromResult(cost.CostId);
    }

    public Task<bool> UpdateAsync(OperationalCost cost, CancellationToken cancellationToken = default)
    {
        var existing = _costs.FirstOrDefault(c => c.CostId == cost.CostId);
        if (existing == null) return Task.FromResult(false);
        existing.Amount = cost.Amount;
        existing.Description = cost.Description;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int costId, CancellationToken cancellationToken = default)
    {
        var existing = _costs.FirstOrDefault(c => c.CostId == costId);
        if (existing == null) return Task.FromResult(false);
        _costs.Remove(existing);
        return Task.FromResult(true);
    }
}

public class MockHarvestRepository : IHarvestRepository
{
    private readonly List<Harvest> _harvests = new();

    public Task<Harvest?> GetByIdAsync(int harvestId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_harvests.FirstOrDefault(h => h.HarvestId == harvestId));

    public Task<PagedResult<Harvest>> GetPagedAsync(
        int? plantingIdFilter,
        HarvestQuality? qualityFilter,
        DateTime? startDate,
        DateTime? endDate,
        string? searchTerm,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var list = _harvests.ToList();
        return Task.FromResult(new PagedResult<Harvest>(list, list.Count, pageIndex, pageSize));
    }

    public Task<IReadOnlyList<Harvest>> GetByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Harvest>>(_harvests.Where(h => h.PlantingId == plantingId).ToList().AsReadOnly());

    public Task<decimal> GetTotalYieldByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_harvests.Where(h => h.PlantingId == plantingId).Sum(h => h.YieldAmount));

    public Task<decimal> GetTotalRevenueByPlantingIdAsync(int plantingId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_harvests.Where(h => h.PlantingId == plantingId).Sum(h => h.TotalRevenue));

    public Task<IReadOnlyList<(string MonthYear, decimal TotalYield, decimal TotalRevenue)>> GetMonthlyHarvestSummaryAsync(int monthsCount, CancellationToken cancellationToken = default)
    {
        var list = _harvests.GroupBy(h => h.HarvestDate.ToString("yyyy-MM"))
            .Select(g => (g.Key, g.Sum(h => h.YieldAmount), g.Sum(h => h.TotalRevenue)))
            .ToList();
        return Task.FromResult<IReadOnlyList<(string MonthYear, decimal TotalYield, decimal TotalRevenue)>>(list.AsReadOnly());
    }

    public Task<int> CreateAsync(Harvest harvest, CancellationToken cancellationToken = default)
    {
        harvest.HarvestId = _harvests.Count + 1;
        _harvests.Add(harvest);
        return Task.FromResult(harvest.HarvestId);
    }

    public Task<bool> UpdateAsync(Harvest harvest, CancellationToken cancellationToken = default)
    {
        var existing = _harvests.FirstOrDefault(h => h.HarvestId == harvest.HarvestId);
        if (existing == null) return Task.FromResult(false);
        existing.YieldAmount = harvest.YieldAmount;
        existing.SellingPricePerUnit = harvest.SellingPricePerUnit;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int harvestId, CancellationToken cancellationToken = default)
    {
        var existing = _harvests.FirstOrDefault(h => h.HarvestId == harvestId);
        if (existing == null) return Task.FromResult(false);
        _harvests.Remove(existing);
        return Task.FromResult(true);
    }
}

public class FinancialAndHarvestTests
{
    private readonly MockCostRepository _costRepo = new();
    private readonly MockHarvestRepository _harvestRepo = new();
    private readonly MockPlantingRepository _plantingRepo = new();
    private readonly SessionService _sessionService = new();

    private readonly CostService _costService;
    private readonly HarvestService _harvestService;

    public FinancialAndHarvestTests()
    {
        _sessionService.SetSession(new CurrentUser(1, "admin", "Admin Pertanian", UserRole.Administrator));

        // Seed an Active planting
        _plantingRepo.CreateAsync(new Planting
        {
            PlantingId = 1,
            PlantingCode = "PL-ACTIVE-01",
            Status = PlantingStatus.Active,
            StartDate = new DateTime(2026, 1, 1),
            ExpectedHarvestDate = new DateTime(2026, 4, 15)
        }).Wait();

        // Seed a Completed planting
        _plantingRepo.CreateAsync(new Planting
        {
            PlantingId = 2,
            PlantingCode = "PL-COMPLETED-01",
            Status = PlantingStatus.Completed,
            StartDate = new DateTime(2025, 1, 1),
            ExpectedHarvestDate = new DateTime(2025, 4, 15)
        }).Wait();

        // Seed a Cancelled planting
        _plantingRepo.CreateAsync(new Planting
        {
            PlantingId = 3,
            PlantingCode = "PL-CANCELLED-01",
            Status = PlantingStatus.Cancelled,
            StartDate = new DateTime(2026, 1, 1),
            ExpectedHarvestDate = new DateTime(2026, 4, 15)
        }).Wait();

        _costService = new CostService(_costRepo, _plantingRepo, _sessionService);
        _harvestService = new HarvestService(_harvestRepo, _plantingRepo, _sessionService);
    }

    [Fact]
    public async Task CreateCost_ValidData_Succeeds()
    {
        var cost = new OperationalCost
        {
            PlantingId = 1,
            Category = CostCategory.Labor,
            CostDate = new DateTime(2026, 1, 10),
            Amount = 1_500_000m,
            Description = "Upah tenaga kerja penyiangan lahan 5 orang."
        };

        var result = await _costService.CreateCostAsync(cost);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value > 0);
    }

    [Fact]
    public async Task CreateCost_NegativeOrZeroAmount_Fails()
    {
        var cost = new OperationalCost
        {
            PlantingId = 1,
            Category = CostCategory.Fuel,
            CostDate = new DateTime(2026, 1, 10),
            Amount = 0m,
            Description = "Bahan bakar traktor."
        };

        var result = await _costService.CreateCostAsync(cost);
        Assert.False(result.IsSuccess);
        Assert.Contains("harus lebih dari nol", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateCost_CancelledPlanting_Fails()
    {
        var cost = new OperationalCost
        {
            PlantingId = 3, // Cancelled planting
            Category = CostCategory.ToolRental,
            CostDate = new DateTime(2026, 1, 10),
            Amount = 500_000m,
            Description = "Sewa pompa air."
        };

        var result = await _costService.CreateCostAsync(cost);
        Assert.False(result.IsSuccess);
        Assert.Contains("Dibatalkan tidak dapat menerima pencatatan biaya baru", result.ErrorMessage);
    }

    [Fact]
    public void Harvest_TotalRevenue_IsCalculatedAutomatically()
    {
        var harvest = new Harvest
        {
            YieldAmount = 2500m,
            SellingPricePerUnit = 6500m
        };

        Assert.Equal(16_250_000m, harvest.TotalRevenue);
    }

    [Fact]
    public async Task CreateHarvest_ValidData_Succeeds()
    {
        var harvest = new Harvest
        {
            PlantingId = 1,
            HarvestDate = new DateTime(2026, 4, 10),
            YieldAmount = 3200m,
            Quality = HarvestQuality.GradeA,
            SellingPricePerUnit = 7000m,
            Notes = "Panen perdana kualitas super."
        };

        var result = await _harvestService.CreateHarvestAsync(harvest);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value > 0);
    }

    [Fact]
    public async Task CreateHarvest_NegativeYield_Fails()
    {
        var harvest = new Harvest
        {
            PlantingId = 1,
            HarvestDate = new DateTime(2026, 4, 10),
            YieldAmount = -100m,
            Quality = HarvestQuality.GradeB,
            SellingPricePerUnit = 6000m
        };

        var result = await _harvestService.CreateHarvestAsync(harvest);
        Assert.False(result.IsSuccess);
        Assert.Contains("harus lebih dari nol", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateHarvest_NegativePrice_Fails()
    {
        var harvest = new Harvest
        {
            PlantingId = 1,
            HarvestDate = new DateTime(2026, 4, 10),
            YieldAmount = 1000m,
            Quality = HarvestQuality.GradeB,
            SellingPricePerUnit = -500m
        };

        var result = await _harvestService.CreateHarvestAsync(harvest);
        Assert.False(result.IsSuccess);
        Assert.Contains("tidak boleh bernilai negatif", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateHarvest_DateBeforePlantingStart_Fails()
    {
        var harvest = new Harvest
        {
            PlantingId = 1, // StartDate is 2026-01-01
            HarvestDate = new DateTime(2025, 12, 1),
            YieldAmount = 1000m,
            SellingPricePerUnit = 6000m
        };

        var result = await _harvestService.CreateHarvestAsync(harvest);
        Assert.False(result.IsSuccess);
        Assert.Contains("tidak boleh mendahului tanggal mulai", result.ErrorMessage);
    }

    [Fact]
    public async Task FieldOfficer_CannotDeleteCostOrHarvest_Denied()
    {
        _sessionService.SetSession(new CurrentUser(3, "petugas", "Petugas Lapangan", UserRole.FieldOfficer));

        var costResult = await _costService.DeleteCostAsync(1);
        Assert.False(costResult.IsSuccess);
        Assert.Contains("tidak memiliki izin", costResult.ErrorMessage);

        var harvestResult = await _harvestService.DeleteHarvestAsync(1);
        Assert.False(harvestResult.IsSuccess);
        Assert.Contains("tidak memiliki izin", harvestResult.ErrorMessage);
    }
}
