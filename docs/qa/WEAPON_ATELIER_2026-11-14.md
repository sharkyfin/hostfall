# Weapon Atelier — 2026-11-14

User scope: quality models for the existing three weapons. Dark industrial
NORTHLINE palette retained; no new weapon class, balance system or upgrade UI.

## Delivered assets and presentation

Original profile-modelled ARC-12, CIRRUS and TITAN replace the first combat-kit
block shapes. Open trigger guards, actual bored tubes, shaped grips, stock frames,
recessed fasteners, heat shields, capacitor housings, readable mesh markings and
restrained cyan/violet accents. Shared authored surface/normal/smoothness maps
are 512px; explicit URP materials have opaque depth-writing surfaces.

Three render meshes per weapon, preserving mechanical groups and sockets:

| Asset | Source triangles | Published length (0.85 scale) | Mechanical groups |
| --- | ---: | ---: | --- |
| ARC-12 | 35,780 | 0.870 m | Body, Bolt, Pump |
| CIRRUS | 25,796 | 0.377 m | Body, Slide, Magazine |
| TITAN | 45,492 | 1.017 m | Body, Bolt, Magazine |

Each renderer may contain several material submeshes; three renderers does not
mean three draw calls. Held and world stands use the same calibrated prefab.
No disconnected camera arms: the existing real-body upper sleeves/forearms/gloves
are posed to grip sockets. Reload support follows the removed power pack. Muzzle
flash uses the muzzle socket; pump/bolt/slide have bounded procedural motion.

Portable editable source, with packed surface maps:
`art-source/weapons/HOSTFALL-WeaponAtelier.blend` (2.6MB).
Reproducible authoring: `tools/blender/make-weapon-atelier.py`.

## Problems found by actual visual review

- First consolidation reset material indices when clearing material slots. Fixed
  by preserving/reapplying polygon assignments after replacing the material table.
- Extruded profiles had inward-facing caps. Recalculated outward shell normals
  before bevel/weighted-normal processing; later renders show solid housings.
- Label basis was reflected, creating mirrored/displaced markings. Corrected the
  right-handed basis and placed text within the manufactured panels.
- Initial UV export retained primitive UV0. Every part now has one physical-scale
  surface UV set; authored detail maps are connected consistently in Unity.
- Initial first-person placement obscured too much of the frame. Calibrated both
  model size and grip-relative position, and reviewed all three real game views.
- Static walking arms did not hold the gun. Added a real-body two-segment grip solve
  and support for moving pump/cell assemblies, retaining the unarmed pose.
- Older authoring could regenerate the previous weapon templates. Added protection
  and shared preparation for the atelier resources.

## Executed checks

- Unity 6000.6.8f1 Play Mode **59/59 PASS** (`play-final.xml`). New tests check
  mesh budget/sockets/maps, actual real-body grip distances, mechanism/reload,
  switching without leftover models, plus unchanged combat/map/save/terminal tests.
- Edit Mode **67/67 PASS** (`edit-final.xml`).
- API compilation PASS, existing Cinemachine nullable warning only.
- Actual Blender multi-angle views and Unity studio/first-person/reload renders
  inspected, not inferred from object names. Evidence in
  `.artifacts/weapon-quality-20261114/review/`.
- Main scene was clean and its original hash verified before targeted promotion.
  Only weapon stand presentation was changed in the scene; no full world rebuild.

Native x86_64 build **Succeeded, 0 errors**, 160,349,608 build bytes.
Bundle: `apps/unity/Builds/macOS/HOSTFALL-Arsenal.app`.
`codesign --verify --deep --strict`: exit 0. The actual standalone process ran
12s with -batchmode -nographics, then exited 0 on controlled termination, with
no script/type/load exceptions. This bounded smoke does not validate a visible
native-window playthrough. Repository verification: **572 GUIDs PASS**.
Native editor Reload was confirmed after hash-guarded scene promotion.
Evidence, XML, signature, smoke and promotion hashes are in the artifact directory. This is procedural mechanical presentation, not a complete
finger/character animation production rig or a manual full campaign playthrough.
