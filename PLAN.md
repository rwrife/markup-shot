# markup-shot — Project Plan

## Scope

A focused Windows 10/11 desktop app for **annotating screenshots and images**. In scope:

- Region screen capture (multi-monitor, DPI-aware) and open/drag-drop of existing images.
- Annotation tools: arrow, line, rectangle, ellipse, freehand ink, text callout, highlighter, blur/pixelate redaction, auto-numbered step badges.
- Non-destructive, layer/object-based editing with per-object select/move/resize/delete and undo/redo.
- Output: copy annotated image to clipboard; save as PNG/JPG; optional crop.
- Global hotkey + system tray for a fast capture → annotate → paste loop.
- Settings persistence (hotkey, default colors/stroke, save folder) under `%APPDATA%\markup-shot`.
- Optional local-AI smart-redaction suggestions via a local OpenAI-compatible endpoint (off by default).

## Architecture / tech approach

- **Language/runtime:** C# on .NET 8.
- **UI:** WPF (MVVM). A `Canvas`/`InkCanvas`-based editing surface renders annotation objects as an ordered display list so each stays individually editable.
- **Core library (`MarkupShot.Core`):** UI-free, unit-testable. Owns the annotation object model (`IAnnotation` implementations), the document/layer model, hit-testing, undo/redo command stack, and image compositing/export (`System.Drawing`/`SkiaSharp` for rasterizing final output).
- **Capture:** full-screen, per-monitor Per-Monitor-V2 DPI-aware overlay window with rubber-band selection; screen pixels grabbed via `Graphics.CopyFromScreen`/BitBlt into a bitmap handed to the editor.
- **Hotkey/tray:** Win32 `RegisterHotKey` for the global capture hotkey; `NotifyIcon` tray host.
- **Redaction:** blur/pixelate applied to selected regions; box-blur/mosaic filters in `MarkupShot.Core` so they're testable and deterministic.
- **Clipboard:** set image data via WPF `Clipboard.SetImage` (with DIB fallback for compatibility).
- **Settings:** JSON persisted under `%APPDATA%\markup-shot`.
- **Optional local-AI:** `IRedactionAdvisor` abstraction with an implementation that calls an Ollama/llama.cpp OpenAI-compatible vision endpoint; reachability probe, timeout, and graceful fallback to manual redaction. Only crops/images go to the **local** endpoint.
- **Testing:** xUnit against `MarkupShot.Core` (annotation model, hit-testing, undo/redo, blur filters, export compositing, GFM-independent serialization of a saved-project format).

## Milestones

1. **M1 — Canvas & I/O:** editing surface, open/drag-drop image, save PNG/JPG, copy to clipboard, undo/redo skeleton.
2. **M2 — Annotation tools:** arrow, line, rectangle, ellipse, freehand ink, text callout, highlighter; color/stroke controls; per-object select/move/resize/delete.
3. **M3 — Capture & tray:** DPI-aware multi-monitor region capture overlay, global hotkey, tray host, settings persistence.
4. **M4 — Redaction & steps:** blur/pixelate redaction tool + auto-numbered step badges.
5. **M5 — Polish & packaging:** undo/redo hardening, save-project format, portable self-contained win-x64 zip, CI on windows-latest.
6. **M6 — Optional local-AI:** smart-redaction suggestion pane with accept/reject, endpoint config, probe + fallback.

## Non-goals

- No cloud sync, hosted sharing, accounts, or telemetry.
- Not a full raster/vector editor (no filters/layers-compositing beyond what redaction/annotation need).
- No mandatory AI — local-AI is strictly optional and off by default.
- No macOS/Linux builds in the initial scope (Windows-first).
- No video/GIF capture in initial scope.

## Packaging target for Windows

- Primary: **portable self-contained `win-x64` zip** (no install, no runtime prerequisite).
- Secondary: **MSIX** installer for Start-menu integration and clean updates.
- CI builds/tests on `windows-latest` via GitHub Actions; release artifacts attached to tagged releases.
