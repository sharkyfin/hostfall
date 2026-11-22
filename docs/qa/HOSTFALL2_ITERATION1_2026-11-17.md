# HOSTFALL 2.0 / iteration1 — in progress, 2026-11-17

This report covers the first vertical slice only, not the full master request.

## Actual implementation

Home-only safe shell, nine local guide chapters and evolving authored VFS. Shared
physical DOM/texture and camera focus. First external desktop has movable, closable,
minimisable Snake/files/document windows and task switching. Three-stage deterministic
Snake owns body, food/RNG, queued turn and partial tick; stage receipts independently
own persistent world consequences. Real vent/grate + R19, local door-power light,
manual physical gates and emergency CIRRUS pickup. Computer mode runs world/AI but
holds player input. Esc exits; pause/resume preserves computer focus. V3 slots and
northline-2 are separate from existing v2.

## Checks completed

- 42/42 pure Simulation checks; 90/90 Unity EditMode tests (including new v3 JSON/store case).
- Four new PlayMode scenarios passed: real home screen + guide/input, partial save/load
  and vent release, typed door rewards/document, CharacterController home-office-return.
- API compilation passed; initial source/meta integrity 657 GUIDs passed (preview added
  later; final integrity still required).
- Native QA build Succeeded / 0 errors; separate identifier
  com.northline.hostfall.computerqa and private-saves assigned before scene load.
- Actual native automated round trip passed at 3584×2240: awakening, home/guide/read,
  real CC route, pistol pickup, Snake start, exact non-clock partial state, vent release,
  11 physical CIRRUS shots, three Snake stages, physical door rewards, return home and
  earned incident read. Receipt and reviewed screenshots under
  .artifacts/hostfall2-20261117/native-first-loop. Exit 0; all 11 production v2 save hashes
  unchanged. This is an automated driver, not a claim of human play or full-campaign QA.

## Problems found and resolved / pending

- Scene unload could autosave after PlayerMotor destruction: guarded owner references.
- App button test needed actual keyboard focus before navigation submit.
- Capsule clearance query originally counted the actor's own movement capsule; corrected
  external-obstacle check, plus real swept emergence lane validation.
- Shell scroll could take a negative highValue with short text; clamped, native render reviewed.
- Unity JSON moves a binary64 clock by one ULP (2.8e-17 s in the reproduction). QA compares
  clocks within 1ps and every other field exactly. Pure model snapshot replay stays exact.
- Native images/inventory exposed new duct beside the security shortcut and intersecting
  an existing maintenance cart. Duct moved to southeast corner; original weapon rack and
  crate retained in shelter. Interior scan found zero external overlaps. Side-door CC test
  passed in both directions, 82/82 full PlayMode regressions passed; native loop for the
  final placement also passed. A subsequent real InputSystem mouse test found physical
  clicks not reaching the panel; pointer mapping/dispatch is being repaired. The native
  driver used navigation submit and therefore did not verify mouse dispatch. Gate remains
  unaccepted until mouse click/drag/window behavior is verified.

No production build or original scene promotion yet. Later computers, games, narrative,
combat overhaul, final cinematic and streaming remain iterations2–4.

## Gate accepted after pointer diagnosis

2026-11-17: native full loop and final vent placement passed; broad PlayMode 82/82.
The extra physical pointer test initially received no events in batch Editor. Pinned
Unity 6000.6 source `UIElementsRuntimeUtility.eventSystemUpdateMode` defaults to
IgnoreIfAppNotFocused. Setting its test-only hook to Always (restored in finally)
made the real InputSystem mouse sequence reach home tabs, launch Snake and start
it. Projection/picking were correct; no production input workaround was added.
Job edb13dbbb0594c039cdf98baa1b40508: 1/1 passed in 3.708 s. Keyboard input in
the native QA window was also observed. CUA coordinate clicks were unavailable
(noWindowsAvailable), so do not describe that as manual native mouse acceptance.
Iteration1 gate is accepted; iteration2 may start. Production 1.x remains untouched.
