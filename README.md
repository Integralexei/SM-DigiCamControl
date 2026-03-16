# digiCamControl — Stop Motion Edition

A modified version of [digiCamControl](https://github.com/dukus/digiCamControl) tailored for stop motion animation production.

## What's Different

This edition adds a set of stop motion–specific tools on top of the standard digiCamControl live view, without removing any existing functionality.

| Feature | Standard | Stop Motion Edition |
|---|---|---|
| Onion Skin overlay | — | ✓ |
| Motion Guides (Bezier arcs) | — | ✓ |
| Image Sequencer RAM playback | лагает | ✓ плавное |

See [CHANGELOG.md](CHANGELOG.md) for a detailed history of changes.

## Added Features

### Onion Skin
Overlays one or more previous (and/or next) frames as a semi-transparent ghost image on top of the live view. Helps animators judge movement between frames.

- Configurable back/forward frame count (0–5 each)
- Opacity slider (0–100%)
- Rendered via WPF GPU compositing — zero per-frame CPU cost
- Cache is rebuilt only when the frame selection changes

### Motion Guides
Quadratic Bezier arcs drawn as canvas overlays on the live view. Used to plan and visualize character movement paths.

- 3-click drawing flow: set start → set end → bend arc
- Live arc preview while bending (orange control point + dashed handles)
- Guides saved per-camera as JSON
- Commands: clear all guides / remove last guide

### Image Sequencer — плавное воспроизведение из RAM

Оригинальный плагин Image Sequencer воспроизводил кадры с диска в реальном времени, что приводило к лагам на любом компьютере. Исправлено двухфазной моделью:

- **Prepare Playback** — все кадры выбранного диапазона декодируются в фоновом потоке с заданной шириной (`Decode Width`) и сохраняются в памяти как замороженные `BitmapSource[]`
- **Play** — каждый тик таймера = одно чтение из массива в памяти, без I/O и декодирования. Гарантированно плавное воспроизведение
- Настройка `Decode Width` (320–1920 px) позволяет контролировать объём используемой памяти (показывается в MB)
- Кэш автоматически сбрасывается при изменении диапазона кадров или ширины декода

### Image Sequencer — исправление создания видео

Кнопка «Create Movie» выдавала ошибку `ffmpeg not found!` при запуске debug-сборки, поскольку `ffmpeg.exe` искался только рядом с исполняемым файлом. Теперь поиск ведётся последовательно в: директории exe → `Program Files\digiCamControl` → переменной PATH.

## Base Version

Built on top of digiCamControl source code.
Original project: https://github.com/dukus/digiCamControl
License: LGPL-2.1
