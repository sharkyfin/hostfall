# Motion and weapon pass — 15 November 2026

## Delivered behaviour

- 12 selected CC0 Quaternius actions exported through Blender to FBX, sampled joint
  anchors and Unity .anim assets. Actual body locomotion/air/landing/interaction and
  firearm actions blend in place; grip IK retains the real body arms. Two exported
  options, Jump_Start and Hit_Chest, are reserved rather than claimed as finished states.
- Camera sway/bob has a 0..1 preference. Held items have bounded inertia and an equip
  transition. FPS firing uses the current view rather than the previous camera frame.
- One carried weapon. The previous gun is physically thrown on replacement; the
  same authored prefab is used, with a measured tight collider and Rigidbody.
  Dropped guns never collide with the player. Ammo, reserve, position, velocity,
  heat and cooldown survive save/load. Collected rack items do not respawn on load.
- ARC-12: individual shells, interrupted reload; CIRRUS: heat/thermal lockout;
  TITAN: hold/release charge with 55..140 base damage. Existing robot region
  multipliers and boss weak-point requirements remain.
- Held models/impact lights are prewarmed and reused. Static map contours and
  room-clipped polygons are cached; moving doors/bridges still update. Ordered
  nonalloc shot queries retain an overflow fallback. Static text uses atlas events.

## Actual inspections and regression coverage

Verification instance: `window-mode-check@e864613ac9f05ec9`, Unity 6000.6.8f1,
graphics-enabled batch Editor. Source scene was not reauthored; source and copy
SHA-256 both `d4360a7f9be605326eb5c4b21ed7d213c49025778a23b8d24911d578a550e59c`.
The main Editor was not used as proof: its bridge intermittently failed to answer.

Actual Unity renders inspected: idle, two walk phases, sprint, full armed body,
left-hand reach, and ARC/CIRRUS/TITAN first-person views. Review images live under
`.artifacts/motion-combat-20261115/review/`. The first full-body capture hid the
helmet because it used the FPS shadow mode; review now explicitly enables visible
head rendering for the external camera. This was a capture setup issue.

New behavioural checks cover actual file-backed save/load and repeated floor swaps,
round-trip isolation, idempotent collection, corrupt counters/ammo/poses, individual
shells, thermal lockout/cooling, saved charge and pause, imported nonstatic keys,
no root displacement, zero camera intensity, cached models/lights and collision layers.
Existing hand socket, precision damage/wall, map/door/bridge and traversal checks remain.

Problems found and corrected:

1. Blender action slots initially sampled a static pose; assigned slots explicitly.
2. Source rig orientation was already applied; removed the second axis flip.
3. Old inventory tests assumed numeric switching; revised them for floor replacement
   and retained magazine checks. Readiness waits include a frame margin.
4. Static-map optimisation accidentally blocked explicit shape refresh. World scheduling
   now caches static shapes while explicit refresh still respects moved/resized colliders.
5. A pause test waited on scaled time; corrected its own wait to real time.
6. A disconnected MCP WebSocket contaminated one repeated test with an editor warning.
   The warning was not suppressed. Another repeat exposed stale static audio references
   after native clips were unloaded; live clip validation now rebuilds that cache.

## Measurements and limits

Same scene and workload; 200 map refreshes after 30 warmups:
**275.0622 ms before → 26.4167 ms after**, approximately 10.4× faster.
The managed allocation counter used in that microbenchmark returned zero even for
the old implementation, so it is not accepted as evidence of zero allocation.

Stationary exploring window, 60 warmup frames + 180 measured, Editor ProfilerRecorder:

| Metric | Before | After |
| --- | ---: | ---: |
| Main Thread median | 0.946 ms | 1.001 ms |
| Main Thread p95 | 2.241 ms | 1.392 ms |
| GC allocated/frame median | 3,706 B | 4,122 B |
| GC allocated/frame p95 | 3,812 B | 4,174 B |

These describe one diagnostic window, not a standalone FPS/GPU guarantee. Median CPU
time rose by about 0.055 ms with the added motion system; p95 improved in this
window, while managed allocation rose by about 416 B/frame. Do not advertise zero
GC or universal removal of stutter. Combat object reuse is independently asserted.

The animation/weapon tests are not a new manual campaign completion or a parkour
collision proof. No motion matching, foot contact solver or new character mesh is claimed.

## Release validation

- Final **69/69 Play Mode PASS**, `play-delivery-result.json` (68.78 s).
- **69/69 Edit Mode PASS**, `edit-final-result.json`; **33/33 Simulation PASS**.
- API compilation PASS; only the existing upstream Cinemachine nullability warning.
- Repository integrity PASS, 602 GUIDs, legacy reference unchanged.
- macOS x86_64 build **Succeeded, 0 errors**, 160,940,641 bytes.
- Ad-hoc bundle signature verification PASS. Native headless smoke ran 12 seconds,
  controlled exit 0, no script/load exceptions. Save writes are disabled in batch mode.
- Deliverable: `apps/unity/Builds/macOS/HOSTFALL-Motion.app`.

Receipts: `.artifacts/motion-combat-20261115/` contains before/after measurements,
full test results, API/integrity logs, native build/signature/smoke and actual captures.
This is a local standalone build, not a published release or a manual playthrough.
