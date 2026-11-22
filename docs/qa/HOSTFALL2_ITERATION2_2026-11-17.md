# Iteration2 — computer world integration (in progress)

Full master gate is **not accepted**. This report records concrete app/mechanism work,
not completion of all twelve external computers, story or Recovery Protocol.

## PAYLOAD RUNNER / XP

- Shared retained desktop/window manager now accepts typed application definitions;
  per-station windows, focused app, position and minimization remain in campaign save.
- Original Luna-inspired UI has a functional taskbar, close/minimize/drag controls,
  file manager/readme/maintenance log and the actual platformer. No proprietary artwork
  or OS installation is used. The nearby and focused monitor still share one DOM/RT.
- Pure 120 Hz platform rules: three levels, acceleration/braking, buffered/coyote jumps,
  gaps, moving platform, conveyor, patrol sentinel, periodic lasers, three packet
  pickups and a checkpoint. Level3 shows memory silhouettes and corrupted messages.
- Tests use real input sequences to finish all levels; they do not set stage/score.
  Retry keeps checkpoint/packets and earned levels; partial physics replays exactly.
- App clock stops when minimized/away; world clock continues independently.

## Actual physical bindings

`logistics.lift`, `logistics.conveyor`, `logistics.bridge`, `logistics.alarm` map to a
real freight lift, two moving Quaternius crates, a hinged bridge with collision/rails,
and a spatial warning tone/red light. Final stage unlocks `door.sec06.robo` and writes
`~/archive/payload_transfer.log` once. The original mezzanine slab is preserved but
its renderer/collider are disabled; four replacement slabs leave actual lift clearance.
The bridge connects the north mezzanine with a separate landing. A CharacterController
crossed the deployed span. Lift movement carries a standing passenger through ascent
and descent; save synchronizes the platform/passenger pose before capture.

Campaign-owned bounded clocks determine lift/belt/bridge/alarm poses after load. Null
new fields in an older iteration1 v3 snapshot default to untouched Payload/logistics;
v2 files remain separate. Reward/action/message consistency is validated on restore.

Author in the isolated Editor copy with `HOSTFALL/2.0/Author Payload Mechanisms` after
`Author First Loop Preview`; do not recreate the original source scene. Scene backup:
`.artifacts/hostfall2-20261117/scene-before-payload.unity`.

## Evidence

- `simulation-games.txt`: 47/47 pure checks passed.
- `api-games.txt`: source API compilation passed; one existing Cinemachine nullable warning.
- 8/8 computer PlayMode integration checks passed in job
  `787866e31a1d47d4982e8a8a7de5ff29` (19.679 s). This includes actual CC bridge passage,
  a full lift cycle, desktop progression and JSON save read. A further raw-keyboard
  input/freeze check was then added; full regression result is tracked below.
- `payload-xp-desktop.png`: actual rendered physical-panel texture inspected.
- `hf2-payload-mechanisms.png`: actual scene render inspected.
- `user-saves-after-payload.json`: all 11 production save hashes unchanged.
- Original NorthlineFacility SHA256 remains
  `569da130d17813bd8fb0922e77bc48e2511d7be51242a7c1877bf3b78689afc7`.
- Source/meta integrity: 667 asset GUIDs passed. No 2.0 production bundle delivered.

## Problems found and corrected during integration

- Headless UI focus suppresses pointer events. Tests scope Unity's pinned-version
  unfocused UI hook, isolate a virtual mouse from physical mouse events, and restore
  settings/devices in finally. No product input replacement was needed.
- Batch frames have variable duration; lift/input checks now observe world motion
  and bounded elapsed time, rather than assume a fixed number of frames is 0.3 s.
- Elevated fixed platforms initially inherited the moving-platform displacement;
  moving platforms now have a separate typed object kind.
- Scene teardown could call save while an additional watcher was already destroyed.
  OnDisable's owner guard now requires every registered enemy to remain alive,
  avoiding a partial teardown snapshot. Pause/menu/quit saves retain their paths.

## Remaining acceptance

Other mandatory/optional applications, unique OS desktop conventions, all physical
progression branches/return routes, evolving story and three-stage Recovery remain.
Do not promote this partial app set as the completed 2.0 campaign. Native Payload and
end-to-end campaign acceptance have not been claimed; no new FPS measurements yet.

## Integration follow-up

