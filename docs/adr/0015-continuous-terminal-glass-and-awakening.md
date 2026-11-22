# 0015 — Continuous terminal glass and a new-campaign awakening

2026-11-16 · Accepted; final verification 2026-11-17.

## Context

The physical TextMesh summary and native focused overlay shared a model but used different titles, wrapping, truncation, style and layout. Entering a computer visibly replaced its contents. The requested opening should reveal the actual player in the first room, with confusion about the incident; old saves must continue normally. The corridor WATCHER should initially be behind a closed door.

## Decision

- A single bounded NearbyTerminalDisplay panel paints the actual terminal DOM onto one 2048×1142 sRGB RenderTexture with a fixed 1536×856 logical canvas and a depth-tested physical quad. Within 5 m the visible workstation is selected; focused interaction retains the same surface/DOM/texture throughout approach, session and return. Once focused, the exact same texture is composited at native output inside the glass bounds for clarity; there is no second text view or fullscreen window. Distant labels remain a reduced representation of the same memory, not a separate script. Text still comes from TerminalMemory/ScenarioPlayback, including partial draft and selected diagnostic session. Walls occlude the screen. No camera per terminal or twelve constantly updating panels.
- A ten-second OpeningSequence reveals the existing body and settles at the authored spawn/eye, with offline synthesized Russian voice and subtitles. It pauses the world in ControlMode.Cinematic, supports skipping, then hands off to the existing controls introduction. Only a new unsaved campaign requests it; loaded saves bypass it without moving their player or changing progress.
- Add the saved `door.soc.corridor` using the existing airlock model. It starts closed and locked until WS-07 completes, then E opens it; the EDGE-01 needed for later progression remains reachable. Old door IDs and old save entries stay intact.
- Four restrained paper warnings mount on walls along the opening route. Panic slogans express the absent staff's fear, without revealing the protagonist's nature or rewriting Super's canonical motivation.

## Consequences

The near/focused terminal is rendered as world geometry and has normal occlusion, rather than native overlaid text. A single offscreen panel has an explicit memory/render budget; native visual and frame-time checks remain required. Existing Field/terminal memory and save schema are preserved. No new commands, real OS runtime, arbitrary player code, lights or external voice services.

Verification: 77 Play / 80 Edit / 33 Simulation PASS, actual native captures and bounded profiling. See `docs/qa/AWAKENING_AND_TERMINAL_SYNC_2026-11-16.md`.
