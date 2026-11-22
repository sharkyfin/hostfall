"""Create the original landscape seen through the evacuation window."""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector, noise


def material(name, rgb, roughness=0.88):
    surface = bpy.data.materials.new(name)
    surface.diffuse_color = (*rgb, 1)
    surface.use_nodes = True
    shader = surface.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*rgb, 1)
    shader.inputs["Roughness"].default_value = roughness
    return surface


def ridge(name, distance, width, depth, height, phase, surface):
    columns, rows = 58, 18
    vertices = []
    triangles = []
    for r in range(rows + 1):
        row = r / rows
        along = row * depth
        for c in range(columns + 1):
            x = (c / columns - 0.5) * width
            n = noise.fractal(Vector((x / 26, (distance + along) / 18, phase)), 1.05, 2, 3)
            shoulders = 0.58 + 0.28 * math.sin(x * 0.074 + phase) + 0.12 * math.cos(x * 0.23 - phase)
            outline = math.sin(row * math.pi) ** 0.65
            flatten = 1 - 0.77 * math.exp(-((x + 3) / 16) ** 2) if distance < 10 else 1
            z = -1.1 + outline * height * max(0.1, shoulders + n * 0.48) * flatten
            vertices.append((x, distance + along, z))
    for r in range(rows):
        for c in range(columns):
            a = r * (columns + 1) + c
            b = a + columns + 1
            triangles.extend([(a, a + 1, b), (a + 1, b + 1, b)])
    land = bpy.data.meshes.new(name)
    land.from_pydata(vertices, [], triangles)
    land.update()
    obj = bpy.data.objects.new(name, land)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(surface)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True


def main():
    export, source = (Path(item) for item in sys.argv[sys.argv.index("--") + 1 :])
    bpy.ops.wm.read_factory_settings(use_empty=True)
    ridge("Valley basin", 0, 150, 48, 8, 1.7, material("Basin slate", (0.075, 0.09, 0.115)))
    ridge("East ridge", 28, 165, 66, 22, 0.2, material("Middle ridge", (0.13, 0.146, 0.163)))
    ridge("Radio horizon", 53, 215, 72, 40, 4.9, material("Horizon shale", (0.19, 0.20, 0.23)))
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    bpy.ops.export_scene.fbx(filepath=str(export), axis_forward="-Z", axis_up="Y",
                            object_types={"MESH"}, bake_anim=False,
                            path_mode="STRIP", use_mesh_modifiers=True)
    print("Authored", len(bpy.data.objects), "terrain meshes", sum(len(mesh.polygons) for mesh in bpy.data.meshes), "triangles")


if __name__ == "__main__":
    main()
