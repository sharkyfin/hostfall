# HOSTFALL 2.0 / Connected computer campaign

The connected Unity 6000.6.8f1 Play Mode scenario reached the arena entrance
in 52.62 seconds of test execution. It started a new game in the shelter and
kept the same CharacterController for each leg. Its eight mandatory receipts
came from the respective puzzle sessions at their authored stations.

The route opens the actual office, server, vault, energy and cryogenic gates,
passes through the timed field, runs the logistics lift/conveyor/bridge
sequence, and crosses the deployed bridge into robotics and security.
It disarms the MINEFIELD floor, reaches the Detective desk and picks up the
archive key from the physical safe. At RBT-01 the sentry navigates to the real
bay. Sensor confirmation and both closed shutters yield the quarantine
clearance. The player returns via the north field release and completes
the three Home Recovery tasks through the projected desktop. The shuttle
carries the same player into the Central Ring. Walking through the core gate
ends the test.

The routing fixture isolates regular combatants and uses deterministic input
for several games. R26, collision, physical world adapters and progression
doors are live. Independent station tests cover their input, failure, retry,
effects and restore. Separate shuttle tests cover a reload at tunnel depth
and the return trip. This is computer progression and geometric clearance
evidence. Full native combat and the finale remain later gates.

The first route attempt ran into the MINEFIELD terminal pedestal. The
passing route steps around the desk from the west. See
`play-connected-to-core.json` and `play-connected-to-core-v2.json` in
`.artifacts/hostfall2-20261117`.

The VENTFLOW file app opens two maintenance letters and a partially corrupted
camera still captured from the authored operator room. The fourth file is
hidden behind the file manager's service-file toggle. Its open window and
selected document round-trip in the v3 slot. The projected desktop shot is
`ventflow-handover-desktop.png`.

The iteration2 cut passed 96 isolated simulation checks and API compilation.
Unity passed all 145 Edit Mode and 109 Play Mode cases. The full Play suite
also reran the connected route, the live R26 scenario and shuttle restore.
The authoring capture left both scene hashes intact. Evidence is in
`edit-iteration2-acceptance.json`, `play-iteration2-acceptance.json`,
`simulation-ventflow-archive.txt` and `api-ventflow-archive.txt`.

The acceptance cut built the Intel macOS application
`apps/unity/Builds/macOS/HOSTFALL-2-Computer-World.app`. Unity reported
41.48 seconds, zero errors and 448 warnings. Code signature verification and
the native Metal startup smoke passed. The 13 original profile files matched
their preflight hashes. Unity wrote its test result XML in the preview profile.
There were no campaign saves in that profile; the later workbench now has a
separate editor profile for test outputs. See `iteration2-preview-build.txt`
and `iteration2-preview-player.log`.
