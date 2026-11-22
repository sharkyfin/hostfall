# HOSTFALL 2.0. Physical horror pass

## Reproduced and repaired

The first collider from the interaction side of PWR-01 was its own `Transformer Cabinet` (1.10 m). The screen collider was behind it (1.69 m) and the glass faced north. The repair removes the old cabinet, flips the glass south and uses a desk of the correct height. From the regular walking route the first hit is now `Screen interaction surface`.

The Compute Vault bridge previously started with a 2.4 m lateral offset of the centre leaf, exposing a gap between wings. The 2.0 scene now has a flush centre span and handrails. The CharacterController climbs the east stairs, crosses east to west, returns west to east and descends to ground level. The earlier 1.x scene hash is unchanged.

The Energy Center no longer has an active MANTIS-6. The new reserve is a grounded Watcher with its own v3 identity. A targeted test sees the player outside the front cone at close range, excludes line of sight through the closed bulkhead and runs a Watcher through the opened Energy Center door.

The logistics reserves previously toggled on at the fight positions. Three checked lanes start behind a solid shutter. Before the first wave is released, the test observes its visible lead robot moving toward the floor-level exit. It then observes both waves and the passage reopening after they are defeated.

The terminal counter procedure, the Matriarch dodge, SUPER's movable cover, boss clearance, final scene and save/restore passed. The full-route test uses real locomotion and physical interactions for circulation, plus scripted input or direct game actions for some puzzle and combat states.

## Evidence

- Unity 6000.6.8f1 Play Mode, 8 passed. Isolated report `.artifacts/hostfall2-horror/final-playmode.xml`. The first test pass discovered the missing v3 enemy save identity. The final pass includes it and saves and reads the Recovery and epilogue checkpoints.
- The strengthened terminal counter test passed after waiting into the held contact window. It verifies zero damage, physical separation and the stun on landing. See `.artifacts/hostfall2-horror/late-counter.xml`.
- Simulation behavioral checks, 117 passed. Unity API compilation and repository integrity passed.
- Original art in `art-source/hostfall2/PowerDistribution.blend` with a reproducible Python export. The visual reference is the placement and organization of machinery in the engineering deck of [Still Wakes the Deep](https://dominiquebuttiens.artstation.com/projects/dyaVOJ?album_id=983376). Native source shapes, geometry and materials are original.
- After the source checkpoint comparison, the promoted scene SHA-256 is `90942f75d2eb94bef5ceb8f8d883f49c2d38199da5fbacc1d13b68afaa7c6f0a`. Its predecessor is `94ba0f2a0599997421e5396188d6f27214807676844de2b564a9d48665e9be29`.
- Intel macOS QA preview built with zero errors and ran on Metal at 1920×1080. The native first-loop replay used the Home archive, played the three VENTFLOW stages, restored an in-progress save, fired eleven shots after a vent emergence and returned to the earned Home evidence. Both inventoried user save files retained their hashes.
- The fixed-viewpoint native sweep sampled 630 frames on the AMD Radeon Pro 5300M. Nearest-rank p95 was 6.90 ms across the sweep and 7.62 ms in the Compute Vault, compared with 8.61 ms and 20.27 ms in the earlier profiled candidate. The Compute Vault median was 6.30 ms versus 4.81 ms earlier. This compares seven matching viewpoints across candidates. Fight-heavy frame times and first-play pacing still need a manual run. See `.artifacts/hostfall2-horror/native-profile/comparison.json`.
- The macOS QA preview is `apps/unity/Builds/macOS/HOSTFALL-2-Horror-Preview.app`. Its application identifier is the separate QA bundle. Replay writes under its explicit private storage directories.
- WebGL built with zero errors. A clean headless Chrome profile advanced from the menu through New Game and the awakening instructions into the workshop, with the player HUD visible and zero browser runtime error events. The local byte-range server is on port 8763 and serves `apps/unity/Builds/Web/HOSTFALL-2-Horror-Preview`. See `.artifacts/hostfall2-horror/web-gameplay/web-browser.json` and its screenshots.

## Open acceptance

Puzzle selection, migration of the remaining computer chores, broader map art/geometry, full manual timing and encounter tuning remain open. The Play Mode run gives route and save coverage for the current preview. Twenty-minute pacing requires the redesigned campaign and a manual run.
