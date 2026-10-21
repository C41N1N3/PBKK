# Product Requirements Document (PRD)
## AgroTrack: Sistem Informasi Manajemen Produksi Pertanian Berbasis Desktop

---

## 1. Ikhtisar Produk (Product Overview)
**AgroTrack** adalah aplikasi desktop modern berbasis C#, WPF, dan SQL Server (ADO.NET) yang dirancang untuk mendukung operasional agribisnis dan pengelolaan pertanian terpadu. Sistem memfasilitasi pencatatan lahan, komoditas, musim tanam (penanaman), aktivitas lapangan harian, logistik bahan pertanian dan stok, biaya operasional, serta hasil panen. AgroTrack menghitung target produksi dibandingkan realisasi aktual, total biaya, pendapatan, laba/rugi bersih, serta menyajikan visualisasi data dan laporan produksi terintegrasi secara langsung dari basis data.

---

## 2. Tujuan & Sasaran (Objectives)
1. **Transparansi Operasional**: Mencatat seluruh siklus hidup penanaman dari persiapan lahan hingga pasca panen.
2. **Akurasi Finansial & Analisis Produksi**: Membandingkan estimasi target hasil panen dengan realisasi nyata, serta mengkalkulasi laba/rugi per musim tanam.
3. **Efisiensi Logistik Pertanian**: Mengontrol persediaan bahan pertanian (pupuk, pestisida, benih) dengan pengurangan stok otomatis dan pencegahan stok negatif.
4. **Keamanan & Akuntabilitas**: Pembagian hak akses berjenjang (Administrator, Manajer, Petugas Lapangan) dan audit log transaksi.
5. **Pengalaman Pengguna Modern (Rich UI)**: Desain antarmuka profesional bertema pertanian, responsif, dilengkapi dukungan tema Terang/Gelap, loading/empty/error state, dan visualisasi data.

---

## 3. Aktor & Matriks Hak Akses (Roles & Permissions)

| Modul / Fitur | Administrator | Manajer | Petugas Lapangan |
| :--- | :---: | :---: | :---: |
| **Login & Logout** | Ya | Ya | Ya |
| **Dashboard** | Lengkap | Lengkap | Terbatas (Operasional) |
| **Manajemen Pengguna** | Penuh (CRUD + Toggle Status) | Baca Saja | Tidak Ada Akses |
| **Master Lahan** | Penuh (CRUD) | Penuh (CRUD) | Baca Saja |
| **Master Komoditas** | Penuh (CRUD) | Penuh (CRUD) | Baca Saja |
| **Manajemen Penanaman** | Penuh (CRUD + Buka Kembali) | Penuh (CRUD + Selesaikan) | Baca Saja |
| **Aktivitas Pertanian** | Penuh | Penuh | Tambah & Ubah (Milik Sendiri) |
| **Bahan & Stok Pertanian** | Penuh (CRUD + Stok) | Penuh (CRUD + Stok) | Baca Stok & Catat Pemakaian |
| **Penggunaan Bahan** | Penuh | Penuh | Tambah saat Aktivitas |
| **Biaya Operasional** | Penuh | Penuh | Tidak Ada Akses |
| **Hasil Panen** | Penuh | Penuh | Tambah & Baca |
| **Laporan Produksi & Laba** | Penuh | Penuh | Tidak Ada Akses |
| **Audit Log** | Penuh | Ringkasan | Tidak Ada Akses |

---

## 4. Fitur Utama Berdasarkan Prioritas

### 4.1 Prioritas MUST (Wajib)
1. **Otentikasi & Shell Aplikasi**:
   - Login dengan validasi kredensial aman (PBKDF2 salted hash).
   - Penolakan akun nonaktif. Pesan error seragam untuk menghindari user enumeration.
   - Session pengguna terpusat dengan role aktif.
   - Navigasi sidebar, header dengan identitas user, tombol ganti tema (Light/Dark), dan konfirmasi logout.
2. **Master Data Lahan**:
   - CRUD Lahan: Kode lahan unik, Nama, Lokasi, Luas (hektare > 0), Jenis Tanah, Status, Deskripsi.
   - Pencegahan penghapusan lahan yang telah terikat transaksi penanaman.
3. **Master Data Komoditas**:
   - CRUD Komoditas: Kode komoditas unik, Nama, Satuan Panen (kg, ton, kuintal), Estimasi Hari Tumbuh (>=0), Deskripsi, Status Aktif.
