-- ============================================================================
-- Script 001: Create Database AgroTrackDb
-- Sistem Informasi Manajemen Produksi Pertanian Berbasis Desktop
-- ============================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'AgroTrackDb')
BEGIN
    CREATE DATABASE [AgroTrackDb]
    COLLATE Latin1_General_CI_AS;
    PRINT 'Database AgroTrackDb berhasil dibuat.';
END
ELSE
BEGIN
    PRINT 'Database AgroTrackDb sudah tersedia.';
END
GO
