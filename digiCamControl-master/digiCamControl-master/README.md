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
| Filmstrip touchpad scroll | — | ✓ |
| Capture hotkey (Ctrl+Enter) | — | ✓ |
| English UI in Live View | partial | ✓ |

See [CHANGELOG.md](CHANGELOG.md) for a detailed history of changes.

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
- Lens distortion correction (barrel/pincushion, ±50 units) — applied once at cache build time

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
- Tooltip: *"Insert Mode — new frames are inserted before the selected frame (orange marker) instead of being appended at the end"*
- Onion Skin automatically re-anchors to the frame before the selected frame when Insert Mode is ON

### Review Mode (no camera)
Allows opening the Live View window even when no camera is connected, so you can inspect the Onion Skin overlay across existing session frames.

- Live View button is enabled when the session contains at least one frame
- The live view feed is blank, but the Onion Skin overlay remains fully functional
- Useful for reviewing animation timing without a connected camera

### Filmstrip Touchpad Scroll
The filmstrip (horizontal frame strip below the live view) can be scrolled with a touchpad left/right swipe, in addition to the mouse wheel.

- Handles `WM_MOUSEHWHEEL` (Windows horizontal wheel message) via `HwndSource` hook
- Works with modern precision touchpads and horizontal scroll wheels
- Vertical mouse wheel continues to scroll horizontally as before

### Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| **Ctrl+Enter** | Capture (same as the Capture button in Live View) |

---

## Bug Fixes vs. Original

| Fix | Description |
|---|---|
| Sidebar layout collapse | Opening Motion Guides expander no longer hides NumericUpDown controls or the Capture/Confirm buttons. Root cause: outer column `Width="Auto"` replaced with `Width="*"`. |
| Image Sequencer — video not created | `GenerateMp4()` passed two separate `-vf` flags to ffmpeg; ffmpeg 3.x+ treats this as an error and exits without producing output. Fixed by merging both filters into a single `-vf fps=25,scale=W:H` chain. Paths are now also quoted to handle spaces in usernames or session names. |
| Image Sequencer — 4K codec | 4K preset switched from `libx265` (H.265) to `libx264` (H.264). H.265 requires a paid Windows codec extension; H.264 plays natively on all Windows machines. |
| Image Sequencer lag | Reduced lag in the image sequencer panel. |

---

## Base Version

Built on top of digiCamControl source code.
Original project: https://github.com/dukus/digiCamControl
License: MIT
