using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Security;
using AgroTrack.Services.Implementations;
using AgroTrack.Services.Interfaces;
using AgroTrack.Utilities;
using Xunit;

namespace AgroTrack.Tests;

public class MockMaterialRepository : IMaterialRepository
{
    private readonly List<AgriculturalMaterial> _materials = new();
    private readonly List<MaterialUsage> _usages = new();

    public Task<AgriculturalMaterial?> GetByIdAsync(int materialId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_materials.FirstOrDefault(m => m.MaterialId == materialId));

    public Task<AgriculturalMaterial?> GetByCodeAsync(string materialCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_materials.FirstOrDefault(m => string.Equals(m.MaterialCode, materialCode, StringComparison.OrdinalIgnoreCase)));

    public Task<PagedResult<AgriculturalMaterial>> GetPagedAsync(
        string? searchTerm,
        MaterialCategory? categoryFilter,
        bool? isLowStockFilter,
        bool? isActiveFilter,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var list = _materials.ToList();
        return Task.FromResult(new PagedResult<AgriculturalMaterial>(list, list.Count, pageIndex, pageSize));
    }

    public Task<IReadOnlyList<AgriculturalMaterial>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AgriculturalMaterial>>(_materials.Where(m => m.IsActive).ToList().AsReadOnly());

    public Task<int> CreateAsync(AgriculturalMaterial material, CancellationToken cancellationToken = default)
    {
        material.MaterialId = _materials.Count + 1;
        _materials.Add(material);
        return Task.FromResult(material.MaterialId);
    }

    public Task<bool> UpdateAsync(AgriculturalMaterial material, CancellationToken cancellationToken = default)
    {
        var existing = _materials.FirstOrDefault(m => m.MaterialId == material.MaterialId);
        if (existing == null) return Task.FromResult(false);
        existing.Stock = material.Stock;
        existing.MinimumStock = material.MinimumStock;
        existing.UnitPrice = material.UnitPrice;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int materialId, CancellationToken cancellationToken = default)
    {
        var existing = _materials.FirstOrDefault(m => m.MaterialId == materialId);
        if (existing == null) return Task.FromResult(false);
        _materials.Remove(existing);
        return Task.FromResult(true);
    }

    public Task<bool> RecordUsageWithStockDeductionAsync(MaterialUsage usage, CancellationToken cancellationToken = default)
    {
        var mat = _materials.FirstOrDefault(m => m.MaterialId == usage.MaterialId);
        if (mat == null || mat.Stock < usage.Quantity) return Task.FromResult(false);

        mat.Stock -= usage.Quantity;
        usage.MaterialUsageId = _usages.Count + 1;
        _usages.Add(usage);
        return Task.FromResult(true);
    }

