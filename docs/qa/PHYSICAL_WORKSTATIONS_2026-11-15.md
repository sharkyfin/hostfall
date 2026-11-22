# Physical workstations, S02 seams and native text

2026-11-15 · Verification workbench: `.artifacts/window-mode-check` / Unity 6000.6.8f1.
Raw logs, model metrics, screenshots and test jobs: `.artifacts/workstation-20261115/`.

## Changes and findings

- S02's south shell had a 0.35 m vertical gap above the low server walls. A separate `Server-to-Sector02 Bulkhead` already occupied the upper area; it was explicitly inventoried, then replaced by contiguous upper wings/portal cap. Low wings meet the actual door jambs. The redundant server lintel was removed. Do not stack new caps over the old bulkhead.
- The armory west wall duplicated the compute vault east wall. Removed only the three duplicate wall/header objects; shared S02 headers now meet the adjacent segments without the former 0.1 m overlapping strips. Door leaf, frames and stable IDs remain.
- Twelve physical workstations now use original Blender notebook/service PC/all-in-one meshes. Existing desks, curated props, sockets and task lighting remain. macOS/Windows families use distinct chrome. No extra lights or real OS runtimes.
- Flash/cartridge and smaller USB pickups use the original ArchiveKey mesh. Interaction colliders fit visible bounds on unit-scale parents. In verification, FBX-root scale initially inflated switch colliders; door/route tests detected this and the colliders were moved onto independent unit-scale mounts. Replaced child media colliders were explicitly restored and serialized.
- A 0.45-second camera approach/return frames the actual screen. Native UI Toolkit text is clipped to the physical glass once settled. The body is hidden during close interaction and restored on return. Archive return refocuses the screen; exit pauses automatic typing without losing history/draft. No manual command field or fullscreen terminal backdrop.
- Expanded scenarios now use their real Cargo/Rbt/Sec/Neuro/Cloud/Super DeviceIds. Simulation.Screens owns bounded output, cursor/wait and idempotent reward state. Diagnosis and successful sessions have distinct resumable memories and an explicit selected session. Work-light switches persist in the world save.
- Save review found an existing campaign restoration error: the number of the first three TerminalDirector sequences was compared with the entire six-objective campaign. Restoration now verifies that prefix and restores the actual campaign count; old saves without expanded transcript fields remain accepted.

## Display policy

Fullscreen output follows the display aspect/resolution. URP renderScale limits the 3D surface to 2,073,600 pixels (Full HD equivalent); UI text remains at native output resolution. This avoids forcing 16:9 bars onto a 16:10 display. One startup migration handles the old fullscreen profile. Scene loads, menu transitions and unrelated preference changes do not reapply a resolution.

