"""Author the Type-IV's original face plate and visible hinge hardware."""
import math
import sys
from pathlib import Path

import bpy


def finish(obj, name, surface):
    obj.name = name
    obj.data.materials.append(surface)
    return obj


def material(name, rgba, metallic, roughness):
    result = bpy.data.materials.new(name)
    result.diffuse_color = rgba
    result.use_nodes = True
    shader = result.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = rgba
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    return result


def slab(name, position, size, surface, bevel, angle=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=position)
    obj = bpy.context.object
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.rotation_euler[1] = math.radians(angle)
    fillet = obj.modifiers.new("Machined edges", "BEVEL")
    fillet.width = bevel
    fillet.segments = 3
    obj.modifiers.new("Face weighted normals", "WEIGHTED_NORMAL")
    return finish(obj, name, surface)


def ball(name, position, size, surface):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=16, radius=0.5, location=position)
    obj = bpy.context.object
    obj.scale = size
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return finish(obj, name, surface)


def main():
    export, source = (Path(item) for item in sys.argv[sys.argv.index("--") + 1:])
    bpy.ops.wm.read_factory_settings(use_empty=True)
    steel = material("Type IV / bead blasted titanium", (0.36, 0.41, 0.42, 1), 0.75, 0.39)
    ceramic = material("Type IV / oxide ceramic", (0.17, 0.22, 0.24, 1), 0.16, 0.42)
    recess = material("Type IV / gasket", (0.024, 0.042, 0.045, 1), 0.16, 0.64)
    lens = material("Type IV / optical cyan", (0.10, 0.68, 0.72, 1), 0.25, 0.30)
    ball("Back of cranial housing", (0, 0.027, 0.028), (0.248, 0.209, 0.245), ceramic)
    slab("Removable brow", (0, -0.052, 0.078), (0.223, 0.145, 0.079), steel, 0.028)
    slab("Optical mask", (0, -0.116, 0.028), (0.200, 0.031, 0.066), recess, 0.018)
    slab("Lower jaw", (0, -0.068, -0.082), (0.139, 0.122, 0.07), steel, 0.024)
    slab("Mouth resonator", (0, -0.132, -0.068), (0.089, 0.016, 0.024), recess, 0.008)
    slab("Nasal bridge", (0, -0.133, -0.011), (0.027, 0.035, 0.051), steel, 0.01)
    for side in (-1, 1):
        x = side * 0.053
        slab("Optical lens", (x, -0.131, 0.032), (0.057, 0.019, 0.021), lens, 0.008)
        slab("Cheek casting", (side * 0.079, -0.087, -0.021), (0.061, 0.079, 0.099), steel, 0.02, side * -15)
        ball("Seated jaw bearing", (side * 0.105, -0.024, -0.058), (0.041, 0.058, 0.062), recess)
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    bpy.ops.export_scene.fbx(filepath=str(export), axis_forward="-Z", axis_up="Y",
                            object_types={"MESH"}, bake_anim=False, path_mode="STRIP")
    print("Authored", len(bpy.data.objects), "parts")


if __name__ == "__main__":
    main()
