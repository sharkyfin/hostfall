# 0014 — Physical workstations and a separate text/output budget

2026-11-15 · Accepted and verified.

## Context

Full HD forced into a 16:10 fullscreen output produces bars and upscaled text. Rendering the entire world at Retina resolution would undo the performance gain. Terminal UI also covered the world with an unrelated fullscreen window, while later authored sessions lost partial output on exit. Several expanded scenarios still reused Ws07/Core01 IDs.

## Decision

- Fullscreen output uses the display's aspect/resolution; URP renderScale caps 3D to 1920×1080 equivalent pixels. Overlay UI stays native. A one-time startup migration handles the old fullscreen profile; scene loads and unrelated preference changes never reset the window.
- Physical screen corners define the camera pose. The camera approaches/returns over 0.45 seconds, using unscaled time while the world is paused. The existing UI Toolkit transcript is clipped to the actual glass only after the camera settles; there is no separate fullscreen terminal backdrop. The actual body is hidden during this close interaction, then restored.
- TerminalDirector remains the owner of the first three sessions. Expanded authored sequences use Simulation.Screens / ScenarioPlayback, with bounded TerminalMemory and persisted cursor/wait/completion. UI presents and ticks the selected model; completion reward can be claimed once. Correct expanded DeviceIds identify each physical workstation.
- Original Blender notebook, all-in-one, service PC, USB/cartridge and switch panel use shared materials, no extra lights and no runtime mesh baking. Small panels toggle existing task lights; switch states are part of Simulation/save data.
- Repair S02 shared walls locally in a backup-protected verification scene. Keep all device/door/robot IDs, routes and authored progression.

## Consequences

The nominal 3D pixel budget is Full HD, rather than a forced 16:9 output raster on every display. Native text still has a cost; actual native profiling is required and no universal FPS guarantee follows from pixel count alone. Old saves without screen/switch fields remain valid. These are fictional operating-system presentations, not installed OS runtimes. Do not restore manual terminal input or external tutorial links.

Verification: 80 Edit / 75 Play (including one local real-backup check) / 33 Simulation PASS.
Native release and limitations: `docs/qa/PHYSICAL_WORKSTATIONS_2026-11-15.md`.