Primary API references consulted during implementation: [URP render scale](https://docs.unity3d.com/6000.0/Manual/urp/universalrp-asset.html), [FullScreenWindow](https://docs.unity3d.com/6000.0/ScriptReference/FullScreenMode.FullScreenWindow.html), [beginContextRendering](https://docs.unity3d.com/6000.0/ScriptReference/Rendering.RenderPipelineManager-beginContextRendering.html). These are editor references, not game UI links.

## Verification status

Implementation-stage full Play Mode run: **74/74 PASS**, including screen focus/synchronisation, archive return, S02 cartridge route, both stairs and 36 door crossings. API compilation and 33 pure simulation checks passed. Final checks are recorded below. This is not a manual playthrough of the whole campaign.


## Final tests and native review

- Unity 6000.6.8f1: **80/80 Edit Mode**, **75/75 Play Mode PASS**. The Play run includes the 74 repository tests plus one verification-copy-only test that loads the recovered real campaign through FacilityRoot and saves a private manual slot. That local test is not shipped in the source project. Full run: 89.32 s.
- **33/33 pure Simulation PASS**, API compilation PASS (one existing Cinemachine nullability warning, zero errors).
- Actual standalone captures of DATA-01, PWR-01 notebook, NEURO-01 all-in-one and CARGO-01 were visually inspected. Final output 3584×2240, system 3584×2240, FullScreenWindow, renderScale 0.5082232. Screen bounds remain on glass. Early captures exposed reversed imported corner handedness and a cached 1920×1080 profile: both were fixed and recaptured. The one-time migration now runs after the first frame, using Display.main.systemWidth/systemHeight.
- Native AMD Radeon Pro 5300M, 540 measured samples across six views, VSync disabled for profiling. 1920×1080 before medians 6.902/6.129 ms; after 6.273/6.001 ms. Final native-text output: median **7.852 ms**, p95 **9.418 ms**, median draw calls 1589, median managed allocation 1632 bytes/frame. Native UI has a measurable cost compared with the smaller output; there is no claim of zero overhead or constant FPS throughout the campaign. Default VSync remains enabled. GPU Resident Drawer/occlusion remain disabled.
- Source scene hash checked before transfer: `06fd575b16dd232c5a7547874523733f375212623e5c0ed8e3c90574286ab706`. Repaired scene and Atelier assets were transferred selectively; source scripts matched the tested copy. Promoted scene hash: `ea9655aa368a52f4491d6eeeeacab14eaa79f5b44e1070ff9c5468a04b8113a8`. Original scene backup remains in the task artifacts. No broad regeneration of the author's world.

## Save isolation incident and recovery

The first normal standalone diagnostic runs exposed an existing isolation bug: FacilityRoot honoured TestStorageDirectory only in batch mode, while the native replay set it during a normal launch. Consequently those runs overwrote the real auto slot with a fresh diagnostic world. Quick and manual saves were not overwritten. This was reported to the user when discovered. All current and damaged files were backed up before repair.

FacilityRoot now honours an explicit private directory in normal and batch launches. Development builds additionally use the separate `com.northline.hostfall.workstationqa` preference domain. A repeated native screen/profile run verified **every real save hash unchanged** (`native-user-save-isolation.json`). Earlier domain backups concerned the legacy `unity.NORTHLINE.HOSTFALL` domain, not current Screenmanager keys; they are not evidence that all modern preferences were restored. The production 1920×1080 saved profile remains eligible for startup migration version 2. Other settings are not replaced from an old preference snapshot.

A checksum-valid November 14 backup, archived as `world-v2.json.damaged-20261115-090214-711717`, contains three completed objectives and the player at DATA-01 (-2.107, 0.035, 25.698). It was rejected because Unity materializes an absent optional weapon roster as a zeroed object. Restore now accepts only that empty legacy shape, while malformed current rosters still fail. Slot listing also infers the six-objective campaign instead of forcing the first three objectives.

The recovered backup passed envelope checksum, domain, field-progress and actual scene-load/save checks. Its canonical form was restored into auto and free manual slot 3; quick hashes are unchanged. `recovery-delivered.json` records hashes and retained backups. **The exact auto snapshot immediately before the diagnostic runs is unavailable**; recovery is from the validated DATA-01 backup, not a claim of lossless recovery of all later play.


## Release and delivery

Native x86_64 release: **Succeeded, 0 errors**, 162,489,923 build bytes, native output enabled. Bundle ID `com.northline.hostfall`; development replay/diagnostic entry points are not compiled into release. `codesign --verify --deep --strict` PASS. A 12-second headless release smoke stayed alive, terminated cleanly with exit 0, logged no checked script/shader/load errors and left all user save hashes unchanged. This smoke is not a graphical playthrough; graphical proof comes from the native development captures above.

Delivered as `apps/unity/Builds/macOS/HOSTFALL-Workstations.app`; the existing `HOSTFALL-Robots.app` path is updated to the same build. Previous Robots bundle retained under task artifacts. Repository source/meta integrity: **636 asset GUIDs PASS**, legacy reference unchanged. No commit or publication performed.


## Handoff check — 2026-11-16

Both delivered bundles still match the tested release byte-for-byte and pass signature verification. The main scene matches the promoted hash. Current auto has advanced to four objectives and has a valid envelope checksum; manual slot 3 retains the recovered three-objective backup. This check only read saves and did not overwrite later play. Receipt: `handoff-check-20261116.json`.
