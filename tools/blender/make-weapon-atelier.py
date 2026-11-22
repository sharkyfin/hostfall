"""HOSTFALL original weapon set, revision 2. Metres; forward -Y / up Z in Blender.
Creates only its own scene. Export keeps animated mechanical groups and grip sockets.
No downloaded meshes/textures. Run inside Blender 4.5 with MCP.
"""
import bpy, bmesh, math, os, json, random
import numpy as np
from mathutils import Vector
from collections import defaultdict
BASE='/Users/mac/Documents/projects/hostfall'
OUT=os.environ.get('HOSTFALL_WEAPON_ART_OUTPUT',BASE+'/apps/unity/Assets/_HOSTFALL/Art/HostfallCombat')
QA=BASE+'/.artifacts/weapon-quality-20261114'
os.makedirs(OUT+'/Textures',exist_ok=True);os.makedirs(QA,exist_ok=True)
scene_name='HOSTFALL Weapon Atelier'
old=bpy.data.scenes.get(scene_name)
if old:
    for o in list(old.objects):bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.scenes.remove(old)
scene=bpy.data.scenes.new(scene_name);bpy.context.window.scene=scene
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
# Subtle authored detail maps, shared by the whole manufactured family.
rng=np.random.default_rng(1704);N=512
noise=rng.normal(0,.018,(N,N));brush=rng.normal(0,.014,(N,1))
grain=np.clip(.88+noise+brush,.76,.97)
for i in range(110):
    y=int(rng.integers(N));x=int(rng.integers(N));length=int(rng.integers(3,55));grain[y,x:min(N,x+length)]-=.025

def image_file(name,pixels):
    image=bpy.data.images.new(name,width=N,height=N,alpha=True)
    image.colorspace_settings.name='Non-Color' if 'Normal' in name or 'Metal' in name else 'sRGB'
    image.pixels.foreach_set(np.asarray(pixels,dtype=np.float32).reshape(-1));image.filepath_raw=OUT+'/Textures/'+name+'.png';image.file_format='PNG';image.save();return image
rgba=np.ones((N,N,4));rgba[:,:,:3]=grain[:,:,None]
base_map=image_file('HF2_Surface',rgba)
gy,gx=np.gradient(grain);normal=np.ones((N,N,4));normal[:,:,0]=.5-gx*.8;normal[:,:,1]=.5-gy*.8;normal[:,:,2]=1
normal_map=image_file('HF2_Normal',normal)
for metallic,name,smooth in [(1,'HF2_MetalSmooth',.56),(0,'HF2_PolymerSmooth',.28),(0,'HF2_PaintSmooth',.62)]:
    m=np.zeros((N,N,4));m[:,:,0]=metallic;m[:,:,3]=np.clip(smooth+noise*2,.1,.85);image_file(name,m)
P={}
def material(key,color,metal=.0,rough=.4,emission=0):
    name='HF2 '+key;m=bpy.data.materials.get(name) or bpy.data.materials.new(name);m.use_nodes=True
    nodes=m.node_tree.nodes;p=next(n for n in nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emission
    m.diffuse_color=(*color,1);P[key]=m
material('Ceramic',(.19,.28,.29),.58,.37);material('Graphite',(.055,.073,.085),.72,.34)
material('Steel',(.38,.44,.46),.88,.28);material('Rubber',(.035,.041,.045),.0,.72)
material('Copper',(.34,.16,.065),.8,.3);material('Ivory',(.66,.70,.64),.25,.48)
material('Cyan',(.04,.46,.56),.25,.28,1.5);material('Violet',(.24,.075,.46),.3,.3,1.5)
# Portable authoring previews use the same packed surface and normal maps.
for m in P.values():
    nodes=m.node_tree.nodes;links=m.node_tree.links;p=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
    for n in list(nodes):
        if n.type not in ['BSDF_PRINCIPLED','OUTPUT_MATERIAL']:nodes.remove(n)
    color=nodes.new('ShaderNodeTexImage');color.image=base_map;base_map.pack()
    mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[1].default_value=p.inputs['Base Color'].default_value
    links.new(color.outputs['Color'],mix.inputs[2]);links.new(mix.outputs[0],p.inputs['Base Color'])
    detail=nodes.new('ShaderNodeTexImage');detail.image=normal_map;normal_map.pack()
    normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.35
    links.new(detail.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],p.inputs['Normal'])
