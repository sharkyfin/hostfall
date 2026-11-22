# ADR 0013 — Surface-mounted armour and budgeted robot work

2026-11-15. Accepted for the user's floating-armour/performance request.

## Context

Two differently shaped robot bodies shared identical armour offsets. Parenting those
plates to Body preserved animation but also preserved the wrong offset. Synchronous
BFS and allocating physics queries could make several enemies search in the same frame.
Each of two MANTIS guardians submitted 141 small mesh renderers.

## Decision

Fit four existing plates to rigid Body-weighted torso triangles in the editor, using
FBX bind poses and correct scale. Keep independent named plates attached to Body.
Readable source import is temporary; no runtime surface search or per-frame mesh bake.

Use reusable local A* data and physics buffers with a shared round-robin scheduler.
Process at most 96 candidate-edge checks or a soft 1.2 ms search budget per frame.
Direct movement safety and floor checks still run immediately; do not freeze distant
robots or weaken collision to claim a performance gain. Recompute paths after transient
obstacles; do not persist derived search heaps in saves.

Cache combat hierarchy and visual broken-part mask. Preserve late-created boss lights
in the death cache. Combine only fixed guardian geometry by shared material, retaining
independent armour, coolant, sensor, core, hatch and all referenced transforms. Reject
empty meshes and unequal index totals. Authoring repairs occur in a backed-up copy.

Keep conventional URP Forward+ in production. GPU Resident Drawer and GPU occlusion
were separately built and run, but failed visual/performance acceptance on this setup.
The diagnostic build helper uses a clean player cache for variants; a successful build
receipt alone is not evidence of a valid rendered frame.

## Consequences

IDs, damage regions, corpse state and save format remain compatible. The fair queue can
introduce a short route-planning delay instead of a simultaneous search spike. A single
physics query can overrun the soft time budget. The local solver is not a new baked
multi-level NavMesh. GPU render selection and measured limitations are in the associated
QA report; unverified rendering settings must not be enabled in the delivered project.
