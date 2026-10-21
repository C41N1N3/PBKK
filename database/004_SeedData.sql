-- ============================================================================
-- Script 004: Seed Data Realistis AgroTrackDb (Bahasa Indonesia)
-- Lingkungan: Development / Pengujian
-- Catatan Keamanan: Akun demo ini HANYA untuk development dan demonstrasi!
-- ============================================================================

USE [AgroTrackDb];
GO

-- 1. Users
-- Password demo:
-- admin   : AdminPassword@123
-- manager : ManagerPassword@123
-- petugas : PetugasPassword@123
-- nonaktif: UserPassword@123
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'admin')
BEGIN
    INSERT INTO dbo.Users (Username, PasswordHash, FullName, Role, IsActive, CreatedAt)
    VALUES 
    ('admin', 'qS8ick4EZHOh9/Nr5wsgIw==:Q/oa+dvWS/wPJcD2WxqXizNbO7W3zrUt0mqsekH4bM0=', 'Hendra Kusuma, S.Kom', 'Administrator', 1, SYSUTCDATETIME()),
    ('manager', 'nBh0lYfUddNbHSDqklmQYw==:DyF/NIZ+pFDuphjo0Qlhr29ZR/5d5p057bEmHDRVkkk=', 'Ir. Budi Santoso, M.Agr', 'Manager', 1, SYSUTCDATETIME()),
    ('petugas', 'AqgBXKcny960lIAKJmZm2A==:BaKt0fOrp68MHnjyXRWutp2a73oYMBzYYbDfZeapDR8=', 'Ahmad Syafi''i', 'FieldOfficer', 1, SYSUTCDATETIME()),
    ('nonaktif', 'O9kzu/8gPj/daw3vu80e5w==:98YGdIYj+V9yR1tsmFgLmaqhNM/ugZowqORMLiKjsHE=', 'Eko Prasetyo (Nonaktif)', 'FieldOfficer', 0, SYSUTCDATETIME());

    PRINT 'Seed data Users berhasil dimasukkan.';
END
GO

-- 2. Lands (5 Lahan)
IF NOT EXISTS (SELECT 1 FROM dbo.Lands WHERE LandCode = 'LHN-001')
BEGIN
    INSERT INTO dbo.Lands (LandCode, LandName, Location, AreaHectares, SoilType, Status, Description, CreatedAt)
    VALUES
    ('LHN-001', 'Lahan Utara', 'Blok A Sukatani, Jawa Barat', 10.50, 'Aluvial', 'Active', 'Kondisi air berlimpah, dekat saluran irigasi primer', SYSUTCDATETIME()),
    ('LHN-002', 'Lahan Selatan', 'Blok C Pasir Mulya, Jawa Barat', 8.00, 'Lempung Berpasir', 'Active', 'Sangat cocok untuk palawija dan jagung hibrida', SYSUTCDATETIME()),
    ('LHN-003', 'Lahan Percobaan', 'Kawasan Agro AgroTech Center', 3.50, 'Andosol', 'Active', 'Lahan riset pemuliaan dan rotasi tanaman musiman', SYSUTCDATETIME()),
    ('LHN-004', 'Lahan Greenhouse', 'Kompleks Hortikultura Blok G', 1.50, 'Tanah Olah Terkendali', 'Active', 'Instalasi greenhouse presisi untuk komoditas bernilai tinggi', SYSUTCDATETIME()),
    ('LHN-005', 'Lahan Timur', 'Blok E Waringin Jaya, Jawa Barat', 12.00, 'Grumusol', 'Resting', 'Dalam masa pemulihan unsur hara pasca panen raya', SYSUTCDATETIME());

    PRINT 'Seed data Lands berhasil dimasukkan.';
END
GO

