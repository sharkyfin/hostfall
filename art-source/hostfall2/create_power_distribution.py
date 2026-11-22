"""Build the NORTHLINE distribution desk and oil-cooled transformer bank."""
from pathlib import Path
import sys

import bpy

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
import create_engineering_kit as kit
from create_engineering_kit import area, box, conduit, cylinder, material, point


def distribution_desk():
    for x in (-.60, .60):
        for y in (-.265, .265):
            box("Bolted foot", (x, y, .027), (.22, .22, .054), steel, .007)
            box("Load-bearing upright", (x, y, .48), (.095, .095, .91), enamel, .012)
            cylinder("Foot anchor", (x - .062, y, .052), (x - .062, y, .065), .019, steel, 6)
    box("Rear electrical enclosure", (0, .16, .52), (1.15, .28, .76), enamel, .025)
    box("Bolted inspection cover", (0, .308, .53), (1.03, .034, .60), steel, .018)
    for x in (-.40, 0, .40):
        cylinder("Armoured cable gland", (x, .23, .14), (x, .23, .075), .039, steel, 10)
        conduit("Armoured cable", [(x, .23, .08), (x, .23, .04), (x, .29, .028)], .018, gasket)
    box("Pressed worktop", (0, 0, .958), (1.64, .83, .084), enamel, .015)
    box("Worktop edge guard", (0, -.409, .958), (1.58, .030, .056), gasket, .008)
    box("Isolation handle plate", (.62, -.18, 1.010), (.22, .19, .020), steel, .007)
    for x in (.55, .69):
        box("Recess guard", (x, -.18, 1.042), (.024, .18, .063), steel, .003)
    box("Disconnect lever", (.62, -.174, 1.037), (.066, .10, .043), red, .012)


def transformer_bank():
    box("Welded drain pan", (0, 0, .07), (5.36, 5.30, .14), steel, .038)
    box("Painted oil vessel", (0, 0, 1.34), (3.87, 3.87, 2.47), enamel, .095)
    for z in (.21, 2.43):
        box("Bolted tank flange", (0, 0, z), (4.12, 4.12, .13), steel, .025)
    for i in range(11):
        q = (i - 5) * .30
        for sign in (-1, 1):
            box("Side cooling fin", (sign * 2.02, q, 1.31), (.37, .081, 1.92), enamel, .026)
            box("Front cooling fin", (q, sign * 2.02, 1.31), (.081, .37, 1.92), enamel, .026)
    for y in (-2.12, 2.12):
        for z in (.48, 2.14):
            cylinder("Oil cooling header", (-1.80, y, z), (1.80, y, z), .076, steel, 10)
    for x in (-.88, 0, .88):
        cylinder("Ceramic bushing", (x, -.72, 2.48), (x, -.72, 3.03), .112, gasket, 12)
        for i in range(4):
            z = 2.58 + i * .087
            cylinder("Glazed insulator rib", (x, -.72, z), (x, -.72, z + .027), .184, ceramic, 12)
        cylinder("Copper terminal", (x, -.72, 2.99), (x, -.72, 3.08), .088, copper, 12)
    box("Oil fill access", (1.16, .83, 2.51), (.64, .59, .13), steel, .035)
    cylinder("Inspection gauge guard", (0, -1.97, 2.31), (0, -2.14, 2.31), .155, steel, 20)
    cylinder("Gauge face", (0, -2.14, 2.31), (0, -2.15, 2.31), .120, ceramic, 20)
    pointer = box("Gauge pointer", (.021, -2.152, 2.337), (.017, .004, .089), gasket, .001)
    pointer.rotation_euler.y = .55


if __name__ == "__main__":
    kit.out, source, preview = (Path(value) for value in sys.argv[sys.argv.index("--") + 1:])
    bpy.ops.wm.read_factory_settings(use_empty=True)
    steel = material("Steel", (.27, .34, .35), .64, .46)
    enamel = material("PetrolEnamel", (.088, .17, .19), .35, .57)
    gasket = material("Gasket", (.031, .039, .040), .10, .74)
    copper = material("Brass", (.34, .19, .087), .68, .50)
    ceramic = material("InstrumentIvory", (.53, .56, .51), .04, .50)
    red = material("IsolationRed", (.42, .080, .056), .18, .54)
    kit.mesh_export("HF2_DistributionDesk", distribution_desk).location = (-3.45, -.75, 0)
    kit.mesh_export("HF2_TransformerBank", transformer_bank).location = (1, .62, 0)
    concrete = material("WorkshopConcrete", (.094, .115, .122), .03, .86)
    box("Workshop floor", (0, 0, -.085), (12, 11, .15), concrete)
    area("Inspection lamp", (-3.7, -4.5, 5.4), (-.7, 0, 1.35), 1050, (.76, .87, 1), 4)
    area("Warm reflected light", (4.5, -1.6, 4.3), (1, 0, .9), 650, (1, .79, .61), 3)
    bpy.ops.object.camera_add(location=(-4.8, -10.5, 5.15))
    camera = bpy.context.object
    point(camera, (-.65, .05, 1.2))
    camera.data.lens = 39
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 16
    scene.cycles.use_denoising = True
    scene.render.resolution_x, scene.render.resolution_y = 1080, 640
    scene.render.filepath = str(preview)
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    bpy.ops.render.render(write_still=True)
