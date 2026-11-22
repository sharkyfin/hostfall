# HOSTFALL 2.0 — audit, 2026-11-17

Actual scene: NorthlineFacility.unity, Unity **6000.6.8f1**, URP 17.6 / Forward+, Input System 1.20.1, Cinemachine package 6.6.0, UI Toolkit. One saved gameplay scene; Runtime, no-engine Simulation, Editor and two test assemblies. Native C++ and legacy React remain independent; neither runs inside Unity. Web build target already exists and remains supported.

Recoverable pre-refactor checkpoint: `.artifacts/hostfall2-20261117/source-checkpoint.tar.gz` (3,722 source files, dirty and untracked included), SHA manifest, initial git status and `head.bundle`. Nothing committed. The master request is preserved beside this document.

## Actual inventory / findings

- **13 interacting computers**: three ComputerTerminal, ten DoorTerminalConsole; twelve WorkstationSurface displays. HOME-PC is the thirteenth and lacks a working surface. HOME-PC and SOC WS-07 both use Ws07: opening home can choose the wrong physical display. New station IDs must distinguish them without rewriting legacy save identities.
- Authored shelter exists at X -5..5 / Z -19..-9, HOME-PC at (-2.6,0,-16.5). Actual current spawn is (-2.65,.08,-1.7), in SOC, not home. Home has bed/workbench and a real unlocked blast door. Move only the new-campaign authored spawn; old v2 slots stay intact.
- 19 saved doors; 13 WatcherController actors plus specialised controllers. Raw station paths, exact poses, access conditions and actor IDs: `.artifacts/hostfall2-20261117/scene-audit.json`. Multi-angle home/SOC images inspected. This is inventory, not a new geometric clearance certification.
- Existing Home computer prints Type-IV information prematurely and runs t00_home commands automatically. Both are incompatible with the new reveal.
- TerminalDirector/ScenarioPlayback actively type/solve all screens. Useful TerminalMemory, VirtualComputer, save envelopes, screen corners, camera, doors and typed IDs can be retained. Automated UI and its input/wait/reward ownership must leave the active route.
- VFS already supports canonical paths, bounded files and persistent terminal memory; shell lacks ls options, less, grep, find, help-command and completion. Process/service mutation belongs to the old route and is not needed by the new home shell.
- 96 knowledge articles are still in Resources/FieldContent. Preserve source data; remove global K/archive entry in modern gameplay and replace the home guide with nine focused terminal-help chapters.
- World-v2 has strict route/three-virtual-host validation and six mission objectives. New campaign requires explicit v3 state and separate v3 slots, not silent reinterpretation of old objective counts.
- ControlState pauses every mode except Exploring; several AI controllers also gate on CanExplore. External desktop must use a live Computer mode while player movement/fire stays disabled. Paused/menu modes must still freeze the world.
- Existing weapons already cover heat pistol (CIRRUS), charged coil weapon (TITAN), shotgun (ARC), EMP, real-body IK/motion and dropped weapon persistence. Improve these rather than add parallel duplicates.
- Existing A* has a shared budget and real floor checks; armour is bone-authored. Keep these safeguards during tactical AI changes. GPU resident/occlusion stay off until new profiling demonstrates an accepted benefit.

## Tools

Unity MCP verified: window-mode-check@e864613ac9f05ec9 (verification copy, no user editor connected at audit start). Blender MCP answered: Blender 4.5.16 LTS, protocol16 current, local scripting available; paid generators/library accounts off. No downloads required to begin. Unity Test Framework is installed. Prior 77 Play / 80 Edit / 33 model checks are a baseline, not acceptance of 2.0.