-- 3. Commodities (5 Komoditas)
IF NOT EXISTS (SELECT 1 FROM dbo.Commodities WHERE CommodityCode = 'KMD-001')
BEGIN
    INSERT INTO dbo.Commodities (CommodityCode, CommodityName, ResultUnit, EstimatedGrowthDays, Description, IsActive, CreatedAt)
    VALUES
    ('KMD-001', 'Padi Ciherang', 'kg', 115, 'Varietas padi unggul tahan wereng dan anakan produktif tinggi', 1, SYSUTCDATETIME()),
    ('KMD-002', 'Jagung Manis Bonanza', 'kg', 75, 'Jagung manis kualitas pasar modern dengan tongkol padat berbobot', 1, SYSUTCDATETIME()),
    ('KMD-003', 'Kedelai Anjasmoro', 'kg', 85, 'Kedelai biji besar cocok untuk produksi tahu dan tempe konsumsi', 1, SYSUTCDATETIME()),
    ('KMD-004', 'Cabai Merah Keriting', 'kg', 80, 'Varietas pedas tahan antraknosa dengan masa panen bertahap panjang', 1, SYSUTCDATETIME()),
    ('KMD-005', 'Tomat Servo', 'kg', 70, 'Tomat dataran rendah tahan Gemini virus dan hasil lebat', 1, SYSUTCDATETIME());

    PRINT 'Seed data Commodities berhasil dimasukkan.';
END
GO

-- 4. AgriculturalMaterials (10 Bahan Pertanian)
IF NOT EXISTS (SELECT 1 FROM dbo.AgriculturalMaterials WHERE MaterialCode = 'MAT-001')
BEGIN
    INSERT INTO dbo.AgriculturalMaterials (MaterialCode, MaterialName, Category, Unit, Stock, MinimumStock, UnitPrice, IsActive, CreatedAt)
    VALUES
    ('MAT-001', 'Benih Padi Ciherang Unggul', 'Seed', 'kg', 150.00, 30.00, 25000.00, 1, SYSUTCDATETIME()),
    ('MAT-002', 'Benih Jagung Hibrida F1', 'Seed', 'kg', 80.00, 20.00, 85000.00, 1, SYSUTCDATETIME()),
    ('MAT-003', 'Benih Cabai Unggul F1', 'Seed', 'pack', 40.00, 10.00, 135000.00, 1, SYSUTCDATETIME()),
    ('MAT-004', 'Pupuk Urea Petro', 'Fertilizer', 'kg', 500.00, 100.00, 12500.00, 1, SYSUTCDATETIME()),
    ('MAT-005', 'Pupuk NPK Phonska Plus', 'Fertilizer', 'kg', 450.00, 100.00, 18500.00, 1, SYSUTCDATETIME()),
    ('MAT-006', 'Pupuk Organik Kascing Super', 'Fertilizer', 'kg', 1200.00, 250.00, 4500.00, 1, SYSUTCDATETIME()),
    ('MAT-007', 'Insektisida Nabati Neem Oil', 'Pesticide', 'liter', 35.00, 10.00, 95000.00, 1, SYSUTCDATETIME()),
    ('MAT-008', 'Fungisida Dithane M-45', 'Fungicide', 'kg', 25.00, 8.00, 110000.00, 1, SYSUTCDATETIME()),
    ('MAT-009', 'Herbisida Selektif Agil', 'Herbicide', 'liter', 15.00, 5.00, 140000.00, 1, SYSUTCDATETIME()),
    ('MAT-010', 'Mulsa Plastik Hitam Perak', 'ConsumableTool', 'rol', 12.00, 4.00, 320000.00, 1, SYSUTCDATETIME());

    PRINT 'Seed data AgriculturalMaterials berhasil dimasukkan.';
END
GO

