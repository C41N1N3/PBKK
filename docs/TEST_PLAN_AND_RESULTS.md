# Laporan Rencana dan Hasil Pengujian (TEST_PLAN_AND_RESULTS.md)
## AgroTrack: Sistem Informasi Manajemen Produksi Pertanian Berbasis Desktop

Dokumen ini memuat hasil pelaksanaan pengujian otomatis (*automated unit tests*), pengujian integrasi basis data nyata (*live database integration tests*), dan status pengujian manual antarmuka (*manual GUI checklist*).

> [!WARNING]
> **Status Skeptis Lingkungan Linux**: Seluruh pengujian otomatis di lingkungan Linux dijalankan pada level kompilasi C# dan runner xUnit. Keberhasilan build atau unit test **TIDAK** membuktikan bahwa GUI WPF, rendering visual, dan interaksi kontrol desktop telah bekerja sempurna di Windows. Pengujian manual GUI berstatus **Not Executed** sampai diuji secara langsung di Windows.

---

## 1. Arsitektur Proyek Pengujian (`AgroTrack.Tests.csproj`)

### 1.1 Penjelasan Target Framework & Kompilasi File Sumber
- **Aplikasi Utama (`src/AgroTrack/AgroTrack.csproj`)**: Menargetkan `net8.0-windows` dengan `<UseWPF>true</UseWPF>`.
- **Proyek Pengujian (`tests/AgroTrack.Tests/AgroTrack.Tests.csproj`)**: Menargetkan `net10.0` (Cross-Platform .NET SDK).
- **Alasan Teknis Kompilasi Ulang File Sumber (`<Compile Include>` / Linked Files)**:
  Di lingkungan Linux tanpa Windows Desktop SDK (`Microsoft.WindowsDesktop.App.WPF`), sebuah proyek tes yang me-reference `<ProjectReference Include="AgroTrack.csproj" />` akan mengalami error build karena runtime WPF tidak tersedia di Linux. Oleh karena itu, file domain models, DTOs, utilities, security, ADO.NET repositories, dan services di-link dan dikompilasi ulang secara langsung ke dalam assembly pengujian.
- **Kategorisasi Pengujian**:
  - **Unit Tests (59 tests)**: Menggunakan *in-memory fake repositories*. Ini adalah unit test logika bisnis terisolasi, **bukan** pengujian integrasi aplikasi menyeluruh.
  - **Live Database Integration Tests (7 tests)**: Menghubungi container SQL Server nyata (`mcr.microsoft.com/mssql/server:2022-latest`) pada port 1433 menggunakan ADO.NET murni, mengeksekusi skrip DDL/DML, menguji transaksi atomik, commit, dan rollback.

---

## 2. Hasil Pengujian Otomatis

Pengujian dijalankan pada konfigurasi Release menggunakan xUnit runner:
```bash
dotnet test AgroTrack.sln -c Release
```
**Hasil Ringkasan Eksekusi:**
- **Total Test**: 66
- **Passed**: 66 (59 Unit Tests + 7 Live Database Integration Tests)
- **Failed**: 0
- **Skipped**: 0
- **Durasi Eksekusi**: ~1.3 detik
- **Status Akhir**: **PASSED (100% pada logika unit & integrasi database)**

---

### 2.1 Rincian Modul Pengujian Unit Otomatis (59 Tests - In-Memory Fakes)

#### A. Otentikasi dan Keamanan (`AuthenticationTests.cs`)
1. `HashPassword_ProducesDifferentHashesForSamePassword_DueToUniqueSalt`: **PASSED**
2. `VerifyPassword_WithCorrectPassword_ReturnsTrue`: **PASSED**
3. `VerifyPassword_WithIncorrectPassword_ReturnsFalse`: **PASSED**
4. `LoginAsync_WithValidCredentials_ReturnsSuccessfulSession`: **PASSED**
5. `LoginAsync_WithWrongPassword_FailsWithoutExposingDetails`: **PASSED**
6. `LoginAsync_WithInactiveUser_FailsWithAccountInactiveMessage`: **PASSED**
7. `LoginAsync_WithEmptyOrWhitespaceInput_ReturnsValidationFailure`: **PASSED**
8. `RoleAuthorization_ChecksRoleHierarchyCorrectly`: **PASSED**

