using AgroTrack.Data;
using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Implementations;
using AgroTrack.Security;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AgroTrack.Tests;

public class DatabaseIntegrationTests
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPasswordHasher _passwordHasher;

    public DatabaseIntegrationTests()
    {
        var connectionString = Environment.GetEnvironmentVariable("AGROTRACK_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set AGROTRACK_CONNECTION_STRING to run SQL Server integration tests.");
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            })
            .Build();

        _connectionFactory = new DbConnectionFactory(config);
        _passwordHasher = new PasswordHasher();
    }

    [Fact]
    public async Task Test_01_LoginRepository_WithSeededAdminAccount_Succeeds()
    {
        var userRepo = new UserRepository(_connectionFactory);
        var admin = await userRepo.GetByUsernameAsync("admin");

        Assert.NotNull(admin);
        Assert.Equal("admin", admin.Username);
        Assert.Equal(UserRole.Administrator, admin.Role);
        Assert.True(admin.IsActive);
        Assert.True(_passwordHasher.VerifyPassword("AdminPassword@123", admin.PasswordHash));
    }

    [Fact]
    public async Task Test_02_LoginRepository_WithNonExistentAccount_ReturnsNull()
    {
        var userRepo = new UserRepository(_connectionFactory);
        var user = await userRepo.GetByUsernameAsync("pengguna_tidak_ada");

        Assert.Null(user);
    }

    [Fact]
    public async Task Test_03_LandRepository_CRUD_PerformsLiveDatabaseOperations()
    {
        var landRepo = new LandRepository(_connectionFactory);
        var uniqueCode = $"LH-TEST-{Guid.NewGuid().ToString("N")[..6]}";

        var newLand = new Land
        {
            LandCode = uniqueCode,
            LandName = "Lahan Uji Integrasi",
            Location = "Blok Barat Daya",
            AreaHectares = 3.75m,
            SoilType = "Aluvial",
            Status = LandStatus.Active,
            Description = "Lahan untuk pengujian integrasi database otomatis"
        };

        // Create
        var newId = await landRepo.CreateAsync(newLand);
        Assert.True(newId > 0);

        // Read
        var fetched = await landRepo.GetByIdAsync(newId);
        Assert.NotNull(fetched);
        Assert.Equal(uniqueCode, fetched.LandCode);
        Assert.Equal(3.75m, fetched.AreaHectares);

        // Update
        fetched.LandName = "Lahan Uji Integrasi Diperbarui";
        fetched.AreaHectares = 4.25m;
        var updated = await landRepo.UpdateAsync(fetched);
        Assert.True(updated);

        // Verify update
        var updatedFetched = await landRepo.GetByIdAsync(newId);
        Assert.NotNull(updatedFetched);
        Assert.Equal("Lahan Uji Integrasi Diperbarui", updatedFetched.LandName);
        Assert.Equal(4.25m, updatedFetched.AreaHectares);
    }

    [Fact]
    public async Task Test_04_CommodityRepository_CRUD_PerformsLiveDatabaseOperations()
    {
        var commodityRepo = new CommodityRepository(_connectionFactory);
        var uniqueCode = $"KMD-{Guid.NewGuid().ToString("N")[..6]}";

        var newCommodity = new Commodity
        {
            CommodityCode = uniqueCode,
            CommodityName = "Bawang Merah Brebes",
            ResultUnit = "kg",
            EstimatedGrowthDays = 60,
            Description = "Komoditas bawang merah unggul",
            IsActive = true
        };

        // Create
        var newId = await commodityRepo.CreateAsync(newCommodity);
        Assert.True(newId > 0);

        // Read
        var fetched = await commodityRepo.GetByIdAsync(newId);
        Assert.NotNull(fetched);
        Assert.Equal(uniqueCode, fetched.CommodityCode);
        Assert.Equal("Bawang Merah Brebes", fetched.CommodityName);
        Assert.Equal(60, fetched.EstimatedGrowthDays);
    }

    [Fact]
    public async Task Test_05_MaterialStock_AtomicDeductionAndRollback_Succeeds()
    {
        var materialRepo = new MaterialRepository(_connectionFactory);

        // 1. Ambil bahan MAT-001 (Benih Padi)
        var matPadi = await materialRepo.GetByCodeAsync("MAT-001");
        Assert.NotNull(matPadi);
        var initialStock = matPadi.Stock;

        // 2. Ambil aktivitas untuk penanaman PLN-2026-001
        var activityRepo = new ActivityRepository(_connectionFactory);
        var activities = await activityRepo.GetByPlantingIdAsync(1);
        Assert.NotEmpty(activities);
        var targetActivity = activities.First();

        // 3. Catat pemakaian 5 kg
        var usage = new MaterialUsage
        {
            ActivityId = targetActivity.ActivityId,
            MaterialId = matPadi.MaterialId,
            Quantity = 5.00m,
            PriceAtUsage = matPadi.UnitPrice,
            CreatedBy = 1
        };

        var recorded = await materialRepo.RecordUsageWithStockDeductionAsync(usage);
        Assert.True(recorded);
        Assert.True(usage.MaterialUsageId > 0);

        // Verifikasi stok berkurang 5 kg
        var matAfterUsage = await materialRepo.GetByIdAsync(matPadi.MaterialId);
        Assert.NotNull(matAfterUsage);
        Assert.Equal(initialStock - 5.00m, matAfterUsage.Stock);

        // 4. Batalkan pemakaian dan verifikasi stok dipulihkan
        var reverted = await materialRepo.RevertUsageWithStockRestorationAsync(usage.MaterialUsageId);
        Assert.True(reverted);

        var matAfterRevert = await materialRepo.GetByIdAsync(matPadi.MaterialId);
        Assert.NotNull(matAfterRevert);
        Assert.Equal(initialStock, matAfterRevert.Stock);
    }

    [Fact]
    public async Task Test_06_DashboardRepository_LiveDatabaseQuery_ReturnsAccurateMetrics()
    {
        var dashboardRepo = new DashboardRepository(_connectionFactory);
        var summary = await dashboardRepo.GetSummaryAsync(null, null);

        Assert.NotNull(summary);
        Assert.True(summary.TotalActiveLands >= 4, "Jumlah lahan aktif harus >= 4 dari seed data (4 aktif, 1 inaktif)");
        Assert.True(summary.TotalLandAreaHectares >= 15.0m, "Total luas lahan harus >= 15 hektar");
        Assert.True(summary.ActivePlantingsCount >= 3, "Penanaman aktif harus >= 3");
        Assert.True(summary.TotalHarvestYield >= 26300m, "Total panen harus mencakup panen padi terverifikasi");
        Assert.True(summary.TotalRevenue >= 174120000m, "Total pendapatan harus mencakup panen padi terverifikasi");
        Assert.True(summary.TotalOperationalCost > 0, "Total biaya operasional harus lebih dari 0");
    }

    [Fact]
    public async Task Test_07_ReportRepository_ProfitLossQuery_ReturnsVerifiedCalculations()
    {
        var reportRepo = new ReportRepository(_connectionFactory);
        var report = await reportRepo.GetProfitLossReportAsync(null, null, null, null, null);

        Assert.NotEmpty(report);

        // Cari PLN-2026-001 dari seed data
        var pln1 = report.FirstOrDefault(r => r.PlantingCode == "PLN-2026-001");
        Assert.NotNull(pln1);
        Assert.Equal(25000.00m, pln1.TargetYield);
        Assert.Equal(26300.00m, pln1.ActualYield);
        Assert.Equal(174120000.00m, pln1.TotalRevenue);
        Assert.Equal(10625000.00m, pln1.TotalMaterialCost);
        Assert.Equal(9550000.00m, pln1.TotalOperationalCost);
        Assert.Equal(20175000.00m, pln1.TotalCost);
        Assert.Equal(153945000.00m, pln1.NetProfit);
    }
}
