# Robot armour and performance — 15 November 2026

## Scope and evidence

Saved authored scene repaired in `.artifacts/window-mode-check`, Unity 6000.6.8f1.
Main editor was not restarted; scene was not regenerated. Source SHA before promotion:
`d4360a7f9be605326eb5c4b21ed7d213c49025778a23b8d24911d578a550e59c`.
Evidence directory: `.artifacts/robot-performance-20261115/`.

## Armour and geometry

13 patrol robots, four plates each, Idle/Walk/Run/Attack at phases 0.1/0.5/0.9:
**624 attachment checks passed**, maximum plate-centre to animated torso distance
**0.009522 m** (plates are 0.035 m thick, embedded by 0.008 m). Body parent checked.
Real Unity renders of both body families and MANTIS front/rear were reviewed.
Temporary readable FBX import restored exactly: QuadShell, Trilobite and MANTIS meta
files match the source after authoring. Runtime does not bake a mesh to attach armour.

Two MANTIS prefabs retain six materials and 15 independent damage components.
Each now has 21 renderers rather than 141. Fixed mesh index totals before and after
combining: **52,008 / 52,008**. The six combined meshes have 5,529 / 1,434 / 472 /
7,236 / 240 / 5,231 vertices. Weak-point/core opening and save restore passed.

An initial combine attempt produced empty meshes because the FBX was not readable.
It was caught before promotion. The authoring tool now rejects unreadable inputs,
empty groups and unequal index totals; the integration test checks nonempty meshes.
Native runs named `invalid-empty-geometry-*` are excluded from all conclusions.

## Behavioural verification

**72/72 Play Mode** (`play-release-result.json`), **69/69 Edit Mode**
(`edit-final-result.json`), **33/33 Simulation**, API compilation PASS (one upstream
Cinemachine nullable warning, zero errors). Repository integrity PASS: 614 asset GUIDs. Native release details below.

The new tests require all 12 queued robots to progress; check the shared candidate-edge
budget; wait at a closed door and arrive after opening; reject a floor gap. Existing
combat tests cover sensor damage, walls, death, weak points, saves, pause and boss clearance.
These are real Unity/CharacterController regressions, not a manual campaign playthrough.

One earlier weapon-timing fixture was interrupted by the now-active patrol. Moving the
fixture outside the world then correctly triggered save validation. The final fixture
keeps a valid drop location and disables enemy objects only inside this isolated test.
The complete suite subsequently passed; no gameplay invulnerability was introduced.

## Measurement protocol

Native development players, AMD Radeon Pro 5300M / Metal. Six identical fixed views,
45 warm-up frames and 90 measured frames each (540 samples/run). Same visuals, no
resolution/shadow reductions. Diagnostic mode uses private saves and is absent from
normal release play. Additional high-resolution runs report their actual dimensions.

The frozen baseline includes the common diagnostic harness and damage appearance cache,
original navigation and original 141-renderer MANTIS. Thus the native comparison isolates
navigation/geometry/render-path changes; it does not quantify every cache change separately.
Unfocused/timeout runs, batch runs with zero draws and the early exception-producing
prototype are excluded. Render Thread recorder is unavailable here and is not reported as
zero cost. Only positive GPU timing samples are used; sample counts are retained.

The synthetic navigation cold window originally peaked at 7.42 ms for 12 searches.
It is a microbenchmark with JIT/OS/GC variability, not a guaranteed frame-time bound.
The hard invariant is at most 96 candidate-edge checks in the shared search queue;
1.2 ms is a soft elapsed budget, excluding immediate movement safety checks.

## Native comparison and final choice

Final runs were serial, with no tests, builds or API compilation running concurrently.
The two 1280×800 passes used opposite variant order. Three additional 2560×1600
baseline/selected comparisons repeated the high-resolution result.

| Variant | 1280×800 engine-frame medians, two runs (ms) | 2560×1600 first comparison (ms) | Decision |
|---|---:|---:|---|
| Frozen baseline | 6.697 / 5.307 | 10.910 | Reference |
| Navigation + complete combined MANTIS, conventional URP | 5.404 / 5.902 | 8.225 | Selected |
| GPU Resident Drawer | 4.842 / 4.401 | 6.983 | Rejected: black compute-view capture in both low-resolution runs |
| GPU Resident Drawer + occlusion | 5.341 / 5.409 | 15.866 | Rejected: high-resolution regression and one black compute-view capture |

Low-resolution frame intervals overlap: there is no evidence of a uniform speed-up at
that resolution. The lower GPU draw counters are not sufficient to accept a variant.
The initial resident build also failed to include required resources (NullReference in
GPUInstanceDataBuffer); a clean-cache player rebuild removed that exception, but did
not resolve the separate black capture acceptance failure. This does not establish an
engine-wide hardware incompatibility or prove a black frame would occur in every play session.
Production GPU Resident Drawer/occlusion remain disabled; original graphics settings retained.

Repeated selected-path high-resolution comparison:

| Run | Baseline median / P95 (ms) | Selected median / P95 (ms) |
|---|---:|---:|
| 1 | 10.910 / 19.697 | 8.225 / 12.001 |
| 2 | 9.497 / 16.453 | 8.105 / 11.196 |
| 3 | 10.801 / 20.605 | 8.013 / 11.596 |

Median of the three run medians: **10.801 → 8.105 ms**. Median managed allocations
at that resolution: **3,408–3,448 → 1,632 bytes/frame**. No resolution, shadow or
lighting-quality reduction. These are uncapped engine-frame intervals in this replay,
not guaranteed displayed FPS or a full-campaign worst-case claim. Positive GPU timing
sample counts vary substantially (including 32/540 in one selected high-resolution run),
so these rows are not used to claim a GPU-time reduction. Full counts and per-view
results are in `profile-summary.json`; raw receipts and PNGs remain beside it.

## Delivered build

`apps/unity/Builds/macOS/HOSTFALL-Robots.app`: x86_64 native release, clean player
build **Succeeded / 0 errors**, 161,202,769-byte build report. Ad-hoc signature verified
with codesign --deep --strict. A separate 12-second headless release smoke remained
running, terminated cleanly with exit 0, and had no script/shader/load errors. Batch
storage is disabled, so user saves were not used by that check.

Promoted only the repaired authored scene, MANTIS prefab, six meshes and their meta files
(15 files). Source scene still matched the initial hash before promotion. Promoted scene:
`06fd575b16dd232c5a7547874523733f375212623e5c0ed8e3c90574286ab706`.
Backups and hashes: `before-promotion/`, `scene-before.unity`, `promotion.json`.
No source renderer/quality settings were changed and no experimental GPU toggle was shipped.