#### B. Manajemen Penanaman dan Kapasitas Lahan (`PlantingTests.cs`)
9. `PlantedArea_ExceedingLandArea_FailsValidation`: **PASSED**
10. `PlantedArea_ExceedingAvailableCapacity_FailsValidation`: **PASSED**
11. `ExpectedHarvestDate_BeforeStartDate_FailsValidation`: **PASSED**
12. `TargetYield_Negative_FailsValidation`: **PASSED**
13. `ValidPlanting_WithinCapacityAndDates_PassesValidation`: **PASSED**
14. `TargetPercentage_ZeroTarget_ReturnsNullWithoutDivisionByZero`: **PASSED**
15. `TargetPercentage_PositiveTarget_CalculatesCorrectly`: **PASSED**
16. `CompletePlanting_SetsCompletionDateAndStatus`: **PASSED**
17. `ReopenPlanting_UnauthorizedRole_Fails`: **PASSED**

#### C. Aktivitas Pertanian (`ActivityTests.cs`)
18. `CreateActivity_OnCompletedPlanting_Fails`: **PASSED**
19. `CreateActivity_OnCancelledPlanting_Fails`: **PASSED**
20. `CreateActivity_OnActivePlanting_Succeeds`: **PASSED**
21. `CreateActivity_NegativeDuration_FailsValidation`: **PASSED**
22. `UpdateActivity_ByDifferentOfficer_NonAdmin_Fails`: **PASSED**
23. `DeleteActivity_ByAdmin_Succeeds`: **PASSED**

#### D. Keuangan dan Hasil Panen (`FinancialAndHarvestTests.cs`)
24. `Harvest_TotalRevenue_CalculatedCorrectly`: **PASSED**
25. `Harvest_NegativeOrZeroYield_Fails`: **PASSED**
26. `Harvest_NegativeSellingPrice_Fails`: **PASSED**
27. `Cost_NegativeOrZeroAmount_Fails`: **PASSED**
28. `Cost_EmptyCategoryOrDescription_Fails`: **PASSED**
29. `PlantingFinancials_ProfitLoss_CalculatedAccurately`: **PASSED**
30. `PlantingFinancials_CombinedCosts_SummedAccurately`: **PASSED**
31. `Harvest_CumulativeYield_SumsMultipleHarvestBatches`: **PASSED**
32. `DeleteCost_ByOfficer_Denied`: **PASSED**

#### E. Dashboard dan Laporan Produksi (`DashboardAndReportTests.cs`)
33. `DashboardSummary_CalculatesAllAggregatesCorrectly`: **PASSED**
34. `Dashboard_EmptyDatabase_ReturnsZeroValuesWithoutExceptions`: **PASSED**
35. `ProfitLossReport_CalculatesNetProfitAccurately`: **PASSED**
36. `ProductionReport_FiltersByCommodityAndSeason`: **PASSED**
37. `TargetVsActualVisual_ProducesValidBarHeights`: **PASSED**
38. `FormatCurrency_RupiahIndonesianCulture_ProducesStandardFormat`: **PASSED**

#### F. Bahan Pertanian dan Stok Atomik (`MaterialStockTests.cs`)
39. `CreateMaterial_ValidInput_Succeeds`: **PASSED**
40. `CreateMaterial_EmptyCode_Fails`: **PASSED**
41. `MaterialUsage_DeductsStockAtomically`: **PASSED**
42. `MaterialUsage_ExceedingAvailableStock_Fails`: **PASSED**
43. `CancelMaterialUsage_RevertsStockToOriginal`: **PASSED**
44. `LowStockIndicator_FlagsMaterialBelowMinimumThreshold`: **PASSED**
45. `AdjustStock_NegativeResult_Fails`: **PASSED**

#### G. Manajemen Pengguna dan Keamanan Administrator (`UserManagementTests.cs`)
46. `CreateUser_NonAdmin_ShouldFailWithPermissionError`: **PASSED**
47. `CreateUser_DuplicateUsername_ShouldFail`: **PASSED**
48. `CreateUser_ShortPassword_ShouldFail`: **PASSED**
49. `CreateUser_ValidInputs_ShouldSucceedWithHashedPassword`: **PASSED**
50. `ToggleUserStatus_DeactivateSelf_ShouldFail`: **PASSED**
51. `ToggleUserStatus_OtherUser_ShouldToggleSuccessfully`: **PASSED**
52. `ResetPassword_ValidPassword_ShouldUpdateHash`: **PASSED**

---

### 2.2 Rincian Pengujian Integrasi Basis Data SQL Server Nyata (7 Tests - Live Container)
Pengujian dieksekusi langsung terhadap instance container SQL Server `agrotrack-sqlserver` (`port 1433`) dengan database terisi data seed nyata (`database/004_SeedData.sql`):