-- 5. Plantings (8 Penanaman dengan berbagai status)
IF NOT EXISTS (SELECT 1 FROM dbo.Plantings WHERE PlantingCode = 'PLN-2026-001')
BEGIN
    DECLARE @adminId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = 'admin');
    DECLARE @managerId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = 'manager');
    
    DECLARE @landUtara INT = (SELECT TOP 1 LandId FROM dbo.Lands WHERE LandCode = 'LHN-001');
    DECLARE @landSelatan INT = (SELECT TOP 1 LandId FROM dbo.Lands WHERE LandCode = 'LHN-002');
    DECLARE @landPercobaan INT = (SELECT TOP 1 LandId FROM dbo.Lands WHERE LandCode = 'LHN-003');
    DECLARE @landGreenhouse INT = (SELECT TOP 1 LandId FROM dbo.Lands WHERE LandCode = 'LHN-004');
    
    DECLARE @kmdPadi INT = (SELECT TOP 1 CommodityId FROM dbo.Commodities WHERE CommodityCode = 'KMD-001');
    DECLARE @kmdJagung INT = (SELECT TOP 1 CommodityId FROM dbo.Commodities WHERE CommodityCode = 'KMD-002');
    DECLARE @kmdKedelai INT = (SELECT TOP 1 CommodityId FROM dbo.Commodities WHERE CommodityCode = 'KMD-003');
    DECLARE @kmdCabai INT = (SELECT TOP 1 CommodityId FROM dbo.Commodities WHERE CommodityCode = 'KMD-004');
    DECLARE @kmdTomat INT = (SELECT TOP 1 CommodityId FROM dbo.Commodities WHERE CommodityCode = 'KMD-005');

    INSERT INTO dbo.Plantings (PlantingCode, LandId, CommodityId, SeasonName, StartDate, ExpectedHarvestDate, CompletionDate, PlantedAreaHectares, TargetYield, Status, Notes, CreatedBy, CreatedAt)
    VALUES
    -- 1. Completed
    ('PLN-2026-001', @landUtara, @kmdPadi, 'Musim Hujan I 2026', '2026-01-10', '2026-05-05', '2026-05-06', 5.00, 25000.00, 'Completed', 'Siklus panen raya berhasil melampaui target', @managerId, SYSUTCDATETIME()),
    -- 2. ReadyToHarvest
    ('PLN-2026-002', @landSelatan, @kmdJagung, 'Musim Kemarau 2026', '2026-07-01', '2026-09-15', NULL, 4.00, 18000.00, 'ReadyToHarvest', 'Tongkol jagung sudah menguning sempurna siap petik', @managerId, SYSUTCDATETIME()),
    -- 3. Active
    ('PLN-2026-003', @landPercobaan, @kmdKedelai, 'Musim Tanam Gadu 2026', '2026-08-10', '2026-11-05', NULL, 2.50, 6000.00, 'Active', 'Memasuki fase pembentukan polong dan pemupukan susulan', @managerId, SYSUTCDATETIME()),
    -- 4. Active
    ('PLN-2026-004', @landGreenhouse, @kmdCabai, 'Musim Hortikultura 2026', '2026-08-01', '2026-10-20', NULL, 1.00, 4500.00, 'Active', 'Pemeliharaan iklim mikro dan pengairan tetes teratur', @adminId, SYSUTCDATETIME()),
    -- 5. Planned
    ('PLN-2026-005', @landUtara, @kmdPadi, 'Musim Hujan II 2026', '2026-11-01', '2027-02-25', NULL, 6.00, 30000.00, 'Planned', 'Rencana penanaman serentak kelompok tani', @managerId, SYSUTCDATETIME()),
    -- 6. Completed
    ('PLN-2026-006', @landGreenhouse, @kmdTomat, 'Musim Awal 2026', '2026-02-15', '2026-04-25', '2026-04-26', 1.20, 8000.00, 'Completed', 'Panen tomat segar terserap ke supermarket lokal', @adminId, SYSUTCDATETIME()),
    -- 7. Cancelled
    ('PLN-2026-007', @landSelatan, @kmdKedelai, 'Musim Percobaan 2026', '2026-03-01', '2026-05-25', '2026-03-15', 3.00, 7000.00, 'Cancelled', 'Dibatalkan akibat curah hujan ekstrem dan genangan drainase', @managerId, SYSUTCDATETIME()),
    -- 8. Active
    ('PLN-2026-008', @landUtara, @kmdJagung, 'Tumpang Sari 2026', '2026-08-15', '2026-10-30', NULL, 3.00, 12000.00, 'Active', 'Pertumbuhan vegetatif sangat subur', @adminId, SYSUTCDATETIME());

    PRINT 'Seed data Plantings berhasil dimasukkan.';
