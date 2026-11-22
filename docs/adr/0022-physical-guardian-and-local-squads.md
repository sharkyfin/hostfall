# ADR 0022 · Physical guardian and local squad tactics

Status · Accepted for the HOSTFALL 2.0 preview, 2026-11-19

The opening computer route shares the same physical world as the combat route.
MANTIS-6 needs readable attacks, independent turret movement and damage that
respects the actual arena. A Watcher group needs to be threatening while leaving
the player an escape window.

MANTIS-6 uses a saved attack clock with fixed warning, impact and recovery
boundaries. Its aim locks before impact. Cannon rays stop at world geometry, a
charge sweeps the real chassis, and ground impact tests a displayed radius.
Component health exposes the reactor during overheating. The 2.0 scene uses
the original 52,008 indices across six materials, rigged to a rigid 19-bone
hierarchy for the turret, recoil slide, wheels and track carriers.

Watchers in a patrol region share recent observed positions while the network
command node is online. The four tactical roles reuse the existing bounded
navigation scheduler. At most two Watchers may commit an attack concurrently.
Losing command removes the shared position and roles. Each Watcher retains its
local sensors. The containment sentry retains its scripted orders.

Version 3 stores the current MANTIS attack and emitted impacts. Unity may turn
an omitted nested reference into an empty serialized object. Only the canonical
empty version-0 value is treated as absent. Older preview saves and the separate
1.x scene still load. The player collider and rendered shuttle hatch now update
together on power-up and docking.