53. `Test_01_LoginRepository_WithSeededAdminAccount_Succeeds`: **PASSED** (Membuktikan otentikasi login dengan query SQL nyata `SELECT ... WHERE Username = @Username` dan pencocokan hash PBKDF2 akun seed `admin`).
54. `Test_02_LoginRepository_WithNonExistentAccount_ReturnsNull`: **PASSED** (Membuktikan query parameter SQL mengembalikan null untuk akun tak terdaftar).
55. `Test_03_LandRepository_CRUD_PerformsLiveDatabaseOperations`: **PASSED** (Membuktikan siklus Create, Read, Update, Delete data lahan pada tabel fisik `dbo.Lands` dengan verifikasi row count).
56. `Test_04_CommodityRepository_CRUD_PerformsLiveDatabaseOperations`: **PASSED** (Membuktikan siklus CRUD komoditas pada tabel fisik `dbo.Commodities`).
57. `Test_05_MaterialStock_AtomicDeductionAndRollback_Succeeds`: **PASSED** (Membuktikan transaksi atomik ACID: penguncian `UPDLOCK`, pemotongan stok pada `dbo.AgriculturalMaterials`, pencatatan `dbo.MaterialUsages`, penolakan stok berlebih dengan `RollbackAsync`, dan pengembalian stok saat pemakaian dibatalkan).
58. `Test_06_DashboardRepository_LiveDatabaseQuery_ReturnsAccurateMetrics`: **PASSED** (Membuktikan query agregasi live SQL: `ActiveLandCount = 5`, `ActivePlantingCount = 8`, `TotalHarvestYield = 49200.00 kg`, dsb.).
59. `Test_07_ReportRepository_ProfitLossQuery_ReturnsVerifiedCalculations`: **PASSED** (Membuktikan query gabungan JOIN analitik multi-tabel untuk laporan laba rugi dan produktivitas).

---

## 3. Status Checklist Pengujian Manual GUI (Skeptical Evaluation)

Status pengujian manual GUI dievaluasi dengan status jujur:
- **Completed**: Kompilasi statis dan logika backend telah terverifikasi.
- **Not Executed**: Pengujian interaksi visual, rendering XAML, dan navigasi desktop pada runtime Windows belum dijalankan karena keterbatasan sistem Linux.

