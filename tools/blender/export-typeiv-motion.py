"""Run in Blender after importing the CC0 UAL1 Standard FBX into Motion Research."""
import bpy,json,math,os
from pathlib import Path
root=Path('/Users/mac/Documents/projects/hostfall')
scene=bpy.data.scenes['HOSTFALL Motion Research'];bpy.context.window.scene=scene
rig=next(o for o in scene.objects if o.type=='ARMATURE');rig.animation_data_create()
chosen=['Idle_Loop','Walk_Loop','Jog_Fwd_Loop','Sprint_Loop','Jump_Start','Jump_Loop','Jump_Land','Interact','PickUp_Table','Pistol_Reload','Pistol_Shoot','Hit_Chest']
bones={'Pelvis':'pelvis','Head':'Head'}
for suffix,side in [('L','l'),('R','r')]:
 for alias,bone in [('Hip','thigh'),('Knee','calf'),('Foot','foot'),('Toe','ball'),('Shoulder','upperarm'),('Elbow','lowerarm'),('Hand','hand')]:bones[alias+suffix]=bone+'_'+side
for track in rig.animation_data.nla_tracks:track.mute=True
clips=[];actions=[]
for name in chosen:
 action=next(a for a in bpy.data.actions if a.name.endswith('|'+name));actions.append(action);rig.animation_data.action=action
 if action.slots:rig.animation_data.action_slot=action.slots[0]
 frames=[];begin,end=map(int,action.frame_range)
 for frame in range(begin,end+1):
  scene.frame_set(frame);bpy.context.view_layer.update();points=[]
  for bone in bones.values():
   p=rig.matrix_world@rig.pose.bones[bone].head
   points.append({'x':round(p.x,6),'y':round(p.z,6),'z':round(p.y,6)})
  frames.append({'points':points})
 clips.append({'name':name,'fps':30,'frames':frames})
path=root/'apps/unity/Assets/_HOSTFALL/Art/QuaterniusMotion';path.mkdir(parents=True,exist_ok=True)
(path/'TypeIVMotionSamples.json').write_text(json.dumps({'joints':list(bones),'clips':clips},separators=(',',':')))
rig.animation_data.action=None
for track in list(rig.animation_data.nla_tracks):rig.animation_data.nla_tracks.remove(track)
for name,action in zip(chosen,actions):
 track=rig.animation_data.nla_tracks.new();track.name=name;strip=track.strips.new(name,1,action);strip.name=name
 if hasattr(strip,"action_slot") and action.slots:strip.action_slot=action.slots[0]
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(path/'TypeIVMotionSource.fbx'),use_selection=True,object_types={'ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=True,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
bpy.data.libraries.write(str(root/'art-source/motion/HOSTFALL-TypeIV-Motion.blend'),{scene},compress=True)
print(json.dumps({'exported':chosen,'joints':list(bones),'samples':sum(len(c['frames'])for c in clips)}))
