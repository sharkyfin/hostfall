# 0027. Pursuit across open doors and the service duct

Date 2026-11-20

## Context

WATCHER R17 stopped at the entrance to Operations even when the airlock leaf had opened. The live scene limited the robot's navigation to Z ≥ 1.65 m. The doorway is centred at Z = 1.00 m. Enlarging the range toward the server in the earlier pass left this edge in place. The original V-14 grille in Operations covered a short bay ending at the solid east wall.

## Decision

Keep the original patrol points and give ordinary Watchers a local search rectangle that follows an observed target. Their search may cross the patrol bounds through an actual doorway. The original per-frame path budget and capsule and floor checks still apply. Authored confinement remains for lockdown waves and the capture encounter. The Operations chase envelope also spans both sides of its airlock for close-range motion. The shut door leaf remains a physical obstacle.

Cut the layered east wall around a grounded service duct. Extend the shaft into a vertical return and stage the crawler beyond the old wall. Use the existing ventilation campaign phase to drive the outer grille and an internal isolation damper. Warning sound and local illumination indicate activity behind the wall before the crawler reaches the grille.

## Consequences

The active scene adds no save fields or mandatory puzzle. Existing v3 flags and threat progress govern the damper, and the original v1 scene retains its authored passages. A later selected scenario can drive more than one vent branch through the physical structure. The current chain of rooms still uses their respective door, hazard and encounter constraints.
