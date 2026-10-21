# Panduan Pengguna (User Guide) - AgroTrack

Selamat datang di **AgroTrack: Sistem Informasi Manajemen Produksi Pertanian Berbasis Desktop**. Panduan ini dirancang untuk membantu pengguna dari berbagai peranan (*Administrator*, *Manajer*, dan *Petugas Lapangan*) dalam mengoperasikan aplikasi secara efektif.

---

## Daftar Isi
1. [Memulai dan Autentikasi](#1-memulai-dan-autentikasi)
2. [Navigasi dan Tata Letak Antarmuka](#2-navigasi-dan-tata-letak-antarmuka)
3. [Pengaturan Tema (Mode Terang & Gelap)](#3-pengaturan-tema-mode-terang--gelap)
4. [Modul Dashboard](#4-modul-dashboard)
5. [Manajemen Master Data Lahan](#5-manajemen-master-data-lahan)
6. [Manajemen Master Data Komoditas](#6-manajemen-master-data-komoditas)
7. [Manajemen Penanaman (Musim Tanam)](#7-manajemen-penanaman-musim-tanam)
8. [Pencatatan Aktivitas Pertanian](#8-pencatatan-aktivitas-pertanian)
9. [Manajemen Bahan Pertanian dan Stok](#9-manajemen-bahan-pertanian-dan-stok)
10. [Pencatatan Biaya Operasional](#10-pencatatan-biaya-operasional)
11. [Pencatatan Hasil Panen](#11-pencatatan-hasil-panen)
12. [Laporan Produksi dan Keuangan](#12-laporan-produksi-dan-keuangan)
13. [Manajemen Pengguna (Administrator)](#13-manajemen-pengguna-administrator)
14. [Penanganan Error dan Validasi](#14-penanganan-error-dan-validasi)

---

## 1. Memulai dan Autentikasi

### 1.1 Masuk ke Sistem (Login)
1. Buka aplikasi AgroTrack. Jendela Login akan muncul secara otomatis.
2. Masukkan **Username** dan **Password** Anda.
3. Klik tombol **Masuk** atau tekan tombol **Enter** pada keyboard.
4. Jika kredensial cocok dan akun berstatus **Aktif**, sistem akan membuat sesi kerja dan membuka layar utama.
5. Jika gagal (password keliru atau akun tidak ditemukan), pesan kesalahan *"Username atau password salah"* akan ditampilkan tanpa membocorkan keberadaan username demi keamanan.

### 1.2 Keluar dari Sistem (Logout)
1. Pada bagian bawah sidebar navigasi kiri, klik tombol **Keluar (Logout)**.
2. Dialog konfirmasi akan muncul. Klik **Ya** untuk mengakhiri sesi.
3. Seluruh sesi memori dibersihkan dan aplikasi kembali ke layar Login.

---

## 2. Navigasi dan Tata Letak Antarmuka

Layar kerja AgroTrack mengusung tata letak modern:
- **Sidebar Kiri**: Berisi logo AgroTrack, informasi pengguna aktif (Nama & Badge Peranan), daftar menu modul navigasi, dan tombol pengaturan tema serta logout. Menu yang tampil disesuaikan secara otomatis berdasarkan hak akses pengguna.
- **Header Atas**: Menampilkan judul halaman modul yang sedang dibuka, jam sistem, dan status konektivitas.
- **Area Konten Utama**: Tempat data ditampilkan menggunakan kartu ringkasan, DataGrid interaktif, bilah pencarian/filter, dan formulir pop-up.

---

## 3. Pengaturan Tema (Mode Terang & Gelap)

AgroTrack menyediakan tema visual lengkap yang ramah mata:
- **Mode Terang (*Light Mode*)**: Desain cerah bernuansa putih dan hijau emerald yang optimal untuk penggunaan di siang hari atau ruangan terang.
- **Mode Gelap (*Dark Mode*)**: Desain modern bernuansa *dark slate* dan *forest green* dengan kontras tinggi yang nyaman di malam hari.
- **Cara Mengganti**: Klik tombol **Ganti Tema** di bagian bawah sidebar untuk beralih secara instan tanpa perlu memuat ulang aplikasi.

---

## 4. Modul Dashboard

Dashboard menyajikan ringkasan kinerja pertanian secara *real-time* langsung dari basis data:
1. **Kartu Statistik Utama**:
   - Total Lahan Aktif dan Total Luas Lahan (hektar).
   - Jumlah Musim Tanam Aktif.
   - Aktivitas Pertanian Hari Ini.
   - Peringatan Jumlah Bahan dengan Stok Menipis.
   - Total Hasil Panen Kumulatif (kg).
   - Total Biaya Operasional dan Pengeluaran Bahan (Rp).
   - Total Pendapatan Penjualan Panen (Rp).
   - Estimasi Keuntungan Bersih (Rp).
2. **Visualisasi Target vs Realisasi Panen**: Menampilkan perbandingan batang target produksi terhadap capaian aktual per musim tanam.
3. **Daftar Peringatan Stok Menipis**: Memberitahukan bahan yang stoknya telah berada di bawah batas minimum pemakaian.
4. **Tombol Segarkan (Refresh)**: Memperbarui seluruh statistik secara instan setelah ada input transaksi baru.

---

## 5. Manajemen Master Data Lahan

Modul ini digunakan untuk mencatat aset lahan pertanian:
- **Pencarian & Filter**: Cari lahan berdasarkan Kode Lahan, Nama, atau Lokasi. Filter berdasarkan jenis tanah (Alluvial, Lempung, Vulkanik, dsb) atau status (Aktif/Nonaktif).
- **Menambah Lahan**: Klik tombol **+ Tambah Lahan**. Isi kode unik (misal: `LH-TMR-02`), nama lahan, luas area dalam hektar (wajib > 0), jenis tanah, dan keterangan.
- **Mengubah Lahan**: Pilih lahan pada tabel, lalu klik **Ubah**.
- **Menonaktifkan Lahan**: Klik **Hapus/Nonaktifkan**. Jika lahan sudah digunakan pada transaksi penanaman aktif, sistem akan secara otomatis menjaga integritas data dengan menawarkan opsi menonaktifkan status agar riwayat transaksi masa lalu tidak rusak.

---

## 6. Manajemen Master Data Komoditas

Mengelola varietas tanaman pertanian:
- **Informasi Komoditas**: Kode komoditas unik, nama tanaman, estimasi umur tanam (hari), dan satuan hasil panen (kg, ton, kuintal).
- **Pencarian**: Pencarian cepat berdasarkan kode atau nama komoditas.
- **Status Komoditas**: Komoditas dapat diaktifkan atau dinonaktifkan sesuai kebutuhan rencana produksi.

---

## 7. Manajemen Penanaman (Musim Tanam)

Inti dari proses produksi pertanian di AgroTrack:
- **Siklus Status Penanaman**:
  1. `Direncanakan` (*Planned*)
  2. `Aktif` (*Active*)
  3. `Siap Panen` (*Ready to Harvest*)
  4. `Selesai` (*Completed*)
  5. `Dibatalkan` (*Cancelled*)
- **Validasi Kapasitas Lahan**: Luas tanam tidak boleh melebihi luas total lahan. Sistem juga memvalidasi agar luas penanaman aktif yang beririsan tidak melebihi kapasitas fisik lahan.
- **Target vs Hasil Aktual**: Menampilkan capaian produksi dalam persentase. Jika target diisi 0, sistem secara aman menampilkan `N/A` tanpa menyebabkan error pembagian dengan nol.
- **Penyelesaian Musim Tanam**: Penanaman yang telah berstatus `Selesai` atau `Dibatalkan` diproteksi secara otomatis agar tidak dapat ditambahi transaksi baru secara tidak sengaja.

---

## 8. Pencatatan Aktivitas Pertanian

Digunakan oleh Petugas Lapangan dan Manajer untuk mendokumentasikan pekerjaan harian:
- **Jenis Aktivitas**: Persiapan lahan, Penanaman, Penyiraman, Pemupukan, Pengendalian hama, Penyiangan, Pemeriksaan, Panen, dan Lainnya.
- **Atribut**: Tanggal kegiatan, penanaman terkait, durasi pengerjaan (jam), deskripsi pekerjaan, dan status pengerjaan.
- **Integrasi**: Menjadi rujukan utama ketika mencatat bahan pertanian yang digunakan pada aktivitas tersebut.

---

## 9. Manajemen Bahan Pertanian dan Stok

Mengelola inventaris bahan habis pakai secara atomik (ACID):
- **Kategori Bahan**: Benih, Pupuk, Pestisida, Herbisida, Fungisida, Peralatan habis pakai, dan Lainnya.
- **Indikator Stok Minimum**: Bahan dengan stok di bawah ambang batas akan ditandai dengan badge peringatan merah.
- **Pencatatan Penggunaan Bahan**:
  - Saat bahan digunakan pada aktivitas tertentu, sistem mencatat kuantitas dan harga satuan snapshot pada saat itu.
  - Stok bahan otomatis terpotong di database dalam transaksi SQL atomik.
  - Jika transaksi penggunaan dibatalkan, stok bahan secara otomatis dikembalikan ke inventaris.
  - Penggunaan bahan melebihi stok yang tersedia akan otomatis ditolak dengan pesan peringatan jelas.

---

## 10. Pencatatan Biaya Operasional

Mencatat seluruh pengeluaran di luar bahan:
- **Kategori Biaya**: Tenaga kerja, Sewa alat, Transportasi, Irigasi, Perawatan, Bahan bakar, dan Lainnya.
- **Format Rupiah**: Nilai uang diformat sesuai standar moneter Indonesia (`Rp X.XXX.XXX,00`).
- **Validasi**: Nilai biaya harus lebih dari Rp 0,00.

---

## 11. Pencatatan Hasil Panen

Mendukung pemanenan bertahap (*batch harvesting*):
- **Pencatatan**: Masukkan penanaman yang dipanen, tanggal panen, kuantitas hasil (kg/ton), kualitas (Grade A, Grade B, Grade C), dan harga jual satuan.
- **Kalkulasi Pendapatan Otomatis**: $Total Pendapatan = Hasil \times Harga Jual$.
- **Akumulasi**: Hasil panen beberapa kali akan dijumlahkan secara otomatis pada detail penanaman, dashboard, dan laporan laba rugi.

---

## 12. Laporan Produksi dan Keuangan

Menyediakan laporan terpadu untuk evaluasi bisnis pertanian:
1. **Laporan Laba Rugi per Penanaman**:
   - Membandingkan Target Produksi vs Hasil Aktual.
   - Menghitung Total Biaya Bahan + Total Biaya Operasional.
   - Menghitung Total Pendapatan Penjualan.
   - Menghasilkan Laba / Rugi Bersih secara presisi.
2. **Filter Fleksibel**: Saring laporan berdasarkan rentang tanggal awal - akhir, lahan tertentu, komoditas, maupun status penanaman.

---

## 13. Manajemen Pengguna (Administrator)

Modul khusus Administrator untuk mengelola akun sistem:
- **Daftar Akun**: Melihat seluruh username, nama lengkap, peranan, dan status aktif.
- **Tambah Akun**: Buat akun baru dengan peranan Administrator, Manajer, atau Petugas Lapangan serta password awal (min. 6 karakter).
- **Ubah Peranan**: Memperbarui nama lengkap dan peranan pengguna.
- **Reset Password**: Mengatur ulang kata sandi pengguna tanpa perlu mengetahui kata sandi lama.
- **Aktifkan / Nonaktifkan**: Mengubah status aktif akun. Sistem secara tegas melarang Administrator menonaktifkan akunnya sendiri demi mencegah penguncian sistem (*lockout*).

---

## 14. Penanganan Error dan Validasi

AgroTrack mengedepankan pengalaman pengguna yang informatif dan aman:
- **Pesan Berbahasa Indonesia**: Seluruh validasi ditampilkan dalam bahasa Indonesia yang santun dan mudah dipahami.
- **Pencegahan Klik Ganda**: Tombol simpan dinonaktifkan saat transaksi database sedang diproses untuk menghindari data duplikat.
- **Tidak Membocorkan Detail Teknis**: Pesan kesalahan sistem ditampilkan secara ringkas kepada pengguna tanpa menampilkan *stack trace* rumit di antarmuka.
