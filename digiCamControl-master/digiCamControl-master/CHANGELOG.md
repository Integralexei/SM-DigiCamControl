# Changelog

All notable changes to the Stop Motion Edition are documented here.

---

## [Unreleased]

---

## [1.4.0] — 2026-03-17

### Fixed — Onion Skin reliability (was silently broken)
- **Deadlock bug**: if any exception fired during frame loading or compositing, `_onionCacheBuildingKey` was permanently stuck, preventing all future cache rebuilds. Fixed with `try-finally` that always resets the key.
- **Stuck-null bug**: if all frames failed to load, the cache key was marked as "done" even though no bitmap was built, blocking all retry attempts. Fixed: key is only committed when a valid composed bitmap exists.
- **Publish race**: `_onionCacheBuildingKey` was checked outside the lock after being set inside. Fixed with a local `bool myBuildSucceeded` set inside the lock.
- **`InvalidateOnionCache` loop**: was clearing only 5 of 24 cache slots. Fixed to clear all 24.

### Changed — Onion Skin anchor
- Selecting a frame in the filmstrip (click = orange border = `IsInsertPoint`) now immediately invalidates and rebuilds the onion skin cache, so the ghost updates without waiting for the next timer tick.
- Anchor was already tied to `IsInsertPoint` when Insert Mode is ON — this change makes the response instant.

---

## [1.3.0] — 2026-03-17

### Added — Onion Skin expanded to 24 frames
- Back/forward frame count raised from 5 to 24.
- Added a slider for back frame count (0–24) in the Onion Skin sidebar card.
- Added a slider for forward frame count (0–24) in the Onion Skin sidebar card.
- NumericUpDown maximum updated to 24 for both back and forward.
- ViewModel array sizes and all loop bounds updated from 5 to 24.

---

## [1.2.0] — 2026-03-17

### Added — Review mode (Live View without camera)
- The Live View button is now enabled when the session contains at least one captured frame, even if no camera is connected.
- When in review mode (no live feed), the Onion Skin overlay continues to update so you can inspect animation timing without a camera.

### Fixed — Sidebar layout collapse
- Opening the Motion Guides expander no longer causes NumericUpDown controls in other panels (Onion Skin) to disappear, and no longer hides the Capture and Confirm buttons.
- Root cause: outer sidebar column had `Width="Auto"`, which passed an infinite measure constraint through the ScrollViewer, collapsing Star-sized columns to zero. Fixed by changing to `Width="*" MaxWidth="350" MinWidth="250"`.

### Changed — English UI in Live View
- All Russian-language labels and instructions in the Live View sidebar have been translated to English:
  - "Предыдущих кадров:" → "Back frames:"
  - "Следующих кадров:" → "Forward frames:"
  - "Удалить последнюю" → "Remove last"
  - Motion Guides drawing instruction translated to English
  - "После кадра: #" → "After frame: #"

---

## [1.1.0] — 2026-03-01

### Added — Motion Guides
- Quadratic Bezier arc overlays on the live view canvas.
  - 3-click drawing flow: click to set start point, click to set end point, move mouse to bend arc, click to confirm.
  - Live preview while bending: orange control point dot + dashed handle lines.
  - Right-click cancels drawing at any step.
  - Guides are saved per-camera as JSON in `DataFolder/MotionGuides_{CameraName}.json`.
  - Clear all guides command (`ClearGuidesCommand`).
  - Remove last guide command (`RemoveLastGuideCommand`), disabled when list is empty.
  - Guide coordinates stored as normalized [0, 1] values — resolution-independent.

---

## [1.0.0] — 2026-03-01

### Added — Onion Skin
- Semi-transparent ghost overlay of surrounding frames on the live view.
  - Back frame count (0–5) and forward frame count (0–5).
  - Opacity slider (0–100%).
  - Anchor frame: last captured frame (or the `IsInsertPoint` frame when Insert Mode is ON).
  - Rendered as a frozen `WriteableBitmap` composited by WPF GPU — no per-frame CPU cost.
  - Image cache rebuilt only when the anchor frame changes; I/O runs outside the cache lock.
  - Fallback: loads from original photo path if thumbnail is missing.
  - Lens distortion correction (barrel/pincushion, ±50 units, applied once at cache build time).
  - UI: collapsible sidebar card with opacity/lens sliders and back/forward counters.

### Added — Insert Mode
- New sidebar card "Insert Mode" in the Live View window.
- When ON (default), each captured frame is inserted immediately after the currently selected frame in the filmstrip instead of being appended at the end.
- Selected frame is indicated by an orange border in the filmstrip; clicking a frame sets it as the insert point, clicking again deselects.

### Fixed — Image Sequencer lag
- Reduced lag when scrolling or selecting frames in the image sequencer panel.

### Fixed — ffmpeg not found
- Fixed crash/error when ffmpeg binary is not in the system PATH.