    public Task<bool> RevertUsageWithStockRestorationAsync(int materialUsageId, CancellationToken cancellationToken = default)
    {
        var usage = _usages.FirstOrDefault(u => u.MaterialUsageId == materialUsageId);
        if (usage == null) return Task.FromResult(false);

        var mat = _materials.FirstOrDefault(m => m.MaterialId == usage.MaterialId);
        if (mat != null)
        {
            mat.Stock += usage.Quantity;
        }

        _usages.Remove(usage);
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<MaterialUsage>> GetUsagesByActivityIdAsync(int activityId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MaterialUsage>>(_usages.Where(u => u.ActivityId == activityId).ToList().AsReadOnly());

    public Task<PagedResult<MaterialUsage>> GetUsagesPagedAsync(int? materialId, int? plantingId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var list = _usages.ToList();
        return Task.FromResult(new PagedResult<MaterialUsage>(list, list.Count, pageIndex, pageSize));
    }
}

public class MaterialStockTests
{
    private readonly MockMaterialRepository _materialRepo = new();
    private readonly MockActivityRepository _activityRepo = new();
    private readonly SessionService _sessionService = new();
    private readonly MaterialService _materialService;

    public MaterialStockTests()
    {
        _sessionService.SetSession(new CurrentUser(1, "admin", "Admin Pertanian", UserRole.Administrator));

        // Seed Material 1: Pupuk Urea (Stock = 100 kg, MinStock = 20 kg, Price = 12000)
        _materialRepo.CreateAsync(new AgriculturalMaterial
        {
            MaterialId = 1,
            MaterialCode = "MAT-UREA",
            MaterialName = "Pupuk Urea Granul",
            Category = MaterialCategory.Fertilizer,
            Unit = "kg",
            Stock = 100m,
            MinimumStock = 20m,
            UnitPrice = 12000m,
            IsActive = true
        }).Wait();

        // Seed Material 2: Inactive Material
        _materialRepo.CreateAsync(new AgriculturalMaterial
        {
            MaterialId = 2,
            MaterialCode = "MAT-OLD",
            MaterialName = "Pestisida Kadaluarsa",
            Category = MaterialCategory.Pesticide,
            Unit = "liter",
            Stock = 50m,
            MinimumStock = 10m,
            UnitPrice = 85000m,
            IsActive = false
        }).Wait();

        // Seed Activity 1
        _activityRepo.CreateAsync(new FarmingActivity
        {
            ActivityId = 1,
            PlantingId = 1,
            ActivityType = ActivityType.Fertilizing,
            Description = "Pemupukan pertama"
        }).Wait();

        _materialService = new MaterialService(_materialRepo, _activityRepo, _sessionService);
    }

    [Fact]
    public async Task CreateMaterial_ValidData_Succeeds()
    {
        var mat = new AgriculturalMaterial
        {
            MaterialCode = "MAT-NPK",
            MaterialName = "Pupuk NPK Mutiara",
            Category = MaterialCategory.Fertilizer,
            Unit = "kg",
            Stock = 50m,
            MinimumStock = 15m,
            UnitPrice = 18000m,
            IsActive = true
        };

        var result = await _materialService.CreateMaterialAsync(mat);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value > 0);
    }

    [Fact]
    public async Task CreateMaterial_NegativeStock_Fails()
    {
        var mat = new AgriculturalMaterial
        {
            MaterialCode = "MAT-NEG",
            MaterialName = "Bahan Negatif",
            Unit = "kg",
            Stock = -10m
        };

        var result = await _materialService.CreateMaterialAsync(mat);
        Assert.False(result.IsSuccess);
        Assert.Contains("tidak boleh bernilai negatif", result.ErrorMessage);
    }

    [Fact]
    public async Task RecordUsage_DeductsStockAtomically()
    {
        var usage = new MaterialUsage
        {
            ActivityId = 1,
            MaterialId = 1, // Urea stock 100 kg
            Quantity = 30m
        };

        var result = await _materialService.RecordMaterialUsageAsync(usage);
        Assert.True(result.IsSuccess);

        var updated = await _materialRepo.GetByIdAsync(1);
        Assert.NotNull(updated);
        Assert.Equal(70m, updated.Stock); // 100 - 30 = 70
    }

    [Fact]
    public async Task RecordUsage_ExceedingStock_Fails()
    {
        var usage = new MaterialUsage
        {
            ActivityId = 1,
            MaterialId = 1, // Current stock 100 kg
            Quantity = 150m // Exceeds 100!
        };

        var result = await _materialService.RecordMaterialUsageAsync(usage);
        Assert.False(result.IsSuccess);
        Assert.Contains("tidak mencukupi", result.ErrorMessage);

        // Stock unchanged
        var updated = await _materialRepo.GetByIdAsync(1);
        Assert.NotNull(updated);
        Assert.Equal(100m, updated.Stock);
    }

    [Fact]
    public async Task RecordUsage_InactiveMaterial_Fails()
    {
        var usage = new MaterialUsage
        {
            ActivityId = 1,
            MaterialId = 2, // Inactive
            Quantity = 5m
        };

        var result = await _materialService.RecordMaterialUsageAsync(usage);
        Assert.False(result.IsSuccess);
        Assert.Contains("berstatus nonaktif", result.ErrorMessage);
    }

    [Fact]
    public async Task CancelUsage_RestoresStockAtomically()
    {
        // First record 25 kg
        var usage = new MaterialUsage
        {
            ActivityId = 1,
            MaterialId = 1,
            Quantity = 25m
        };
        await _materialService.RecordMaterialUsageAsync(usage);

        var afterUse = await _materialRepo.GetByIdAsync(1);
        Assert.Equal(75m, afterUse?.Stock); // 100 - 25 = 75

        // Now cancel usage (ID 1)
        var cancelResult = await _materialService.CancelMaterialUsageAsync(1);
        Assert.True(cancelResult.IsSuccess);

        var restored = await _materialRepo.GetByIdAsync(1);
        Assert.Equal(100m, restored?.Stock); // 75 + 25 = 100 restored!
    }

    [Fact]
    public async Task FieldOfficer_CannotCreateMaterial_Denied()
    {
        _sessionService.SetSession(new CurrentUser(3, "petugas", "Petugas Lapangan", UserRole.FieldOfficer));

        var mat = new AgriculturalMaterial
        {
            MaterialCode = "MAT-ILLEGAL",
            MaterialName = "Pupuk Ilegal",
            Unit = "kg"
        };

        var result = await _materialService.CreateMaterialAsync(mat);
        Assert.False(result.IsSuccess);
        Assert.Contains("tidak memiliki izin", result.ErrorMessage);
    }
}
