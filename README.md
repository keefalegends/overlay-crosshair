# Windows Crosshair Overlay

Aplikasi overlay crosshair ringan dan modern untuk Windows, dirancang khusus untuk game yang tidak memiliki crosshair bawaan (hardcore FPS, mode realism, senjata sniper/shotgun hip-fire, atau game simulasi).

---

## Fitur Utama

- **Click-Through Transparan**: Menggunakan Win32 Extended Styles (`WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE`) sehingga klik mouse dan keyboard langsung tembus ke game tanpa delay atau kehilangan fokus.
- **Ringan & Cepat**: Dibangun dengan C# .NET WPF hardware-accelerated (DirectX), ukuran executable hanya ~200 KB, tanpa beban CPU/GPU.
- **8 Gaya Crosshair**:
  - Classic Cross (`+`)
  - Center Dot (`•`)
  - Cross + Dot (`+•`)
  - T-Shape (`⊥`)
  - Circle (`O`)
  - Circle + Dot (`⊙`)
  - Chevron (`^`)
  - Box (`□`)
- **Outline Kontras (Border)**: Menjamin crosshair selalu terlihat jelas, baik di latar peta salju/terang maupun lorong gelap.
- **Kustomisasi Lengkap**:
  - Panjang garis (Size)
  - Ketebalan garis (Thickness)
  - Jarak tengah (Gap)
  - Ukuran Dot
  - Opacity (Transparansi)
  - Preset warna neon (Hijau, Cyan, Merah, Kuning, Putih, Ungu, Oranye) + Custom HEX Picker
- **Kalibrasi Posisi (Offset X & Y)**: Geser posisi crosshair pixel-per-pixel jika tembakan senjata game sedikit melenceng dari titik tengah monitor.
- **Auto Re-centering**: Otomatis menyesuaikan posisi saat resolusi monitor berubah.
- **Penyimpanan Otomatis**: Semua preferensi tersimpan otomatis di `crosshair_settings.json`.

---

## Tombol Pintas Global (In-Game Hotkeys)

Dapat ditekan langsung kapan saja saat sedang bermain game:

| Tombol | Fungsi |
|---|---|
| **`F10`** | **Toggle Overlay ON / OFF** (Sembunyikan/tampilkan crosshair seketika) |
| **`F9`** | **Buka / Tutup Pengaturan** (Show/Hide Settings Dashboard) |
| **`Page Up`** | Ganti ke gaya crosshair **berikutnya** |
| **`Page Down`** | Ganti ke gaya crosshair **sebelumnya** |

---

## Cara Menjalankan

### Cara Cepat (Langsung Pakai):
Cukup **double-click** file:
```text
CrosshairOverlay.exe
```
di folder ini.

1. Jendela pengaturan (Settings) akan terbuka beserta crosshair di tengah layar.
2. Atur gaya, warna, dan ukuran sesuai selera (kamu bisa lihat perubahannya secara langsung di layar).
3. Klik tombol hijau **"Play Game (Hide Settings)"** atau tekan **`F9`** untuk menyembunyikan menu pengaturan dan fokus bermain.
4. Di dalam game, jika ingin mematikan crosshair sementara, tekan **`F10`**.

---

## Catatan Penting untuk Gamer

> [!TIP]
> **Mode Tampilan Game**:
> Selalu atur opsi tampilan game kamu ke **Borderless Windowed** (atau Windowed).
> Mode *Exclusive Fullscreen* bawaan Windows versi lama terkadang memprioritaskan render game di atas layer DWM Windows, sedangkan *Borderless Windowed* memungkinkan overlay berjalan mulus 100% tanpa gangguan dan tanpa delay.

> [!NOTE]
> **Aman dari Anti-Cheat**:
> Aplikasi ini tidak menginjeksi DLL, tidak mengaitkan memory game, dan tidak memodifikasi file game apa pun. Ini adalah jendela Windows transparan murni.

---

## Cara Build Ulang dari Source Code

Jika ingin memodifikasi atau meng-compile ulang:
```bash
# Build & Jalankan secara langsung
dotnet run

# Build standalone Release executable
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false -o .
```
