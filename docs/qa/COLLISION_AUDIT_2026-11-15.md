# Collision visibility and boundaries — 15 November 2026

## Scope and method

Inspected the saved `NorthlineFacility` scene in Unity 6000.6.8f1 through the explicitly
selected verification instance `window-mode-check@e864613ac9f05ec9`. The main editor's
scene was clean; its saved SHA-256 matched the verification scene before work.
No full FacilityBuilder rebuild was used. Existing authoring and weapon assets remain.

The initial inventory contains **1,141 enabled non-trigger colliders**, excluding the
player. A separate reference-mesh pass sampled **33,874 side contacts** against **3,352
visible static meshes**. Its 4,109 contact differences are *review candidates*, not 4,109
bugs: floor bevels, decorative wall recesses and continuous railing proxies deliberately
differ from triangle geometry. Proxy transforms use position/rotation/lossyScale, so this
pass is a heuristic for sheared hierarchies. Dedicated CharacterController tests determine
whether reviewed mismatches actually interfere with movement.

## Confirmed defects and repair

1. **Seven ARC-12/CIRRUS display colliders** retained the previous primitive weapon
   dimensions and offsets. They extended into visually empty space by as much as 0.48m.
   Their profiles now follow the visible authored meshes. They still receive interaction
   rays; the TITAN pedestal retains its structural collider.
2. **Eight service-bay light colliders** copied a world-axis bounding box into a rotated
   local collider. Their long/thin axes were exchanged. Bounds are now measured from mesh
   corners transformed into the collider's local coordinate system. The EnvironmentPass
   creation path uses the same corrected coordinate conversion.
3. **S10 panoramic armored window at Z=206** had no solid collider. A real controller
   following the game's 0.8m jump passed through it, reaching Z=206.98 and the void.
   The existing visible glass now has a matching BoxCollider; the builder preserves it.
4. **Two open south shoulders of the S10 evacuation room** at X≈2.5–3 / 17–17.5,
   Z≈192: the room widened beyond the preceding corridor without closing the ends.
   A controller walked out to Z=190.90. Two visible concrete sections now close these
   shoulders; no invisible perimeter wall was introduced.

The 15 excessive-profile cases each obstructed the isolated real-character clearance
check before repair. All pass afterward. The window and south-shoulder checks also failed
before their respective repairs and pass afterward. These tests isolate a collider only
when attributing the profile defect; ordinary stair and door tests use actual scene geometry.

## Wider route and boundary checks

- **18 opened doors / 36 directions** traversed with the real CharacterController.
  Progression locks are opened by restoring each door's own open state; no static
  architecture is disabled. Threat colliders are held out of this circulation check.
- The first door probe requested too much travel beyond the logistics/robotics doorway
  and met the adjacent raised mezzanine edge after crossing the aperture. The final probe
  requires crossing the aperture with full-body clearance, without requiring the raised
  deck beyond it to be at ground level. This was a probe correction, not a removed wall.
- Existing tests cover both directions on compute-vault stairs, bridge/landings,
  logistics and robotics stairs, citadel/dispatch upper access and all four trench exits.
- Fixed a coverage error in the previous boundary scanner: its hard-coded Z=-12..181
  range omitted part of the shelter and the final rooms. The current scanner derives
  bounds from scene solids: **X=-28..90, Z=-21..208**, 0.5m spacing.
- Final full-range ground scan: **40,922 body-clear samples, zero exposed-edge samples**.
  The expanded scan initially found eight samples belonging to the two S10 shoulders.
- The lower-floor connectivity snapshot has one main connected region (40,592 samples) spanning the
  shelter through the final room, three enclosed security cells, and tiny clearance
  islands beside props. Grid connectivity is a static heuristic with door/threat
  colliders excluded, not proof of progression, jumping or reaching upper floors.

## Validation

- **63/63 Unity Play Mode PASS**, 67.78s.
- **67/67 Unity Edit Mode PASS**, 1.85s.
- API compilation **PASS**, 0 errors.
- Source/meta integrity **PASS**, 576 GUIDs.
- Native macOS x86_64 build **Succeeded / 0 errors**, 160,350,965 bytes.
- Bundle signature **PASS**; native headless process ran 12s, controlled exit 0, no script/load errors.
- Delivered bundle: `apps/unity/Builds/macOS/HOSTFALL-MapFix.app`.
- Scene promotion guarded by matching the original saved hash; only the reviewed scene was transferred.
- Actual Unity renders reviewed: service bay, logistics, final-room shoulders and window.

Evidence: `.artifacts/collision-audit-20261114/` (the task started on 14 November and
continued after local midnight). Inventory, mesh-contact candidates, baseline failures,
final test jobs, door crossings, full-range boundary reports and actual camera images
are retained there. `scene-final.diff` records only this targeted scene change against
the task's saved backup. No save format, terminal behavior, weapon balance or AI changes.

## Limits

This is an editor physics/geometry audit with scripted real-character traversal. It is
not a manual campaign completion. The ground scan is not a proof of every possible dash,
stacked-prop jump, thin triangle contact or upper-edge escape. The mesh pass does not
establish pixel-perfect collision on every decorative recess. No claim is made that all
possible map defects have been eliminated.


Post-promotion editor note: the main saved scene hash equals the verified scene and the
native build passed. A live reload confirmation in the main GUI could not be obtained:
its Unity MCP WebSocket disconnected while awaiting command results, and native input
subsequently returned `noWindowsAvailable`. The GUI was not restarted or force-closed.
This does not change the verification-copy test/build evidence; do not describe the
main interactive reload or a manual game run as verified.
