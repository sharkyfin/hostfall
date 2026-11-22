# Type-IV motion source

Quaternius, Universal Animation Library 1, **Standard** free edition, CC0 1.0.
The downloaded Standard archive contains 43 actions; the larger commercial tiers
are not required or included. Source: https://quaternius.itch.io/universal-animation-library
and https://quaternius.com/packs/universalanimationlibrary.html . Retrieved 2026-11-15.
Original license is retained in License.txt. No CRACK assets were extracted.

Selected actions: Idle_Loop, Walk_Loop, Jog_Fwd_Loop, Sprint_Loop, Jump_Start,
Jump_Loop, Jump_Land, Interact, PickUp_Table, Pistol_Reload, Pistol_Shoot, Hit_Chest.
Blender 4.5.16 exports the selected skeleton/NLA tracks to TypeIVMotionSource.fbx
and 492 samples of 16 world-space joint anchors at 30 fps to TypeIVMotionSamples.json.
The converted Unity coordinates are (Blender world X, Z, Y). The importer rig
already contains the source orientation; do not apply a second axis flip.
Blender action slots must be assigned before sampling layered FBX actions.

Editor menu HOSTFALL/Tools/Import Type-IV Motion creates the 12 legacy .anim assets
and TypeIVMotionLibrary in Resources/Motion. OperatorMotion samples cached states
and retargets their joint anchors to the existing articulated Type-IV body.
PlayerMotor/CharacterController exclusively owns position and collision; source
root translation is removed. Weapon sockets retain priority over imported arm
poses so the real hands stay attached to the held model. Jump_Start and Hit_Chest
are available for later blending; not every exported clip is an active gameplay state.

Editable source: art-source/motion/HOSTFALL-TypeIV-Motion.blend.
Exporter: tools/blender/export-typeiv-motion.py.
