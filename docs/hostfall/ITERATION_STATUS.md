# Iteration status — 2026-11-19

This page records the prior acceptance gates. The [November 20 campaign plan](HORROR_CAMPAIGN_2026-11-20.md)
now governs new work on pacing, terminal content, geometry and horror encounters.

The first two MASTER_REQUEST iterations were accepted on 2026-11-19. The combined preview contains the combat and finale from iterations 3 and 4. Full 2.0 release acceptance remains open.

| Iteration | Steps and dependencies | Gate | Status |
|---|---|---|---|
| 1 Foundation + vertical slice | 1a audit/checkpoint/canon → 1b pure campaign/home shell/Snake + tests → 1c physical home/Win98 UI and save-v3 → 1d typed vents/doors/ambush → 1e actual round-trip home/Snake/fight/archive + native acceptance | Entire first loop works, exact partial save/resume, no global Knowledge/autoplay, user saves intact | accepted: 42 pure / 90 Edit / 82 broad Play + physical-pointer test / native full loop |
| 2 Computer world + narrative | reusable window/apps → eight mandatory + four optional distinct games → physical effects/graph/shortcuts → evolving story → 3-stage home Recovery → all game/progression tests | all 13 PCs interactive, unique games and physical consequences; central route reached | accepted: continuous CharacterController route to the core gate, 96 pure / 145 Edit / 109 Play |
| 3 Combat + horror | perception/nav/collision audit → squad roles/token budget → tank attacks/phases → weapons/counter → vent/horror/intensity → 3 minibosses + final avatar → arena/interaction acceptance | tactical readable enemies/bosses, meaningful weapon roles and physical ventilation | integrated and tested in Unity; manual difficulty tuning remains open |
| 4 Polish + final QA | reviewed art/animation/dressing → cinematic reveal → measured zone streaming/performance → target builds → end-to-end play/restore/route checks | full home-to-finale route and actual final cinematic | connected route, cinematic and epilogue restore passed; Mac and Web candidates built and started; fixed-view Metal measurements recorded |

## Verified so far

- Master request read; actual Unity 6000.6.8f1 retained.
- Recoverable dirty/untracked source checkpoint (3,722 files) and git bundle created.
- Actual Unity MCP scene audit: 13 PCs, 19 doors, 13 watcher actors, home/SOC renders inspected. Home duplicate ID / absent surface and wrong spawn confirmed.
- Blender 4.5.16 MCP answered with current protocol16; no generators needed to start.
- Pure HomeShell / 9-chapter HomeGuide / deterministic 3-stage Snake / typed campaign rewards and threat clock implemented. 42/42 pure checks, 90/90 full Unity EditMode tests, API compilation pass.
- V3 persistence and interactive Computer mode wired behind a separate mission definition; original saved scene remains unchanged.
- Isolated Hostfall2Facility preview authored with 13 unique station IDs, home model/surface/spawn, two typed door bindings, visible vent/grate/R19 and emergency sidearm. 5/5 new PlayMode scenarios passed including CC home round trip and side-door clearance. Native first-loop QA passed (11 shots + three Snake stages + return/read); final placement rerun and 82/82 broader regressions passed. Physical projected mouse test passed after enabling Unity’s test-only unfocused event mode; no production input change.

## Combined preview

The connected Play test uses one CharacterController through the mandatory
computers, the PAYLOAD lockdown and both waves, containment, Home Recovery,
the shuttle, the three phases of SUPER, the evacuation shutters and the final
cutscene. It restores the resulting epilogue save. The test uses deterministic
computer and combat input. Separate combat checks cover physical weak points,
clear and blocked attack paths, the Matriarch leap, and the terminal counter.
The combined preview adds the Security Warden and Harvester, alongside MANTIS-6,
network-dependent Watcher squads and the expanded ventilation grab.

The combined run passed 115 pure checks, 160 Edit Mode checks and 120 Play Mode
checks. The Mac candidate signed and started. A manual sweep of all optional
passages and measured streaming of facility zones remain open. See
`docs/qa/HOSTFALL2_COMBINED_CANDIDATE_2026-11-19.md`.

## Iteration2 work in progress

- Reused the same physical panel and retained window manager for Windows XP; added
  a typed app catalog and per-station desktop save validation.
- PAYLOAD RUNNER has 3 deterministic levels, acceleration/braking, buffered/coyote
  jumps, gaps, a moving platform, conveyor, periodic lasers, a patrol sentinel,
  collectible packets and a checkpoint. Real control-sequence tests finish all levels;
  retry preserves earned milestones/packets and exact partial physics restores.
