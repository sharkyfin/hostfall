# Computers and applications

IDs identify authored stations, not GameObject names. Existing DeviceIds remain legacy aliases for old persistence; new pc.* IDs are unique. Home is not the SOC WS-07. Every listed external computer eventually gets file manager, documents, actual movable app windows, saveable state and a distinct game. Unimplemented apps must not be shown as working or shipped as dead buttons.

| Station | Actual source / area | Original OS styling | Application / consequence | Iteration |
|---|---|---|---|---|
| pc.home | t00_home / shelter | Linux shell; Terminal, Knowledge, Messages | bounded VFS, nine help chapters, evolving archive | 1 |
| pc.ventflow | SOC WS-07 | Windows 98 | Snake, 3 milestones; vent seals / door power / server access | 1 |
| pc.netdef | CORE-01 / server | GNOME | network wave defence; resources/routes/isolation; robot network changes | 2 |
| pc.circuit | PWR-01 / energy | Arch KDE | power connectors, relays and capacity trade-offs; lights/turrets/doors | 2 |
| pc.signal | QNT-01 / quantum | Windows 11 | phase/frequency synchronisation; timed energy barrier | 2 |
| pc.payload | CARGO-01 / logistics | Windows XP | 3-level inertial platformer, checkpoints/platforms/lasers; lift/conveyor/bridge/alarm | 2 |
| pc.minefield | SEC-07 / security | Windows 7 | solver-verified Minesweeper matching a real room; safe laser route | 2 |
| pc.detective | NEURO-01 / research | macOS | file/mail/photo evidence and chronology; safe/key/archival displays | 2 |
| pc.containment | RBT-01 / robotics | Arch workstation | CCTV/door/patrol/trap tactics; central access | 2 |
| pc.buffer | EDGE-01 / passage | compact retro workstation | temporal memory buffer reconstruction; optional supply cache | 2 |
| pc.fragments | DATA-01 / vault | storage workstation | disk block packing/defragmentation; optional shortcut/supplies | 2 |
| pc.quorum | CLOUD-09 / dispatch | dispatch desktop | dependency/time-slot scheduling, not network wave defence; optional relay/cache | 2 |
| pc.integrity | SUPER-10 / core | quarantine console | bounded checksum range-search to isolate corrupted firmware; optional boss telemetry | 2 |

## First complete game

Snake: 16×12 cells; queued non-reversing turns; length growth; 3 speed/obstacle stages; diagnostic pickups; score, loss, restart and completion. Earned milestones survive retry; exact body/food/direction/seed/partial tick survive leaving and save/load. Food chosen only from a reachable free region. Stage1 seals vent.alpha, stage2 energises access, stage3 unlocks progression and publishes a home log once. Warned vent.beta may already release a robot; closing vents cannot erase an emerged enemy. World keeps running, Esc exits immediately, typing keys do not shoot or move the player.

## OS references (study only; no imported logos/proprietary assets)

[Microsoft window anatomy](https://learn.microsoft.com/en-us/windows/win32/winmsg/about-windows), [Windows 7 UX guide](https://learn.microsoft.com/en-us/windows/win32/uxguide/guidelines), [GNOME HIG](https://developer.gnome.org/hig/), [KDE HIG](https://develop.kde.org/hig/), [Apple windows](https://developer.apple.com/design/human-interface-guidelines/windows). Use structural conventions (classic bevels/taskbar, dock/menu bar, header bars, active window states) in original UI Toolkit styles and icons. These are simulated systems, never actual Windows/macOS/Linux installations.

## Implemented Network application

Eight-node graph, three waves, four CPU units shared with isolation, infection/purge
and switchable routing. Later waves inject at relay and final branch nodes. GNOME
window/file conventions use original assets. World consequences are bounded local
robot radio, one saved outage, two physical reserves, vault/energy access and a home
quarantine journal. See docs/qa/HOSTFALL2_NETWORK_2026-11-17.md for actual checks and
limits; the table above is the target catalog, not a list of completed apps.

## Implemented Circuit application

Original Plasma-like desktop and retained electrical schematic; reciprocal rotating
contacts, five load relays, TIE, two selectable sources, shared component budgets,
trip/cooling/manual reset. Lighting/vent/main route versus dark shortcut/defence are
real alternative configurations. Exact partial state and one archive reward persist.
Physical light/shutter/turret/door and both-way CC checks plus broader 91/91 Play
regression passed; details in HOSTFALL2_CIRCUIT_2026-11-17.md.


## Implemented Signal application

Original Fluent-style desktop and live three-channel oscilloscope. The player tunes
frequency and phase, enables channels and uses polarity inversion. Three calibrations
narrow tolerance and time. The final lock creates a 20-second physical gate window,
with a separate north actuator for later return. Ventilation cools the installation.
See docs/qa/HOSTFALL2_SIGNAL_2026-11-17.md.


## Implemented Minefield application

Three fixed 6×6 grids independently solved using only clues and flags. Screen
coordinates match the authored sensor floor. Correct flags disable their beams,
and mistakes start a local alarm. All three wins disarm the floor and open the
CCTV and neuroarchive shutters. The actual round trip was verified in Play Mode.
See docs/qa/HOSTFALL2_MINEFIELD_2026-11-17.md.

## Implemented Digital Detective application

Four ordered events each require a collected admissible source. The actual date
and the published date require separate citations. The original desktop uses
independent files, mail, photos, notes and recordings. The verdict opens a real
safe. A separate key pickup opens the memory vault. The observation room plays
the verified archive on its five monitors. See
[Digital Detective QA](../qa/HOSTFALL2_DETECTIVE_2026-11-18.md).

## Implemented Containment application

RBT-01 displays the quarantine lane and three live CCTV angles. Patrol,
calibration and capture orders drive the same physical R26. Two visible shutters
respond to beam occupancy and release when the player enters the cage. The
verified relay switch opens the Central Ring and publishes a home report.
[Containment QA](../qa/HOSTFALL2_CONTAINMENT_2026-11-18.md)

## Implemented Buffer Recall application

Three recall sequences use visible pulses and decoy signals. Playback and answers
can be resumed or edited, and a completed sequence bank opens the EDGE-01 repair
locker. Its physical kit can be taken once and restores 35 chassis health.
[Buffer QA](../qa/HOSTFALL2_BUFFER_2026-11-18.md)

## Implemented Fragment Dock application

Place five rotatable fragments around immutable boot cells. The journal remains
contiguous. The saved volume unlocks the original SEC-02 armory. [Fragment Dock QA](../qa/HOSTFALL2_FRAGMENTS_2026-11-18.md)

## Implemented Quorum Dispatch application

Assign six jobs to three witnesses within seven ticks. Valid scheduling respects
power, witness permission, time windows and predecessor completion. Dispatch
opens the western path from the Ring to logistics.
[Quorum QA](../qa/HOSTFALL2_QUORUM_2026-11-18.md)

## Integrity Audit and home Recovery

SUPER-10 provides checksums for contiguous spans of three service banks. Each
bank has a finite probe budget. A confirmed report identifies three actual
service points in the arena. The workstation is accessible from the Ring.

Home Recovery opens after the eight mandatory receipts. The player uses six
archived records to reconstruct a directed network, assign credentials and
order startup operations. Completing all three stages releases the shuttle
and core gate. [Integrity and Recovery QA](../qa/HOSTFALL2_RECOVERY_2026-11-18.md)
