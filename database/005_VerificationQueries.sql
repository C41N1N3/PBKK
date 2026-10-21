-- ============================================================================
-- Script 005: Query Verifikasi Instalasi dan Integritas AgroTrackDb
-- ============================================================================

USE [AgroTrackDb];
GO

PRINT '=== 1. VERIFIKASI JUMLAH BARIS TIAP TABEL ===';
SELECT 'Users' AS TableName, COUNT(*) AS Row_Count FROM dbo.Users
UNION ALL SELECT 'Lands', COUNT(*) FROM dbo.Lands
UNION ALL SELECT 'Commodities', COUNT(*) FROM dbo.Commodities
UNION ALL SELECT 'Plantings', COUNT(*) FROM dbo.Plantings
UNION ALL SELECT 'FarmingActivities', COUNT(*) FROM dbo.FarmingActivities
UNION ALL SELECT 'AgriculturalMaterials', COUNT(*) FROM dbo.AgriculturalMaterials
UNION ALL SELECT 'MaterialUsages', COUNT(*) FROM dbo.MaterialUsages
UNION ALL SELECT 'OperationalCosts', COUNT(*) FROM dbo.OperationalCosts
UNION ALL SELECT 'Harvests', COUNT(*) FROM dbo.Harvests
UNION ALL SELECT 'AuditLogs', COUNT(*) FROM dbo.AuditLogs
UNION ALL SELECT 'UserPreferences', COUNT(*) FROM dbo.UserPreferences;
GO

PRINT '=== 2. VERIFIKASI KALKULASI LABA-RUGI PER PENANAMAN ===';
SELECT 
    p.PlantingCode,
    p.SeasonName,
    l.LandName,
    c.CommodityName,
    p.TargetYield,
    ISNULL(h.TotalActualYield, 0) AS TotalActualYield,
    CASE 
        WHEN p.TargetYield > 0 THEN ROUND((ISNULL(h.TotalActualYield, 0) / p.TargetYield) * 100, 2)
        ELSE NULL 
    END AS AchievementPercentage,
    ISNULL(m.TotalMaterialCost, 0) AS TotalMaterialCost,
    ISNULL(o.TotalOperationalCost, 0) AS TotalOperationalCost,
    (ISNULL(m.TotalMaterialCost, 0) + ISNULL(o.TotalOperationalCost, 0)) AS TotalCost,
    ISNULL(h.TotalRevenue, 0) AS TotalRevenue,
    (ISNULL(h.TotalRevenue, 0) - (ISNULL(m.TotalMaterialCost, 0) + ISNULL(o.TotalOperationalCost, 0))) AS NetProfitOrLoss
FROM dbo.Plantings p
INNER JOIN dbo.Lands l ON p.LandId = l.LandId
INNER JOIN dbo.Commodities c ON p.CommodityId = c.CommodityId
LEFT JOIN (
    SELECT PlantingId, SUM(YieldAmount) AS TotalActualYield, SUM(TotalRevenue) AS TotalRevenue
    FROM dbo.Harvests
    GROUP BY PlantingId
) h ON p.PlantingId = h.PlantingId
LEFT JOIN (
    SELECT fa.PlantingId, SUM(mu.Subtotal) AS TotalMaterialCost
    FROM dbo.MaterialUsages mu
    INNER JOIN dbo.FarmingActivities fa ON mu.ActivityId = fa.ActivityId
    GROUP BY fa.PlantingId
) m ON p.PlantingId = m.PlantingId
LEFT JOIN (
    SELECT PlantingId, SUM(Amount) AS TotalOperationalCost
    FROM dbo.OperationalCosts
    GROUP BY PlantingId
) o ON p.PlantingId = o.PlantingId
ORDER BY p.PlantingId;
GO
