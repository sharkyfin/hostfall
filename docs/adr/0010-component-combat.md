# ADR 0010 — Damage volumes and a tracked guardian

2026-11-14. Requested combat/geometry revision, following ADR 0007.

The old shotgun gave the same 55 damage for one pellet and a full blast. A normal
hit put WATCHER into TurnOff, making a living target look dead. Both tracked units
also shared an identity, preventing trustworthy per-actor persistence.

Simulation.Combat owns health, destroyed modules, death, pose and AI timers for
WATCHER patrols and the two tracked guardians. RobotCombatController binds this
state to authored meshes/hit volumes and performs final legacy-controller shutdown
once. Other existing archetypes retain their specific legacy armour/phase contracts.
Stable IDs survive; the freight crawler gains its own previously missing ID.
Optional combat/loadout save fields preserve compatibility with previous saves.

Damage queries sort physical hits front-to-back. Dedicated layer 9 contains damage
volumes, excluded from movement/vision queries and player collision. A wall blocks
shots; movement capsules do not mask the smaller sensor volumes. ARC-12 accounts
for every pellet; CIRRUS rewards precise aim; TITAN uses a slower heavy shot. The
camera centre, captured before recoil, defines aim.

MANTIS-6 has two exposed cooling modules, protected hull, vulnerable sensor/drives
and a rear core whose hatch opens after both cooling modules fail. Hull fire alone
cannot kill the sealed guardian. A committed attack telegraph allows a sidestep;
line of sight is rechecked before damage. EMP and broken drives slow/disable it.
Patrol movement uses local bounded floor-checked search, not unconstrained pursuit.

Original bevelled models are generated in the local Blender workshop. Reproducible
source: tools/blender/make-combat-models.py. No external AI service, API credentials
or paid asset dependency. Transient armour fragments are cosmetic, non-colliding,
short-lived and paused with gameplay. No enemy-health widgets are added to the HUD.
Player health is separated from radar; a 4 px solid centre dot supplies aim.

Authoring is a targeted repair in the isolated verification copy, never a full scene
regeneration. Existing author changes and device/puzzle identities are preserved.
