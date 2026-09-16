# Crosshair Overlay

A free, lightweight, esports-grade crosshair overlay for Windows. Built for games that don't provide a built-in crosshair.

[![Release](https://img.shields.io/github/v/release/keefalegends/overlay-crosshair?color=10B981&label=Download)](https://github.com/keefalegends/overlay-crosshair/releases/latest/download/CrosshairOverlay.exe)
[![License: MIT](https://img.shields.io/badge/license-MIT-10B981)](LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)

---

## Download

**[→ Download CrosshairOverlay.exe](https://github.com/keefalegends/overlay-crosshair/releases/latest/download/CrosshairOverlay.exe)**

No installation required. Single self-contained `.exe`. No .NET runtime needed.

---

## Features

- **Anti-Cheat Safe** — Pure DWM transparent overlay. No DLL injection, no memory hooks.
- **Zero Input Lag** — Native Win32 `WS_EX_TRANSPARENT` pass-through. Clicks go straight to the game.
- **8 Reticle Shapes** — Classic Cross, Center Dot, Cross+Dot, T-Shape, Circle, Circle+Dot, Chevron, Box
- **Full Customization** — Size, thickness, center gap, dot size, opacity, outline contrast, color (preset + custom HEX)
- **Screen Calibration** — Pixel-level Offset X/Y nudge with one-click center reset
- **Live Preview** — Settings panel shows real-time reticle preview
- **Auto-save** — Settings persist automatically via `crosshair_settings.json`

---

## Hotkeys

| Key | Action |
|---|---|
| `F10` | Toggle overlay on/off |
| `F9` | Show/hide settings |
| `Page Up / Down` | Cycle reticle shape |

---

## Usage

1. Download and run `CrosshairOverlay.exe`
2. Configure shape, color, and size in the settings panel
3. Set your game to **Borderless Windowed** mode
4. Press `F9` to hide settings and start playing

---

## Stack

| Layer | Tech |
|---|---|
| Language | C# 13 / .NET 10 |
| UI Framework | WPF (Windows Presentation Foundation) |
| Rendering | `DrawingContext` — DirectX hardware-accelerated vector |
| Overlay | Win32 `WS_EX_TRANSPARENT \| WS_EX_LAYERED \| WS_EX_TOOLWINDOW` |
| Persistence | JSON (`System.Text.Json`) |
| Distribution | Single-file self-contained executable (`PublishSingleFile=true`) |

---

## Build from Source

```bash
# Run in development
dotnet run

# Build standalone release exe
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./dist
```

Requires .NET 10 SDK.

---

## License

MIT © [KeefaLegends](https://github.com/keefalegends)
