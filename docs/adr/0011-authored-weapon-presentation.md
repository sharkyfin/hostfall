# ADR 0011 — Authored weapons, sockets and real body hands

2026-11-14. The initial combat kit established three weapons but used simple shapes,
a shared rigid presentation pose and independent weapon placement. The player asked
for quality weapon models, consistent with the existing dark industrial palette.

The Weapon Atelier contains original profile-modelled ARC-12, CIRRUS and TITAN assets,
shared fine surface maps, outward shell normals, physical-scale UVs, readable mesh
markings and real bores/trigger guards. Each asset has three render meshes, semantic
moving groups and GripRight/GripLeft/Muzzle sockets. Import preserves the authored
origin and resolves explicit URP materials. The same prefab is used in the hands and
on physical weapon stands; a single calibrated prefab scale applies to both.

WeaponPresentation animates the pump, bolt, slide and power pack. It derives the
muzzle light from the actual muzzle socket. OperatorVisual poses existing upper
sleeves, forearms and gloves against the sockets with a two-segment solve; it does
not create disconnected camera arms. Unarmed poses are restored when not holding
an active model. This is procedural presentation, not a new character animation rig.

Damage, weapon ownership, ammunition and saves retain ADR 0010's contracts. No new
weapon type, upgrade menu, combat balance change or dependency/service is introduced.
Source authoring is reproducible with tools/blender/make-weapon-atelier.py. The older
combat author protects these resources once the atelier surface maps are present.
Author/QA runs happen in the isolated project before scene/hash-guarded promotion.