current=None;group=None;roots=[];meshes=[]
def empty(name,p=(0,0,0),parent=None):
    o=bpy.data.objects.new(name,None);scene.collection.objects.link(o);o.location=p;o.parent=parent;return o
def begin(name):
    global current,group,meshes
    current=empty('AT_'+name);group=empty('Body',parent=current);roots.append(current);meshes=[]
def set_group(name,p=(0,0,0)):
    global group
    group=empty(name,p,current);return group

def finish(o,name,mat='Graphite',bevel=.003):
    o.name=name;bpy.context.view_layer.objects.active=o
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.clear();o.data.materials.append(P[mat]);o.parent=group
    bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
    if bevel:
        b=o.modifiers.new('Controlled edge radius','BEVEL');b.width=bevel;b.segments=3;b.limit_method='ANGLE'
        bpy.ops.object.modifier_apply(modifier=b.name)
    for poly in o.data.polygons:poly.use_smooth=True
    n=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');n.keep_sharp=True;n.weight=50;bpy.ops.object.modifier_apply(modifier=n.name)
    # Physical scale, planar triplanar-style UVs for fine surface grain.
    for old_uv in list(o.data.uv_layers):o.data.uv_layers.remove(old_uv)
    uv=o.data.uv_layers.new(name='SurfaceUV')
    for poly in o.data.polygons:
        axis=max(range(3),key=lambda a:abs(poly.normal[a]));axes=[a for a in range(3) if a!=axis]
        for loop in poly.loop_indices:
            v=o.data.vertices[o.data.loops[loop].vertex_index].co;uv.data[loop].uv=(v[axes[0]]*14,v[axes[1]]*14)
    meshes.append(o);return o

def box(name,p,size,mat='Graphite',b=.003):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.scale=size;return finish(o,name,mat,b)
def profile(name,points,width,mat='Graphite',x=0,b=.003):
    vertices=[(x+side*width/2,y,z) for side in [-1,1] for y,z in points];n=len(points)
    faces=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);scene.collection.objects.link(o);return finish(o,name,mat,b)
def ring_profile(name,outer,inner,width,mat='Steel',x=0):
    n=len(outer);assert n==len(inner)
    vs=[(x+side*width/2,y,z) for side in [-1,1] for poly in [outer,inner] for y,z in poly];fs=[]
    for i in range(n):
        j=(i+1)%n
        fs.extend([(i,j,2*n+j,2*n+i),(n+j,n+i,3*n+i,3*n+j),(i,n+i,n+j,j),(2*n+j,3*n+j,3*n+i,2*n+i)])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vs,[],fs);mesh.update();o=bpy.data.objects.new(name,mesh);scene.collection.objects.link(o);return finish(o,name,mat,.0017)
def tube(name,p,radius,length,wall=.004,mat='Steel',axis='Y',vertices=32):
    # Annular closed cross-section: there is an actual bore, not a black end cap.
    vs=[]
    for axial,r in [(-length/2,radius),(length/2,radius),(length/2,radius-wall),(-length/2,radius-wall)]:
        for i in range(vertices):
            a=2*math.pi*i/vertices;u=r*math.cos(a);v=r*math.sin(a)
            q=(u,axial,v) if axis=='Y' else (axial,u,v) if axis=='X' else (u,v,axial)
            vs.append(tuple(p[k]+q[k] for k in range(3)))
    fs=[]
    for ring in range(4):
        next_ring=(ring+1)%4
        for i in range(vertices):j=(i+1)%vertices;fs.append((ring*vertices+i,ring*vertices+j,next_ring*vertices+j,next_ring*vertices+i))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vs,[],fs);mesh.update();o=bpy.data.objects.new(name,mesh);scene.collection.objects.link(o);return finish(o,name,mat,.0007)
