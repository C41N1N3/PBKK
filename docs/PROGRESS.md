# Project Progress Tracker (PROGRESS.md)
## AgroTrack: Sistem Informasi Manajemen Produksi Pertanian Berbasis Desktop

Dokumen ini melacak status kemajuan pengerjaan tiap fase, build status, dan pemenuhan kriteria penyelesaian (*Definition of Done*).

> [!NOTE]
> Proyek dievaluasi secara skeptis: status penyelesaian backend/database dipisahkan secara tegas dari status verifikasi runtime visual GUI desktop.

---

## Ringkasan Status Fase

| Fase | Deskripsi | Status | Keterangan |
| :---: | :--- | :---: | :--- |
| **0** | Inspeksi & Perencanaan | **Completed** | Lingkungan Linux, .NET SDK, target .NET 8 WPF terverifikasi. Dokumen PRD, Desain DB, Arsitektur, Rencana Kerja selesai. |
| **1** | Fondasi Proyek | **Completed** | Solution `AgroTrack.sln`, proyek WPF & xUnit, MVVM core, navigation framework, converters, connection factory. |
| **2** | Basis Data & Skrip SQL | **Completed** | Skrip DDL & DML, perbaikan syntax `AgriculturalMaterials`, verifikasi eksekusi aktual pada SQL Server container, row count valid. |
| **3** | Autentikasi & Shell | **Completed** | PBKDF2 hashing, UserRepository, LoginView, MainWindow Shell dengan sidebar navigasi responsif role. |
| **4** | Master Data (Lahan & Komoditas) | **Completed** | CRUD Lahan & Komoditas, validasi luas/satuan, DataGrid, UI state (Loading, Empty, Error), pencarian, filter, pagination. |
| **5** | Manajemen Penanaman | **Completed** | CRUD Penanaman, validasi kapasitas lahan, detail musim tanam, kalkulasi target pencapaian terproteksi div-by-zero. |
| **6** | Aktivitas Lapangan | **Completed** | CRUD Aktivitas pertanian, filter penanaman & petugas, proteksi status penanaman selesai/dibatalkan. |
| **7** | Biaya & Panen | **Completed** | CRUD Biaya Operasional & Hasil Panen bertahap, kalkulasi pendapatan $Revenue = Yield \times Price$, kalkulasi laba rugi. |
| **8** | Dashboard & Laporan | **Completed** | Kartu statistik agregasi SQL, visual target vs aktual live database, laporan laba rugi per penanaman & laporan produksi. |
| **9** | Bahan & Stok (*Should*) | **Completed** | Manajemen bahan pertanian, transaksi pemakaian bahan & pengurangan/pengembalian stok atomik (ACID), indikator stok minimum. |
| **10**| Polish UI & Manajemen Pengguna | **Completed** | Modul Pengguna (Admin), harmonisasi tema Terang & Gelap, normalisasi resource key XAML, penghapusan duplicate key. |
| **11**| QA & Audit Skeptis | **Partially Completed** | 59 unit test lulus 100%, 7 live DB integration test lulus 100%, Build Debug & Release 0 error 0 warning. **Pengujian GUI runtime desktop WPF berstatus Not Executed di Linux dan menunggu verifikasi di Windows.** |

---

## Log Riwayat Pengerjaan

