# Panduan Instalasi dan Konfigurasi AgroTrack

Dokumen ini memandu pengembang, penguji, dan dosen pengampu dalam melakukan instalasi, konfigurasi basis data, kompilasi proyek, dan menjalankan aplikasi **AgroTrack: Sistem Informasi Manajemen Produksi Pertanian Berbasis Desktop**.

---

## 1. Prasyarat Sistem

Sebelum menjalankan aplikasi, pastikan sistem memenuhi persyaratan berikut:

### 1.1 Perangkat Keras Minimum
- **Prosesor**: Dual-Core 2.0 GHz atau lebih tinggi (disarankan Quad-Core 2.5 GHz+).
- **RAM**: Minimal 4 GB (disarankan 8 GB atau lebih).
- **Ruang Penyimpanan Kosong**: Minimal 2 GB.
- **Resolusi Layar**: Minimal 1280 x 720 piksel (disarankan 1920 x 1080 piksel).

### 1.2 Perangkat Lunak
- **Sistem Operasi**: Windows 10/11 (untuk menjalankan aplikasi WPF) atau Linux/macOS (untuk pengembangan, kompilasi, verifikasi arsitektur, dan eksekusi unit test cross-platform).
- **SDK**: .NET SDK 8.0 LTS (`net8.0-windows` untuk WPF, `net10.0` / `net8.0` untuk test runner).
- **Basis Data**: Microsoft SQL Server (SQL Server 2019/2022, SQL Server Express, SQL Server LocalDB, atau SQL Server di Docker Container).

---

## 2. Setup Basis Data (SQL Server)

Basis data AgroTrack dapat di-setup baik menggunakan SQL Server lokal maupun container Docker.

### 2.1 Menjalankan SQL Server via Docker (Opsional / Alternatif Cepat)
Jika menggunakan Docker di Windows/Linux:
```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<YOUR_STRONG_PASSWORD>" \
   -p 1433:1433 --name agrotrack-sql -d \
   mcr.microsoft.com/mssql/server:2022-latest
```

### 2.2 Eksekusi Skrip SQL Berurutan
Masuk ke SQL Server Management Studio (SSMS), Azure Data Studio, atau CLI `sqlcmd`, lalu jalankan skrip di folder `database/` secara berurutan:

1. **`database/001_CreateDatabase.sql`**: Membuat database `AgroTrackDb` dengan collation `Latin1_General_CI_AS`.
2. **`database/002_CreateTables.sql`**: Membuat seluruh tabel relasional (`Users`, `Lands`, `Commodities`, `Plantings`, `FarmingActivities`, `AgriculturalMaterials`, `MaterialUsages`, `OperationalCosts`, `Harvests`, `AuditLogs`, `UserPreferences`) lengkap dengan Primary Key, Foreign Key, Check Constraints, dan Defaults.
3. **`database/003_CreateIndexes.sql`**: Membuat indeks performa pada kolom pencarian, filter, dan foreign key.
4. **`database/004_SeedData.sql`**: Memasukkan data awal realistis berbahasa Indonesia lengkap dengan password ter-hash PBKDF2:
   - 3 Akun Pengguna (`admin`, `manajer`, `petugas`).
   - 5 Lahan pertanian (`LH-UTR-01`, `LH-SLT-01`, `LH-TMR-01`, `LH-PRC-01`, `LH-GH-01`).
   - 5 Komoditas (`PDI-IR64`, `JGN-BSI`, `KDL-WLS`, `CBI-KRW`, `TMT-RMA`).
   - 8 Musim tanam / Penanaman dengan berbagai status (`Direncanakan`, `Aktif`, `Siap Panen`, `Selesai`, `Dibatalkan`).
   - 20 Aktivitas pertanian lapangan.
   - 10 Master bahan pertanian & stok.
   - 15 Penggunaan bahan pertanian (transaksional).
   - 15 Biaya operasional terperinci.
   - 10 Transaksi hasil panen bertahap.
5. **`database/005_VerificationQueries.sql`**: Menjalankan query verifikasi integritas data dan ringkasan metrik.

