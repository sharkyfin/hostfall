"""Build original wall fixtures and a spill kit for NORTHLINE."""
from pathlib import Path
import sys

import bpy
sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
import create_engineering_kit as kit
from create_engineering_kit import area, box, conduit, cylinder, material, point


def bolt(x, y, z):
    cylinder("Captive bolt", (x, y, z), (x, y - .009, z), .011, steel, 6, .001)


def repeater():
    box("Flush mounting backplate", (0, -.016, .18), (.76, .032, .36), steel)
    box("Pressed cover", (0, -.053, .18), (.72, .082, .328), enamel, .016)
    box("Recessed bezel", (-.072, -.093, .197), (.49, .020, .198), rubber)
    box("Smoked glass", (-.072, -.105, .197), (.456, .012, .168), glass)
    for i in range(5):
        box("Recessed status cell", (-.226 + i*.076, -.112, .194), (.039, .003, .021),
            green if i < 3 else steel, .002)
    for i in range(4):
        box("Speaker slot", (.246, -.096, .132 + i*.037), (.105, .006, .012), rubber, .003)
    for x in (-.328, .328):
        for z in (.051, .307):
            bolt(x, -.097, z)


def junction():
    box("Wall flange", (0, -.014, .265), (.44, .028, .38), steel)
    box("Gasketed body", (0, -.078, .265), (.367, .142, .296), iron, .022)
    box("Cast lid", (0, -.154, .266), (.358, .027, .286), enamel, .015)
    for x in (-.141, .141):
        for z in (.158, .373):
            bolt(x, -.169, z)
    for x in (-.123, 0, .123):
        cylinder("Sealed cable gland", (x, -.080, .130), (x, -.080, .080), .033, steel, 8)
        conduit("Armoured conduit", [(x,-.08,.087),(x,-.080,.028),(x,-.052,-.040),(x,-.037,-.079)], .016, rubber)
        box("Conduit saddle", (x, -.031, -.045), (.070, .027, .032), steel)
    box("Recessed disconnect guard", (.017, -.171, .260), (.134, .020, .099), rubber)
    lever = box("Insulated lever", (.017, -.188, .245), (.056, .037, .061), red, .006)
    lever.rotation_euler.x = -.31


def spill_kit():
    box("Welded tray base", (0, 0, .032), (.51, .38, .052), iron)
    for x in (-.239, .239):
        box("Folded side wall", (x, 0, .102), (.019, .372, .149), enamel)
    for y in (-.176, .176):
        box("Reinforced lip", (0, y, .074), (.47, .022, .098), enamel)
    for x in (-.132, -.018, .096):
        cylinder("Wrapped absorbent roll", (x, -.126, .124), (x, .043, .124), .047, cloth, 20)
        cylinder("Wrap band", (x, -.068, .124), (x, -.041, .124), .048, rubber, 20)
    cylinder("Hand tool grip", (-.154, .102, .097), (.050, .102, .097), .019, red, 12)
    cylinder("Hand tool shaft", (.044, .102, .097), (.184, .102, .097), .012, steel, 10)
    box("Service clamp jaws", (.178, .101, .096), (.079, .058, .031), steel, .004)


if __name__ == "__main__":
    kit.out, source, render = (Path(p) for p in sys.argv[sys.argv.index("--") + 1:])
    bpy.ops.wm.read_factory_settings(use_empty=True)
    steel = material("Steel", (.29, .36, .39), .73, .42)
    enamel = material("PetrolEnamel", (.077, .151, .163), .43, .53)
    iron = material("DarkCasting", (.073, .097, .104), .51, .57)
    glass = material("SmokedGlass", (.019, .045, .047), .23, .32)
    green = material("IndicatorPaint", (.20, .41, .33), .17, .55)
    red = material("IsolationRed", (.37, .046, .031), .31, .48)
    rubber = material("Gasket", (.023, .028, .031), .06, .76)
    cloth = material("AbsorbentCloth", (.41, .38, .30), .02, .89)
    concrete = material("WorkshopConcrete", (.090, .112, .123), .03, .86)
    kit.mesh_export("HF2_ContactRepeater", repeater).location = (-.55, .095, .88)
    kit.mesh_export("HF2_GlandBox", junction).location = (.36, .095, .61)
    kit.mesh_export("HF2_SpillKit", spill_kit).location = (.27, -.30, .04)
    box("Removable wall test panel", (0,.21,.83), (2.35,.235,1.68), concrete)
    box("Workshop floor", (0,-.56,-.044), (2.4,1.91,.16), concrete)
    area("Service light", (-.8,-.68,1.8), (0,0,.65), 68, (.68,.85,1), 1)
    area("Warm bounce", (.82,-.65,1.29), (.3,0,.46), 56, (1,.79,.61), .85)
    bpy.ops.object.camera_add(location=(.10,-3.02,1.59))
    camera = bpy.context.object
    point(camera, (0,-.02,.68))
    camera.data.lens = 44
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 22
    scene.cycles.use_denoising = True
    scene.world = bpy.data.worlds.new("Engineering test bay")
    scene.world.color = (.035, .035, .035)
    scene.render.resolution_x, scene.render.resolution_y = 1200, 780
    scene.render.filepath = str(render)
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    bpy.ops.render.render(write_still=True)
