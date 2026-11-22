using System.Collections;
using System.Linq;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Hostfall.Tests
{
 public sealed class ColliderProfileTests
 {
  [TearDown]public void Reset()=>WorldSaveController.FreshStartRequested=false;
  [UnityTest]public IEnumerator EvacuationShouldersStopTheCharacterBeforeTheVoid()
  {
   WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
   var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.enabled=false;var cc=motor.GetComponent<CharacterController>();
   foreach(float x in new[]{2.7f,17.3f})
   {
    cc.enabled=false;cc.transform.position=new Vector3(x,.05f,193);cc.enabled=true;Physics.SyncTransforms();
    for(int i=0;i<60;i++)cc.Move(new Vector3(0,-.04f,-.035f));
    Assert.That(cc.transform.position.z,Is.GreaterThan(192.2f),"Open south shoulder of evacuation room at X="+x);
    Assert.That(cc.transform.position.y,Is.GreaterThan(-.1f));
   }
  }
  [UnityTest]public IEnumerator PanoramicWindowStopsAnActualJumpingCharacter()
  {
   WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
   var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.enabled=false;var cc=motor.GetComponent<CharacterController>();
   cc.enabled=false;cc.transform.position=new Vector3(10,.05f,204.6f);cc.enabled=true;Physics.SyncTransforms();
   float velocity=Mathf.Sqrt(.8f*44);float maximum=cc.transform.position.z;
   for(int i=0;i<60;i++)
   {
    velocity-=22*.02f;cc.Move(new Vector3(0,velocity*.02f,.07f));maximum=Mathf.Max(maximum,cc.transform.position.z);
    if(cc.transform.position.y< -1)break;
   }
   Assert.That(maximum,Is.LessThan(205.8f),"Jump passed through the panoramic armored window into the void");
  }
  [UnityTest]public IEnumerator CharacterCanPassBesideWeaponAndFixtureProfiles()
  {
   WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
   var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.enabled=false;var cc=motor.GetComponent<CharacterController>();
   var candidates=Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>c.GetComponent<WeaponPickup>()!=null||c.GetComponent<PulseSidearmPickup>()!=null||c.name=="Prop_Light_Small"&&c.transform.parent.parent.name=="NORTHLINE / Service Bays").ToArray();
   Assert.That(candidates.Length,Is.EqualTo(15));
   // Isolate each suspect proxy so a nearby wall cannot hide or cause its result.
   var solids=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.enabled&&c!=cc).ToArray();foreach(var c in solids)c.enabled=false;
   var failures=new System.Collections.Generic.List<string>();
   foreach(var c in candidates){
    var rs=c.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
    Vector3 n=c.transform.parent.forward;var side=Vector3.Cross(Vector3.up,n);float extent=Vector3.Dot(b.extents,new Vector3(Mathf.Abs(n.x),0,Mathf.Abs(n.z)));
    var start=b.center+n*(extent+cc.radius+.06f)-side*.7f;start.y=b.center.y-.85f;
    cc.enabled=false;cc.transform.position=start;cc.enabled=true;c.enabled=true;Physics.SyncTransforms();
    for(int i=0;i<40;i++)cc.Move(side*.035f);
    if(Vector3.Dot(cc.transform.position-start,side)<1.35f)failures.Add(c.transform.parent.name+" / "+c.name+" stops character beside visible outline");
    // Collider remains available to the interaction ray after tightening.
    Assert.That(c.Raycast(new Ray(b.center+n*2,-n),out var hit,3),Is.True,c.name+" interaction surface");c.enabled=false;
   }
   foreach(var c in solids)c.enabled=true;WorldSaveController.FreshStartRequested=false;
   Assert.That(failures,Is.Empty,string.Join("\n",failures));
  }
 }
}