> [!TIP]
> Jika sewaktu-waktu ingin mengembalikan basis data development ke kondisi awal yang bersih, jalankan skrip `database/ResetDevelopmentDatabase.sql`.

---

## 3. Konfigurasi Connection String

Buka file konfigurasi di `src/AgroTrack/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=AgroTrackDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

Sesuaikan parameter `Server`, `User Id`, dan `Password` jika menggunakan instance SQL Server lokal yang berbeda (misalnya `Server=(localdb)\\mssqllocaldb;Database=AgroTrackDb;Integrated Security=True;TrustServerCertificate=True;`).

Contoh template yang aman tanpa kredensial disediakan di `src/AgroTrack/appsettings.example.json`.

---

## 4. Kompilasi Proyek (Build)

### 4.1 Restore Dependencies
Jalankan di root repositori:
```bash
dotnet restore AgroTrack.sln
```

### 4.2 Build Konfigurasi Debug
```bash
dotnet build AgroTrack.sln -c Debug
```
*Hasil:* Seluruh proyek `AgroTrack` dan `AgroTrack.Tests` terkompilasi dengan 0 errors dan 0 warnings.

### 4.3 Build Konfigurasi Release
```bash
dotnet build AgroTrack.sln -c Release
```
*Hasil:* Biner produksi terkompilasi di `src/AgroTrack/bin/Release/net8.0-windows/`.

---

## 5. Menjalankan Unit Test

Jalankan perintah berikut untuk mengeksekusi 59 unit test komprehensif:
```bash
dotnet test AgroTrack.sln
```
Atau secara spesifik pada proyek test:
```bash
dotnet test tests/AgroTrack.Tests/AgroTrack.Tests.csproj
```

Pengujian mencakup:
- Aturan hashing dan verifikasi kata sandi PBKDF2 (salt unik).
- Autentikasi dan penolakan akun nonaktif.
- Aturan kapasitas luas tanam lahan.
- Validasi rentang tanggal penanaman dan panen.
- Aturan penyelesaian musim tanam dan status proteksi aktivitas.
- Perhitungan pendapatan panen ($Revenue = Yield \times Price$).
- Perhitungan total biaya operasional dan total biaya bahan.
- Perhitungan laba rugi bersih ($Profit = Revenue - Cost$).
- Persentase target panen dengan proteksi pembagian dengan nol.
- Pengurangan dan pengembalian stok bahan pertanian atomik (ACID).
- Peringatan stok di bawah batas minimum.
- Manajemen pengguna dan otorisasi hak akses Administrator.

---

## 6. Menjalankan Aplikasi

Pada lingkungan Windows dengan .NET 8 Desktop Runtime:
```bash
dotnet run --project src/AgroTrack/AgroTrack.csproj
```
Atau buka file `AgroTrack.sln` di Visual Studio 2022 (v17.8+), pilih konfigurasi `Debug` atau `Release`, lalu tekan `F5`.

---

## 7. Akun Demo Bawaan (Development Only)

Kredensial berikut cocok dengan akun seed pada `database/004_SeedData.sql`:

| Peranan (Role) | Username | Password | Deskripsi Hak Akses |
|---|---|---|---|
| **Administrator** | `admin` | `AdminPassword@123` | Akses penuh ke seluruh modul, pengaturan pengguna, dan semua transaksi. |
| **Manager** | `manager` | `ManagerPassword@123` | Akses dashboard, master data, penanaman, biaya, hasil panen, dan laporan. |
| **Petugas Lapangan** | `petugas` | `PetugasPassword@123` | Akses operasional lapangan: pencatatan aktivitas, penggunaan bahan, dan panen. |
| **Akun nonaktif (uji)** | `nonaktif` | `UserPassword@123` | Untuk memverifikasi penolakan login akun nonaktif. |

> [!WARNING]
> Kredensial di atas hanya ditujukan untuk keperluan demonstrasi dan pengujian tugas kuliah (development). Jangan gunakan password default ini pada lingkungan produksi.