4. **Manajemen Penanaman (Musim Tanam)**:
   - CRUD Penanaman: Kode unik, referensi Lahan dan Komoditas, Nama Musim, Tanggal Mulai, Tanggal Estimasi Panen, Luas Tanam, Target Hasil, Status (Direncanakan, Aktif, Siap Panen, Selesai, Dibatalkan), Catatan.
   - Validasi kapasitas: Luas tanam tidak boleh melebihi luas lahan fisik, dan irisan periode musim tanam aktif tidak boleh melampaui kapasitas lahan.
5. **Aktivitas Pertanian Lapangan**:
   - Pencatatan aktivitas berdasar penanaman: Jenis (Persiapan Lahan, Penanaman, Penyiraman, Pemupukan, dsb), Tanggal, Durasi (jam), Petugas, Status, Catatan.
   - Validasi: Penanaman Selesai atau Dibatalkan menolak aktivitas baru.
6. **Biaya Operasional**:
   - Pencatatan biaya berdasar penanaman: Kategori (Tenaga kerja, Sewa alat, Transportasi, Irigasi, dsb), Tanggal, Jumlah (Rupiah > 0), Catatan.
7. **Hasil Panen**:
   - Pencatatan panen (mendukung panen bertahap): Tanggal, Jumlah Panen (> 0), Kualitas (Grade A/B/C), Harga Jual per Satuan (>= 0), Total Pendapatan (`YieldAmount * SellingPricePerUnit`).
8. **Kalkulasi Bisnis & Analisis**:
   - `TotalBiayaOperasional` = Sum(Biaya Operasional).
   - `TotalHasilAktual` = Sum(Yield Panen).
   - `TotalPendapatan` = Sum(Total Pendapatan Panen).
   - `LabaRugi` = TotalPendapatan - (TotalBiayaOperasional + TotalBiayaBahan).
   - `% Pencapaian Target` = (TotalHasilAktual / TargetHasil) * 100 (jika TargetHasil > 0, aman terhadap zero-division).
9. **Dashboard Real-Time**:
   - Kartu statistik (Lahan Aktif, Luas Total, Penanaman Aktif, Aktivitas Hari Ini, Bahan Menipis, Total Panen, Total Biaya, Total Pendapatan, Estimasi Keuntungan).
   - Grafik/Visual Target vs Realisasi, Distribusi Komoditas, Aktivitas Terkini.
   - Seluruh metrik diambil langsung dari query database SQL Server.
10. **Laporan Produksi & Keuntungan**:
    - Ringkasan komprehensif per musim tanam: Target vs Realisasi, Rincian Biaya, Total Pendapatan, dan Margin Keuntungan.
11. **UX & State Management**:
    - Loading indicator saat eksekusi query async.
    - Empty state informatif jika data kosong atau hasil filter tidak ditemukan.
    - Error banner dengan tombol muat ulang (retry).
    - Notifikasi Toast berhasil/gagal.
    - Dialog konfirmasi sebelum penghapusan data penting.

### 4.2 Prioritas SHOULD (Penting)
1. **Modul Bahan & Persediaan (Agricultural Materials)**:
   - CRUD Bahan: Kode unik, Nama, Kategori (Benih, Pupuk, Pestisida, dsb), Satuan, Stok, Stok Minimum, Harga Satuan.
   - Penggunaan Bahan pada Aktivitas (`MaterialUsage`) dengan snapshot harga.
   - Pengurangan stok otomatis dalam transaksi database (`SqlTransaction`).
   - Pembatalan transaksi mengembalikan stok secara atomik.
   - Peringatan dini jika stok di bawah batas minimum.
2. **Audit Logging**:
   - Pencatatan aksi penting (Login, Tambah, Ubah, Hapus) pada tabel `AuditLog`.
3. **Manajemen Pengguna Lengkap**:
   - Formulir pembuatan pengguna baru, ubah role, reset password aman, dan toggle aktivasi akun.

---

## 5. Kebutuhan Non-Fungsional
1. **Integritas Data**: Menggunakan Foreign Key, Unique Constraint, Check Constraint, dan Database Transaction.
2. **Kinerja**: Parameterized query terindeks secara optimal; operasi I/O database berjalan asynchronous (`async/await`) agar antarmuka pengguna tidak macet/freeze.
3. **Keamanan**: Tidak ada plain-text password; tidak ada SQL injection vulnerability; kredensial database terisolasi di file konfigurasi.
4. **Portabilitas & Konsistensi**: Menggunakan .NET 8 LTS dengan WPF, didukung cross-platform targeting pack.