- Map integration now uses explicit MapGeometrySource metadata. All 13 modern PCs
  have terminal markers; the lift, bridge and moving crates refresh as movers, while
  four static gallery slabs remain cached. The original disabled slab is inactive
  so the map no longer fills the physical lift opening. Actual map assertions pass.
- Full PlayMode job `370233d14bd04b289e34e6724a4e4e87`: 85/86 passed in 109.929 s,
  including all nine new computer cases. The sole failure was the old CIRRUS timing
  test calling TryFire before PlayerWeapon.Update decremented the current frame.
  It now waits for a bounded, observed zero cooldown; weapon balance was unchanged.
- Follow-up job `93ed1238f1fd465a9cad7983eac49a5e`: 16/16 passed in 32.305 s
  (9 computer, 6 map, 1 weapon timing). This is a targeted pass after the full-run
  failure, not a claim that the earlier 86-test run was entirely green.
- Raw InputSystem keyboard test drives PAYLOAD, confirms the 3D body/fire remain
  held, and confirms minimization freezes the app. Physical devices/settings restore
  in finally. The complete lift cycle and bridge walk tests passed together.
- Reviewed logistics scene copied into the 2.0 source preview; original scene untouched.
  Hash/backup receipt: `payload-scene-promotion.json`. Source integrity: 668 GUIDs.

- Full EditMode job `44cbadaa546c41faa993236a21ecf96e`: 95/95 passed in 1.885 s.
  Final API compile and 668-GUID source integrity passed. These checks do not replace
  remaining native/end-to-end iteration2 acceptance.

## NETWORK DEFENSE follow-up

GNOME desktop/rules, three waves, physical radio/outage/reserve consequences and
vault/energy passage are integrated and promoted to the source preview. Full Edit
104/104 and affected computer/enemy/restore Play 28/28 pass. Failed geometry/timing/
FMOD attempts and exact save/scene receipts are recorded separately in
[Network QA](HOSTFALL2_NETWORK_2026-11-17.md). No new native bundle delivered.

## CIRCUIT BREAKER follow-up

KDE-style desktop, reciprocal wiring and genuine power trade-offs are integrated.
Live lights, shutters, defensive turret, cryo route and maintenance shortcut respond;
CC exposed and confirmed the repair of the T-junction crossing wall. 62 pure checks,
111/111 Edit and 91/91 full Play pass. Checked preview promoted, source original
unchanged. The newer observed user auto files predate the full regression and were
preserved/backed up. Details: [Circuit QA](HOSTFALL2_CIRCUIT_2026-11-17.md).


## SIGNAL SYNC

Three calibrations, live heat, timed logistics access and physical return were
verified in the source preview. 70 pure, 119 Edit and 23 affected Play checks passed.
See [Signal QA](HOSTFALL2_SIGNAL_2026-11-17.md).


## MINEFIELD

The solver-verified sensor floor, alarm, exact partial board restore and physical
neuroarchive passage are integrated. Full Edit Mode passed 129/129. Broad Play Mode
passed 94/96 and both corrected checks passed on follow-up.
See [Minefield QA](HOSTFALL2_MINEFIELD_2026-11-17.md).

## DIGITAL DETECTIVE

Original desktop, saved evidence and playback, separate safe and key receipts,
physical CCTV glass and both-way traversal verified. Full Edit Mode 129/129,
targeted Play Mode 2/2. See [Detective QA](HOSTFALL2_DETECTIVE_2026-11-18.md).

## CONTAINMENT

R26 patrol transfer, focused live CCTV, both shutter interlocks, offline
alternate clearance, physical Ring traversal and private restore verified.
Full Edit Mode 133/133 and full Play Mode 100/100. [Containment QA](HOSTFALL2_CONTAINMENT_2026-11-18.md)

## BUFFER RECALL

Physical monitor recording, private save, corridor crossing and one-use repair
locker passed. 135/135 Edit Mode and targeted Play Mode passed.
[Buffer QA](HOSTFALL2_BUFFER_2026-11-18.md)

## FRAGMENT DOCK

Saved partial data, verified placements, original armory unlock and
both-way CharacterController traversal passed. [Fragment Dock QA](HOSTFALL2_FRAGMENTS_2026-11-18.md)

## QUORUM DISPATCH

Witness scheduling, private restore and the west return loop passed. 139/139
full Edit Mode. A separate local Intel Mac preview built and started through this
slice. [Quorum and macOS preview QA](HOSTFALL2_QUORUM_2026-11-18.md)