- **2026-10-19 11:02**: Inisialisasi inspeksi lingkungan. Terdeteksi OS Linux (x86_64), .NET SDK 10.0.113, Podman container engine.
- **2026-10-19 11:07**: Pengujian kompilasi WPF `net8.0-windows` di Linux dengan `<EnableWindowsTargeting>true</EnableWindowsTargeting>` berhasil sempurna (0 Warning, 0 Error).
- **2026-10-19 11:15**: Penyusunan dokumen perencanaan Fase 0 selesai (`DECISIONS.md`, `PRD_AGROTRACK.md`, `DATABASE_DESIGN.md`, `ARCHITECTURE.md`, `IMPLEMENTATION_PLAN.md`, `TEST_PLAN_AND_RESULTS.md`, `PROGRESS.md`).
- **2026-10-19 11:45**: Fondasi proyek Fase 1 selesai (MVVM framework, `ViewModelBase`, `RelayCommand`, `AsyncRelayCommand`, Navigation, Theme engine, App.xaml.cs Composition Root).
- **2026-10-19 12:10**: Skrip basis data SQL Server Fase 2 selesai (`001_CreateDatabase.sql`, `002_CreateTables.sql`, `003_CreateIndexes.sql`, `004_SeedData.sql`, `005_VerificationQueries.sql`, `ResetDevelopmentDatabase.sql`).
- **2026-10-19 12:35**: Implementasi autentikasi dan shell Fase 3 selesai (`UserRepository`, `PasswordHasher`, `AuthenticationService`, `SessionService`, `LoginViewModel`, `LoginView`, `MainWindow`).
- **2026-10-19 12:55**: Implementasi Master Data Lahan dan Komoditas Fase 4 selesai (`LandRepository`, `LandService`, `LandViewModel`, `LandView`, `CommodityRepository`, `CommodityService`, `CommodityViewModel`, `CommodityView`).
- **2026-10-19 13:15**: Implementasi Musim Tanam Fase 5 selesai (`PlantingRepository`, `PlantingService`, `PlantingViewModel`, `PlantingView`, unit tests `PlantingTests.cs`).
- **2026-10-19 13:25**: Implementasi Aktivitas Pertanian Fase 6 selesai (`ActivityRepository`, `ActivityService`, `ActivityViewModel`, `ActivityView`, unit tests `ActivityTests.cs`).
- **2026-10-19 13:35**: Implementasi Biaya Operasional dan Hasil Panen Fase 7 selesai (`CostRepository`, `CostService`, `CostViewModel`, `CostView`, `HarvestRepository`, `HarvestService`, `HarvestViewModel`, `HarvestView`, unit tests `FinancialAndHarvestTests.cs`).
- **2026-10-19 13:45**: Implementasi Dashboard dan Laporan Fase 8 selesai (`DashboardRepository`, `DashboardService`, `DashboardViewModel`, `DashboardView`, `ReportRepository`, `ReportService`, `ReportViewModel`, `ReportView`, unit tests `DashboardAndReportTests.cs`).
- **2026-10-19 13:50**: Implementasi Bahan Pertanian dan Stok Fase 9 selesai (`MaterialRepository`, `MaterialService`, `MaterialViewModel`, `MaterialView`, unit tests `MaterialStockTests.cs`).
- **2026-10-19 13:55**: Implementasi Manajemen Pengguna dan Polish UI Fase 10 selesai (`UserService`, `UserManagementViewModel`, `UserManagementView`, registrasi DI, penyempurnaan ResourceDictionaries).
- **2026-10-19 14:02**: QA awal diselesaikan.
- **2026-10-19 14:35**: **Audit Ulang Skeptis Dimulai**:
  1. Pemeriksaan Podman container `agrotrack-sqlserver` pada port 1433: aktif dan siap menerima koneksi.
  2. Eksekusi skrip SQL aktual: menemukan dan memperbaiki bug syntax kolom kategori `AgriculturalMaterials` pada `database/004_SeedData.sql`. Verifikasi data aktual dengan `005_VerificationQueries.sql` sukses.
  3. Penambahan suite pengujian integrasi database nyata `DatabaseIntegrationTests.cs` (7 test cases) mencakup otentikasi query SQL nyata, CRUD Lahan & Komoditas, transaksi pemotongan dan rollback stok atomik, serta query agregasi dashboard dan laporan. Total test lulus: 66.
  4. Sanitasi keamanan: menghapus fallback password hardcoded dari `DbConnectionFactory.cs`, menambahkan dukungan `appsettings.local.json` di luar Git, dan membersihkan kredensial plaintext.
  5. Audit statis XAML: memperbaiki resource keys yang hilang di `Styles.xaml` dan tema, serta menghapus duplikasi `x:Key` pada `PlantingView.xaml`.
  6. Penyusunan `docs/WINDOWS_RUNTIME_TEST_CHECKLIST.md` dan pemutakhiran seluruh dokumen dengan status jujur: GUI WPF berstatus *Not Executed* di Linux dan membutuhkan verifikasi di Windows.