def cylinder(name,p,radius,length,mat='Steel',axis='Y',vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=length,location=p);o=bpy.context.object
    if axis=='Y':o.rotation_euler[0]=math.pi/2
    if axis=='X':o.rotation_euler[1]=math.pi/2
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=False);return finish(o,name,mat,.0015)
def wire(name,points,radius=.003,mat='Copper'):
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.resolution_u=1;curve.bevel_depth=radius;curve.bevel_resolution=2
    line=curve.splines.new('POLY');line.points.add(len(points)-1)
    for pt,co in zip(line.points,points):pt.co=(*co,1)
    obj=bpy.data.objects.new(name,curve);scene.collection.objects.link(obj);bpy.context.view_layer.objects.active=obj;obj.select_set(True);bpy.ops.object.convert(target='MESH');return finish(bpy.context.object,name,mat,0)
def screw(x,y,z):
    return tube('Recessed hex fastener',(x,y,z),.0052,.003,.002,'Steel','X',6)
def stamp(text,p,size=.013,side=1,mat='Ivory'):
    bpy.ops.object.text_add(location=p);o=bpy.context.object;o.data.body=text;o.data.size=size;o.data.extrude=.00012;o.data.space_character=1.12
    # local text X runs along Y; local text Y runs up Z; normal faces right/left.
    from mathutils import Matrix
    o.rotation_euler=Matrix(((0,0,side),(side,0,0),(0,1,0))).to_4x4().to_euler()
    bpy.ops.object.convert(target='MESH');return finish(bpy.context.object,'Engraved '+text,mat,0)
def trigger_grip(sidearm=False):
    profile('Angled grip core',[(.038,-.022),(.103,-.022),(.158,-.153),(.137,-.181),(.078,-.177),(.02,-.055)],.058,'Rubber',b=.006)
    for side in [-1,1]:
        profile('Grip side panel',[(.047,-.047),(.097,-.044),(.139,-.151),(.128,-.165),(.085,-.160),(.036,-.064)],.006,'Graphite',x=side*.03,b=.002)
        for z in [-.073,-.092,-.111,-.13,-.149]:
            rib=box('Grip diagonal checkering',(side*.034,.075-(z+.073)*.32,z),(.004,.047,.004),'Rubber',.001);rib.rotation_euler[0]=-.35
        screw(side*.035,.07,-.062);screw(side*.035,.119,-.146)
    outer=[(-.048,-.024),(.04,-.023),(.061,-.059),(.04,-.098),(-.041,-.094),(-.064,-.065)]
    inner=[(-.04,-.034),(.032,-.033),(.048,-.060),(.032,-.086),(-.034,-.083),(-.051,-.064)]
    ring_profile('Open trigger guard',outer,inner,.052,'Graphite')
    profile('Curved trigger',[(.009,-.028),(.02,-.031),(.009,-.061),(-.006,-.073),(-.011,-.068),(.003,-.05)],.012,'Steel',b=.001)
def sights(yfront,yback,height):
    box('Sight base',(0,yback,height),(.048,.034,.012),'Graphite',.002)
    for x in [-.019,.019]:box('Rear sight ear',(x,yback,height+.012),(.009,.023,.021),'Steel',.0015)
    box('Front sight blade',(0,yfront,height+.009),(.008,.017,.021),'Steel',.0015)
    box('Front sight phosphor',(0,yfront+.009,height+.016),(.004,.001,.006),'Cyan',.0004)
def stock(end=.37):
    outer=[(.13,.105),(end,.106),(end+.012,-.05),(.31,-.061),(.13,.024)]
    inner=[(.173,.083),(end-.026,.083),(end-.024,-.022),(.31,-.034),(.173,.034)]
    ring_profile('Skeleton stock',outer,inner,.058,'Graphite')
    profile('Cheek weld',[(.16,.115),(.29,.115),(.31,.097),(.16,.097)],.076,'Ceramic',b=.003)
    box('Recoil pad',(0,end+.02,.024),(.085,.035,.17),'Rubber',.006)
    for z in [-.03,-.01,.01,.03,.05,.07]:box('Butt pad rib',(0,end+.039,z),(.078,.004,.007),'Graphite',.001)
