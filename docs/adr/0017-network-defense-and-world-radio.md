# 0017 — Network defence, saved radio cadence and physical reserves

2026-11-17 · Implemented and checked under ADR0016 / iteration2.

## Context

The GNOME application must change real robot coordination, outages, reinforcements
and physical access. A UI-only strategy or a timer that restarts on scene load would
break the requested live-world interaction and partial saves.

## Decision

NetworkDefenseSession owns an eight-node directed graph, three attack waves, CPU/
isolation budget, infection, purge charges and exact 30 Hz process state. The UI
issues typed selections/routes/defence commands and advances only the focused app.
NetworkSecuritySystem owns world-clock outage, radio cadence and reserve release
progress independently of the window. Milestone receipts remain idempotent.

ServerNetworkController projects that state onto a bounded local relay of actual
sightings, two authored reserve actors and a physical duct. Isolation stops shared
memory, while local vision/hearing survive; outages temporarily stop connected
actors. Already released reserves are never removed when future releases are cancelled.
Enemy IDs and combat memory remain stable and saved. No runtime BakeMesh or new
navigation scheduler is introduced. This is a foundation for iteration3 squads,
not a claim that tactical group AI is finished.

## Consequences

The release lane and player bypass require capsule and CharacterController checks.
The initial north duct obstructed the desk bypass and was moved into the east rack
bay; its Quaternius rack was retained in a clear vault position. Window layout is
bounded above the taskbar. The physical screen still has one DOM/render texture.
V3 adds optional network/security fields; older iteration1/v2 data remain readable
under their explicit version boundaries. A consumed outage must not replay on load.
