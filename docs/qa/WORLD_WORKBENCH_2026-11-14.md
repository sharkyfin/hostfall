# World geometry and local tools — 2026-11-14

## Delivered

Native macOS x86_64 bundle: `apps/unity/Builds/macOS/HOSTFALL-Workbench.app`.
Unity 6000.6.8f1 `BuildDesktop`: **Success, exit 0**; bundle signature verification passed.
The previous build's process ended before a final Unity result was recorded; a cached
final build was rerun and its successful result is in `desktop-final.log` / `desktop-exit.txt`.

Main editor was opened on the repaired `NorthlineFacility` scene through native Unity MCP.
It previously held a clean unnamed scene; no unsaved authoring scene was overwritten.
Native Blender MCP confirmed protocol 16/current addon, Blender 4.5.16 LTS, telemetry off.
No user activation, API key or paid generation service was needed.

## Defects found and repaired

- **10 stairs** in S04/S05/S06/S07/S09: reversed visual ascent, collider shear under
  nonuniform scale, early deck edges and incorrect top transitions. Source stair ascent
  is local -Z. Closed mesh walk surfaces replace tilted box proxies. S02's earlier repair
  is retained. Logistics has two explicit top landings; citadel/dispatch deck transitions
  are aligned; all four quantum trench exits reach the relay bridge.
- **S06/S09 footprint overlap**: S09's floor, low ceiling and side walls occupied a 10m
  strip inside robotics. The ingress now begins beyond Z=95.5; the threshold joins it to
  S06. Nine racks were relocated into the ring/backup rooms and reduced below their
  ceiling. Rail-Eye's track and serialized rig/path bindings follow the relocated rail.
- **Open ground boundaries**: initial scan found 256 exposed-edge samples at a small
  number of seams. Visible infill closes the SOC service-end cap, citadel foyer/atrium,
  witness room ends and dispatch north edge. A missing cloud threshold was added.
  Final scan: **39,338 body-clear floor samples, zero exposed edges**.
- Quarter-turned rails and S04 cable bundles had their long and thin axes exchanged.
  Their measured world dimensions now match the authored route.
- Large primitive floors/walls had stretched UVs and only four additional lights per
  object. Metric UV meshes and the existing URP renderer's Forward+ lighting path make
  local pools usable without raising global ambient illumination.

## Curated environment work

Eight small wall-mounted maintenance/supply bays reuse existing Quaternius SciFi trim
materials, crates, barrels, vents and fixtures; the curated Poly Haven cart/cooling section
is reused. No additional asset pack was downloaded. The original Blender service junction
has 784 vertices / 756 polygons and three explicit opaque URP materials. Its procedural
source and provenance are versioned. The authored scene contains 52 additional prefab
instances in these bays; route centres remain clear.

Actual visual review caught a reversed cabinet face, which was corrected. Physical tests
caught a crate/cart overlap and an initial security-bay intersection with a cell divider;
placement was corrected before the final test runs. Functional placement was checked,
not inferred from file presence. Material census includes assigned materials on disabled
source submeshes; visible use is confirmed separately in the camera captures.

## Verification

| Check | Actual result |
| --- | --- |
| Unity Play Mode, final full run | **50/50 PASS**, 36.11 s |
| Unity Edit Mode, final full run | **63/63 PASS**, 1.98 s |
| Source/meta integrity | **527 GUID PASS** |
| API compilation | **PASS**, 0 errors |
| Native macOS build | **Success / exit 0** |
| Bundle `codesign --verify --deep --strict` | **PASS** |
| Promotion guard | Original saved scene hash matched before transfer |
| Main/copy comparison | 311 promoted assets + 99 runtime/UI/test files match |

New real CharacterController regressions cover logistics ascent/descent and deck entry,
both robotics stairs and headroom, citadel/dispatch up/down routes, four trench exits,
representative outer-edge blocking and grounded/nonintersecting service-bay floor props.
Existing save, automatic-terminal, exploration/map and UI checks also passed. The existing
Edit Mode material test was generalized from one cart instance to all curated instances;
its original SOC aisle/position assertions remain.

## Evidence

Under `.artifacts/world-workbench/`:

- `before/geometry.json`, `before/review/`: initial measurements/images.
- `geometry.json`, `boundary.json`, `review/`: final measured geometry and 11 actual Unity
  camera renders. Cabinet/pipe/cart/crate detail: `review/service-bay.png`;
  warm supply bay: `review/freight-service.png`; physical ascent:
  `review/robotics-west-stairs.png`.
- `playmode-final.json`, `editmode-final.json`: real Unity MCP test jobs and summaries.
- `desktop-final.log`, `desktop-exit.txt`: final build result.
- `source-scene.sha256`, `NorthlineFacility.before.unity`, `promoted-assets.json`,
  `source-verification.json`: backup, promotion guard and source comparison.
- `api-final.log`, `integrity-final.log`: supplementary checks.

Blender evidence: `.artifacts/tooling/images/1.png` (front/three-quarter/back), import
receipt `junction-import.json`, readiness reports. The first Blender preview was occluded
by the default cube; the user's original scene was retained and review moved into its own
asset scene before accepting the model.

## Limits

The ground scan uses 0.5m spacing, floor rays and capsule sweeps; it excludes progression
locks and threats. It does not prove jump containment, arbitrary mesh-triangle separation,
all upper perimeter drops, combat balance or a complete campaign run. Camera renders and
scripted physical traversal are actual editor evidence, not manual native gameplay or an
FPS benchmark. Native app build/signature are verified; a complete manual campaign and
hardware frame-budget assessment remain separate acceptance work.

No gameplay commands, learning links or HUD widgets were added. Simulation/save schema
and device IDs are unchanged. Full scene regeneration and automatic commits were not used.
See ADR 0009 and `docs/tooling/EDITOR_WORKBENCH.md` for tool operation and primary references.