def sockets(left,muzzle):
    empty('GripRight',(0,.09,-.105),current);empty('GripLeft',left,group if group.name=='Pump' else current)
    empty('Muzzle',muzzle,current);empty('Ejection',(.065,.01,.073),current)
# ARC-12: robust service shotgun, long single bore, feed tube and a shaped pump.
begin('ARC12');body=group
profile('Forged receiver',[(-.185,.025),(-.16,.111),(-.11,.133),(.11,.133),(.159,.095),(.157,.017),(.045,-.018),(-.14,-.013)],.107,'Ceramic',b=.006)
profile('Upper cover',[(-.16,.117),(-.115,.147),(.10,.147),(.15,.114)],.082,'Graphite',b=.003)
for side in [-1,1]:
    profile('Recessed receiver insert',[(-.13,.041),(-.115,.102),(.102,.102),(.122,.075),(.105,.025),(-.095,.021)],.005,'Graphite',x=side*.055,b=.002)
    for y in [-.11,.10]:screw(side*.059,y,.075)
    stamp('ARC-12',(side*.060,-.11 if side>0 else .08,.082),.018,side)
    stamp('NORTHLINE  /  FIELD SYSTEMS',(side*.060,-.11 if side>0 else .105,.042),.0062,side)
    box('Charge bar bezel',(side*.061,-.07,.044),(.003,.068,.014),'Steel',.001)
    for i in range(5):box('Charge segment',(side*.063,-.096+i*.011,.044),(.001,.007,.006),'Cyan',.0003)
# Ejection bolt is a separate moving assembly.
set_group('Bolt');box('Ejection port recess',(.060,.005,.096),(.008,.075,.023),'Rubber',.001)
box('Bolt face',(.063,.01,.096),(.006,.056,.016),'Steel',.001)
cylinder('Bolt handle',(.081,.04,.09),.007,.035,'Graphite','X',16)
group=body
trigger_grip();stock();sights(-.50,.092,.151)
tube('Main bored barrel',(0,-.37,.093),.023,.425,.004,'Steel')
tube('Muzzle collar',(0,-.584,.093),.029,.042,.007,'Graphite')
tube('Muzzle lip',(0,-.608,.093),.024,.008,.004,'Steel')
cylinder('Feed tube',(0,-.33,.025),.018,.39,'Graphite')
cylinder('Feed cap',(0,-.527,.025),.020,.02,'Steel')
for y in [-.22,-.25,-.28]:tube('Induction band',(0,y,.093),.028,.012,.004,'Copper')
# Open heat shield: vent gaps are actual negative space between ribs.
for x in [-.027,.027]:
    box('Heat shield spine',(x,-.393,.105),(.007,.24,.025),'Graphite',.002)
    for y in [-.49,-.458,-.426,-.394,-.362,-.33]:box('Shield bridge',(x,y,.126),(.008,.012,.032),'Ceramic',.002)
for x in [-.027,.027]:box('Shield upper flange',(x,-.406,.145),(.014,.255,.009),'Graphite',.002)
set_group('Pump')
profile('Ergonomic pump',[(-.465,.016),(-.438,.037),(-.288,.037),(-.266,.012),(-.28,-.037),(-.438,-.045),(-.47,-.025)],.116,'Graphite',b=.006)
for side in [-1,1]:
    for y in [-.44,-.415,-.39,-.365,-.34,-.315]:box('Pump rib',(side*.059,y,-.005),(.006,.012,.047),'Rubber',.002)
sockets((0,-.295,-.037),(0,-.613,.093));group=body
# CIRRUS: compact frame, independent slide and detachable capacitor magazine.
begin('CIRRUS');body=group
profile('Service frame',[(-.20,.015),(-.185,.074),(.071,.074),(.092,.048),(.071,-.019),(-.037,-.023),(-.08,.0)],.070,'Ceramic',b=.004)
trigger_grip(True)
for side in [-1,1]:
    profile('Frame side inset',[(-.17,.012),(-.16,.048),(.06,.048),(.066,.011),(-.04,-.01)],.004,'Graphite',x=side*.036,b=.0015)
    screw(side*.039,-.135,.028);screw(side*.039,.05,.027)
    stamp('CIRRUS',(side*.043,-.09 if side>0 else -.025,.098),.011,side)
    stamp('NORTHLINE  /  C-09',(side*.040,-.11 if side>0 else .048,.022),.005,side)
