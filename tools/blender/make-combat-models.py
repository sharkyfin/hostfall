"""Original HOSTFALL hard-surface combat kit. Blender 4.5; no external assets.
Only the named authoring scene is replaced. Unity units: metres, forward +Z.
"""
import bpy,math,os,json
from mathutils import Vector
scene_name='HOSTFALL Combat Workshop'
old=bpy.data.scenes.get(scene_name)
if old:
    for obj in list(old.objects):bpy.data.objects.remove(obj,do_unlink=True)
    bpy.data.scenes.remove(old)
scene=bpy.data.scenes.new(scene_name);bpy.context.window.scene=scene
palette={}
def mat(key,color,metal=.0,rough=.4,emit=0):
    m=bpy.data.materials.get('HF Combat '+key) or bpy.data.materials.new('HF Combat '+key);m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emit
    palette[key]=m
mat('ceramic',(.22,.29,.285),.55,.38);mat('graphite',(.065,.085,.10),.65,.33)
mat('steel',(.36,.43,.47),.85,.3);mat('rubber',(.025,.032,.038),.1,.72)
mat('copper',(.40,.20,.08),.8,.3);mat('cyan',(.035,.55,.68),.35,.26,2)
mat('red',(.62,.035,.018),.3,.23,2);mat('violet',(.38,.065,.65),.4,.24,2)
mat('bone',(.60,.61,.54),.4,.5)
roots={}
def root(name):
    o=bpy.data.objects.new(name,None);scene.collection.objects.link(o);roots[name]=o;return o