END
GO

-- 6. FarmingActivities (20 Aktivitas)
IF NOT EXISTS (SELECT 1 FROM dbo.FarmingActivities WHERE ActivityType = 'Persiapan lahan')
BEGIN
    DECLARE @petugasId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = 'petugas');
    DECLARE @p1 INT = (SELECT PlantingId FROM dbo.Plantings WHERE PlantingCode = 'PLN-2026-001');
    DECLARE @p2 INT = (SELECT PlantingId FROM dbo.Plantings WHERE PlantingCode = 'PLN-2026-002');
    DECLARE @p3 INT = (SELECT PlantingId FROM dbo.Plantings WHERE PlantingCode = 'PLN-2026-003');
    DECLARE @p4 INT = (SELECT PlantingId FROM dbo.Plantings WHERE PlantingCode = 'PLN-2026-004');

    INSERT INTO dbo.FarmingActivities (PlantingId, ActivityType, ActivityDate, Description, OfficerId, DurationHours, Status, CreatedAt)
    VALUES
    (@p1, 'Persiapan lahan', '2026-01-11', 'Pembajakan tanah menggunakan traktor dan pembuatan bedengan sawah', @petugasId, 8.0, 'Completed', SYSUTCDATETIME()),
    (@p1, 'Penanaman', '2026-01-15', 'Penanaman bibit padi metode jajar legowo 2:1', @petugasId, 7.5, 'Completed', SYSUTCDATETIME()),
    (@p1, 'Pemupukan', '2026-01-28', 'Pemupukan dasar dengan Urea dan NPK Phonska', @petugasId, 5.0, 'Completed', SYSUTCDATETIME()),
    (@p1, 'Penyiangan', '2026-02-15', 'Pembersihan gulma liar di sela-sela rumpun padi', @petugasId, 6.0, 'Completed', SYSUTCDATETIME()),
    (@p1, 'Pengendalian hama', '2026-03-05', 'Penyemprotan pestisida nabati antisipasi wereng coklat', @petugasId, 4.0, 'Completed', SYSUTCDATETIME()),
    (@p1, 'Penyiraman', '2026-03-20', 'Pengaturan debit air pintu irigasi penggenangan berkala', @petugasId, 3.5, 'Completed', SYSUTCDATETIME()),
    (@p1, 'Panen', '2026-05-05', 'Pemanenan padi menggunakan combine harvester kelompok tani', @petugasId, 9.0, 'Completed', SYSUTCDATETIME()),

    (@p2, 'Persiapan lahan', '2026-07-02', 'Penggemburan tanah dan perapihan saluran drainase jagung', @petugasId, 7.0, 'Completed', SYSUTCDATETIME()),
    (@p2, 'Penanaman', '2026-07-05', 'Penanaman benih jagung manis dengan tugal 2 biji per lubang', @petugasId, 6.5, 'Completed', SYSUTCDATETIME()),
    (@p2, 'Pemupukan', '2026-07-20', 'Aplikasi pupuk NPK tahap I dan pembumbunan tanah', @petugasId, 5.0, 'Completed', SYSUTCDATETIME()),
    (@p2, 'Pemeriksaan', '2026-08-10', 'Monitoring kesehatan tanaman dan pencegahan ulat grayak', @petugasId, 4.0, 'Completed', SYSUTCDATETIME()),
    (@p2, 'Pengendalian hama', '2026-08-18', 'Penyemprotan insektisida Curacron pada daun muda', @petugasId, 4.5, 'Completed', SYSUTCDATETIME()),
    (@p2, 'Pemeriksaan', '2026-09-12', 'Inspeksi kematangan bulir jagung untuk persiapan panen', @petugasId, 3.0, 'Completed', SYSUTCDATETIME()),

    (@p3, 'Persiapan lahan', '2026-08-11', 'Olah tanah minimum dan tabur pupuk organik kascing', @petugasId, 6.0, 'Completed', SYSUTCDATETIME()),
    (@p3, 'Penanaman', '2026-08-14', 'Tanam benih kedelai anjasmoro dengan jarak tanam rapat', @petugasId, 5.5, 'Completed', SYSUTCDATETIME()),
    (@p3, 'Penyiangan', '2026-09-02', 'Penyiangan rumput liar sekitar pangkal batang kedelai', @petugasId, 4.5, 'Completed', SYSUTCDATETIME()),
    (@p3, 'Penyiraman', '2026-09-15', 'Irigasi tetes berkala untuk menjaga kelembapan polong', @petugasId, 3.0, 'InProgress', SYSUTCDATETIME()),

    (@p4, 'Persiapan lahan', '2026-08-02', 'Sterilisasi greenhouse dan pemasangan mulsa plastik baru', @petugasId, 8.0, 'Completed', SYSUTCDATETIME()),
    (@p4, 'Penanaman', '2026-08-05', 'Pindah tanam bibit cabai merah umur 21 hari ke bedengan', @petugasId, 6.0, 'Completed', SYSUTCDATETIME()),
    (@p4, 'Pemupukan', '2026-08-22', 'Fertigasi nutrisi mikro dan kalsium pencegah rontok bunga', @petugasId, 4.0, 'Completed', SYSUTCDATETIME());

    PRINT 'Seed data FarmingActivities berhasil dimasukkan.';
