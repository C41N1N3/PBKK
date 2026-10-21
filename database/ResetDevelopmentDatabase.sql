-- ============================================================================
-- Script: ResetDevelopmentDatabase.sql
-- PERHATIAN: Skrip ini HANYA untuk database development AgroTrackDb lokal!
-- ============================================================================

USE [master];
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'AgroTrackDb')
BEGIN
    ALTER DATABASE [AgroTrackDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [AgroTrackDb];
    PRINT 'Database AgroTrackDb lama telah dihapus.';
END
GO

CREATE DATABASE [AgroTrackDb] COLLATE Latin1_General_CI_AS;
PRINT 'Database AgroTrackDb baru berhasil dibuat.';
GO
