# HOSTFALL original workstation props

Created in Blender 4.5 LTS on 2026-11-15 specifically for HOSTFALL. Original procedural geometry; no downloaded meshes, paid generators or third-party OS graphics.

Reproduction: `tools/blender/make-workstation-atelier.py` through the local Blender MCP.
Editable scene: `art-source/workstations/HOSTFALL-Workstations.blend`.

- HF_FieldNotebook: rugged notebook, 8,904 triangles, 3 mesh renderers.
- HF_StudioConsole: alloy all-in-one, 8,112 triangles, 3 mesh renderers.
- HF_OperationsPC: monitor, keyboard and service tower, 9,300 triangles, 3 mesh renderers.
- HF_ArchiveKey: protective USB media body, open connector sleeve/contact tongue, 2,484 triangles, 2 mesh renderers.
- HF_ServicePanel: local task-light switch, 684 triangles, 2 mesh renderers.

Shared URP materials; indicators use emission without additional lights. Models have metre scale, do not use animations or runtime mesh baking. Screen corner markers are authored separately for camera framing/native text projection. Imported FBX handedness is resolved in WorkstationWorkshop. Mesh renderer counts are not draw-call counts: the chassis contains shared material submeshes.

OS family changes the terminal chrome. These are fictional presentations of trusted game sessions, not running macOS/Windows installations.
