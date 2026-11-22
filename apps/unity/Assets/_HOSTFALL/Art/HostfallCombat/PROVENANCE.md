# HOSTFALL Combat Workshop — original assets

Created locally in Blender 4.5.16 LTS on 2026-11-14 for HOSTFALL.
Authoring source: tools/blender/make-combat-models.py (project root).
No third-party meshes, downloaded textures or generated cloud assets are used.

- HF_ARC12.fbx: twin-tube shockgun, receiver, stock, grip and pump.
- HF_CIRRUS.fbx: compact capacitor sidearm.
- HF_TITAN.fbx: heavy multi-coil inductor.
- HF_MANTIS6.fbx: tracked industrial guardian; independent sensor, cooling pods,
  rear core/hatch and removable skirt armour.

Metre-sized geometry, bevelled normals, explicit URP material assets. Mesh part names
are semantic bindings used by damage volumes; do not merge sensor/coolant/core parts.
The .blend authoring snapshot is in .artifacts/combat-rework-20261114/CombatModels.blend.


## Weapon Atelier revision 2 — 2026-11-14

The three weapon FBX files now come from `tools/blender/make-weapon-atelier.py`.
Editable portable source: `art-source/weapons/HOSTFALL-WeaponAtelier.blend`.
Original profile/annular meshes, engraved mesh labels and generated HF2 surface,
normal and smoothness textures; no external source assets or generator services.
Three mechanical render meshes per weapon, 25.8k–45.5k triangles, with semantic
grip/muzzle sockets. Prefab scale 0.85 is shared by held and world models.
MANTIS-6 remains the original combat workshop model. Do not centre weapon bounds
at import: GripRight/socket origin is part of the real-body presentation contract.
