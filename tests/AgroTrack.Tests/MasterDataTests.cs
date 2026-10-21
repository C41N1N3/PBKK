using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Security;
using AgroTrack.Services.Implementations;
using AgroTrack.Utilities;
using Xunit;

namespace AgroTrack.Tests;

public class MockLandRepository : ILandRepository
{
    private readonly List<Land> _lands = new();

    public Task<Land?> GetByIdAsync(int landId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_lands.FirstOrDefault(l => l.LandId == landId));

    public Task<Land?> GetByCodeAsync(string landCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_lands.FirstOrDefault(l => string.Equals(l.LandCode, landCode, StringComparison.OrdinalIgnoreCase)));

    public Task<PagedResult<Land>> GetPagedAsync(string? searchTerm, LandStatus? statusFilter, string? soilTypeFilter, int pageIndex, int pageSize, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<Land>(_lands, _lands.Count, pageIndex, pageSize));

    public Task<IReadOnlyList<Land>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Land>>(_lands.Where(l => l.Status == LandStatus.Active).ToList().AsReadOnly());

    public Task<IReadOnlyList<string>> GetDistinctSoilTypesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(new List<string> { "Aluvial", "Lempung" }.AsReadOnly());

    public Task<int> CreateAsync(Land land, CancellationToken cancellationToken = default)
    {
        land.LandId = _lands.Count + 1;
        _lands.Add(land);
        return Task.FromResult(land.LandId);
    }

    public Task<bool> UpdateAsync(Land land, CancellationToken cancellationToken = default)
    {
        var existing = _lands.FirstOrDefault(l => l.LandId == land.LandId);
        if (existing == null) return Task.FromResult(false);
        existing.LandName = land.LandName;
        existing.AreaHectares = land.AreaHectares;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int landId, CancellationToken cancellationToken = default)
    {
        var existing = _lands.FirstOrDefault(l => l.LandId == landId);
        if (existing == null) return Task.FromResult(false);
        _lands.Remove(existing);
        return Task.FromResult(true);
    }

    public int MockActivePlantingsCount { get; set; } = 0;

    public Task<int> GetActivePlantingsCountAsync(int landId, CancellationToken cancellationToken = default) =>
        Task.FromResult(MockActivePlantingsCount);
}

public class MockCommodityRepository : ICommodityRepository
{
    private readonly List<Commodity> _commodities = new();

    public Task<Commodity?> GetByIdAsync(int commodityId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_commodities.FirstOrDefault(c => c.CommodityId == commodityId));

    public Task<Commodity?> GetByCodeAsync(string commodityCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_commodities.FirstOrDefault(c => string.Equals(c.CommodityCode, commodityCode, StringComparison.OrdinalIgnoreCase)));

    public Task<PagedResult<Commodity>> GetPagedAsync(string? searchTerm, bool? isActiveFilter, int pageIndex, int pageSize, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<Commodity>(_commodities, _commodities.Count, pageIndex, pageSize));

    public Task<IReadOnlyList<Commodity>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Commodity>>(_commodities.Where(c => c.IsActive).ToList().AsReadOnly());

    public Task<int> CreateAsync(Commodity commodity, CancellationToken cancellationToken = default)
    {
        commodity.CommodityId = _commodities.Count + 1;
        _commodities.Add(commodity);
        return Task.FromResult(commodity.CommodityId);
    }

    public Task<bool> UpdateAsync(Commodity commodity, CancellationToken cancellationToken = default)
    {
        var existing = _commodities.FirstOrDefault(c => c.CommodityId == commodity.CommodityId);
        if (existing == null) return Task.FromResult(false);
        existing.CommodityName = commodity.CommodityName;
        return Task.FromResult(true);
    }

    public Task<bool> SetActiveStatusAsync(int commodityId, bool isActive, CancellationToken cancellationToken = default)
    {
        var existing = _commodities.FirstOrDefault(c => c.CommodityId == commodityId);
        if (existing == null) return Task.FromResult(false);
        existing.IsActive = isActive;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int commodityId, CancellationToken cancellationToken = default)
    {
        var existing = _commodities.FirstOrDefault(c => c.CommodityId == commodityId);
        if (existing == null) return Task.FromResult(false);
        _commodities.Remove(existing);
        return Task.FromResult(true);
    }

    public int MockPlantingsCount { get; set; } = 0;

    public Task<int> GetPlantingsCountAsync(int commodityId, CancellationToken cancellationToken = default) =>
        Task.FromResult(MockPlantingsCount);
}

public class MasterDataTests
{
    private readonly MockLandRepository _landRepo = new();
    private readonly MockCommodityRepository _commodityRepo = new();
    private readonly SessionService _sessionService = new();

    public MasterDataTests()
    {
        // Set active user as Manager by default
        _sessionService.SetSession(new CurrentUser(2, "manager", "Manajer Kebun", UserRole.Manager));
    }

    [Fact]
    public async Task CreateLand_ZeroOrNegativeArea_FailsValidation()
    {
        var landService = new LandService(_landRepo, _sessionService);
        var land = new Land
        {
            LandCode = "LHN-TEST",
            LandName = "Lahan Test",
            Location = "Lokasi",
            AreaHectares = 0,
            SoilType = "Aluvial"
        };

        var result = await landService.CreateLandAsync(land);

        Assert.False(result.IsSuccess);
        Assert.Contains("Luas lahan harus lebih dari nol", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateLand_EmptyCodeOrName_FailsValidation()
    {
        var landService = new LandService(_landRepo, _sessionService);
        var land = new Land
        {
            LandCode = "   ",
            LandName = "",
            Location = "Lokasi",
            AreaHectares = 5.0m,
            SoilType = "Aluvial"
        };

        var result = await landService.CreateLandAsync(land);

        Assert.False(result.IsSuccess);
        Assert.Contains("Kode lahan wajib diisi", result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteLand_WithActivePlantings_Fails()
    {
        var landService = new LandService(_landRepo, _sessionService);
        _landRepo.MockActivePlantingsCount = 2;

        var result = await landService.DeleteLandAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Contains("masih digunakan dalam 2 penanaman aktif", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateCommodity_NegativeGrowthDays_FailsValidation()
    {
        var commodityService = new CommodityService(_commodityRepo, _sessionService);
        var commodity = new Commodity
        {
            CommodityCode = "KMD-TEST",
            CommodityName = "Padi Uji",
            ResultUnit = "kg",
            EstimatedGrowthDays = -10
        };

        var result = await commodityService.CreateCommodityAsync(commodity);

        Assert.False(result.IsSuccess);
        Assert.Contains("tidak boleh bernilai negatif", result.ErrorMessage);
    }

    [Fact]
    public async Task FieldOfficer_CannotCreateLand_DeniedByAuthorization()
    {
        _sessionService.SetSession(new CurrentUser(3, "petugas", "Petugas Lapangan", UserRole.FieldOfficer));
        var landService = new LandService(_landRepo, _sessionService);

        var land = new Land
        {
            LandCode = "LHN-099",
            LandName = "Lahan Ilegal",
            Location = "Lokasi",
            AreaHectares = 2.0m,
            SoilType = "Aluvial"
        };

        var result = await landService.CreateLandAsync(land);

        Assert.False(result.IsSuccess);
        Assert.Contains("tidak memiliki izin", result.ErrorMessage);
    }
}
