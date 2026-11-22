# 0026. Physical horror and reinforcement routes

Date 2026-11-20

## Context

The user found a workstation occluded by its own cabinet, a broken line across the Compute Vault galleries, reinforcements appearing in the logistics aisle and an oversize guardian in the Energy Center. The revised campaign target is twenty minutes and at most five player-approved puzzles.

## Decision

Use a continuous bridge in the 2.0 scene and give the Energy Center a clear south approach to PWR-01. Author the distribution desk and transformer in Blender with a shared paint and metal palette, one mesh per asset and simple collision. Replace the Energy Center crawler with floor-level patrols. Keep short-range sight directional at distance and available around the chassis at close range with line of sight. Rooms share navigation bounds only where there is an authored opening. Reuse the existing scheduled path checks.

Logistics reserves start inside a physical shutter bay and traverse a grounded approach before control passes to their AI. The spent terminal ambush moves through a held contact and a visible fall. Matriarch's strike uses the collision-limited landing position. SUPER changes which physical cover is available in the last phase.

## Consequences

The terminal state machine and v3 receipt validation keep their current contracts during the puzzle selection. The reserve's new identity is accepted only in v3. Prior saves can omit it. The inactive Energy Center crawler can still occur in an old save roster. The 1.x authored scene retains its original bridge and assets. A native frame sweep and a fresh manual run of the redesigned campaign are separate release checks.
