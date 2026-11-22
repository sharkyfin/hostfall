"""Original manufactured props, isolated scene; preserves the user's existing Blender scenes."""
import bpy,math,json,os
from mathutils import Vector
BASE='/Users/mac/Documents/projects/hostfall'
OUT=BASE+'/apps/unity/Assets/_HOSTFALL/Art/WorkstationAtelier'
QA=BASE+'/.artifacts/workstation-20261115';os.makedirs(OUT,exist_ok=True);os.makedirs(QA,exist_ok=True)
scene=bpy.data.scenes.get('HOSTFALL Workstation Atelier') or bpy.data.scenes.new('HOSTFALL Workstation Atelier')
bpy.context.window.scene=scene
for o in list(scene.objects):bpy.data.objects.remove(o,do_unlink=True)
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
mats={}
for name,color,metal,rough in [('Enamel',(.17,.24,.25),.55,.37),('Graphite',(.022,.03,.038),.2,.52),('Metal',(.48,.53,.54),.85,.3),('Accent',(.12,.62,.67),.35,.28),('Screen',(.008,.018,.025),.0,.35),('Contact',(.62,.38,.12),.8,.3)]:
 m=bpy.data.materials.get('WS_'+name) or bpy.data.materials.new('WS_'+name);m.use_nodes=True
 n=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');n.inputs['Base Color'].default_value=(*color,1);n.inputs['Metallic'].default_value=metal;n.inputs['Roughness'].default_value=rough;mats[name]=m
roots=[];parts=[];root=None

def begin(name):
 global root,parts
 root=bpy.data.objects.new(name,None);scene.collection.objects.link(root);roots.append(root);parts=[]

def box(name,location,size,material='Enamel',bevel=.004,rotation=None):
 bpy.ops.mesh.primitive_cube_add(size=1,location=location);o=bpy.context.object;o.name=name;o.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if rotation:o.rotation_euler=rotation
 o.parent=root;o.data.materials.append(mats[material])
 if bevel:
  b=o.modifiers.new('Machined edge','BEVEL');b.width=min(bevel,min(size)*.38);b.segments=2;bpy.ops.object.modifier_apply(modifier=b.name)
  n=o.modifiers.new('Surface normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=n.name)
 parts.append(o);return o

def cylinder(name,location,radius,depth,material='Metal',rotation=(0,0,0)):
 bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=radius,depth=depth,location=location,rotation=rotation);o=bpy.context.object;o.name=name;o.parent=root;o.data.materials.append(mats[material]);parts.append(o);return o

def marker(name,p):
 o=bpy.data.objects.new(name,None);scene.collection.objects.link(o);o.parent=root;o.location=p;return o

def display(center,width,height,tilt=0):
 # Right=X, up=(0,sin(tilt),cos(tilt)); right x up points towards the user (-Y).
 up=Vector((0,math.sin(tilt),math.cos(tilt)));c=Vector(center)
 box('DisplayGlass',center,(width,.004,height),'Screen',0,(-tilt,0,0))
 for n,x,y in [('DisplayTL',-1,1),('DisplayTR',1,1),('DisplayBL',-1,-1),('DisplayBR',1,-1)]:marker(n,c+Vector((x*width/2,0,0))+up*y*height/2)

def keyboard(y,z,width=.46,rows=5):
 for row in range(rows):
  for col in range(13):box('Key',(-width/2+(col+.5)*width/13,y+row*.023,z),(width/13-.004,.018,.005),'Graphite',.001)
 box('Space',(.0,y-.026,z),(.19,.018,.005),'Graphite',.001)

def finish():
 global parts
 # Only three mesh objects; screen remains separately addressable.
 for category,items in [('Chassis',[o for o in parts if not o.name.startswith('DisplayGlass') and o.data.materials[0]!=mats['Accent']]),('Indicators',[o for o in parts if o.data.materials[0]==mats['Accent']])]:
  if not items:continue
  bpy.ops.object.select_all(action='DESELECT')
  for o in items:o.select_set(True)
  bpy.context.view_layer.objects.active=items[0];bpy.ops.object.join();o=bpy.context.object;o.name=category
  bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 bpy.ops.object.select_all(action='DESELECT');root.select_set(True)
 for o in root.children_recursive:o.select_set(True)
 bpy.context.view_layer.objects.active=root
 bpy.ops.export_scene.fbx(filepath=OUT+'/'+root.name+'.fbx',use_selection=True,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=False,use_custom_props=True)

begin('HF_FieldNotebook')
box('Magnesium lower shell',(0,0,.018),(.55,.36,.036),'Metal',.009)
box('Recessed keyboard deck',(0,0,.038),(.535,.344,.014),'Enamel',.007)
keyboard(-.025,.049)
box('Trackpad gasket',(0,-.111,.048),(.185,.075,.003),'Graphite',.003)
box('Trackpad',(0,-.111,.050),(.173,.064,.003),'Metal',.003)
for x in [-.218,.218]:cylinder('Hinge',(x,.143,.054),.018,.075,'Graphite',(0,math.pi/2,0))
angle=math.radians(12);c=(0,.184,.232)
box('Display housing',c,(.55,.025,.342),'Enamel',.008,(-angle,0,0))
box('Screen gasket',(0,.169,.234),(.515,.006,.303),'Graphite',.003,(-angle,0,0))
display((0,.164,.235),.489,.276,angle)
box('Power key',(.23,-.04,.05),(.025,.025,.008),'Accent',.002)
for side in [-1,1]:
 for y in [-.05,.015,.08]:box('Side IO',(side*.275,y,.024),(.003,.036,.012),'Graphite',.001)