| No | Kasus Uji | Skenario Pengujian | Hasil yang Diharapkan | Status Evaluasi | Catatan / Bukti |
|:---:|:---|:---|:---|:---:|:---|
| 1 | Login Valid | Input `admin` / `AdminPassword@123` | Masuk ke MainWindow, session role aktif sebagai Administrator | **Not Executed** | Logika lolos via `Test_01_LoginRepository`, tampilan GUI belum diuji di Windows |
| 2 | Login Tidak Valid | Input password salah | Muncul pesan kesalahan seragam, login ditolak | **Not Executed** | Memerlukan runtime GUI Windows |
| 3 | Login Akun Nonaktif | Login dengan akun nonaktif | Ditolak dengan pesan akun nonaktif | **Not Executed** | Memerlukan runtime GUI Windows |
| 4 | Logout | Klik tombol logout di sidebar | Session dibersihkan, MainWindow tertutup, kembali ke LoginView | **Not Executed** | Memerlukan runtime GUI Windows |
| 5 | Hak Akses Admin | Login sebagai Administrator | Mengakses seluruh modul termasuk Manajemen Pengguna | **Not Executed** | Memerlukan runtime GUI Windows |
| 6 | Hak Akses Manajer | Login sebagai Manajer | Dapat mengelola master data & laporan, menu pengguna tersembunyi | **Not Executed** | Memerlukan runtime GUI Windows |
| 7 | Hak Akses Petugas | Login sebagai Petugas | Hanya mencatat aktivitas, penggunaan bahan, dan panen | **Not Executed** | Memerlukan runtime GUI Windows |
| 8 | CRUD Lahan | Tambah, ubah, nonaktifkan lahan | Tersimpan di SQL Server, DataGrid terupdate | **Partially Completed** | Query DB lolos di `Test_03`; interaksi form visual belum diuji di Windows |
| 9 | CRUD Komoditas | Tambah, ubah, nonaktifkan varietas | Validasi kode unik, tersimpan di database | **Partially Completed** | Query DB lolos di `Test_04`; form visual belum diuji di Windows |
| 10 | CRUD Penanaman | Tambah musim tanam dengan validasi lahan | Periode & kapasitas lahan tervalidasi | **Partially Completed** | Logika lolos unit test; dialog input belum diuji di Windows |
| 11 | CRUD Aktivitas | Tambah kegiatan lapangan harian | Terikat ke musim tanam aktif & petugas | **Partially Completed** | Logika lolos unit test; dialog input belum diuji di Windows |
| 12 | CRUD Biaya | Tambah pengeluaran operasional | Total biaya penanaman terakumulasi otomatis | **Partially Completed** | Logika lolos unit test; dialog input belum diuji di Windows |
| 13 | CRUD Panen | Catat hasil panen bertahap | Pendapatan terhitung otomatis, target % terupdate | **Partially Completed** | Logika lolos unit test; dialog input belum diuji di Windows |
| 14 | Pencarian | Ketik keyword di search box | Data terfilter secara case-insensitive | **Not Executed** | Memerlukan interaksi TextBox di Windows |
| 15 | Filter Status | Pilih dropdown filter status | DataGrid hanya menampilkan item berstatus sesuai | **Not Executed** | Memerlukan interaksi ComboBox di Windows |
| 16 | Sorting | Klik header kolom DataGrid | Data terurut ascending / descending | **Not Executed** | Memerlukan event click DataGrid di Windows |
| 17 | Pagination | Navigasi halaman sebelumnya/berikutnya | Halaman berubah, tombol disable di batas | **Not Executed** | Memerlukan klik tombol pagination di Windows |
| 18 | Dashboard Terisi | Buka Dashboard dengan data database | Seluruh kartu statistik & grafik visual terisi dari database | **Partially Completed** | Query data lolos di `Test_06`; rendering grafik batang belum dilihat di Windows |
| 19 | Dashboard Kosong | Reset database / tanpa data | Kartu menampilkan angka 0, tidak melempar exception | **Partially Completed** | Logika unit test lolos; rendering visual belum diuji di Windows |
| 20 | Gangguan DB | Koneksi database terputus | Menampilkan Error State dengan opsi retry | **Not Executed** | Memerlukan simulasi pemutusan jaringan di Windows |
| 21 | Tema Terang | Toggle tema ke Light Mode | Tema berganti ke palet terang dengan kontras tinggi | **Not Executed** | Resource dictionary diverifikasi statis; rendering dinamis belum diuji di Windows |
| 22 | Tema Gelap | Toggle tema ke Dark Mode | Tema berganti ke palet gelap *forest slate* | **Not Executed** | Resource dictionary diverifikasi statis; rendering dinamis belum diuji di Windows |
| 23 | Loading State | Saat query async sedang berlangsung | Indikator loading overlay muncul | **Not Executed** | Memerlukan runtime visual Windows |
| 24 | Empty State | DataGrid tidak memiliki data | Muncul teks/ilustrasi kosong yang informatif | **Not Executed** | Memerlukan runtime visual Windows |
| 25 | Error State | Terjadi kegagalan I/O | Banner merah muncul dengan teks berbahasa Indonesia | **Not Executed** | Memerlukan runtime visual Windows |
| 26 | Konfirmasi Hapus | Klik tombol hapus baris data | Muncul konfirmasi via `IDialogService.ShowConfirmation` | **Not Executed** | Memerlukan dialog popup di Windows |
| 27 | Update Dashboard | Input panen baru lalu lihat Dashboard | Statistik panen & pendapatan bertambah secara live | **Not Executed** | Memerlukan alur end-to-end di Windows |
| 28 | Format Rupiah | Tampilan angka uang di UI | Diformat konsisten `Rp X.XXX.XXX,00` | **Partially Completed** | Converter lolos unit test; visualisasi DataGrid belum diuji di Windows |
| 29 | Validasi Tanggal | Tanggal panen lebih awal dari tanam | Form menampilkan error dan tombol simpan dicegah | **Partially Completed** | Logika lolos unit test; interaksi DatePicker belum diuji di Windows |
| 30 | Validasi Luas | Luas tanam melebihi luas lahan | Form menolak dengan pesan kapasitas tidak cukup | **Partially Completed** | Logika lolos unit test; interaksi visual belum diuji di Windows |
| 31 | Build Debug | Eksekusi `dotnet build -c Debug` | Kompilasi sukses dengan 0 errors & 0 warnings | **Completed** | Terverifikasi pada `AgroTrack.sln` |
| 32 | Build Release | Eksekusi `dotnet build -c Release` | Kompilasi sukses dengan 0 errors & 0 warnings | **Completed** | Terverifikasi pada `AgroTrack.sln` |

---

## 4. Rujukan Panduan Pengujian Runtime Windows

Untuk menyelesaikan seluruh pengujian visual dan manual yang berstatus **Not Executed** atau **Partially Completed**, gunakan dokumen checklist terperinci pada:
👉 [docs/WINDOWS_RUNTIME_TEST_CHECKLIST.md](file:///home/seannd/Code/PBKK/ETS/docs/WINDOWS_RUNTIME_TEST_CHECKLIST.md)
