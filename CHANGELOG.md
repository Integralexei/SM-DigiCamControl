# Changelog

All notable changes to the Stop Motion Edition are documented here.

---

## [Unreleased]

---

## [1.1.0] — 2026-03-01

### Added
- **Motion Guides** — Quadratic Bezier arc overlays on the live view canvas.
  - 3-click drawing flow: click to set start point, click to set end point, move mouse to bend arc, click to confirm.
  - Live preview while bending: orange control point dot + dashed handle lines.
  - Right-click cancels drawing at any step.
  - Guides are saved per-camera as JSON in `DataFolder/MotionGuides_{CameraName}.json`.
  - Clear all guides command (`ClearGuidesCommand`).
  - Remove last guide command (`RemoveLastGuideCommand`), disabled when list is empty.
  - Guide coordinates stored as normalized [0, 1] values — resolution-independent.

---

## [1.0.0] — 2026-03-01

### Added
- **Onion Skin** — Semi-transparent ghost overlay of surrounding frames on the live view.
  - Configurable back frame count (0–5) and forward frame count (0–5).
  - Opacity slider (0–100%).
  - Anchor frame: always the last frame in the session.
  - Rendered as a frozen `WriteableBitmap` composited by WPF GPU — no per-frame CPU cost.
  - Image cache rebuilt only when the anchor frame changes; I/O runs outside the cache lock.
  - Fallback: loads from original photo path if thumbnail is missing.
  - UI: collapsible sidebar card with sliders and back/forward counters.