for x in [-.245,.245]:
 for y in [-.14,.14]:cylinder('Deck fastener',(x,y,.046),.004,.004,'Metal')
finish()

begin('HF_StudioConsole')
box('Weighted base',(0,.025,.016),(.31,.22,.032),'Graphite',.009)
box('Stand',(0,.10,.156),(.052,.05,.26),'Metal',.006)
box('All-in-one enclosure',(0,.10,.413),(.76,.064,.49),'Metal',.012)
box('Rear service cover',(0,.137,.413),(.51,.018,.33),'Enamel',.008)
box('Front gasket',(0,.063,.428),(.726,.008,.414),'Graphite',.004)
display((0,.057,.428),.698,.389)
box('Chin',(0,.057,.199),(.716,.009,.028),'Enamel',.004)
box('Power indicator',(.325,.049,.199),(.012,.004,.003),'Accent',.001)
box('Keyboard body',(0,-.208,.015),(.50,.18,.03),'Metal',.006);keyboard(-.25,.033,.46)
box('Track surface',(.329,-.21,.016),(.118,.16,.032),'Enamel',.01)
finish()

begin('HF_OperationsPC')
box('Monitor foot',(0,.06,.016),(.33,.24,.032),'Graphite',.007)
box('Monitor upright',(0,.12,.19),(.065,.065,.35),'Metal',.006)
box('Monitor chassis',(0,.105,.43),(.78,.065,.47),'Graphite',.01)
box('Monitor bezel',(0,.068,.43),(.75,.01,.44),'Enamel',.004)
display((0,.061,.436),.713,.396)
box('Status bar',(0,.054,.207),(.18,.003,.004),'Accent',.001)
box('Tower',( .53,.09,.21),(.235,.38,.42),'Enamel',.012)
box('Tower front',( .53,-.106,.21),(.21,.012,.385),'Graphite',.006)
for z in [.10,.12,.14,.16,.18,.20,.22,.24]:box('Vent',(.53,-.114,z),(.16,.004,.006),'Metal',.001)
box('Power button',(.465,-.116,.362),(.022,.007,.022),'Accent',.003)
for x in [.50,.54]:box('Front USB',(x,-.116,.325),(.027,.004,.012),'Metal',.001)
box('Keyboard',(0,-.22,.017),(.50,.19,.034),'Enamel',.006);keyboard(-.26,.037)
box('Mouse',(.334,-.21,.023),(.068,.115,.046),'Graphite',.015)
finish()

begin('HF_ArchiveKey')
box('Armoured data body',(0,.018,.016),(.071,.133,.032),'Enamel',.006)
box('Spine',(0,.02,.033),(.045,.09,.006),'Metal',.003)
for side in [-1,1]:
 for y in [-.016,.0,.016,.032]:box('Grip groove',(side*.034,y,.019),(.004,.008,.018),'Graphite',.001)
box('Status lens',(0,-.015,.037),(.026,.018,.003),'Accent',.002)
# Four rails around a genuinely open connector mouth, not a solid cube.
for x in [-.020,.020]:box('USB sleeve side',(x,-.07,.015),(.003,.044,.018),'Metal',.001)
for z in [.007,.023]:box('USB sleeve face',(0,-.07,z),(.040,.044,.003),'Metal',.001)
box('Contact tongue',(0,-.072,.014),(.033,.034,.005),'Graphite',.001)
for x in [-.012,-.004,.004,.012]:box('Gold contact',(x,-.081,.017),(.004,.016,.001),'Contact',.0003)
for x in [-.022,.022]:box('Lanyard arm',(x,.098,.016),(.009,.038,.028),'Metal',.003)
box('Lanyard bridge',(0,.113,.016),(.049,.009,.028),'Metal',.003)
finish()

begin('HF_ServicePanel')
box('Wall enclosure',(0,.025,.105),(.17,.05,.21),'Enamel',.007)
box('Service face',(0,-.004,.105),(.149,.01,.188),'Graphite',.004)
cylinder('Switch bezel',(0,-.018,.115),.039,.02,'Metal',(math.pi/2,0,0))
cylinder('Switch cap',(0,-.033,.115),.031,.016,'Accent',(math.pi/2,0,0))
for x in [-.06,.06]:
 for z in [.025,.185]:cylinder('Face screw',(x,-.011,z),.004,.004,'Metal',(math.pi/2,0,0))
box('Indicator slit',(0,-.012,.043),(.048,.004,.005),'Accent',.001)
finish()

summary=[]
for i,r in enumerate(roots):
 meshes=[o for o in r.children_recursive if o.type=='MESH'];summary.append({'name':r.name,'renderers':len(meshes),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)})
 r.location.x=i*1.4
bpy.context.view_layer.update()
os.makedirs(BASE+'/art-source/workstations',exist_ok=True)
bpy.data.libraries.write(BASE+'/art-source/workstations/HOSTFALL-Workstations.blend',{scene},fake_user=True,compress=True)
open(QA+'/model-metrics.json','w').write(json.dumps(summary,indent=2));print(json.dumps(summary))