END
GO

-- 7. OperationalCosts (15 Biaya Operasional)
IF NOT EXISTS (SELECT 1 FROM dbo.OperationalCosts WHERE Category = 'Tenaga kerja')
BEGIN
    DECLARE @adminUser INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = 'admin');
    DECLARE @managerUser INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = 'manager');
    DECLARE @pln1 INT = (SELECT PlantingId FROM dbo.Plantings WHERE PlantingCode = 'PLN-2026-001');
    DECLARE @pln2 INT = (SELECT PlantingId FROM dbo.Plantings WHERE PlantingCode = 'PLN-2026-002');
    DECLARE @pln3 INT = (SELECT PlantingId FROM dbo.Plantings WHERE PlantingCode = 'PLN-2026-003');
    DECLARE @pln4 INT = (SELECT PlantingId FROM dbo.Plantings WHERE PlantingCode = 'PLN-2026-004');
    DECLARE @pln6 INT = (SELECT PlantingId FROM dbo.Plantings WHERE PlantingCode = 'PLN-2026-006');

    INSERT INTO dbo.OperationalCosts (PlantingId, CostDate, Category, Description, Amount, CreatedBy, CreatedAt)
    VALUES
    (@pln1, '2026-01-12', 'Sewa alat', 'Sewa traktor bajak sawah 5 hektare', 2500000.00, @managerUser, SYSUTCDATETIME()),
    (@pln1, '2026-01-16', 'Tenaga kerja', 'Upah 10 buruh tanam padi harian', 1500000.00, @managerUser, SYSUTCDATETIME()),
    (@pln1, '2026-02-16', 'Tenaga kerja', 'Upah tenaga penyiangan gulma', 900000.00, @managerUser, SYSUTCDATETIME()),
    (@pln1, '2026-04-01', 'Irigasi', 'Retribusi operasional pintu air bendungan', 450000.00, @managerUser, SYSUTCDATETIME()),
    (@pln1, '2026-05-05', 'Sewa alat', 'Sewa mesin perontok padi (combine harvester)', 3000000.00, @managerUser, SYSUTCDATETIME()),
    (@pln1, '2026-05-06', 'Transportasi', 'Ongkos truk angkut gabah ke gudang penjemuran', 1200000.00, @managerUser, SYSUTCDATETIME()),

    (@pln2, '2026-07-03', 'Sewa alat', 'Sewa bajak rotavator lahan jagung', 1800000.00, @managerUser, SYSUTCDATETIME()),
    (@pln2, '2026-07-06', 'Tenaga kerja', 'Upah buruh tugal tanam jagung', 1000000.00, @managerUser, SYSUTCDATETIME()),
    (@pln2, '2026-08-01', 'Bahan bakar', 'Solar operasional mesin pompa air', 650000.00, @managerUser, SYSUTCDATETIME()),
    (@pln2, '2026-08-20', 'Perawatan', 'Perbaikan saluran irigasi pipa PVC', 350000.00, @managerUser, SYSUTCDATETIME()),

    (@pln3, '2026-08-12', 'Tenaga kerja', 'Upah olah tanah dan tabur kompos', 850000.00, @managerUser, SYSUTCDATETIME()),
    (@pln3, '2026-08-15', 'Tenaga kerja', 'Upah tanam kedelai', 750000.00, @managerUser, SYSUTCDATETIME()),

    (@pln4, '2026-08-03', 'Perawatan', 'Pergantian filter nozel fertigasi greenhouse', 500000.00, @adminUser, SYSUTCDATETIME()),
    (@pln4, '2026-08-06', 'Tenaga kerja', 'Upah pindah tanam bibit cabai ke bedeng', 600000.00, @adminUser, SYSUTCDATETIME()),

    (@pln6, '2026-04-26', 'Transportasi', 'Pengiriman tomat segar ke gudang distributor', 800000.00, @adminUser, SYSUTCDATETIME());

    PRINT 'Seed data OperationalCosts berhasil dimasukkan.';
