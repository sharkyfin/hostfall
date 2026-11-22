using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Hostfall.Core;
using Hostfall.World;
using Hostfall.Player;
using Hostfall.Persistence;
namespace Hostfall.Tests
{
 public sealed class CollisionAuditTests
 {
  [TearDown]public void Reset()=>WorldSaveController.FreshStartRequested=false;
  [Serializable]public class Crossing{public string id;public Vector3 center,normal,stopped;public bool passed;public string[] blockers;}
  [Serializable]public class Report{public Crossing[] doors;}
  [UnityTest]public IEnumerator AllOpenDoorsAllowACharacterCrossingInBothDirections()
  {
   WorldSaveController.FreshStartRequested=true;
   yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
   var motor=UnityEngine.Object.FindAnyObjectByType<PlayerMotor>();motor.enabled=false;var cc=motor.GetComponent<CharacterController>();
   foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))if(c.GetComponentInParent<IDamageable>()!=null&&c.gameObject.layer!=CollisionLayers.Player)c.enabled=false;
   var doors=UnityEngine.Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None);var rows=new List<Crossing>();
   var centers=new Dictionary<FacilityDoor,Bounds>();
   foreach(var d in doors){var leaf=(Transform)typeof(FacilityDoor).GetField("leaf",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(d);var col=leaf.GetComponentInChildren<Collider>();centers[d]=col.bounds;
    var state=d.CaptureEntry();state.opened=true;state.unlocked=true;state.offset=(float)typeof(FacilityDoor).GetField("travel",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(d);d.RestoreEntry(state);}
   Physics.SyncTransforms();
   foreach(var d in doors){var b=centers[d];var normal=b.size.x>b.size.z?Vector3.forward:Vector3.right;var side=Vector3.Cross(Vector3.up,normal);
    foreach(float sign in new[]{-1f,1f}){
     var row=new Crossing{id=d.DoorId,center=b.center,normal=normal*sign};var blocks=new HashSet<string>();
     foreach(float lane in new[]{0f,-.55f,.55f}){
      var start=b.center+side*lane-normal*sign*1.15f;start.y=b.min.y+.1f;
      if(!Physics.Raycast(start+Vector3.up*.35f,Vector3.down,out var floor,1,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore)){blocks.Add("missing start floor");continue;}start.y=floor.point.y+.05f;
      cc.enabled=false;cc.transform.position=start;cc.enabled=true;Physics.SyncTransforms();
      for(int i=0;i<100;i++){cc.Move(normal*sign*.035f+Vector3.down*.04f);if(Vector3.Dot(cc.transform.position-start,normal*sign)>2.2f){row.passed=true;break;}}
      if(row.passed)break;row.stopped=cc.transform.position;
      foreach(var c in Physics.OverlapCapsule(row.stopped+Vector3.up*.36f,row.stopped+Vector3.up*1.45f,.37f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore))blocks.Add(PathOf(c.transform));
     }
     row.blockers=blocks.ToArray();rows.Add(row);
    }
   }
   var dir=Path.GetFullPath("../../.artifacts/collision-audit-20261114");Directory.CreateDirectory(dir);File.WriteAllText(dir+"/door-crossings.json",JsonUtility.ToJson(new Report{doors=rows.ToArray()},true));
   WorldSaveController.FreshStartRequested=false;
   Assert.That(rows.Where(r=>!r.passed).Select(r=>r.id+" "+r.normal+": "+string.Join(",",r.blockers)),Is.Empty);
  }
  static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
 }
}
