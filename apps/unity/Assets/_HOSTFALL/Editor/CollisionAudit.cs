using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Hostfall.Core;
using Hostfall.World;
using Object=UnityEngine.Object;
namespace Hostfall.Editor
{
 public static class CollisionAudit
 {
  [Serializable]public class Entry{public string path,type,visualOwner;public Vector3 center,size,visualCenter,visualSize;public bool door,threat;public int renderers;}
  [Serializable]public class Report{public Entry[] solids;}
  [Serializable]public class SurfaceGap{public string path;public Vector3 point,normal;public float visualGap;}
  [Serializable]public class SurfaceReport{public int meshes,samples;public SurfaceGap[] gaps;}
  [MenuItem("HOSTFALL/Tools/Audit Collider Surfaces")]
  public static void Surfaces() => Surfaces(Output);
  internal static void Surfaces(string output)
  {
   if(!Application.dataPath.Contains(".artifacts/"))throw new InvalidOperationException("Run in verification copy");
   var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   if(original.isDirty)throw new InvalidOperationException("Unsaved scene");
   var solids=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.enabled&&!c.isTrigger&&c.gameObject.layer!=CollisionLayers.Player&&c.GetComponentInParent<FacilityDoor>()==null&&c.GetComponentInParent<IDamageable>()==null).ToArray();
   Directory.CreateDirectory(output);
   if(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.gameObject.layer==31))throw new InvalidOperationException("Audit reference layer 31 is occupied");
   var filters=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(m=>m.sharedMesh!=null&&m.TryGetComponent<Renderer>(out var r)&&r.enabled&&r.gameObject.activeInHierarchy).ToArray();
   var temp=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
   var gaps=new List<SurfaceGap>();int samples=0;bool back=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;
   try{
    foreach(var m in filters){var go=new GameObject(m.name);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,temp);go.layer=31;go.transform.SetPositionAndRotation(m.transform.position,m.transform.rotation);go.transform.localScale=m.transform.lossyScale;go.AddComponent<MeshCollider>().sharedMesh=m.sharedMesh;}
    Physics.SyncTransforms();
    foreach(var c in solids){
     if(!(c is BoxCollider)||c.GetComponentInChildren<MeshFilter>()==null)continue;
     var b=c.bounds;
     foreach(var d in new[]{Vector3.left,Vector3.right,Vector3.forward,Vector3.back})
     foreach(float f in new[]{-.65f,0f,.65f})foreach(float h in new[]{.2f,.5f,.8f}){
      var tangent=new Vector3(d.z,0,-d.x);var p=b.center+tangent*(Mathf.Abs(tangent.x)*b.extents.x+Mathf.Abs(tangent.z)*b.extents.z)*f;p.y=b.min.y+b.size.y*h;
      var start=p+d*(Mathf.Abs(d.x)*b.extents.x+Mathf.Abs(d.z)*b.extents.z+.4f);
      if(!c.Raycast(new Ray(start,-d),out var hit,1f))continue;
      if(Mathf.Abs(hit.normal.y)>.3f)continue;samples++;
      // Probe both sides of the contact. Decorative recesses <12cm are tolerated.
      if(Physics.Raycast(hit.point+hit.normal*.12f,-hit.normal,out var visible,.3f,1<<31,QueryTriggerInteraction.Ignore))continue;
      float gap=Physics.Raycast(hit.point+hit.normal*.12f,-hit.normal,out visible,1.2f,1<<31,QueryTriggerInteraction.Ignore)?visible.distance-.12f:1.2f;
      gaps.Add(new SurfaceGap{path=PathOf(c.transform),point=hit.point,normal=hit.normal,visualGap=gap});
     }
    }
    File.WriteAllText(Path.Combine(output,"surface-gaps.json"),JsonUtility.ToJson(new SurfaceReport{meshes=filters.Length,samples=samples,gaps=gaps.ToArray()},true));
   }finally{Physics.queriesHitBackfaces=back;EditorSceneManager.CloseScene(temp,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);}
   Debug.Log("Collider surface audit: "+samples+" probes, "+gaps.Count+" review candidates");
  }
  [Serializable]public class GridReport{public string[] rows;public int walkable;public Vector3 origin;public float spacing;}
  [MenuItem("HOSTFALL/Tools/Scan Collision Floor Grid")]
  public static void GroundGrid() => GroundGrid(FacilityBuilder.ScenePath, Output, true);
  internal static void GroundGrid(string scenePath, string output, bool capture)
  {
   if(!Application.dataPath.Contains(".artifacts/"))throw new InvalidOperationException("Run in verification copy");
   if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Unsaved scene");
   EditorSceneManager.OpenScene(scenePath);
   try{
    foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))if(c.GetComponentInParent<FacilityDoor>()!=null||c.GetComponentInParent<IDamageable>()!=null||c.gameObject.layer==CollisionLayers.Player)c.enabled=false;
    Physics.SyncTransforms();
    var solids=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.enabled&&!c.isTrigger).ToArray();
    var bounds=solids[0].bounds;foreach(var c in solids.Skip(1))bounds.Encapsulate(c.bounds);
    var origin=new Vector3(Mathf.Floor(bounds.min.x)-1,0,Mathf.Floor(bounds.min.z)-1);
    int width=Mathf.CeilToInt((Mathf.Ceil(bounds.max.x)+1-origin.x)*2)+1;
    var rows=new string[Mathf.CeilToInt((Mathf.Ceil(bounds.max.z)+1-origin.z)*2)+1];int free=0;
    for(int z=0;z<rows.Length;z++){
     var row=new char[width];
     for(int x=0;x<row.Length;x++){
      var p=origin+new Vector3(x*.5f,.45f,z*.5f);
      if(!Physics.Raycast(p,Vector3.down,out var floor,2,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore)){row[x]=' ';continue;}
      p.y=floor.point.y;
      bool blocked=Physics.CheckCapsule(p+Vector3.up*.37f,p+Vector3.up*1.5f,.3f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore);
      row[x]=blocked?'#':'.';if(!blocked)free++;
     }
     rows[z]=new string(row);
    }
    Directory.CreateDirectory(output);
    File.WriteAllText(Path.Combine(output,"floor-grid.json"),JsonUtility.ToJson(new GridReport{rows=rows,walkable=free,origin=origin,spacing=.5f},true));
    if(capture)FacilityReview.CaptureWorld();
   }finally{EditorSceneManager.OpenScene(scenePath);}
  }
  [MenuItem("HOSTFALL/Tools/Review Final Sector Boundaries")]
  public static void CaptureFinalSector()
  {
   var brains=Object.FindObjectsByType<Unity.Cinemachine.CinemachineBrain>(FindObjectsSortMode.None);
   var states=brains.Select(b=>b.enabled).ToArray();var camera=Object.FindAnyObjectByType<Camera>();
   var p=camera.transform.position;var r=camera.transform.rotation;float fov=camera.fieldOfView;bool enabled=camera.enabled;
   Directory.CreateDirectory(Output+"/review");
   try{foreach(var brain in brains)brain.enabled=false;camera.enabled=false;
    FacilityReview.Shot(camera,Output+"/review/evac-shoulders.png",new Vector3(10,1.64f,198),new Vector3(10,1.6f,191.8f),96);
    FacilityReview.Shot(camera,Output+"/review/evac-window.png",new Vector3(10,1.64f,202),new Vector3(10,2,206),86);
   }finally{camera.transform.SetPositionAndRotation(p,r);camera.fieldOfView=fov;camera.enabled=enabled;for(int i=0;i<brains.Length;i++)brains[i].enabled=states[i];}
  }
  static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
  public static string Output=>Path.GetFullPath("../../.artifacts/collision-audit-20261114");
  [MenuItem("HOSTFALL/Tools/Audit Collision Visibility")]
  public static void Scan()
  {
   if(!Application.dataPath.Contains(".artifacts/"))throw new InvalidOperationException("Run in verification copy");
   if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Scene has unsaved changes");
   EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);Physics.SyncTransforms();
   var entries=new List<Entry>();
   foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.enabled&&!c.isTrigger&&c.gameObject.layer!=CollisionLayers.Player))
   {
    var t=c.transform;var rs=t.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!(r is ParticleSystemRenderer)).ToArray();
    for(int i=0;rs.Length==0&&i<2&&t.parent!=null;i++){t=t.parent;rs=t.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!(r is ParticleSystemRenderer)).ToArray();}
    Bounds b=new Bounds();if(rs.Length>0){b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);}
    entries.Add(new Entry{path=PathOf(c.transform),type=c.GetType().Name,center=c.bounds.center,size=c.bounds.size,door=c.GetComponentInParent<FacilityDoor>()!=null,threat=c.GetComponentInParent<IDamageable>()!=null,visualOwner=PathOf(t),renderers=rs.Length,visualCenter=b.center,visualSize=b.size});
   }
   Directory.CreateDirectory(Output);File.WriteAllText(Output+"/inventory.json",JsonUtility.ToJson(new Report{solids=entries.ToArray()},true));
   Debug.Log("Collision visibility inventory: "+entries.Count);
  }
 }
}
