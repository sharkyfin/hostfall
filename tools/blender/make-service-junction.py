"""Original HOSTFALL prop. Execute in Blender; only its own named collection is replaced."""
import bpy,json,os,math
from mathutils import Vector
name='HOSTFALL_ServiceJunction'
old=bpy.data.collections.get(name)
if old:
    for obj in list(old.objects):bpy.data.objects.remove(obj,do_unlink=True)
    bpy.data.collections.remove(old)
collection=bpy.data.collections.new(name);bpy.context.scene.collection.children.link(collection)
def material(name,color,metal=.0,rough=.6):
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    return m
steel=material('HF junction / enamel',(.21,.28,.27),.5)
dark=material('HF junction / gasket',(.025,.035,.04),.2)
brass=material('HF junction / warning',(.65,.32,.065),.45)
parts=[]
def box(n,p,s,m,bevel=.008):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=n;o.dimensions=s
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for c in list(o.users_collection):c.objects.unlink(o)
    collection.objects.link(o);o.data.materials.append(m)
    if bevel:
        mod=o.modifiers.new('Manufactured edges','BEVEL');mod.width=bevel;mod.segments=2
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(o);return o
box('Cabinet',(0,0,.5),(.68,.16,1.0),steel)
box('Door gasket',(0,-.09,.5),(.61,.025,.9),dark)
box('Door',(0,-.111,.5),(.58,.024,.87),steel)
box('Latch',(.23,-.143,.49),(.035,.025,.12),brass)
box('Service stripe',(-.24,-.132,.5),(.018,.008,.70),brass,.002)
for z in [.21,.26,.31]:box('Vent slot',(0,-.132,z),(.24,.009,.012),dark,.002)
for x in [-.265,.265]:
 for z in [.11,.89]:box('Fastener',(x,-.134,z),(.018,.009,.018),brass,.003)
for x in [-.16,.16]:box('Cable gland',(x,0,-.024),(.07,.08,.055),dark)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();obj=bpy.context.object;obj.name=name
bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
# Frame the actual object in the existing viewport. Preserve the user's cameras and objects.
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':
  from mathutils import Quaternion
  area.spaces.active.region_3d.view_location=(0,0,.5);area.spaces.active.region_3d.view_distance=2.2
  area.spaces.active.region_3d.view_rotation=(Vector((1,-2,1.0))).to_track_quat('Z','Y')
print(json.dumps({'object':obj.name,'dimensions':list(obj.dimensions),'vertices':len(obj.data.vertices),'polygons':len(obj.data.polygons),'materials':len(obj.data.materials)}))