- First/second/third level activates a physical freight lift/conveyor/hinged bridge;
  the last also raises an alarm and unlocks robotics. Home receives a transfer log.
- Saved logistics clocks drive physical poses; lift passengers use CharacterController.
- 47 pure checks, 95/95 EditMode and source API compile pass. Nine computer PlayMode checks plus six map/one weapon check pass; earlier full run was 85/86 before its timing-test fix. Scene/UI integration results are kept
  in docs/qa/HOSTFALL2_ITERATION2_2026-11-17.md. Full iteration2 gate remains open.

NETWORK DEFENSE now has three real waves, shared CPU/isolation budget, infection,
routing/purge and saved packets. Its world adapter relays sightings, releases two
physical reserves, applies an idempotent outage and unlocks vault/energy. 104/104
EditMode and 28/28 affected computer/enemy/restore PlayMode passed. Scene and user-save
receipts plus failed FMOD runs are documented in HOSTFALL2_NETWORK_2026-11-17.md.

CIRCUIT BREAKER now owns a real reciprocal-contact power graph, shared capacity,
manual relays/source switches and overload/reset. Current power drives retained
lights, louvers, defensive induction turret, cryo door and actual SOC service bypass.
CC found the T-junction wall crossing; visible wall/collider repaired together and
both-way path passed. 62 pure / 111 Edit / 91 full Play passed. Checked preview
promoted; current user save advance predates the full tests and is preserved/backed
up. See HOSTFALL2_CIRCUIT_2026-11-17.md.

SIGNAL SYNC now runs three oscillator calibrations with saved phase and hold state.
Its independent world clock drives heat, recharge and the physical timed gate.
Play Mode confirmed the existing robot approaches, occupied gates hold safely across
reload, and the northern actuator permits return. Source preview promoted after
70 pure, 119 Edit Mode and 23 affected Play Mode checks.
See docs/qa/HOSTFALL2_SIGNAL_2026-11-17.md.

MINEFIELD now maps three solver-verified boards to 36 real floor sensors and two
doors to the neuroarchive. Restored flags drive the same physical sensors and
CharacterController walked the finished route in both directions. Full Edit Mode
129/129 passed. Broad Play Mode passed 94/96 and the two corrected checks passed
on follow-up. See docs/qa/HOSTFALL2_MINEFIELD_2026-11-17.md.

DIGITAL DETECTIVE verifies a cited chronology and two dates. Its saved source
windows, physical safe and key, five CCTV faces and two-way vault traversal passed
80 pure, 129 Edit Mode and two targeted Play Mode tests. The source preview was
promoted. See docs/qa/HOSTFALL2_DETECTIVE_2026-11-18.md.

CONTAINMENT reassigns R26 in the physical scene, streams one live CCTV view and
unlocks the Central Ring after interlock verification. Safe egress, destroyed
sentry fallback and both-way Ring walk passed. 84 pure, 133 Edit and 100 Play
checks passed. Source preview promoted. See docs/qa/HOSTFALL2_CONTAINMENT_2026-11-18.md.

BUFFER RECALL restores pulse sessions and releases a one-use physical repair
locker at EDGE-01. Preview, partial save, full sequence, corridor traversal and
one-time claim passed. See docs/qa/HOSTFALL2_BUFFER_2026-11-18.md.

FRAGMENT DOCK packs five blocks in the physical DATA-01 workstation. Its saved
partial volume, overlap rejection, armory unlock and traversal passed.
See docs/qa/HOSTFALL2_FRAGMENTS_2026-11-18.md.

QUORUM DISPATCH verified witness dependencies, partial save, the physical west
route both ways and reload. 139 Edit Mode checks and two targeted Play runs passed.
A separate native Intel preview built and started. See
docs/qa/HOSTFALL2_QUORUM_2026-11-18.md.

INTEGRITY AUDIT verifies three banks at the Ring and lights three service markers
in the arena. The targeted Unity Play test and 141/141 full Edit passed at that cut.

Home RECOVERY verifies three stages from archived evidence and opens the service
shuttle and core gate. The targeted Play round trip passed, including a real save
and load inside the moving shuttle. This test earned earlier receipts through
drivers. A separate native Intel bundle with all applications started successfully.
See docs/qa/HOSTFALL2_RECOVERY_2026-11-18.md.
