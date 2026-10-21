# Architectural and Environment Decisions (DECISIONS.md)

Dokumen ini mencatat keputusan arsitektural, teknis, dan lingkungan pengembangan untuk proyek **AgroTrack: Sistem Informasi Manajemen Produksi Pertanian Berbasis Desktop**.

---

## 1. Lingkungan Pengembangan & SDK Target

* **Sistem Operasi**: Linux (x86_64, CachyOS / Arch Linux base).
* **.NET SDK Terpasang**: `10.0.113` (SDK .NET 10).
* **Target Framework**: `net8.0-windows` (.NET 8 LTS).
* **Targeting Pack Cross-Platform**: `<EnableWindowsTargeting>true</EnableWindowsTargeting>` dikonfigurasi pada file proyek WPF (`AgroTrack.csproj`) sehingga kompilasi dan build XAML/WPF dapat berjalan mulus pada lingkungan Linux dengan SDK .NET 10 / .NET 8 tanpa batasan OS Windows.
* **Hasil Verifikasi Build**: Kompilasi uji `net8.0-windows` dengan referensi targeting pack WPF terverifikasi sukses (0 Warning, 0 Error).

---

## 2. Pilihan Database & Akses Data

* **Database Engine**: Microsoft SQL Server 2022 (Developer / Express edition).
* **Lingkungan Database Development**: Dijalankan secara lokal melalui SQL Server container (menggunakan runtime container rootless `podman` yang tersedia di sistem operasi atau instalasi SQL Server lokal pada port default `1433`).
* **Koneksi ADO.NET**: `Microsoft.Data.SqlClient` (versi stabil 5.2+).
* **Pencegahan ORM**: Sesuai mandat, Entity Framework **tidak digunakan**. Seluruh operasi basis data menggunakan ADO.NET murni dengan parameterized query (`SqlCommand`, `SqlParameter`), `SqlTransaction`, dan `IDbConnectionFactory`.
* **Keamanan Kredensial**: File konfigurasi `appsettings.json` menyediakan template aman (default ke localhost dengan fallback konfigurasi environment variable `AGROTRACK_CONNECTION_STRING`), tanpa menyimpan secret produksi di git.

---

## 3. Arsitektur dan Pola Desain

* **Pola UI**: Model-View-ViewModel (MVVM) murni tanpa framework pihak ketiga yang berat, menggunakan:
  * `ViewModelBase` mengimplementasikan `INotifyPropertyChanged`.
  * `RelayCommand` dan `AsyncRelayCommand` mengimplementasikan `ICommand`.
  * `ObservableCollection<T>` untuk data binding dua arah pada DataGrid dan ComboBox.
* **Separation of Concerns**:
  * **View (XAML & View Code-Behind)**: Hanya menangani inisialisasi komponen UI dan delegasi interaksi visual. Tidak ada query SQL, tidak ada koneksi database, tidak ada business logic.
  * **ViewModel**: Mengatur state UI, command execution, validasi input antarmuka, loading/empty/error state.
  * **Service Layer**: Mengatur aturan bisnis (business logic), validasi kapasitas lahan, validasi stok bahan, otorisasi role, perhitungan target vs realisasi, dan orkestrasi transaksi.
  * **Repository Layer**: Mengatur eksekusi parameterized SQL query ADO.NET ke SQL Server.
  * **Model & DTO**: Representasi entitas domain dan struktur transfer data untuk dashboard/laporan agregasi.
* **Inversi Kendali / Dependency Injection**: Menggunakan composition root sederhana dan bersih via `Microsoft.Extensions.DependencyInjection` terpusat di `App.xaml.cs`.

---

## 4. Keamanan dan Otorisasi

* **Password Hashing**: Menggunakan algoritma kriptografis modern `PBKDF2` (menggunakan `Rfc2898DeriveBytes` dengan HMAC-SHA256, salt 16-byte kriptografis acak, dan 100.000 iterasi). Password tidak pernah disimpan dalam bentuk plain text.
* **Otorisasi Role-Based**:
  * 3 Role: `Administrator`, `Manager`, `FieldOfficer`.
  * Pengecekan izin dilakukan dua lapis: pada UI (visibilitas/enable command) dan pada Service Layer (sebelum mutasi data dieksekusi).
* **Sanitasi Input**: Seluruh input teks di-`Trim()`. Nilai spasi kosong dianggap invalid. Parameter SQL wajib digunakan di seluruh query untuk mencegah SQL Injection.

---

## 5. UI/UX & Design System

* **Palet Warna**:
  * Primary: Deep Forest Green (`#1E5E3A`, `#2D7D46`)
  * Secondary: Earth Warm Green & Neutral Amber (`#3A7D44`, `#4C9A2A`)
  * Surface Dark: `#181F1A`, Surface Light: `#F4F7F4`
  * Status Colors: Success Green, Warning Orange, Danger Red, Info Blue
* **Tema**: Dukungan dinamis Light Mode & Dark Mode dengan ResourceDictionary yang dapat diubah saat runtime.
* **Rich UI Elements**:
  * Sidebar navigasi interaktif dengan badge indikator role pengguna aktif.
  * Header ringkas dengan breadcrumb halaman dan tombol toggle tema & logout.
  * Kartu statistik ringkasan, DataGrid dengan styling modern, badge status berbahasa Indonesia.
  * Visual perbandingan target vs hasil menggunakan visualisasi bar WPF berbasis Data Template dan converters.
  * Komponen terstandarisasi untuk Loading State, Empty State, and Error State.
