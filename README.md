# AgroTrack: Sistem Informasi Manajemen Produksi Pertanian Berbasis Desktop

[![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen.svg)]()
[![Target Framework](https://img.shields.io/badge/.NET-8.0%20LTS-blue.svg)]()
[![Architecture](https://img.shields.io/badge/Architecture-MVVM%20Pure-orange.svg)]()
[![Database](https://img.shields.io/badge/Database-SQL%20Server%20(ADO.NET)-red.svg)]()
[![Unit Tests](https://img.shields.io/badge/Unit%20Tests-59%20Passed-success.svg)]()
[![DB Integration](https://img.shields.io/badge/DB%20Integration-7%20Passed-success.svg)]()
[![GUI Runtime](https://img.shields.io/badge/GUI%20Runtime-Pending%20Windows-yellow.svg)]()

> **Tugas Evaluasi Tengah Semester (ETS) - Mata Kuliah Pemrograman Berbasis Kerangka Kerja (PBKK)**

---

## 1. Deskripsi Aplikasi

**AgroTrack** adalah aplikasi desktop modern berbasis C# dan Windows Presentation Foundation (WPF) yang dirancang khusus untuk memfasilitasi pengelola pertanian dalam merencanakan, mencatat, dan memantau seluruh siklus produksi agrikultur secara efisien, akurat, dan terpadu.

Aplikasi ini mencakup pencatatan lahan pertanian, komoditas, musim tanam, aktivitas lapangan harian, inventaris bahan pertanian (pupuk/benih/pestisida) dengan pemotongan dan pengembalian stok atomik (ACID), biaya operasional, dan pemanenan bertahap. Seluruh statistik, visual perbandingan target vs aktual, serta laporan laba rugi dihitung dan diagregasikan secara dinamis langsung dari basis data SQL Server murni menggunakan ADO.NET (`Microsoft.Data.SqlClient`) tanpa Entity Framework.

> [!NOTE]
> **Catatan Status Audit Lingkungan Pengembangan**:
> Logika bisnis, keamanan PBKDF2, ADO.NET repository, dan integrasi SQL Server telah tervalidasi 100% pada container SQL Server nyata (66 test cases lolos). Namun, rendering antarmuka pengguna WPF desktop berstatus **Pending / Not Executed** di lingkungan Linux dan membutuhkan verifikasi langsung pada sistem operasi Windows sesuai panduan di [docs/WINDOWS_RUNTIME_TEST_CHECKLIST.md](file:///home/seannd/Code/PBKK/ETS/docs/WINDOWS_RUNTIME_TEST_CHECKLIST.md).

---

## 2. Fitur Utama

### 2.1 Fitur Wajib (*Must Features*)
- **Autentikasi & Otorisasi Pengguna**: Login aman dengan enkripsi salt PBKDF2, penanganan akun nonaktif, proteksi sesi kerja, dan pembatasan hak akses berbasis peranan (*Administrator*, *Manager*, *FieldOfficer*).
- **Dasbor Eksekutif Real-Time**: Kartu ringkasan metrik lahan aktif, penanaman berjalan, pekerjaan hari ini, peringatan stok menipis, total hasil panen, akumulasi biaya operasional, pendapatan kotor, dan laba/rugi bersih live dari database.
- **Visualisasi Dinamis**: Visual batang perbandingan target produksi terhadap capaian aktual panen secara proporsional.
- **Manajemen Master Data Lahan & Komoditas**: Pencatatan spesifikasi lahan (luas hektar, tipe tanah, status) dan komoditas (satuan panen, estimasi umur tanam).
- **Manajemen Siklus Penanaman (Musim Tanam)**: Siklus hidup terstruktur (*Direncanakan*, *Aktif*, *Siap Panen*, *Selesai*, *Dibatalkan*) dengan validasi ketat kapasitas lahan dan irisan periode tanam.
- **Aktivitas Pertanian**: Pendokumentasian kegiatan teknis lapangan (persiapan, penyiraman, pemupukan, dsb.) terhubung ke petugas lapangan.
- **Biaya Operasional**: Pencatatan seluruh pos pengeluaran terperinci dengan format moneter standar Rupiah (`Rp X.XXX.XXX,00`).
- **Hasil Panen & Kalkulasi Finansial**: Dukungan pemanenan bertahap (*batch harvesting*), perhitungan otomatis pendapatan ($Revenue = Yield \times Price$), serta laporan laba/rugi per musim tanam.
- **Pencarian, Filter, Sorting, & Pagination**: Eksplorasi data yang responsif, case-insensitive, dan terhindar dari pemborosan memori.
- **Rich UI & Dual Theme System**: Dukungan Mode Terang (*Light Mode*) dan Mode Gelap (*Dark Mode*) dengan konsistensi tata letak, *loading state*, *empty state*, *error state*, dan dialog konfirmasi.

### 2.2 Fitur Lanjutan (*Should Features*)
- **Manajemen Bahan & Stok Inventaris**: Pencatatan master benih, pupuk, pestisida, dan peralatan habis pakai.
- **Transaksi Stok Atomik (ACID)**: Pengurangan stok otomatis saat pemakaian dicatat dan pengembalian stok jika pemakaian dibatalkan menggunakan SQL Transaction (`IsolationLevel.RepeatableRead` + `UPDLOCK`).
- **Peringatan Stok Minimum**: Visualisasi badge peringatan jika stok berada di bawah ambang batas aman.
- **Manajemen Pengguna Lengkap**: Administrator dapat mendaftarkan akun baru, mengubah hak akses, mereset password, dan menonaktifkan pengguna tanpa risiko *self-lockout*.

---

## 3. Arsitektur dan Teknologi

- **Bahasa Pemrograman**: C# 12 / .NET 8 LTS
- **Teknologi UI**: Windows Presentation Foundation (WPF), XAML, Dynamic Resource Dictionaries
- **Pola Arsitektur**: Model-View-ViewModel (MVVM) murni
  - `ViewModelBase` (`INotifyPropertyChanged`)
  - `RelayCommand` dan `AsyncRelayCommand` (`ICommand`)
  - `ObservableCollection<T>`
- **Akses Data**: ADO.NET murni via `Microsoft.Data.SqlClient` (100% Parameterized Query, transaksi atomik, tanpa Entity Framework)
- **Dependency Injection**: `Microsoft.Extensions.DependencyInjection` (Composition Root di `App.xaml.cs`)
- **Keamanan**: `Rfc2898DeriveBytes` (PBKDF2 dengan salt kriptografis unik 16-byte, 100.000 iterasi SHA256)
- **Pengujian**:
  - 59 Automated Unit Tests (xUnit + In-memory Fakes)
  - 7 Live SQL Server Database Integration Tests (`DatabaseIntegrationTests.cs`)

---

## 4. Struktur Proyek

```
AgroTrack/
├── AgroTrack.sln                 # Visual Studio Solution File
├── AgroTrack.slnx                # Modern XML Solution File
├── src/
│   └── AgroTrack/                # Proyek Utama WPF Client (.NET 8 LTS)
│       ├── App.xaml / App.xaml.cs
│       ├── MainWindow.xaml / MainWindow.xaml.cs
│       ├── appsettings.json      # Konfigurasi koneksi database (Sanitized)
│       ├── appsettings.example.json
│       ├── Models/               # Entitas domain bisnis
│       ├── DTOs/                 # Data transfer objects untuk analitik & laporan
│       ├── Enums/                # Enumerasi status, role, kategori
│       ├── Views/                # 11 UserControl XAML antarmuka pengguna
│       ├── ViewModels/           # State, commands, dan validasi UI
│       ├── Repositories/         # Lapisan akses data ADO.NET (Interfaces & Implementations)
│       ├── Services/             # Lapisan logika bisnis & transaksi (Interfaces & Implementations)
│       ├── Security/             # Hashing PBKDF2 & representasi CurrentUser
│       ├── Commands/             # RelayCommand & AsyncRelayCommand
│       ├── Converters/           # ValueConverters untuk binding XAML
│       ├── Themes/               # LightTheme.xaml & DarkTheme.xaml
│       ├── Resources/            # Styles.xaml (design system & kontrol)
│       └── Utilities/            # Result<T> pattern & PagedResult<T>
├── tests/
│   └── AgroTrack.Tests/          # Proyek Pengujian Unit & Integrasi Basis Data
│       ├── AuthenticationTests.cs
│       ├── PlantingTests.cs
│       ├── ActivityTests.cs
│       ├── FinancialAndHarvestTests.cs
│       ├── DashboardAndReportTests.cs
│       ├── MaterialStockTests.cs
│       ├── UserManagementTests.cs
│       └── DatabaseIntegrationTests.cs # 7 pengujian koneksi live SQL Server
├── database/                     # Skrip SQL Server terurut
│   ├── 001_CreateDatabase.sql
│   ├── 002_CreateTables.sql
│   ├── 003_CreateIndexes.sql
│   ├── 004_SeedData.sql
│   ├── 005_VerificationQueries.sql
│   └── ResetDevelopmentDatabase.sql
└── docs/                         # Dokumentasi Lengkap Proyek
    ├── PRD_AGROTRACK.md
    ├── DATABASE_DESIGN.md
    ├── ARCHITECTURE.md
    ├── INSTALLATION_GUIDE.md
    ├── USER_GUIDE.md
    ├── TEST_PLAN_AND_RESULTS.md
    ├── FINAL_AUDIT.md
    └── WINDOWS_RUNTIME_TEST_CHECKLIST.md # Checklist pengujian runtime Windows
```

---

## 5. Konfigurasi Basis Data

Konfigurasi koneksi berada di `src/AgroTrack/appsettings.json` atau file lokal `appsettings.local.json` (diabaikan oleh Git untuk keamanan kredensial):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=AgroTrackDb;User Id=sa;Password=<YOUR_PASSWORD>;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  }
}
```

---

## 6. Kompilasi dan Menjalankan Pengujian

### 6.1 Kompilasi (Build)
```bash
# Debug Build
dotnet build AgroTrack.sln -c Debug

# Release Build
dotnet build AgroTrack.sln -c Release
```

### 6.2 Menjalankan Automated Tests (66 Test Cases)
```bash
# Menjalankan 59 unit test + 7 live database integration test
dotnet test AgroTrack.sln -c Release
```

### 6.3 Menjalankan Aplikasi WPF (Di Windows)
```powershell
dotnet run --project src\AgroTrack\AgroTrack.csproj -c Release
```
Atau buka `AgroTrack.sln` di Visual Studio 2022 pada Windows dan tekan **F5**.

---

## 7. Akun Demo Bawaan (Hasil Seeding Database)

Tersedia akun bawaan dari skrip seeding `database/004_SeedData.sql`:

| Peranan | Username | Password | Ruang Lingkup Akses |
|---|---|---|---|
| **Administrator** | `admin` | `AdminPassword@123` | Seluruh modul, transaksi, laporan, dan manajemen pengguna |
| **Manager** | `manager` | `ManagerPassword@123` | Dashboard, master data, musim tanam, biaya, panen, dan laporan |
| **Petugas Lapangan** | `petugas` | `PetugasPassword@123` | Operasional lapangan: pencatatan aktivitas, penggunaan bahan, panen |
| **Akun Nonaktif** | `nonaktif` | `UserPassword@123` | Akun dinonaktifkan (untuk pengujian penolakan login) |

---

## 8. Rujukan Pengujian Runtime Windows

Sebelum aplikasi dipresentasikan atau digunakan secara produksi, lakukan pengujian antarmuka pengguna pada sistem operasi Windows sesuai panduan di:
👉 [docs/WINDOWS_RUNTIME_TEST_CHECKLIST.md](file:///home/seannd/Code/PBKK/ETS/docs/WINDOWS_RUNTIME_TEST_CHECKLIST.md)