set_group('Slide')
profile('Faceted upper slide',[(-.219,.066),(-.20,.115),(.067,.115),(.089,.092),(.085,.06),(-.195,.054)],.082,'Graphite',b=.003)
for side in [-1,1]:
    for y in [.008,.024,.04,.056]:box('Slide serration',(side*.043,y,.088),(.003,.007,.032),'Steel',.001)
    for y in [-.17,-.137,-.104]:
        box('Slide port recess',(side*.043,y,.087),(.002,.022,.016),'Rubber',.001)
        box('Port lower rail',(side*.045,y,.075),(.003,.025,.004),'Steel',.001)
tube('Bored emitter jacket',(0,-.205,.084),.025,.10,.006,'Steel')
tube('Emitter guard',(0,-.26,.084),.029,.025,.008,'Graphite')
cylinder('Recessed emitter lens',(0,-.263,.084),.014,.004,'Cyan')
sights(-.19,.065,.124)
group=body
box('Slide stop',(.044,.018,.041),(.011,.04,.012),'Steel',.0015)
set_group('Magazine')
profile('Capacitor magazine',[(.082,-.04),(.117,-.055),(.156,-.17),(.148,-.183),(.092,-.18),(.068,-.066)],.047,'Steel',b=.003)
box('Magazine heel',(0,.126,-.187),(.072,.09,.022),'Graphite',.004)
for side in [-1,1]:box('Cell latch',(side*.037,.126,-.185),(.004,.025,.01),'Ceramic',.001)
group=body;sockets((-.023,.091,-.10),(0,-.277,.084))
# TITAN: a large open induction assembly; the rails have physical depth and air gaps.
begin('TITAN');body=group
profile('Pressure receiver',[(-.25,.019),(-.215,.135),(-.12,.163),(.10,.147),(.16,.096),(.142,-.017),(-.15,-.036)],.145,'Ceramic',b=.007)
for side in [-1,1]:
    profile('Receiver side plate',[(-.205,.028),(-.18,.107),(-.11,.124),(.10,.109),(.119,.033),(-.13,-.009)],.008,'Graphite',x=side*.075,b=.003)
    for y,z in [(-.18,.074),(.1,.071),(-.08,.027)]:screw(side*.081,y,z)
    stamp('TITAN',(side*.082,-.15 if side>0 else .085,.091),.02,side);stamp('W10  /  INDUSTRIAL INDUCTOR',(side*.082,-.15 if side>0 else .095,.043),.006,side)
    cylinder('Rear capacitor',(side*.101,.018,.105),.027,.19,'Graphite')
    for y in [-.07,.093]:tube('Capacitor collar',(side*.101,y,.105),.032,.014,.006,'Steel')
    wire('Protected power cable',[(side*.103,-.07,.115),(side*.11,-.13,.17),(side*.096,-.215,.17),(side*.075,-.26,.12)],.004)
trigger_grip();stock(.40)
tube('Induction barrel',(0,-.455,.095),.029,.43,.006,'Steel')
for side in [-1,1]:
    profile('Tapered forward rail',[(-.23,.055),(-.23,.145),(-.63,.135),(-.69,.115),(-.69,.062),(-.61,.042)],.026,'Graphite',x=side*.069,b=.004)
    box('Rail ceramic insert',(side*.084,-.443,.10),(.009,.28,.044),'Ceramic',.002)
    for i,y in enumerate([-.285,-.355,-.425,-.495,-.565]):
        tube('Inductor winding',(0,y,.095),.064,.027,.010,'Copper')
        box('Coil saddle',(side*.064,y,.041),(.04,.036,.022),'Graphite',.002)
    box('Guard spine',(side*.067,-.455,.16),(.017,.43,.018),'Steel',.002)
    box('Status capillary',(side*.078,-.41,.163),(.003,.22,.003),'Violet',.0005)