END
GO

-- 8. Harvests (10 Transaksi Panen)
IF NOT EXISTS (SELECT 1 FROM dbo.Harvests WHERE Notes = 'Panen raya padi Ciherang utama')
BEGIN
    DECLARE @adminU INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = 'admin');
    DECLARE @managerU INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = 'manager');
    DECLARE @pln1Id INT = (SELECT PlantingId FROM dbo.Plantings WHERE PlantingCode = 'PLN-2026-001');
    DECLARE @pln2Id INT = (SELECT PlantingId FROM dbo.Plantings WHERE PlantingCode = 'PLN-2026-002');
    DECLARE @pln6Id INT = (SELECT PlantingId FROM dbo.Plantings WHERE PlantingCode = 'PLN-2026-006');

    INSERT INTO dbo.Harvests (PlantingId, HarvestDate, YieldAmount, Quality, SellingPricePerUnit, TotalRevenue, Notes, CreatedBy, CreatedAt)
    VALUES
    (@pln1Id, '2026-05-05', 14500.00, 'Grade A', 6800.00, 98600000.00, 'Panen raya padi Ciherang utama', @managerU, SYSUTCDATETIME()),
    (@pln1Id, '2026-05-06', 11800.00, 'Grade B', 6400.00, 75520000.00, 'Panen raya padi Ciherang petak kedua', @managerU, SYSUTCDATETIME()),

    (@pln2Id, '2026-09-14', 8500.00, 'Grade A', 4500.00, 38250000.00, 'Petik perdana jagung manis premium', @managerU, SYSUTCDATETIME()),
    (@pln2Id, '2026-09-16', 7800.00, 'Grade A', 4500.00, 35100000.00, 'Petik kedua jagung manis', @managerU, SYSUTCDATETIME()),
    (@pln2Id, '2026-09-18', 2100.00, 'Grade B', 3800.00, 7980000.00, 'Petik penutup jagung manis', @managerU, SYSUTCDATETIME()),

    (@pln6Id, '2026-04-18', 2200.00, 'Grade A', 9500.00, 20900000.00, 'Petik tahap 1 tomat hidroponik', @adminU, SYSUTCDATETIME()),
    (@pln6Id, '2026-04-20', 2500.00, 'Grade A', 9500.00, 23750000.00, 'Petik tahap 2 tomat lebat', @adminU, SYSUTCDATETIME()),
    (@pln6Id, '2026-04-22', 2000.00, 'Grade A', 9000.00, 18000000.00, 'Petik tahap 3 tomat', @adminU, SYSUTCDATETIME()),
    (@pln6Id, '2026-04-24', 1100.00, 'Grade B', 8000.00, 8800000.00, 'Petik tahap 4 tomat', @adminU, SYSUTCDATETIME()),
    (@pln6Id, '2026-04-26', 600.00, 'Grade C', 6500.00, 3900000.00, 'Petik akhir pembersihan tanaman tomat', @adminU, SYSUTCDATETIME());

    PRINT 'Seed data Harvests berhasil dimasukkan.';
