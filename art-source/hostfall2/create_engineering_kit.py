"""Build original, metre-scale NORTHLINE fittings and an editable lighting study."""
from math import cos, pi, radians, sin
from pathlib import Path
import sys

import bpy
from mathutils import Vector


def material(name, color, metal=0, rough=.53, glow=0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Metallic'].default_value = metal
    shader.inputs['Roughness'].default_value = rough
    shader.inputs['Emission Color'].default_value = (*color, 1)
    shader.inputs['Emission Strength'].default_value = glow
    if metal > .3 and glow == 0:
        nodes = mat.node_tree.nodes
        speckle = nodes.new('ShaderNodeTexNoise')
        speckle.inputs['Scale'].default_value = 46
        patina = nodes.new('ShaderNodeValToRGB')
        patina.color_ramp.elements[0].position = .22
        patina.color_ramp.elements[0].color = (*(v * .63 for v in color), 1)
        patina.color_ramp.elements[1].position = .76
        patina.color_ramp.elements[1].color = (*(v * 1.12 for v in color), 1)
        mat.node_tree.links.new(speckle.outputs['Fac'], patina.inputs['Fac'])
        mat.node_tree.links.new(patina.outputs['Color'], shader.inputs['Base Color'])
    return mat


def part(name, mat, edge=0):
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    if edge:
        bevel = obj.modifiers.new('Machined and painted edges', 'BEVEL')
        bevel.width, bevel.segments = edge, 2
        obj.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL')
    return obj


def box(name, at, size, mat, edge=.007):
    bpy.ops.mesh.primitive_cube_add(size=1, location=at)
    bpy.context.object.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return part(name, mat, edge)


def cylinder(name, start, end, radius, mat, sides=20, edge=.003):
    delta = Vector(end) - Vector(start)
    bpy.ops.mesh.primitive_cylinder_add(vertices=sides, radius=radius, depth=delta.length,
                                       location=(Vector(start) + Vector(end)) * .5)
    bpy.context.object.rotation_euler = delta.to_track_quat('Z', 'Y').to_euler()
    obj = part(name, mat, edge)
    for face in obj.data.polygons:
        face.use_smooth = len(face.vertices) == 4
    return obj


def conduit(name, points, radius, mat):
    curve = bpy.data.curves.new(name, 'CURVE')
    curve.dimensions, curve.resolution_u = '3D', 8
    curve.bevel_depth, curve.bevel_resolution, curve.use_fill_caps = radius, 3, True
    spline = curve.splines.new('BEZIER')
    spline.bezier_points.add(len(points) - 1)
    for handle, point in zip(spline.bezier_points, points):
        handle.co = point
        handle.handle_left_type = handle.handle_right_type = 'AUTO'
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    curve.materials.append(mat)


def caption(text, at, size, mat):
    curve = bpy.data.curves.new(text, 'FONT')
    curve.body, curve.size, curve.extrude = text, size, .0004
    obj = bpy.data.objects.new(text, curve)
    obj.location, obj.rotation_euler = at, (pi / 2, 0, 0)
    bpy.context.collection.objects.link(obj)
    curve.materials.append(mat)


def dial(x, y, z, r):
    cylinder('Gasket and gauge body', (x, y + .035, z), (x, y, z), r, rubber)
    cylinder('Brass gauge rim', (x, y, z), (x, y - .012, z), r * .96, brass)
    cylinder('Pressure scale', (x, y - .013, z), (x, y - .017, z), r * .82, paper)
    for i in range(9):
        angle = radians(30 + 37.5 * i)
        tick = box('Gauge tick', (x + r * .60 * cos(angle), y - .019,
                                z + r * .60 * sin(angle)), (.004, .003, .013), rubber, 0)
        tick.rotation_euler[1] = pi / 2 - angle
    needle = box('Gauge needle', (x - r * .15, y - .021, z + r * .20), (.006, .003, r * .58), red, 0)
    needle.rotation_euler[1] = -.57
    caption('bar', (x - r * .30, y - .021, z - r * .36), r * .36, rubber)


def manifold():
    box('Bolted backer', (0, -.027, .56), (1.01, .055, 1.11), enamel)
    box('Recessed instruction plaque', (0, -.063, .94), (.80, .025, .19), rubber)
    caption('NORTHLINE  /  V-14', (-.35, -.079, .958), .063, paper)
    caption('ISOLATE BEFORE SERVICE', (-.35, -.080, .898), .036, paper)
    for x in (-.380, .38):
        for z in (.12, .81):
            cylinder('Backer fixing', (x, -.051, z), (x, -.083, z), .019, steel, 6)
    for direction in (-1, 1):
        conduit('Swept return pipe', [(direction * .43, -.077, .16), (direction * .43, -.15, .28),
                    (direction * .33, -.157, .33), (direction * .20, -.157, .33)], .045, oxidized)
        cylinder('Pipe port union', (direction * .43, -.052, .16), (direction * .43, -.111, .16), .065, steel, 8)
        cylinder('Service union', (direction * .20, -.157, .33), (direction * .15, -.157, .33), .065, brass, 8)
    cylinder('Valve cast housing', (0, -.154, .33), (0, -.285, .33), .123, oxide)
    cylinder('Stem and central nut', (0, -.285, .33), (0, -.339, .33), .034, brass, 8)
    bpy.ops.mesh.primitive_torus_add(major_segments=28, minor_segments=8, major_radius=.154,
                                    minor_radius=.017, location=(0, -.341, .33), rotation=(pi / 2, 0, 0))
    part('Red quarter-turn handwheel', red)
    for angle in (0, 2 * pi / 3, 4 * pi / 3):
        cylinder('Handwheel spoke', (0, -.342, .33), (.15 * cos(angle), -.342, .33 + .15 * sin(angle)), .011, red, 10)
    conduit('Gauge capillary', [(.12, -.16, .34), (.24, -.17, .42), (.26, -.16, .59), (.23, -.16, .65)], .016, brass)
    dial(.23, -.143, .69, .104)


def pump():
    box('Drip tray', (0, 0, .041), (1.38, .67, .072), enamel)
    for y in (-.27, .27):
        box('Angle iron skid member', (0, y, .078), (1.33, .073, .061), steel)
        for x in (-.56, .55):
            cylinder('Anchor bolt', (x, y, .106), (x, y, .141), .022, brass, 6)
    for x in (-.37, .34):
        box('Pump saddle', (x, 0, .15), (.29, .48, .15), oxide)
    cylinder('Motor barrel', (-.59, 0, .38), (-.04, 0, .38), .235, enamel, 28)
    for x in (-.53, -.46, -.39, -.32, -.25, -.18):
        cylinder('Cooling fin', (x, 0, .38), (x + .025, 0, .38), .254, steel, 24)
    cylinder('Motor end cap', (-.67, 0, .38), (-.58, 0, .38), .215, oxide)
    cylinder('Shaft guard', (-.055, 0, .38), (.15, 0, .38), .134, rubber)
    cylinder('Volute casting', (.12, 0, .38), (.43, 0, .38), .268, oxidized, 32)
    cylinder('Machined flange', (.424, 0, .38), (.478, 0, .38), .238, steel, 28)
    for i in range(8):
        a = i * pi / 4
        y, z = .184 * cos(a), .38 + .184 * sin(a)
        cylinder('Flange fastener', (.473, y, z), (.50, y, z), .020, brass, 6)
    cylinder('Suction flange', (.48, 0, .38), (.655, 0, .38), .10, steel)
    conduit('Discharge elbow', [(.28, 0, .58), (.28, -.035, .73), (.33, -.16, .79), (.46, -.20, .79)], .071, oxidized)
    cylinder('Discharge coupling', (.443, -.20, .79), (.558, -.20, .79), .097, steel, 12)
    box('Junction box', (-.30, -.125, .625), (.246, .224, .135), oxide)
    box('Motor id plaque', (-.30, -.243, .626), (.199, .012, .07), rubber)
    caption('P-07   RETURN', (-.39, -.251, .615), .026, paper)
    conduit('Armoured cable tail', [(-.41, -.16, .625), (-.56, -.20, .62), (-.67, -.26, .48),
                 (-.62, -.27, .18), (-.51, -.28, .118)], .019, rubber)
    dial(.015, -.227, .643, .076)
    conduit('Pressure pickup', [(.03, -.205, .59), (.10, -.22, .49), (.23, -.21, .45)], .012, brass)


def light():
    box('Pressed steel backplate', (0, -.03, .16), (.56, .06, .32), oxide)
    box('Weather sealed gasket', (0, -.068, .16), (.48, .033, .235), rubber)
    box('Ribbed warm white diffuser', (0, -.106, .16), (.389, .085, .163), diffuser, .026)
    for x in (-.23, -.155, -.077, 0, .077, .155, .23):
        conduit('Impact cage rib', [(x, -.065, .054), (x, -.145, .070), (x, -.162, .16),
                     (x, -.146, .251), (x, -.065, .266)], .009, steel)
    for z in (.093, .226):
        cylinder('Cage stringer', (-.248, -.152, z), (.248, -.152, z), .009, steel, 12)
    for x in (-.255, .255):
        cylinder('Captive mounting screw', (x, -.052, .16), (x, -.085, .16), .015, brass, 6)


def mesh_export(name, build):
    before = set(bpy.data.objects)
    build()
    pieces = set(bpy.data.objects) - before
    bpy.ops.object.select_all(action='DESELECT')
    for obj in pieces:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = min(pieces, key=lambda obj: obj.name)
    bpy.ops.object.convert(target='MESH')
    bpy.ops.object.join()
    asset = bpy.context.object
    asset.name = name
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.export_scene.fbx(filepath=str(out / f'{name}.fbx'), use_selection=True,
                            axis_forward='-Z', axis_up='Y', object_types={'MESH'}, bake_anim=False)
    return asset


def point(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()


def area(name, position, target, watts, color, size):
    data = bpy.data.lights.new(name, 'AREA')
    data.energy, data.color, data.shape, data.size = watts, color, 'DISK', size
    lamp = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(lamp)
    lamp.location = position
    point(lamp, target)


if __name__ == '__main__':
    out, source, render = (Path(item) for item in sys.argv[sys.argv.index('--') + 1:])
    bpy.ops.wm.read_factory_settings(use_empty=True)
    steel = material('Steel', (.31, .37, .39), .77, .36)
    enamel = material('PetrolEnamel', (.071, .155, .171), .53, .43)
    brass = material('Brass', (.31, .208, .086), .64, .53)
    rubber = material('Gasket', (.023, .031, .037), .05, .78)
    red = material('IsolationRed', (.43, .055, .034), .31, .47)
    paper = material('InstrumentIvory', (.73, .71, .61), .08, .67)
    oxidized = material('CastIron', (.101, .204, .189), .47, .55)
    oxide = material('DarkCasting', (.07, .084, .092), .57, .61)
    diffuser = material('WarmDiffuser', (.74, .63, .45), .07, .37, 2)
    floor = material('WorkshopConcrete', (.081, .105, .110), .05, .85)

    wall = mesh_export('HF2_IsolationManifold', manifold)
    wall.location = (-.89, .15, .40)
    skid = mesh_export('HF2_ReturnPump', pump)
    skid.location = (.55, -.52, 0)
    lamp = mesh_export('HF2_InspectionLight', light)
    lamp.location = (.78, .15, 1.35)
    box('Rear wall of test bay', (0, .285, 1.11), (3.9, .27, 2.22), floor, .02)
    box('Test bay concrete', (0, -.50, -.10), (4.0, 2.38, .20), floor)
    for z in (.23, 1.84):
        box('Stiffener frame', (0, .105, z), (3.3, .10, .055), oxide)
    area('Amber inspection bounce', (1.25, -.94, 2.05), (.65, -.11, .68), 60, (1, .78, .58), .8)
    area('Cold deck edge', (-1.47, -.80, 2.0), (-.35, -.17, .68), 103, (.47, .75, 1), 1.6)
    area('Soft service fill', (.18, -2.44, 1.06), (0, 0, .66), 24, (.68, .84, .91), 1.6)
    bpy.ops.object.camera_add(location=(.18, -3.45, 1.78))
    camera = bpy.context.object
    point(camera, (0, -.08, .77))
    camera.data.lens = 35
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 20
    scene.cycles.use_denoising = True
    scene.world = bpy.data.worlds.new('Dark test bay')
    scene.world.color = (.035, .035, .035)
    scene.render.resolution_x, scene.render.resolution_y, scene.render.resolution_percentage = 1350, 900, 100
    scene.render.filepath = str(render)
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    bpy.ops.render.render(write_still=True)