tube('Open muzzle yoke',(0,-.706,.095),.073,.035,.014,'Ceramic',vertices=12)
for x in [-.078,.078]:box('Yoke rail clamp',(x,-.688,.095),(.03,.06,.088),'Graphite',.003)
tube('Muzzle focus ring',(0,-.736,.095),.042,.027,.012,'Graphite')
tube('Inductor inner ring',(0,-.752,.095),.028,.006,.006,'Violet')
sights(-.60,.08,.174)
set_group('Magazine',(0,-.18,0))
profile('Replaceable power pack',[(-.06,-.018),(.08,-.018),(.124,-.087),(.107,-.132),(-.047,-.132),(-.077,-.095)],.126,'Graphite',b=.005)
for side in [-1,1]:
    box('Pack crash rail',(side*.065,.015,-.088),(.009,.14,.028),'Rubber',.002)
    for y in [-.035,.005,.045]:box('Pack vent',(side*.071,y,-.084),(.003,.019,.014),'Steel',.001)
box('Pack release tab',(0,.113,-.10),(.045,.018,.029),'Steel',.002)
group=body
set_group('Bolt');box('Recoil carriage',(0,-.202,.17),(.092,.065,.028),'Graphite',.003)
group=body;sockets((0,-.18,-.05),(0,-.77,.095))
# Consolidate manufacturing pieces by mechanical assembly. This reduces hundreds
# of primitive renderers to 3-4 meshes per weapon while preserving moving groups.
summary=[]
for idx,r in enumerate(roots):
    children=list(r.children_recursive)
    for g in [o for o in children if o.type=='EMPTY' and o.parent==r and o.name.split('.')[0] in ['Body','Pump','Bolt','Magazine','Slide']]:
        items=[o for o in g.children if o.type=='MESH']
        if not items:continue
        bpy.ops.object.select_all(action='DESELECT')
        for o in items:o.select_set(True)
        bpy.context.view_layer.objects.active=items[0];bpy.ops.object.join();joined=bpy.context.object;joined.name=g.name.split('.')[0]+' mesh'
        # Consolidate duplicate material slots generated by join.
        unique=[];indices={}
        for slot in joined.data.materials:
            if slot not in unique:unique.append(slot)
            indices[len(indices)]=unique.index(slot)
        mapped=[indices[poly.material_index] for poly in joined.data.polygons]
        joined.data.materials.clear()
        for m in unique:joined.data.materials.append(m)
        for poly,index in zip(joined.data.polygons,mapped):poly.material_index=index
    # GripRight is the asset origin in Unity. Move children in world coordinates.
    grip=next(o for o in r.children_recursive if o.name.startswith('GripRight'))
    origin=grip.matrix_world.translation.copy()
    for child in list(r.children):child.location-=origin
    r.location=(0,0,0)
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action='DESELECT');r.select_set(True)
    for o in r.children_recursive:o.select_set(True)
    bpy.context.view_layer.objects.active=r
    path=OUT+'/'+r.name.replace('AT_','HF_')+'.fbx'
    bpy.ops.export_scene.fbx(filepath=path,use_selection=True,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=False,use_custom_props=True)
    parts=[o for o in r.children_recursive if o.type=='MESH'];triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in parts)
    pts=[o.matrix_world@Vector(v) for o in parts for v in o.bound_box]
    summary.append({'name':r.name,'renderers':len(parts),'triangles':triangles,'size':[round(max(v[k] for v in pts)-min(v[k] for v in pts),4) for k in range(3)],'file':path})
    r.location.x=idx*1.6
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=QA+'/WeaponAtelier.blend',copy=True)
os.makedirs(BASE+'/art-source/weapons',exist_ok=True)
bpy.data.libraries.write(BASE+'/art-source/weapons/HOSTFALL-WeaponAtelier.blend',{scene},fake_user=True,compress=True)
open(QA+'/model-metrics.json','w').write(json.dumps(summary,indent=2));print(json.dumps(summary))
