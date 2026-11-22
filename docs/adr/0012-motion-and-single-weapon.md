# ADR 0012 — Imported body motion and one carried weapon

2026-11-15. Accepted for the user's animation/performance/weapon request.

## Context

The Type-IV body had a procedural leg swing and fixed unarmed hands. Three weapons
shared magazine-reload behaviour and remained available in invisible inventory.
Every impact created/destroyed a light; changing guns recreated the held prefab.
Radar refresh recalculated static geometry. These were avoidable sources of work.

## Decision

Use 12 selected Quaternius UAL1 Standard CC0 actions, exported with Blender and
converted into joint-anchor clips. Retarget onto the existing body, retaining
socket-driven armed IK. The CharacterController owns motion. Camera bob/sway has
one intensity preference, including zero. No disconnected arm model is introduced.

Carry one gun. Picking up another throws the previous actual prefab into the world
with a Rigidbody, ammo, reserve, cooldown and heat. Simulation owns stable item IDs
and collected authored IDs; Unity owns physics and synchronises pose before save.
The optional simulation.weapons and player weapon carryVersion preserve world-v2
compatibility. Legacy extra guns become floor items, not lost inventory.

ARC-12 loads shells individually and can interrupt reload. CIRRUS trades firing
cadence for thermal lockout. TITAN charges while held and fires on release. These
use different timing and damage; robot hit-zone rules remain intact.

Prewarm the three held models and three impact lights. Cache hearing targets,
static map footprints and clipped polygons; update moving doors/bridges separately.
Use a reusable ordered hit buffer, retaining a full-buffer fallback for correctness.
Static world labels use font texture rebuild events instead of per-frame polling.

## Consequences

Dropped guns do not collide with the player and cannot become invisible route
blockers. They remain interactable and collide with the environment. Menus freeze
their physics. Reload, heat, cooldown and charge state enter saves. The library is
curated rather than adding a large unrelated character package. Motion matching,
root-motion navigation and a new animation framework are outside this change.
Measured results and limitations belong in the associated QA report.
