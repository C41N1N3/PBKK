-- ============================================================================
-- Script 002: Create Tables, Constraints, and Relationships for AgroTrackDb
-- ============================================================================

USE [AgroTrackDb];
GO

-- 1. Users
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY CLUSTERED,
        Username NVARCHAR(50) NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
        PasswordHash NVARCHAR(256) NOT NULL,
        FullName NVARCHAR(100) NOT NULL,
        Role NVARCHAR(30) NOT NULL CONSTRAINT CK_Users_Role CHECK (Role IN ('Administrator', 'Manager', 'FieldOfficer')),
        IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL
    );
    PRINT 'Tabel Users berhasil dibuat.';
END
GO

-- 2. Lands (Lahan)
IF OBJECT_ID(N'dbo.Lands', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Lands (
        LandId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Lands PRIMARY KEY CLUSTERED,
        LandCode NVARCHAR(30) NOT NULL CONSTRAINT UQ_Lands_LandCode UNIQUE,
        LandName NVARCHAR(100) NOT NULL,
        Location NVARCHAR(200) NOT NULL,
        AreaHectares DECIMAL(10,2) NOT NULL CONSTRAINT CK_Lands_AreaHectares CHECK (AreaHectares > 0),
        SoilType NVARCHAR(50) NOT NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT CK_Lands_Status CHECK (Status IN ('Active', 'Resting', 'Maintenance', 'Inactive')),
        Description NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Lands_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL
    );
    PRINT 'Tabel Lands berhasil dibuat.';
END
GO

-- 3. Commodities (Komoditas)
IF OBJECT_ID(N'dbo.Commodities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Commodities (
        CommodityId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Commodities PRIMARY KEY CLUSTERED,
        CommodityCode NVARCHAR(30) NOT NULL CONSTRAINT UQ_Commodities_CommodityCode UNIQUE,
        CommodityName NVARCHAR(100) NOT NULL,
        ResultUnit NVARCHAR(20) NOT NULL,
        EstimatedGrowthDays INT NOT NULL CONSTRAINT CK_Commodities_GrowthDays CHECK (EstimatedGrowthDays >= 0),
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Commodities_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Commodities_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL
    );
    PRINT 'Tabel Commodities berhasil dibuat.';
END
GO

-- 4. Plantings (Penanaman / Musim Tanam)
IF OBJECT_ID(N'dbo.Plantings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Plantings (
        PlantingId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Plantings PRIMARY KEY CLUSTERED,
        PlantingCode NVARCHAR(30) NOT NULL CONSTRAINT UQ_Plantings_PlantingCode UNIQUE,
        LandId INT NOT NULL CONSTRAINT FK_Plantings_Lands REFERENCES dbo.Lands(LandId),
        CommodityId INT NOT NULL CONSTRAINT FK_Plantings_Commodities REFERENCES dbo.Commodities(CommodityId),
        SeasonName NVARCHAR(100) NOT NULL,
        StartDate DATE NOT NULL,
        ExpectedHarvestDate DATE NOT NULL,
        CompletionDate DATE NULL,
        PlantedAreaHectares DECIMAL(10,2) NOT NULL CONSTRAINT CK_Plantings_Area CHECK (PlantedAreaHectares > 0),
        TargetYield DECIMAL(12,2) NOT NULL CONSTRAINT CK_Plantings_TargetYield CHECK (TargetYield >= 0),
        Status NVARCHAR(30) NOT NULL CONSTRAINT CK_Plantings_Status CHECK (Status IN ('Planned', 'Active', 'ReadyToHarvest', 'Completed', 'Cancelled')),
        Notes NVARCHAR(1000) NULL,
        CreatedBy INT NOT NULL CONSTRAINT FK_Plantings_Users REFERENCES dbo.Users(UserId),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Plantings_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT CK_Plantings_Dates CHECK (ExpectedHarvestDate >= StartDate)
    );
    PRINT 'Tabel Plantings berhasil dibuat.';
END
GO

-- 5. FarmingActivities (Aktivitas Lapangan)
IF OBJECT_ID(N'dbo.FarmingActivities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FarmingActivities (
        ActivityId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FarmingActivities PRIMARY KEY CLUSTERED,
        PlantingId INT NOT NULL CONSTRAINT FK_Activities_Plantings REFERENCES dbo.Plantings(PlantingId),
        ActivityType NVARCHAR(50) NOT NULL,
        ActivityDate DATE NOT NULL,
        Description NVARCHAR(1000) NOT NULL,
        OfficerId INT NOT NULL CONSTRAINT FK_Activities_Users REFERENCES dbo.Users(UserId),
        DurationHours DECIMAL(5,2) NULL CONSTRAINT CK_Activities_Duration CHECK (DurationHours IS NULL OR DurationHours >= 0),
        Status NVARCHAR(30) NOT NULL CONSTRAINT CK_Activities_Status CHECK (Status IN ('Scheduled', 'InProgress', 'Completed', 'Cancelled')),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Activities_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL
    );
    PRINT 'Tabel FarmingActivities berhasil dibuat.';
END
GO

-- 6. AgriculturalMaterials (Bahan Pertanian & Stok)
IF OBJECT_ID(N'dbo.AgriculturalMaterials', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgriculturalMaterials (
        MaterialId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AgriculturalMaterials PRIMARY KEY CLUSTERED,
        MaterialCode NVARCHAR(30) NOT NULL CONSTRAINT UQ_Materials_MaterialCode UNIQUE,
        MaterialName NVARCHAR(100) NOT NULL,
        Category NVARCHAR(50) NOT NULL,
        Unit NVARCHAR(20) NOT NULL,
        Stock DECIMAL(10,2) NOT NULL CONSTRAINT CK_Materials_Stock CHECK (Stock >= 0),
        MinimumStock DECIMAL(10,2) NOT NULL CONSTRAINT CK_Materials_MinStock CHECK (MinimumStock >= 0),
        UnitPrice DECIMAL(18,2) NOT NULL CONSTRAINT CK_Materials_UnitPrice CHECK (UnitPrice >= 0),
        IsActive BIT NOT NULL CONSTRAINT DF_Materials_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Materials_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL
    );
    PRINT 'Tabel AgriculturalMaterials berhasil dibuat.';
END
GO

-- 7. MaterialUsages (Penggunaan Bahan)
IF OBJECT_ID(N'dbo.MaterialUsages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MaterialUsages (
        MaterialUsageId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MaterialUsages PRIMARY KEY CLUSTERED,
        ActivityId INT NOT NULL CONSTRAINT FK_MaterialUsages_Activities REFERENCES dbo.FarmingActivities(ActivityId),
        MaterialId INT NOT NULL CONSTRAINT FK_MaterialUsages_Materials REFERENCES dbo.AgriculturalMaterials(MaterialId),
        Quantity DECIMAL(10,2) NOT NULL CONSTRAINT CK_MaterialUsages_Quantity CHECK (Quantity > 0),
        PriceAtUsage DECIMAL(18,2) NOT NULL,
        Subtotal DECIMAL(18,2) NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_MaterialUsages_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy INT NOT NULL CONSTRAINT FK_MaterialUsages_Users REFERENCES dbo.Users(UserId)
    );
    PRINT 'Tabel MaterialUsages berhasil dibuat.';
END
GO

-- 8. OperationalCosts (Biaya Operasional)
IF OBJECT_ID(N'dbo.OperationalCosts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OperationalCosts (
        CostId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OperationalCosts PRIMARY KEY CLUSTERED,
        PlantingId INT NOT NULL CONSTRAINT FK_Costs_Plantings REFERENCES dbo.Plantings(PlantingId),
        CostDate DATE NOT NULL,
        Category NVARCHAR(50) NOT NULL,
        Description NVARCHAR(500) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL CONSTRAINT CK_Costs_Amount CHECK (Amount > 0),
        CreatedBy INT NOT NULL CONSTRAINT FK_Costs_Users REFERENCES dbo.Users(UserId),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Costs_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL
    );
    PRINT 'Tabel OperationalCosts berhasil dibuat.';
END
GO

-- 9. Harvests (Hasil Panen)
IF OBJECT_ID(N'dbo.Harvests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Harvests (
        HarvestId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Harvests PRIMARY KEY CLUSTERED,
        PlantingId INT NOT NULL CONSTRAINT FK_Harvests_Plantings REFERENCES dbo.Plantings(PlantingId),
        HarvestDate DATE NOT NULL,
        YieldAmount DECIMAL(12,2) NOT NULL CONSTRAINT CK_Harvests_Yield CHECK (YieldAmount > 0),
        Quality NVARCHAR(30) NOT NULL,
        SellingPricePerUnit DECIMAL(18,2) NOT NULL CONSTRAINT CK_Harvests_Price CHECK (SellingPricePerUnit >= 0),
        TotalRevenue DECIMAL(18,2) NOT NULL,
        Notes NVARCHAR(500) NULL,
        CreatedBy INT NOT NULL CONSTRAINT FK_Harvests_Users REFERENCES dbo.Users(UserId),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Harvests_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL
    );
    PRINT 'Tabel Harvests berhasil dibuat.';
END
GO

-- 10. AuditLogs (Audit Jejak Transaksi)
IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs (
        AuditLogId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED,
        UserId INT NULL CONSTRAINT FK_AuditLogs_Users REFERENCES dbo.Users(UserId) ON DELETE SET NULL,
        ActionType NVARCHAR(50) NOT NULL,
        EntityName NVARCHAR(50) NOT NULL,
        EntityId NVARCHAR(50) NULL,
        Description NVARCHAR(1000) NOT NULL,
        Timestamp DATETIME2 NOT NULL CONSTRAINT DF_AuditLogs_Timestamp DEFAULT SYSUTCDATETIME()
    );
    PRINT 'Tabel AuditLogs berhasil dibuat.';
END
GO

-- 11. UserPreferences (Preferensi Tampilan)
IF OBJECT_ID(N'dbo.UserPreferences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserPreferences (
        PreferenceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserPreferences PRIMARY KEY CLUSTERED,
        UserId INT NOT NULL CONSTRAINT UQ_UserPreferences_User UNIQUE CONSTRAINT FK_UserPreferences_Users REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
        Theme NVARCHAR(20) NOT NULL CONSTRAINT DF_UserPreferences_Theme DEFAULT 'Light' CONSTRAINT CK_UserPreferences_Theme CHECK (Theme IN ('Light', 'Dark')),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserPreferences_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
    PRINT 'Tabel UserPreferences berhasil dibuat.';
END
GO
