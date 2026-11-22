# HOSTFALL 2.0 · Combined candidate validation

The candidate combines the terminal, combat and story work in the same
`Hostfall2Facility` scene. Positive evidence includes native Metal startup,
115 pure checks, 160/160 Unity Edit Mode checks, 120/120 Unity Play Mode
checks, an API compile and the 812-GUID repository audit.

The connected Unity scenario starts in the shelter and drives one actual
CharacterController along the authored route. It completes the mandatory
computers, both PAYLOAD response waves, live R26 containment, Home Recovery,
shuttle travel and the three-phase core encounter. It opens the trust and
evacuation doors, finishes the cutscene, crosses the final threshold and
restores an epilogue save. Computer and combat stages use deterministic
test input and test damage where appropriate. The isolated core test checks
cover, physical weak-point ray hits, arena risers and persistence.

An initial candidate run exposed a missing whitelist entry for each of the
three new logistics units. The v3 save validation now accepts those exact
authored IDs. Save retry backs off when storage fails. An earlier regression
also exposed a capture button being used before the next UI refresh, so the
connected test now waits for the enabled control. The actor's model and
shutter sensors still have to confirm capture.

The workbench audit checked the vertical support of 12 worklight buttons.
Two visible mounts were added at EDGE and the power center, where the
clearances had been 0.032 m and 1.032 m. The Matriarch placement additionally
checks its whole leg envelope, floor and front lane. The Type-IV face and
northern valley were exported from two original Blender projects. The viewport
profile, boss views and build logs are in `.artifacts/hostfall2-20261117`.

The source scene and packaged candidate use the checked scene hash
`31c1516341840b010a6a2ffed1ae7e08a513be2a267a599a739f64034591c3ce`.
The original 1.x scene kept its checkpoint hash. The candidate retains the
2.0 preview profile with a separate profiling build. All 16 inventoried
user profile files matched before and after the first native startup.

The isolated Metal QA player also repeated the first loop in a real 1920×1080
window. Eleven physical shots defeated the released robot. The run passed the
Home knowledge click, three Snake stages, partial save/restore, gated doors,
the return trip and the final archive read. See
`.artifacts/hostfall2-20261117/native-window-loop/first-loop-receipt.json`.

The mini-game tests drive the rules and separately exercise the physical UI.
The Mac replay steers all three Snake stages through normal directional commands.
The Unity suite drives all three PAYLOAD levels through movement/jumps and tests
keyboard routing independently. NETWORK DEFENSE, CIRCUIT BREAKER, SIGNAL SYNC,
MINEFIELD, DIGITAL DETECTIVE and CONTAINMENT have rule and world-integration
checks. Optional BUFFER RECALL, FRAGMENT DOCK, QUORUM DISPATCH and INTEGRITY AUDIT
have separate desktop, save/restore and reward checks. The full-route test
uses direct game actions for some stages and omits optional detours.

The Mac replay sampled 630 frames in seven fixed viewpoints at 1920×1080 on the
AMD Radeon Pro 5300M using Metal. Median frame time was 3.69 ms, overall p95
was 8.10 ms and compute-room p95 was 19.81 ms. The service passage had the
largest spike at 80.61 ms. Twelve frames exceeded 16.67 ms. Only four GPU
timings were populated, so the sweep supports frame-time comparisons but does
not establish a GPU bottleneck or sustained combat performance. See
`.artifacts/hostfall2-20261117/native-profile/performance-summary.json`.

`tools/unity.sh web-candidate` builds a WebGL 2 player from a staged copy. The
staged package manifest excludes the editor AI tools: Inference pulls in the
App UI TextMate archive, which was compiled for an older Embind ABI. Keeping
only the archive out also leaves unresolved managed references. The source
project retains both editor packages. The Web candidate built with zero errors
and is available in `apps/unity/Builds/Web/HOSTFALL-2-Candidate`. Chrome with a
fresh profile loaded the main menu, accepted both New Game clicks and rendered
the opening cutscene at 1280×800. The browser console had no uncaught exceptions.
Screenshots and console records are in `.artifacts/hostfall2-20261117/web-browser`.
The browser uses the in-session campaign state. Persistent save slots are
available in the macOS player.

Manual testing of optional routes and difficulty, and measured zone streaming,
remain open for full release acceptance.

## Journal follow-up · 2026-11-20

