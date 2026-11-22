# 0018 — Circuit graph owns live power distribution

2026-11-17 · HOSTFALL 2.0 / iteration2.

## Context

CIRCUIT BREAKER must offer continuing power trade-offs, not a single solved button
or permanent unlock that ignores the saved wiring. Its bypass must be a real route.

## Decision

A pure reciprocal-contact graph owns rotations, source switches, relays, connected
capacity/load, explicit energizing, breaker trip/cooling/reset and one commission
receipt. MAIN supplies 6 units, AUX 3; all five loads require 13. The TIE relay can
combine buses. Changing wiring disconnects power before applying the new graph.

World-clock induction-pulse cadence and louver travel belong to that same saved
model; UI focus does not stop powered equipment. CircuitPowerController projects
power onto existing route lights, the cryo door, the SOC maintenance-bypass door,
visible ventilation louvers and an original-art local defensive turret. No global
ambient fill or new navigation scheduler is introduced.

FacilityDoor has an optional computer-power authority; legacy doors retain their
existing access rules. This authority overrides a stale serialized unlock. Power
loss waits for the real player to leave the doorway before closing. Existing door
pose capture remains; logical circuit state reapplies authority at scene creation.

A single commission receipt creates the home journal. Current outputs always derive
from the current wiring; claiming commission never makes all loads permanently live.
V3's optional circuit field defaults to untouched initial wiring for older previews.

## Consequences

The useful choice is lit main route plus ventilation versus a dark powered bypass
and local defence. The shortcut is the real energy-to-SOC maintenance corridor, not
the armory supply dead end. Real CC traversal found the connector's north wall
crossing the T junction; its visible mesh and collider were trimmed together.
Source preview promotion requires passage/restore checks and original-scene hashes.
See docs/qa/HOSTFALL2_CIRCUIT_2026-11-17.md. This does not accept the full iteration2
or replace the later combat, art, streaming, native and final-campaign gates.
