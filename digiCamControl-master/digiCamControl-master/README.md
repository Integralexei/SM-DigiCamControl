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
| English UI in Live View | partial | ✓ |

See [CHANGELOG.md](CHANGELOG.md) for a detailed history of changes.

---

## Added Features

### Onion Skin
Overlays up to 24 previous and/or next frames as semi-transparent ghost images on top of the live view. Each ghost is progressively more transparent — the nearest frame is most opaque, the farthest is nearly invisible. Helps animators judge movement across multiple frames at once.

- Back/forward frame count: 0–24 each, controlled by slider and numeric input
- Opacity slider (0–100%)
- **Anchor**: when Insert Mode is ON, ghost frames are counted relative to the currently selected (orange) frame; otherwise anchored to the last captured frame
- Rendered via WPF GPU compositing — zero per-frame CPU cost
- Cache is rebuilt only when anchor or frame count changes
- Fallback: loads from original photo path if thumbnail is missing
- Lens distortion correction (barrel/pincushion, ±50 units)

### Motion Guides
Quadratic Bezier arcs drawn as canvas overlays on the live view. Used to plan and visualize character movement paths.

- 3-click drawing flow: set start → set end → bend arc
- Live arc preview while bending (orange control point + dashed handles)
- Right-click cancels at any step
- Guides saved per-camera as JSON in `DataFolder/MotionGuides_{CameraName}.json`
- Commands: clear all guides / remove last guide

### Insert Mode
Controls where newly captured frames are inserted in the session filmstrip.

- **ON (default)**: each new frame is inserted immediately after the currently selected (orange) frame in the filmstrip
- **OFF**: new frames are appended at the end
- Selecting a frame in the filmstrip sets it as the insert point (orange border); clicking it again deselects
- Onion Skin automatically re-anchors to the selected frame when Insert Mode is ON

### Review Mode (no camera)
Allows opening the Live View window even when no camera is connected, so you can inspect the Onion Skin overlay across existing session frames.

- Live View button is enabled when the session contains at least one frame
- The live view feed is blank, but the Onion Skin overlay remains fully functional
- Useful for reviewing animation timing without a connected camera

---

## Bug Fixes vs. Original

| Fix | Description |
|---|---|
| Sidebar layout collapse | Opening Motion Guides expander no longer hides NumericUpDown controls or the Capture/Confirm buttons. Root cause: outer column `Width="Auto"` replaced with `Width="*"`. |
| Image Sequencer lag | Reduced lag in the image sequencer panel. |
| ffmpeg not found | Fixed error when ffmpeg is not in PATH. |

---

## Base Version

Built on top of digiCamControl source code.
Original project: https://github.com/dukus/digiCamControl
License: MIT