current=None
def finish(o,name,p,s,m,bevel=.01):
    o.name=name;o.location=p;o.scale=s
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(palette[m]);o.parent=current
    if bevel:
        mod=o.modifiers.new('Machined edge','BEVEL');mod.width=bevel;mod.segments=2;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
        n=o.modifiers.new('Panel normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=n.name)
    return o
# Coordinates below: Blender X right, Y aft, Z up. Nose points -Y.
def box(n,p,s,m='graphite',b=.01):
    bpy.ops.mesh.primitive_cube_add(size=1);return finish(bpy.context.object,n,p,s,m,b)
def cyl(n,p,r,length,m='steel',axis='Y',v=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=v,radius=r,depth=length)
    o=finish(bpy.context.object,n,p,(1,1,1),m,min(r*.12,.008))
    if axis=='Y':o.rotation_euler[0]=math.pi/2
    if axis=='X':o.rotation_euler[1]=math.pi/2
    return o
def bolts(p,width):
    for x in [-width/2,width/2]:cyl('Captive fastener',(x,p[1],p[2]),.009,.014,'steel',axis='X',v=8)
def weapon_base(name,length,width):
    global current
    current=root(name)
    box('Receiver',(0,0,.02),(width,length,.10),'ceramic')
    grip=box('Grip',(0,.08,-.09),(.055,.09,.16),'rubber');grip.rotation_euler[0]=-.22
    box('Trigger guard',(0,.0,-.06),(.065,.09,.018),'steel',.004)
    box('Top rail',(0,-.025,.08),(.035,length*.7,.018),'steel',.003)
    box('Rear sight',(0,.10,.10),(.055,.035,.04),'graphite',.004)
    for x in [-1,1]:
        for y in [-.1,0,.1]:box('Side fastener',(x*width*.51,y,.02),(.009,.015,.015),'steel',.002)
weapon_base('HF_ARC12',.34,.095)
for z in [.025,-.025]:
    cyl('Barrel',(0,-.32,z),.025,.37,'steel');cyl('Bore',(0,-.51,z),.018,.012,'rubber')
box('Pump',(0,-.24,-.07),(.10,.20,.08),'graphite')
for y in [-.31,-.27,-.23,-.19]:box('Pump grip',(0,y,-.114),(.104,.014,.012),'rubber',.002)
box('Stock spine',(0,.27,.025),(.045,.22,.045),'steel')
box('Shoulder pad',(0,.40,.015),(.09,.055,.15),'rubber')
for x in [-.052,.052]:
    for y in [-.15,-.1,-.05]:box('Receiver vent',(x,y,.025),(.006,.025,.045),'rubber',.001)
box('Status window',(.052,.04,.045),(.006,.045,.008),'cyan',.001)
box('Sight insert',(0,-.13,.105),(.012,.02,.008),'red',.002)
weapon_base('HF_CIRRUS',.21,.075)
box('Slide',(0,-.075,.035),(.083,.25,.065),'graphite')
cyl('Pulse emitter',(0,-.22,.035),.028,.08,'steel');cyl('Emitter lens',(0,-.267,.035),.017,.012,'cyan')
box('Battery heel',(0,.10,-.19),(.067,.095,.025),'ceramic')
for y in [-.13,-.08,-.03]:
    for x in [-.044,.044]:box('Slide vent',(x,y,.05),(.006,.018,.03),'rubber',.002)
box('Charge gauge',(.044,.055,.055),(.005,.038,.008),'cyan',.001)
weapon_base('HF_TITAN',.40,.14)
box('Power cell',(0,.05,-.045),(.17,.20,.14),'graphite')
box('Induction spine',(0,-.35,.035),(.065,.50,.07),'steel')
for x in [-.065,.065]:
    box('Magnetic rail',(x,-.35,.035),(.028,.43,.08),'ceramic')
    for y in [-.18,-.24,-.30,-.36,-.42,-.48]:box('Coil segment',(x,y,.035),(.045,.025,.12),'copper',.004)
    box('Capillary', (x,-.34,.101),(.008,.35,.006),'violet',.001)
box('Emitter crown',(0,-.59,.035),(.18,.06,.14),'graphite')
box('Emitter aperture',(0,-.625,.035),(.055,.012,.04),'violet',.002)
box('Stock spine',(0,.31,.025),(.07,.20,.06),'steel');box('Recoil pad',(0,.43,.025),(.12,.07,.18),'rubber')
# Articulated tracked industrial guardian. Dedicated named weakpoint meshes survive export.
current=root('HF_MANTIS6')
box('Lower frame',(0,0,.50),(2.1,2.9,.38),'graphite',.09)
box('Armored glacis',(0,-.68,.88),(2.05,1.35,.55),'ceramic',.10).rotation_euler[0]=-.16
box('Aft engine deck',(0,.90,.89),(1.9,1.1,.45),'graphite',.07)
for side in [-1,1]:
    x=side*1.30
    box('Bogie guard '+str(side),(x,0,.62),(.65,3.4,.32),'ceramic',.08)
    for y in [-1.20,-.72,-.24,.24,.72,1.20]:
        cyl('Drive wheel',(x,y,.40),.31,.58,'rubber','X',20)
        cyl('Hub',(x+side*.30,y,.40),.15,.04,'steel','X',12)
    for y in [i*.19-1.5 for i in range(17)]:
        box('Tread shoe',(x,y,.12),(.70,.17,.11),'graphite',.015)
        box('Track return',(x,y,.77),(.64,.17,.08),'rubber',.012)
    # Ceramic skirt plates are individual detachable pieces.
    for i,y in enumerate([-.98,-.32,.34,1.0]):
        plate=box(('ArmorL' if side<0 else 'ArmorR')+str(i),(x+side*.37,y,.72),(.10,.58,.45),'ceramic',.025)
    box('Turret shoulder',(side*.85,-.10,1.31),(.55,1.25,.46),'ceramic',.06)
    pod=cyl('CoolingLeft' if side<0 else 'CoolingRight',(side*1.06,.55,1.47),.18,.72,'steel','Z',16)
    cyl('Coolant glass',(side*1.06,.55,1.47),.185,.46,'cyan','Z',16)
    cyl('Reservoir cap',(side*1.06,.55,1.86),.21,.06,'graphite','Z',12)
    for z in [1.19,1.73]:box('Reservoir harness',(side*1.06,.55,z),(.44,.45,.08),'ceramic',.015)
# Gimbal / the low broad head, clear front sensor cluster.
cyl('Turret bearing',(0,-.1,1.19),.65,.20,'steel','Z',24)
box('Turret',(0,-.30,1.63),(1.18,1.26,.58),'graphite',.10)
box('Brow armor',(0,-.62,1.98),(1.36,.72,.17),'ceramic',.04)
for x in [-.28,0,.28]:
    cyl('Optical socket',(x,-.955,1.78),.105,.09,'steel','Y',16)
    cyl('Sensor' if x==0 else 'Aux optic',(x,-1.008,1.78),.066,.018,'red','Y',16)
cyl('Arc barrel',(0,-1.30,1.48),.17,1.1,'graphite','Y',12)
for y in [-.95,-1.17,-1.39,-1.61]:cyl('Emitter collar',(0,y,1.48),.21,.10,'steel','Y',12)
cyl('Muzzle',(0,-1.87,1.48),.22,.15,'ceramic','Y',12);cyl('Emitter',(0,-1.955,1.48),.12,.018,'cyan','Y',16)
for x in [-.68,-.34,0,.34,.68]:
    box('Engine louver',(x,1.18,1.15),(.20,.70,.09),'steel',.01)
box('Core housing',(0,1.47,1.1),(.72,.12,.60),'graphite',.04)
cyl('Core',(0,1.55,1.10),.20,.12,'cyan','Y',20)
box('CoreHatch',(0,1.65,1.10),(.68,.08,.56),'ceramic',.04)
# Save clean metre-based FBX selections; preserve the authoring scene and the user's scene.
output='/Users/mac/Documents/projects/hostfall/apps/unity/Assets/_HOSTFALL/Art/HostfallCombat'
os.makedirs(output,exist_ok=True)
summary=[]
for i,(name,r) in enumerate(roots.items()):
    if name!="HF_MANTIS6" and os.path.exists(output+"/Textures/HF2_Surface.png"):continue
    bpy.ops.object.select_all(action='DESELECT');r.select_set(True)
    children=list(r.children_recursive)
    for child in children:child.select_set(True)
    bpy.context.view_layer.objects.active=r
    bpy.ops.export_scene.fbx(filepath=output+'/'+name+'.fbx',use_selection=True,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=False)
    pts=[o.matrix_world@Vector(v) for o in children if o.type=='MESH' for v in o.bound_box]
    lo=[min(v[k] for v in pts) for k in range(3)];hi=[max(v[k] for v in pts) for k in range(3)]
    summary.append({'name':name,'mesh_parts':len(children),'size':[round(hi[k]-lo[k],3) for k in range(3)],'polygons':sum(len(o.data.polygons) for o in children if o.type=='MESH')})
    r.location.x=i*6
bpy.ops.wm.save_as_mainfile(filepath='/Users/mac/Documents/projects/hostfall/.artifacts/combat-rework-20261114/CombatModels.blend',copy=True)
print(json.dumps(summary))
