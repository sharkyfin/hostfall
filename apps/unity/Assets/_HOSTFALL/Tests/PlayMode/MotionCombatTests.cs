using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Hostfall.Core;
using Hostfall.Mapping;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.UI;
namespace Hostfall.Tests
{
 public sealed class MotionCombatTests
 {
  [UnityTest]public IEnumerator MotionReviewAndReusableCombatObjects()
  {
   WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
   var controls=UnityEngine.Object.FindAnyObjectByType<ControlState>();controls.SetMode(ControlMode.Exploring);
   var motor=UnityEngine.Object.FindAnyObjectByType<PlayerMotor>();motor.enabled=false;
   var state=motor.Capture(false);state.position=new Vector3(0,.05f,-4);state.yaw=0;state.pitch=0;state.targetPitch=0;motor.Restore(state);
   var body=UnityEngine.Object.FindAnyObjectByType<OperatorVisual>();
   var weapon=motor.GetComponent<PlayerWeapon>();var motion=motor.GetComponent<OperatorMotion>();
   var directory=Path.GetFullPath("../../.artifacts/motion-combat-20261115/review");Directory.CreateDirectory(directory);
   var camera=Camera.main;var bodyCamera=new GameObject("Motion review camera").AddComponent<Camera>();bodyCamera.transform.position=motor.transform.position+new Vector3(2.3f,1.25f,2.8f);bodyCamera.transform.LookAt(motor.transform.position+Vector3.up*.9f);bodyCamera.fieldOfView=38;bodyCamera.nearClipPlane=.05f;
   weapon.Unequip();yield return null;Save(bodyCamera,"idle");
   state.planarVelocity=Vector3.forward*3.4f;state.hasWeapon=false;motor.Restore(state);
   yield return new WaitForSeconds(.4f);Save(bodyCamera,"walk-a");yield return new WaitForSeconds(.25f);Save(bodyCamera,"walk-b");
   state.planarVelocity=Vector3.forward*5.4f;motor.Restore(state);yield return new WaitForSeconds(.4f);Save(bodyCamera,"sprint");
   state.planarVelocity=Vector3.zero;motor.Restore(state);yield return new WaitForSeconds(.4f);
   weapon.PickupWeapon(ActiveWeaponType.Arc12Shotgun,6,18);yield return new WaitForSeconds(.35f);Save(camera,"arc-fps");
   var old=weapon.HeldModel;int impactCount=motor.GetComponentsInChildren<Light>(true).Length;
   for(int i=0;i<3;i++){Assert.That(weapon.TryFire(),Is.True);yield return new WaitForSeconds(.7f);}
   Assert.That(motor.GetComponentsInChildren<Light>(true).Length,Is.EqualTo(impactCount),"Firing reuses impact lights");
   weapon.PickupWeapon(ActiveWeaponType.CirrusPulseSidearm,15,45);yield return new WaitForSeconds(.35f);Save(camera,"cirrus-fps");
   weapon.PickupWeapon(ActiveWeaponType.TitanbreakerHeavyInductor,3,9);yield return new WaitForSeconds(.35f);weapon.BeginCharge();yield return new WaitForSeconds(.5f);Save(camera,"titan-charge");
   weapon.ReleaseCharge();weapon.PickupWeapon(ActiveWeaponType.Arc12Shotgun,3,11);yield return new WaitForSeconds(.35f);Assert.That(weapon.HeldModel,Is.SameAs(old),"Held models are cached");Save(bodyCamera,"armed-body");
   body.ReachTo(motor.transform.position+new Vector3(-.3f,1.3f,.7f));yield return new WaitForSeconds(.22f);Save(bodyCamera,"interact");
   yield return new WaitForSeconds(.4f);var drops=UnityEngine.Object.FindObjectsByType<DroppedWeapon>(FindObjectsSortMode.None);Assert.That(drops.Length,Is.EqualTo(3));
   foreach(var drop in drops){Assert.That(drop.GetComponent<Rigidbody>(),Is.Not.Null);Assert.That(drop.gameObject.layer,Is.EqualTo(CollisionLayers.WeaponItem));}
   Assert.That(Physics.GetIgnoreLayerCollision(CollisionLayers.Player,CollisionLayers.WeaponItem),Is.True,"Dropped guns never block the route");
   UnityEngine.Object.Destroy(bodyCamera.gameObject);LogAssert.NoUnexpectedReceived();
   void Save(Camera cam,string name)
   {
    if(cam==bodyCamera)body.SetView(false,true);
    var target=RenderTexture.GetTemporary(1280,800,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var previous=RenderTexture.active;cam.targetTexture=target;cam.Render();RenderTexture.active=target;
    var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();File.WriteAllBytes(directory+"/"+name+".png",image.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.Destroy(image);RenderTexture.ReleaseTemporary(target);
    if(cam==bodyCamera)body.SetView(true,true);
   }
  }
  [TearDown]public void Reset(){WorldSaveController.FreshStartRequested=false;WorldSaveController.TestStorageDirectory=null;}
  [UnityTest]public IEnumerator PickupDropsOldAmmoAndSurvivesAnActualSaveLoad()
  {
   WorldSaveController.TestStorageDirectory=Path.GetFullPath("../../.artifacts/motion-combat-20261115/test-saves");
   WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
   var controls=UnityEngine.Object.FindAnyObjectByType<ControlState>();controls.SetMode(ControlMode.Exploring);
   var weapon=UnityEngine.Object.FindAnyObjectByType<PlayerWeapon>();
   weapon.PickupWeapon(ActiveWeaponType.Arc12Shotgun,3,11,"weapon.test.arc");
   weapon.PickupWeapon(ActiveWeaponType.CirrusPulseSidearm,8,23,"weapon.test.pulse");yield return new WaitForSeconds(.1f);
   var dropped=UnityEngine.Object.FindObjectsByType<DroppedWeapon>(FindObjectsSortMode.None);Assert.That(dropped.Length,Is.EqualTo(1));string id=dropped[0].Id;
   var root=UnityEngine.Object.FindAnyObjectByType<FacilityRoot>();var data=root.Persistence.Capture();GameSaveData.Validate(data);
   Assert.That(data.simulation.weapons.drops[0].ammo,Is.EqualTo(3));Assert.That(data.simulation.weapons.drops[0].reserve,Is.EqualTo(11));
   Assert.That(root.Persistence.Save("quick"),Is.True);root.Persistence.Load("quick");yield return null;yield return null;
   weapon=UnityEngine.Object.FindAnyObjectByType<PlayerWeapon>();Assert.That(weapon.ActiveWeapon,Is.EqualTo(ActiveWeaponType.CirrusPulseSidearm));Assert.That(weapon.CurrentAmmo,Is.EqualTo(8));
   dropped=UnityEngine.Object.FindObjectsByType<DroppedWeapon>(FindObjectsSortMode.None);Assert.That(dropped.Length,Is.EqualTo(1));Assert.That(dropped[0].Id,Is.EqualTo(id));
   dropped[0].Interact();yield return null;
   Assert.That(weapon.ActiveWeapon,Is.EqualTo(ActiveWeaponType.Arc12Shotgun));Assert.That(weapon.CurrentAmmo,Is.EqualTo(3));Assert.That(weapon.ReserveAmmo,Is.EqualTo(11));
   Assert.That(UnityEngine.Object.FindObjectsByType<DroppedWeapon>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
   weapon.SwitchWeapon(ActiveWeaponType.CirrusPulseSidearm);Assert.That(weapon.ActiveWeapon,Is.EqualTo(ActiveWeaponType.Arc12Shotgun),"Cannot summon a dropped weapon from an old slot");
  }
  [UnityTest]public IEnumerator ShellLoadingOverheatAndChargeAreDifferentMechanics()
  {
   WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
   var controls=UnityEngine.Object.FindAnyObjectByType<ControlState>();controls.SetMode(ControlMode.Exploring);
   // Isolate weapon timing from live patrol damage while retaining a valid world drop position.
   foreach(var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
    if(component is Hostfall.World.IDamageable && component is not PlayerHealth)component.gameObject.SetActive(false);
   UnityEngine.Object.FindAnyObjectByType<PlayerMotor>().enabled=false;
   var weapon=UnityEngine.Object.FindAnyObjectByType<PlayerWeapon>();
   weapon.Restore(new Hostfall.Simulation.WeaponLoadoutData{carryVersion=1,equipped=true,arc=0,arcReserve=3});
   Assert.That(weapon.StartReload(),Is.True);yield return new WaitForSeconds(.55f);Assert.That(weapon.CurrentAmmo,Is.EqualTo(1));Assert.That(weapon.IsReloading,Is.True);
   Assert.That(weapon.TryFire(),Is.True);Assert.That(weapon.IsReloading,Is.False);Assert.That(weapon.ReserveAmmo,Is.EqualTo(2));
   weapon.PickupWeapon(ActiveWeaponType.CirrusPulseSidearm,15,45);yield return new WaitForSeconds(.32f);
   for(int i=0;i<10&&!weapon.IsOverheated;i++)
   {
    // A coroutine can resume before PlayerWeapon.Update decrements this frame's timer.
    float deadline=Time.time+.5f;while(weapon.Capture().cooldown>0&&Time.time<deadline)yield return null;
    Assert.That(weapon.Capture().cooldown,Is.Zero,"Weapon cooldown must settle");
    Assert.That(weapon.TryFire(),Is.True);yield return new WaitForSeconds(.19f);
   }
   Assert.That(weapon.IsOverheated,Is.True);Assert.That(weapon.TryFire(),Is.False);yield return new WaitForSeconds(2.6f);Assert.That(weapon.IsOverheated,Is.False);
   weapon.PickupWeapon(ActiveWeaponType.TitanbreakerHeavyInductor,3,9);yield return new WaitForSeconds(.32f);
   Assert.That(weapon.BeginCharge(),Is.True);yield return new WaitForSeconds(.85f);Assert.That(weapon.Charge,Is.GreaterThan(.98f));
   var savedCharge=weapon.Capture();Assert.That(savedCharge.charging,Is.True);weapon.Restore(savedCharge);Assert.That(weapon.Charge,Is.GreaterThan(.98f));
   controls.SetMode(ControlMode.Paused);float charge=weapon.Charge;yield return new WaitForSecondsRealtime(.15f);Assert.That(weapon.Charge,Is.EqualTo(charge));
   controls.SetMode(ControlMode.Exploring);Assert.That(weapon.ReleaseCharge(),Is.True);Assert.That(weapon.CurrentAmmo,Is.EqualTo(2));Assert.That(weapon.Charge,Is.Zero);
  }
  [UnityTest]public IEnumerator ImportedMotionHasActualKeysAndDoesNotMoveTheController()
  {
   WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
   var library=Resources.Load<OperatorMotionLibrary>("Motion/TypeIVMotionLibrary");Assert.That(library,Is.Not.Null);Assert.That(library.clips.Length,Is.EqualTo(12));
   var root=new GameObject("Animation proof");var joint=new GameObject("HandL");joint.transform.SetParent(root.transform,false);
   var clip=library.Find("Walk_Loop");clip.SampleAnimation(root,0);var first=joint.transform.localPosition;clip.SampleAnimation(root,clip.length*.4f);Assert.That(Vector3.Distance(first,joint.transform.localPosition),Is.GreaterThan(.03f));UnityEngine.Object.Destroy(root);
   var motor=UnityEngine.Object.FindAnyObjectByType<PlayerMotor>();motor.enabled=false;var position=motor.transform.position;
   UnityEngine.Object.FindAnyObjectByType<ControlState>().SetMode(ControlMode.Exploring);var motion=motor.GetComponent<OperatorMotion>();
   motion.PlayAction("Interact",.3f);yield return new WaitForSeconds(.35f);Assert.That(motor.transform.position,Is.EqualTo(position),"Animation has no gameplay root motion");
   motor.MotionIntensity=0;yield return null;Assert.That(motion.CameraOffset,Is.EqualTo(Vector3.zero));
  }
  [UnityTest]public IEnumerator FrameWindowBenchmark()
  {
   WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
   UnityEngine.Object.FindAnyObjectByType<ControlState>().SetMode(ControlMode.Exploring);
   UnityEngine.Object.FindAnyObjectByType<PlayerMotor>().enabled=false;
   for(int i=0;i<60;i++)yield return null;
   using var cpu=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal,"Main Thread",1);
   using var gc=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",1);
   var samples=new long[180];var allocations=new long[180];
   for(int i=0;i<samples.Length;i++){yield return null;samples[i]=cpu.LastValue;allocations[i]=gc.LastValue;}
   Array.Sort(samples);Array.Sort(allocations);
   File.WriteAllText(Path.GetFullPath("../../.artifacts/motion-combat-20261115/frame-window.json"),"{\"frames\":180,\"cpuValid\":"+cpu.Valid.ToString().ToLowerInvariant()+",\"gcValid\":"+gc.Valid.ToString().ToLowerInvariant()+",\"cpuMedianNs\":"+samples[90]+",\"cpuP95Ns\":"+samples[171]+",\"gcMedianBytes\":"+allocations[90]+",\"gcP95Bytes\":"+allocations[171]+"}");
   WorldSaveController.FreshStartRequested=false;
  }
  [UnityTest]public IEnumerator MapRefreshBenchmark()
  {
   WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
   var ui=UnityEngine.Object.FindAnyObjectByType<FacilityUI>();
   for(int i=0;i<30;i++)ui.RefreshMap();
   var start=GC.GetAllocatedBytesForCurrentThread();var clock=Stopwatch.StartNew();
   for(int i=0;i<200;i++)ui.RefreshMap();clock.Stop();long allocated=GC.GetAllocatedBytesForCurrentThread()-start;
   var dir=Path.GetFullPath("../../.artifacts/motion-combat-20261115");Directory.CreateDirectory(dir);
   File.WriteAllText(dir+"/map-benchmark.json","{\"iterations\":200,\"milliseconds\":"+clock.Elapsed.TotalMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"allocatedBytes\":"+allocated+"}");
   WorldSaveController.FreshStartRequested=false;
  }
 }
}
