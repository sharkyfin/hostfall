# ADR 0025 · Wall lettering and physical clearance

2026-11-20

## Context

The shelter exit had two oversized displays and detached labels in its sightline.
Editor footage rendered several authored TextMeshes with the stock font material.
Home's scheduled keyboard focus could run while the projected panel was hidden.

## Decision

The 2.0 desktops omit their location headers and the on-screen exit button.
Escape continues to close the computer. Home restores focus once the monitor is
interactive and the shell is visible. The physical screen shares the existing
terminal draft, history and Knowledge state.

The scene replaces the large cases with compact relay units and removes the floating
entrance and legacy sector titles. Short labels sit on opaque wall plates. The
workbench uses the runtime depth shader for every world TextMesh during frame
capture and restores each renderer afterwards. Only decorative emissive markings
and oversized switch bodies use the new matte materials.

Use original relay, cable-entry and spill-kit meshes, attached using the existing
collision surfaces. Fit each switch collider to its rotated mesh and check desk
contact and clearance. The freestanding energy-centre control retains its
floor-bolted support. Knobs, enamel and sealed cable glands were inspired by the
engineering work on Beira D. [Still Wakes the Deep engineering](https://dominiquebuttiens.artstation.com/projects/dyaVOJ?album_id=983376)

## Consequences

The physical reader and workstation IDs keep serving saved progress. Visible
navigation uses the map and the wall plates. The floor and side-surface scans flag
candidates for review, and Play Mode validates passages using the real
CharacterController.
