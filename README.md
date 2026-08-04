# markup-shot

**Windows screenshot annotation & markup — capture, mark up, and share in seconds.** Offline & privacy-first.

## Overview

markup-shot is a lightweight Windows desktop tool for annotating screenshots and images. Capture a region of your screen (or open an existing image), then add arrows, boxes, ellipses, freehand ink, text callouts, highlights, blur/pixelate redaction, and auto-numbered step badges. When you're done, copy the result straight to your clipboard or save it to disk.

Everything runs **locally** — no cloud upload, no account, no telemetry. An optional local-AI mode can suggest regions to redact (faces, emails, keys) using a tiny vision model via Ollama/llama.cpp, but the core tool works fully without it.

## Motivation

Explaining a bug, writing docs, or giving design feedback almost always means marking up a screenshot. The built-in Windows tools are minimal, and the popular annotation apps push you toward cloud accounts and uploads. markup-shot keeps the fast "capture → annotate → paste" loop entirely on your machine, with the redaction tools you actually need when sharing anything sensitive.

## Use cases

- **Bug reports & PRs** — box the broken element, arrow to it, add a text note, paste into GitHub.
- **Docs & tutorials** — drop auto-numbered step badges (1, 2, 3…) onto a workflow screenshot.
- **Design feedback** — highlight regions and scribble notes over a mockup.
- **Sharing safely** — blur/pixelate account numbers, tokens, emails, and faces before you send.
- **Quick crops & callouts** — trim an image and emphasize one part without opening a full editor.

## How to use (Windows quickstart)

> Status: early scaffold. Build steps below describe the target workflow.

1. Download the latest portable `markup-shot-win-x64.zip` from Releases (or build from source — see below).
2. Unzip and run `MarkupShot.exe`. It lives in the tray.
3. Press the global hotkey (default **Ctrl+Shift+M**) to start a region capture, or open an existing image with **File → Open** / drag-and-drop.
4. Pick a tool from the toolbar (arrow, box, ellipse, ink, text, highlight, blur, step badge), draw on the canvas, and adjust color/stroke/size.
5. **Copy** (Ctrl+C) sends the annotated image to the clipboard, or **Save** (Ctrl+S) writes a PNG/JPG.

### Build from source

```powershell
# Requires .NET 8 SDK on Windows 10/11
git clone https://github.com/rwrife/markup-shot.git
cd markup-shot
dotnet build -c Release
dotnet run --project src/MarkupShot
```

## Example workflow

1. Ctrl+Shift+M → drag a rectangle over the broken dialog.
2. Click **Box**, draw a red rectangle around the misaligned button.
3. Click **Arrow**, point from your text note to the button.
4. Click **Blur**, drag over the email address in the header.
5. Ctrl+C → paste directly into a GitHub issue comment. Done.

## Local-AI integration (optional)

markup-shot can optionally connect to a local model server (Ollama or any llama.cpp OpenAI-compatible endpoint) for **smart redaction suggestions** — it proposes bounding boxes over likely-sensitive content (faces, emails, API keys, account numbers) that you accept or reject before applying blur.

- Works with tiny local vision models (MiniCPM-V class) and small text models for OCR-then-classify pipelines.
- Fully **off by default**. The endpoint is configurable; a reachability probe runs first and the app degrades gracefully to manual redaction if no model is available.
- Only the captured image (or crops of it) is sent to your **local** endpoint — never to the cloud.

## Current status / milestones

- [ ] M1 — Core canvas + open/save image, clipboard copy
- [ ] M2 — Annotation tools (arrow, box, ellipse, ink, text, highlight)
- [ ] M3 — Region screen capture + global hotkey + tray
- [ ] M4 — Redaction (blur/pixelate) + auto-numbered step badges
- [ ] M5 — Undo/redo, settings persistence, portable packaging
- [ ] M6 — Optional local-AI smart redaction

See [PLAN.md](PLAN.md) for scope, architecture, and packaging details.

## License

MIT