The new journal shows the same active stage as campaign progression, with
instructions specific to each computer and its world outcome. The initial
VENTFLOW briefing appears in the actual journal window, and later plot entries
stay hidden until reached. The repeated connected Unity route verified the
Minefield and Recovery stage transitions and the completed epilogue state
after reload.

The new Home starts with a blank transcript. Loading an older v3 snapshot
removes the obsolete initial banner and preserves later output, history and
draft. The check passed in Unity. The updated run passed 116 simulation checks,
161 Edit Mode tests and the targeted Play Mode route. Source API compilation
and the 813-GUID repository audit also passed.

The signed macOS journal candidate is
`apps/unity/Builds/macOS/HOSTFALL-2-Journal.app`. It started on Metal with
14 user-profile save files unchanged.

The updated WebGL 2 candidate also built with zero errors. A fresh headless
Chrome profile started the game, dismissed the introduction and opened the
VENTFLOW briefing with J. The harness disabled the browser's pointer grab.
The 1280×800 screenshot is in
`.artifacts/hostfall2-20261117/web-journal/web-step-4.png`.

## Home evidence and engineering atmosphere · 2026-11-20

The protected scene copy uses four fitted instances from three original Blender
models. Wall placement probes rejected overlapping props. The pump instance
has a collision volume and a checked floor contact. A local inspection light
and motor loop dress the route. The central generator has separate copper and
guard materials, and the original corridor indicators remain in use. The
retained scene checkpoint matched the original v2.0 source hash. The scene
kept its asset identity during transfer.

The 117 simulation and 162 Unity Edit Mode checks passed. Three Play Mode
checks passed in the same editor copy. They covered opening an earned
Home record on the physical screen, the controller round trip through actual
doors and the fresh-campaign route through all mandatory computers and Recovery.
Source API compilation and the 839-GUID source audit passed.

The signed Intel build `apps/unity/Builds/macOS/HOSTFALL-2-Immersion.app`
started on Metal. This startup check preserved all 14 user save hashes.
Unity reported no build errors. The release build includes the 960×540,
nine-second main menu loop captured in the authored facility. A separate
Play Mode check in the graphical macOS editor observed the playhead advance
and the actual video texture appear on the menu. Opening New Game released
the video player and exposed the story brief. Three frames and the Blender
render are under `.artifacts/hostfall2-immersion`.

The final WebGL 2 candidate compiled without errors. A fresh Chrome profile
decoded the same loop at 1280×800, opened the New Game brief and started the
awakening cinematic. The browser stopped the menu video after the transition.
The local 127.0.0.1 port 8762 preview server supports byte ranges for the
MP4. The browser receipts and four screenshots are under
`.artifacts/hostfall2-immersion/browser-final`. The harness disabled the
browser pointer grab for the automated interaction.

## Wall lettering and passage clearance · 2026-11-20

The shelter opening now has two 0.76 m relay faces in place of the 2.70 m
cases. The old information overlays and sector title boards were removed.
The two new room plates are backed by solid wall surfaces. The wall relay
and cable-entry box have fitted colliders. The moved spill kit clears the
vent release volume. Eleven worklight controls sit on their stations and the
energy-centre switch has verified ground contact through its bolted stand.

The graphics editor rendered 412 bright letter pixels in the unobstructed
depth test and zero behind an opaque wall. The nine-second menu loop was
re-recorded from the fixed scene. The revised doorway was photographed with
its actual leaf retracted in both directions.

The 0.5 m ground grid covered 469 × 237 samples. The inspection also took
39,418 side-surface probes against 3,414 visual meshes. The 3,896 remaining
surface candidates include mismatches on undersides, kit trim and physical
rail proxies. This list is a heuristic inventory for visual review. The
ground grid covers the lower layer of the map.

All 117 simulation checks, 162 Edit Mode tests and six targeted Play Mode
tests passed. Play Mode exercised the shelter return, the energy bypass, the vent clearance, the
physical computer pointer, the mandatory computer route to the core and
typing through the runtime focus dispatcher with no preceding click.
The 851-GUID audit and API compilation also passed. Raw results and camera
frames are under `.artifacts/hostfall2-clearance`.

The signed macOS `HOSTFALL-2-Clearance.app` built without errors and started
on Metal. It includes the revised menu film. All 14 save hashes in the
observed user profiles matched across the separate startup check.
The updated WebGL 2 candidate built without errors and played the new film
in Chrome at 1280×800. The browser opened the story briefing and started the
awakening cinematic. There were zero unhandled browser exceptions. The
browser check used the byte-range preview server on port 8762.
