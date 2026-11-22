# Combat workshop acceptance — 2026-11-14

## Changes and concrete defects found

- A shotgun pellet previously inflicted the same damage as the complete blast.
  ARC-12 now resolves eight individual hits (10 base damage each); CIRRUS uses
  24 damage and TITAN 95. Aim comes from the camera centre before recoil.
- A nonfatal hit previously selected TurnOff. WATCHER now reserves shutdown for
  death/power state; hits alert it without a false death animation. Hits no longer
  teleport the player back to a post. Patrol health is 260; sensors multiply damage
  by 2.2, drives take 0.65 and progressively reduce mobility.
- Local collision/floor-checked search replaces movement that merely stopped at
  obstacles. A found endpoint tolerance mismatch prevented SENTRY from reversing
  its patrol: the path discarded a waypoint at 0.15m while patrol required 0.1m.
  Final waypoint tolerance is now 0.06m. Both directions pass in the real scene.
- Four extra autonomous patrols have stable aux-watch-01..04 IDs, floor/clearance
  checked spawn points and entry-facing orientation. Existing devices are retained.
- Reactor and freight crawlers previously shared one ID. They now have independent
  saved damage/poses and IDs. MANTIS-6 is an original 141-part Blender mesh with
  tracks, wheels, armour, cooling reservoirs, optics and a rear core/hatch.
  Hull damage alone cannot destroy the sealed core. Both reservoirs must fail;
  the opened rear core takes increased damage. EMP, drives, attack telegraph,
  wall occlusion, pause and collision-aware turns are represented explicitly.
- Bounded, non-colliding armour fragments detach at damage thresholds. Coolant
  glass disappears with destroyed reservoirs; death disables hit volumes/lights
  and emission. Partial damage, modules, pose and AI timers are saved.
- Three different authored Blender weapons replace one shared block model.
  Owned weapons, active choice and ammunition restore with the slot.
- Health is independent at bottom-left; radar/location remain top-left. A solid
  4px dot is centred, hidden with modal HUD. No enemy health widgets were added.
- S02 warped hanging cable meshes were replaced by narrow conduits/brackets and
  upper finishing caps. At S06/SEC07 the duplicated foyer shell was removed;
  the 6m structural aperture is now filled up to the actual 3.55m door frame.

## Executed verification

- Unity 6000.6.8f1, isolated window-mode-check project, explicit MCP instance.
- Play Mode **56/56 PASS**, latest run: `play-release.xml`. Covers real scene
  reload, sustained damage/death, weakpoint exposure, damaged save validation,
  sorted shots blocked by walls, no corpse hit volumes, real route around cover,
  guardian footprint rejecting a turn into a wall, pause, HUD centre/layout,
  upper S02 shell and SEC07 shoulder gaps, plus existing world/map/terminal tests.
- Edit Mode **67/67 PASS**, `edit-release.xml`.
- Independent Simulation behavioral checks **33/33 PASS**.
- API compilation PASS (existing Cinemachine nullable warning only).
- Repository source/meta integrity PASS, **555 asset GUIDs** before final promotion.
- Main editor external-change dialog was resolved through native computer use
  with Reload. The main MCP then confirmed NorthlineFacility loaded, isDirty=false,
  and zero console error entries.
- Original main scene hash matched its captured backup before promotion. No full
  FacilityBuilder rebuild; existing author assets/ProBuilder package retained.
- Actual Blender multi-angle and Unity Camera.Render images inspected. Model
  spawns were corrected after the first guardian view showed reactor equipment
  obscuring it. UI panel capture verifies the separate health and centre dot.
- Native x86_64 desktop build **Succeeded, 0 errors**; signature and smoke results
  PASS (`codesign --verify --deep --strict`). A real standalone process ran for
  12s with -batchmode -nographics, then exited cleanly on controlled termination
  (code 0), with no script/type/load exceptions. Reflection-probe HDR warnings
  in no-graphics mode are expected; this is not a native-window visual playtest.
  Bundle: `apps/unity/Builds/macOS/HOSTFALL-Combat.app`, 155,617,877 build bytes.

Evidence: `.artifacts/combat-rework-20261114/` (XML, compilation, promotion hash,
HUD, Blender snapshot/result); `.artifacts/world-workbench/combat/` (Unity renders
of S02, SEC07, guardian, patrol facing and all three weapons).
Authoring source: `tools/blender/make-combat-models.py`; decision: ADR 0010.

## Limits

These are automated behavioral/physics checks and actual editor renders, not a
manual full campaign playthrough. Other specialised archetypes retain their
legacy armour/phase controllers. Local bounded search does not infer locked-door
keys or navigate between floors. No claim of a fully bug-free world or a user
playtest/A-B study. No external cloud generator or paid asset dependency added.
