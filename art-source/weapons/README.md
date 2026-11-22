# HOSTFALL Weapon Atelier

Open HOSTFALL-WeaponAtelier.blend in Blender 4.5 LTS. It contains only the original
weapon authoring scene; surface/normal maps are packed. Source manufacturing meshes
are consolidated into three mechanical assemblies per weapon. Semantic grip/muzzle
empties and movable pump/bolt/slide/cell groups survive FBX export.

Reproducible geometry/texture authoring: tools/blender/make-weapon-atelier.py.
Unity uses HF_ARC12, HF_CIRRUS, HF_TITAN in Assets/_HOSTFALL/Art/HostfallCombat.
WeaponAtelierAuthor creates shared held/world prefabs and explicit URP materials.
Prefab scale is 0.85 for both held weapons and world stands. No downloaded meshes,
paid generator, external account or model-service dependency is used.
