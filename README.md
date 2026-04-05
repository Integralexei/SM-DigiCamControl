# digiCamControl — Stop Motion Edition

A modified version of [digiCamControl](https://github.com/dukus/digiCamControl) tailored for stop motion animation production.

## What's Different

This edition adds a set of stop motion–specific tools on top of the standard digiCamControl live view, without removing any existing functionality.

| Feature | Standard | Stop Motion Edition |
|---|---|---|
| Onion Skin overlay | — | ✓ |
| Motion Guides (Bezier arcs) | — | ✓ |
| Review mode (no camera) | — | ✓ |
| Insert Mode (capture at position) | — | ✓ |
| Renumber Sequence (fix filename order) | — | ✓ |
| Filmstrip touchpad scroll | — | ✓ |
| Capture hotkey (Ctrl+Enter) | — | ✓ |
| Create Video (working) | broken | ✓ |
| English UI in Live View | partial | ✓ |

---

## Download & Run

1. Go to the [Releases](../../releases) page and download the latest `.zip`
2. Unzip anywhere
3. Double-click `run.bat` — or `CameraControl.exe` directly
4. Requirements: Windows 10/11, .NET Framework 4.8 (included in Windows by default)

> **Camera not required** — you can open Live View in Review Mode to inspect frames from an existing session.

---

## Added Features

### Onion Skin
Overlays the previous frame as a semi-transparent ghost image on top of the live view. A single opacity slider (0–100%) controls how strongly the ghost is visible — great for judging movement from one frame to the next.

- **Ghost frame selection:**
  - Insert Mode ON + orange frame selected → shows the frame immediately *before* the selected frame
  - Insert Mode ON, no frame selected → shows the last captured frame
  - Insert Mode OFF → always shows the last captured frame
  - If the insert point is the very first frame, no ghost is shown (no previous frame exists)
- Opacity slider (0–100%): controls ghost visibility via WPF GPU compositing — zero per-frame CPU cost
- Cache is rebuilt only when the reference frame changes
- Fallback: loads from original photo path if thumbnail is missing
- Pixel-perfect alignment: the loaded frame is normalized to the exact live view bitmap dimensions, compensating for JPEG MCU-boundary rounding that would otherwise cause a 1–3 px edge gap

### Motion Guides
Quadratic Bezier arcs drawn as canvas overlays on the live view. Used to plan and visualize character movement paths.

- 3-click drawing flow: set start → set end → bend arc
- Live arc preview while bending (orange control point + dashed handles)
- Right-click cancels at any step
- Guides saved per-camera as JSON in `DataFolder/MotionGuides_{CameraName}.json`
- Commands: clear all guides / remove last guide

### Insert Mode
Controls where newly captured frames are inserted in the session filmstrip.

- **ON**: each new frame is inserted immediately before the currently selected (orange) frame in the filmstrip
- **OFF**: new frames are appended at the end
- Toggle button in the filmstrip toolbar (camera+ icon); highlights when active
- Onion Skin automatically re-anchors to the frame before the selected frame when Insert Mode is ON

### Renumber Sequence
Renames all files in the session on disk so their filenames match the visual sequence order in the filmstrip.

Available in the **Image Sequencer** window ("Renumber Sequence" button in the sidebar).

- When frames are captured in Insert Mode, their filenames reflect capture order, not visual order. After renaming, `frame_0001.jpg`, `frame_0002.jpg`, … match the filmstrip left-to-right.
- Two-pass rename (all files → temp names, then → final names) prevents collisions regardless of the original naming.
- Handles RAW+JPG pairs: both files in a pair get the same counter number.
- Updates the session file after renaming so the app stays consistent.
- Shows a confirmation dialog before proceeding (cannot be undone).

### Review Mode (no camera)
Allows opening the Live View window even when no camera is connected, so you can inspect the Onion Skin overlay across existing session frames.

- Live View button is enabled when the session contains at least one frame
- The live view feed is blank, but the Onion Skin overlay remains fully functional
- Useful for reviewing animation timing without a connected camera

### Filmstrip Touchpad Scroll
The filmstrip (horizontal frame strip) can be scrolled with a touchpad left/right swipe, in addition to the mouse wheel.

- Handles `WM_MOUSEHWHEEL` (Windows horizontal wheel message)
- Works with modern precision touchpads and horizontal scroll wheels

### Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| **Ctrl+Enter** | Capture (same as the Capture button in Live View) |

---

## Bug Fixes vs. Original

| Fix | Description |
|---|---|
| Onion Skin lag | The original implementation composited all ghost frames in software (CPU Blit) on every live view frame — causing severe lag at any frame rate. Rewritten to load the reference frame once, then control visibility purely via WPF `Image.Opacity`. The GPU handles all blending; the CPU does zero work per live view frame. |
| Sidebar layout collapse | Opening Motion Guides expander no longer hides NumericUpDown controls or the Capture/Confirm buttons. Root cause: outer column `Width="Auto"` replaced with `Width="*"`. |
| Create Video — no output | `GenerateMp4()` passed two separate `-vf` flags to ffmpeg; ffmpeg 3.x+ treats this as an error and exits without producing output. Fixed by merging both filters into a single `-vf fps=25,scale=W:H` chain. Paths are now also quoted to handle spaces in usernames or session names. |
| Create Video — 4K codec | 4K preset switched from `libx265` (H.265) to `libx264` (H.264). H.265 requires a paid Windows codec extension; H.264 plays natively on all Windows machines. |
| Capture double-press crash | Pressing Capture twice quickly no longer crashes the app. `CaptureInThread` now checks `CaptureInProgress` before spawning a new thread — previously two concurrent threads both called `CameraDevice.CapturePhotoNoAf()`, causing a device-level race condition. |

---

## Base Version

Built on top of digiCamControl source code.
Original project: https://github.com/dukus/digiCamControl
License: MIT
