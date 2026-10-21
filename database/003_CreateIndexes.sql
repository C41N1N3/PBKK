-- ============================================================================
-- Script 003: Create Performance Indexes for AgroTrackDb
-- ============================================================================

USE [AgroTrackDb];
GO

-- Users
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Username' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE NONCLUSTERED INDEX IX_Users_Username ON dbo.Users(Username) INCLUDE (FullName, Role, IsActive);
GO

-- Lands
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Lands_Status' AND object_id = OBJECT_ID('dbo.Lands'))
    CREATE NONCLUSTERED INDEX IX_Lands_Status ON dbo.Lands(Status) INCLUDE (LandCode, LandName, AreaHectares);
GO

-- Commodities
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Commodities_IsActive' AND object_id = OBJECT_ID('dbo.Commodities'))
    CREATE NONCLUSTERED INDEX IX_Commodities_IsActive ON dbo.Commodities(IsActive) INCLUDE (CommodityCode, CommodityName, ResultUnit);
GO

-- Plantings
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Plantings_LandId_Status' AND object_id = OBJECT_ID('dbo.Plantings'))
    CREATE NONCLUSTERED INDEX IX_Plantings_LandId_Status ON dbo.Plantings(LandId, Status) INCLUDE (CommodityId, StartDate, ExpectedHarvestDate, PlantedAreaHectares);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Plantings_CommodityId_Status' AND object_id = OBJECT_ID('dbo.Plantings'))
    CREATE NONCLUSTERED INDEX IX_Plantings_CommodityId_Status ON dbo.Plantings(CommodityId, Status);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Plantings_Dates' AND object_id = OBJECT_ID('dbo.Plantings'))
    CREATE NONCLUSTERED INDEX IX_Plantings_Dates ON dbo.Plantings(StartDate, ExpectedHarvestDate, Status);
GO

-- FarmingActivities
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Activities_PlantingId_Date' AND object_id = OBJECT_ID('dbo.FarmingActivities'))
    CREATE NONCLUSTERED INDEX IX_Activities_PlantingId_Date ON dbo.FarmingActivities(PlantingId, ActivityDate, Status);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Activities_OfficerId' AND object_id = OBJECT_ID('dbo.FarmingActivities'))
    CREATE NONCLUSTERED INDEX IX_Activities_OfficerId ON dbo.FarmingActivities(OfficerId, ActivityDate);
GO

-- AgriculturalMaterials
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Materials_Category_Active' AND object_id = OBJECT_ID('dbo.AgriculturalMaterials'))
    CREATE NONCLUSTERED INDEX IX_Materials_Category_Active ON dbo.AgriculturalMaterials(Category, IsActive) INCLUDE (MaterialCode, MaterialName, Stock, MinimumStock);
GO

-- MaterialUsages
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MaterialUsages_ActivityId' AND object_id = OBJECT_ID('dbo.MaterialUsages'))
    CREATE NONCLUSTERED INDEX IX_MaterialUsages_ActivityId ON dbo.MaterialUsages(ActivityId, MaterialId);
GO

-- OperationalCosts
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Costs_PlantingId_Date' AND object_id = OBJECT_ID('dbo.OperationalCosts'))
    CREATE NONCLUSTERED INDEX IX_Costs_PlantingId_Date ON dbo.OperationalCosts(PlantingId, CostDate) INCLUDE (Category, Amount);
GO

-- Harvests
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Harvests_PlantingId_Date' AND object_id = OBJECT_ID('dbo.Harvests'))
    CREATE NONCLUSTERED INDEX IX_Harvests_PlantingId_Date ON dbo.Harvests(PlantingId, HarvestDate) INCLUDE (YieldAmount, TotalRevenue);
GO

-- AuditLogs
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditLogs_Timestamp' AND object_id = OBJECT_ID('dbo.AuditLogs'))
    CREATE NONCLUSTERED INDEX IX_AuditLogs_Timestamp ON dbo.AuditLogs(Timestamp DESC) INCLUDE (UserId, ActionType, EntityName);
GO

PRINT 'Seluruh indeks kinerja berhasil dibuat.';
GO