END
GO

-- 9. MaterialUsages (15 Penggunaan Bahan)
IF NOT EXISTS (SELECT 1 FROM dbo.MaterialUsages)
BEGIN
    DECLARE @officerUser INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = 'petugas');
    
    DECLARE @act1 INT = (SELECT TOP 1 ActivityId FROM dbo.FarmingActivities WHERE Description LIKE '%Penanaman bibit padi%');
    DECLARE @act2 INT = (SELECT TOP 1 ActivityId FROM dbo.FarmingActivities WHERE Description LIKE '%Pemupukan dasar dengan Urea%');
    DECLARE @act3 INT = (SELECT TOP 1 ActivityId FROM dbo.FarmingActivities WHERE Description LIKE '%Penyemprotan pestisida nabati%');
    DECLARE @act4 INT = (SELECT TOP 1 ActivityId FROM dbo.FarmingActivities WHERE Description LIKE '%Penanaman benih jagung%');
    DECLARE @act5 INT = (SELECT TOP 1 ActivityId FROM dbo.FarmingActivities WHERE Description LIKE '%Aplikasi pupuk NPK tahap I%');
    DECLARE @act6 INT = (SELECT TOP 1 ActivityId FROM dbo.FarmingActivities WHERE Description LIKE '%Tanam benih kedelai%');
    DECLARE @act7 INT = (SELECT TOP 1 ActivityId FROM dbo.FarmingActivities WHERE Description LIKE '%Sterilisasi greenhouse%');
    DECLARE @act8 INT = (SELECT TOP 1 ActivityId FROM dbo.FarmingActivities WHERE Description LIKE '%Fertigasi nutrisi mikro%');

    DECLARE @matBenihPadi INT = (SELECT TOP 1 MaterialId FROM dbo.AgriculturalMaterials WHERE MaterialCode = 'MAT-001');
    DECLARE @matBenihJagung INT = (SELECT TOP 1 MaterialId FROM dbo.AgriculturalMaterials WHERE MaterialCode = 'MAT-002');
    DECLARE @matBenihCabai INT = (SELECT TOP 1 MaterialId FROM dbo.AgriculturalMaterials WHERE MaterialCode = 'MAT-003');
    DECLARE @matUrea INT = (SELECT TOP 1 MaterialId FROM dbo.AgriculturalMaterials WHERE MaterialCode = 'MAT-004');
    DECLARE @matNPK INT = (SELECT TOP 1 MaterialId FROM dbo.AgriculturalMaterials WHERE MaterialCode = 'MAT-005');
    DECLARE @matKompos INT = (SELECT TOP 1 MaterialId FROM dbo.AgriculturalMaterials WHERE MaterialCode = 'MAT-006');
    DECLARE @matNeem INT = (SELECT TOP 1 MaterialId FROM dbo.AgriculturalMaterials WHERE MaterialCode = 'MAT-007');
    DECLARE @matDithane INT = (SELECT TOP 1 MaterialId FROM dbo.AgriculturalMaterials WHERE MaterialCode = 'MAT-008');
    DECLARE @matMulsa INT = (SELECT TOP 1 MaterialId FROM dbo.AgriculturalMaterials WHERE MaterialCode = 'MAT-010');

    IF (@act1 IS NOT NULL AND @matBenihPadi IS NOT NULL)
    BEGIN
        INSERT INTO dbo.MaterialUsages (ActivityId, MaterialId, Quantity, PriceAtUsage, Subtotal, CreatedAt, CreatedBy)
        VALUES
        (@act1, @matBenihPadi, 100.00, 25000.00, 2500000.00, SYSUTCDATETIME(), @officerUser),
        (@act2, @matUrea, 200.00, 12500.00, 2500000.00, SYSUTCDATETIME(), @officerUser),
        (@act2, @matNPK, 150.00, 18500.00, 2775000.00, SYSUTCDATETIME(), @officerUser),
        (@act2, @matKompos, 300.00, 4500.00, 1350000.00, SYSUTCDATETIME(), @officerUser),
        (@act3, @matNeem, 10.00, 95000.00, 950000.00, SYSUTCDATETIME(), @officerUser),
        (@act3, @matDithane, 5.00, 110000.00, 550000.00, SYSUTCDATETIME(), @officerUser),

        (@act4, @matBenihJagung, 40.00, 85000.00, 3400000.00, SYSUTCDATETIME(), @officerUser),
        (@act5, @matUrea, 100.00, 12500.00, 1250000.00, SYSUTCDATETIME(), @officerUser),
        (@act5, @matNPK, 120.00, 18500.00, 2220000.00, SYSUTCDATETIME(), @officerUser),
        (@act5, @matKompos, 250.00, 4500.00, 1125000.00, SYSUTCDATETIME(), @officerUser),

        (@act6, @matKompos, 200.00, 4500.00, 900000.00, SYSUTCDATETIME(), @officerUser),
        (@act6, @matNPK, 50.00, 18500.00, 925000.00, SYSUTCDATETIME(), @officerUser),

        (@act7, @matMulsa, 4.00, 320000.00, 1280000.00, SYSUTCDATETIME(), @officerUser),
        (@act7, @matBenihCabai, 15.00, 135000.00, 2025000.00, SYSUTCDATETIME(), @officerUser),
        (@act8, @matNPK, 30.00, 18500.00, 555000.00, SYSUTCDATETIME(), @officerUser);

        PRINT 'Seed data MaterialUsages berhasil dimasukkan.';
    END
END
GO

-- 10. AuditLogs Inisial
IF NOT EXISTS (SELECT 1 FROM dbo.AuditLogs)
BEGIN
    DECLARE @adm INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = 'admin');
    INSERT INTO dbo.AuditLogs (UserId, ActionType, EntityName, EntityId, Description, Timestamp)
    VALUES
    (@adm, 'SYSTEM_INIT', 'Database', 'AgroTrackDb', 'Inisialisasi skema database dan seed data sistem AgroTrack', SYSUTCDATETIME()),
    (@adm, 'SEED_USERS', 'Users', '4', 'Pembuatan 4 akun demo (admin, manager, petugas, nonaktif)', SYSUTCDATETIME()),
    (@adm, 'SEED_LANDS', 'Lands', '5', 'Input master 5 lahan pertanian utama', SYSUTCDATETIME());

    PRINT 'Seed data AuditLogs berhasil dimasukkan.';
END
GO

PRINT 'Penyuntikan seluruh Seed Data AgroTrackDb selesai dengan sukses!';
GO
