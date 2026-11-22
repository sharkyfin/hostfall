# ADR 0009 — Measured world repairs and local editor workbench

Date: 2026-11-14. Status: accepted for the current authorized geometry/tooling pass.

## Context

The scene remained traversable at coarse main checkpoints while upper routes were broken.
Several nonuniformly scaled stair instances had reversed visual ascent and a tilted box
collision plane that did not match their mesh. S09 also occupied part of S06. Large floor
objects received only four additional lights in the existing URP Forward configuration.
Model sources were present, but their use and physical placement needed explicit review.

## Decision

Use local Unity/Blender MCP with explicit instance selection, measured scene reports,
actual camera captures and CharacterController regressions. Keep a separate verification
copy and hash-guard promotion instead of regenerating the authored scene. Author small
original Blender props from versioned scripts and reuse the existing curated palette.

Replace only reviewed circulation defects: native-coordinate stair surface meshes,
corrected deck/rail endpoints, separated sector footprints and visible missing wall sections.
Use metric UVs on large primitive surfaces and URP Forward+ for clustered local lights.
No change to gameplay control, offline architecture, renderer family or simulation ownership.

## Consequences

Tool configuration is local to this Mac; instructions and a readiness probe are versioned.
No paid cloud generators are required. Real editor camera evidence complements numerical
checks; neither a grid scan nor automated traversal claims a completed campaign run.
Forward+ must be built and rendered on the target Mac; screenshots alone do not establish
an FPS budget. Existing third-party provenance stays with its sources; the service junction
is original project geometry, not an asset downloaded from an external pack.
