using System.Collections;
using System.Linq;
using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hostfall.Tests
{
    public sealed class WorldCirculationTests
    {
        [UnitySetUp] public IEnumerator FreshScene()
        {WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;}
        [TearDown]public void Reset()=>WorldSaveController.FreshStartRequested=false;
        private CharacterController Controller()
        {
            var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.enabled=false;
            // Test static circulation with progression doors open and robots held still.
            foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
                if(c.gameObject.layer!=CollisionLayers.Player&&(c.GetComponentInParent<IDamageable>()!=null||c.GetComponentInParent<FacilityDoor>()!=null))c.enabled=false;
            Physics.SyncTransforms();return motor.GetComponent<CharacterController>();
        }
        [UnityTest]public IEnumerator LogisticsBothAccessStairsReachTheWorkstationLevel()
        {
            var cc=Controller();
            foreach(bool north in new[]{false,true})
            {
                Put(cc,new Vector3(4,.08f,north?79:65));
                Walk(cc,new Vector3(4,0,north?75.8f:68.2f));
                Assert.That(cc.transform.position.y,Is.EqualTo(1).Within(.15f),"stair top");
                Walk(cc,new Vector3(5.5f,0,north?75.8f:68.2f));
                Assert.That(cc.transform.position.y,Is.EqualTo(1).Within(.15f),"cross to deck");
                Walk(cc,new Vector3(4,0,north?75.8f:68.2f));Walk(cc,new Vector3(4,0,north?79:65));
                Assert.That(cc.transform.position.y,Is.EqualTo(0).Within(.15f),"descent");
            }
            yield return null;
        }
        [UnityTest]public IEnumerator RoboticsBothStairsReachTheCatwalkWithoutHittingCloudCeiling()
        {
            var cc=Controller();
            foreach(bool east in new[]{false,true})
            {
                float start=east?49.1f:17.9f,end=east?43.5f:23.5f;
                Put(cc,new Vector3(start,.08f,91.5f));Walk(cc,new Vector3(end,0,91.5f));
                Assert.That(cc.transform.position.y,Is.EqualTo(4.3f).Within(.15f),"catwalk arrival");
                Assert.That(Physics.CheckCapsule(cc.transform.position+Vector3.up*.4f,cc.transform.position+Vector3.up*1.5f,.28f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore),Is.False,"head clearance");
                Walk(cc,new Vector3(start,0,91.5f));Assert.That(cc.transform.position.y,Is.EqualTo(0).Within(.15f));
            }
            yield return null;
        }
        [UnityTest]public IEnumerator CitadelAndDispatchReachTheirUpperFloorsInBothDirections()
        {
            var cc=Controller();
            foreach(var r in new[]{(38f,20.35f,26f,3.3f),(20f,112.25f,119f,3.725f)})
            {
                Put(cc,new Vector3(r.Item1,.08f,r.Item2));Walk(cc,new Vector3(r.Item1,0,r.Item3));
                Assert.That(cc.transform.position.y,Is.EqualTo(r.Item4).Within(.15f),"upper arrival");
                Walk(cc,new Vector3(r.Item1,0,r.Item2));Assert.That(cc.transform.position.y,Is.EqualTo(0).Within(.15f));
            }
            yield return null;
        }
        [UnityTest]public IEnumerator QuantumTrenchHasFourUsableEscapeStairs()
        {
            var cc=Controller();
            foreach(float x in new[]{-2.45f,2.45f})foreach(bool north in new[]{false,true})
            {
                Put(cc,new Vector3(x,-1.12f,north?61.35f:38.65f));
                Walk(cc,new Vector3(x,0,north?58.9f:41.1f));
                Walk(cc,new Vector3(x>0?1.3f:-1.3f,0,north?59:41));
                Assert.That(cc.transform.position.y,Is.EqualTo(.125f).Within(.16f),"bridge arrival");
            }
            yield return null;
        }
        [UnityTest]public IEnumerator RingRacksFitTheirRoomAndHaveNoCeilingInTheRoboticsCatwalk()
        {
            var ring=GameObject.Find("Sector 09 - Cloud Control");
            foreach(Transform child in ring.transform)
                if(child.name=="High-Density Memory Module Column")
                {var b=child.GetComponentsInChildren<Renderer>()[0].bounds;Assert.That(b.max.y,Is.LessThan(5.45f));Assert.That(child.position.z,Is.GreaterThan(95.5f));}
            foreach(float x in new[]{23.5f,30f,43.5f})
            {
                Assert.That(Physics.Raycast(new Vector3(x,4.4f,91.5f),Vector3.up,out var hit,2,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore),Is.False,"head obstruction: "+(hit.collider!=null?hit.collider.name:""));
            }
            yield return null;
        }
        [UnityTest]public IEnumerator RepairedOuterEdgesStopTheCharacterBeforeTheVoid()
        {
            var cc=Controller();
            foreach(var route in new[]{(new Vector3(11.2f,0,-2.5f),Vector3.right,1f),(new Vector3(35,0,124),Vector3.forward,1.2f),(new Vector3(59,0,121),Vector3.forward,1.2f),(new Vector3(59,0,103),Vector3.back,1.2f),(new Vector3(39,0,49),Vector3.forward,1.2f),(new Vector3(35,0,54),Vector3.right,1.2f)})
            {
                Put(cc,route.Item1+Vector3.up*.08f);
                for(int n=0;n<90;n++)cc.Move(route.Item2*.045f+Vector3.down*.04f);
                Assert.That(Vector3.Dot(cc.transform.position-route.Item1,route.Item2),Is.LessThan(route.Item3),"open boundary from "+route.Item1);
                Assert.That(cc.transform.position.y,Is.GreaterThan(-.2f),"fell into void");
            }
            yield return null;
        }
        [UnityTest]public IEnumerator ServiceBayFloorPropsAreGroundedAndDoNotIntersectSolidEquipment()
        {
            var root=GameObject.Find("NORTHLINE / Service Bays");Assert.That(root,Is.Not.Null);var issues=new System.Collections.Generic.List<string>();
            foreach(Transform bay in root.transform)
            foreach(Transform prop in bay)
            {
                if(prop.name!="Prop_Crate4"&&prop.name!="Prop_Barrel_Large"&&prop.name!="Maintenance cart")continue;
                var c=prop.GetComponent<BoxCollider>();Assert.That(c,Is.Not.Null,prop.name);
                var b=c.bounds;
                Assert.That(Physics.Raycast(new Vector3(b.center.x,.2f,b.center.z),Vector3.down,.5f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore),Is.True,bay.name+" missing floor");
                foreach(var other in Physics.OverlapBox(b.center,b.extents,Quaternion.identity,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore))
                {
                    if(other==c||other.transform.IsChildOf(prop)||other.GetComponentInParent<IDamageable>()!=null)continue;
                    if(Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,other,other.transform.position,other.transform.rotation,out var dir,out float distance)&&distance>.03f&&Mathf.Abs(dir.y)<.5f)
                        issues.Add(bay.name+" / "+prop.name+" intersects "+other.name+" by "+distance);
                }
            }
            Assert.That(issues,Is.Empty,string.Join("; ",issues));
            yield return null;
        }
        private static void Put(CharacterController cc,Vector3 p)
        {cc.enabled=false;cc.transform.position=p;cc.enabled=true;Physics.SyncTransforms();}
        private static void Walk(CharacterController cc,Vector3 target)
        {
            for(int i=0;i<1500;i++)
            {
                var d=target-cc.transform.position;d.y=0;if(d.magnitude<.06f){for(int settle=0;settle<20;settle++)cc.Move(Vector3.down*.04f);return;}
                cc.Move(Vector3.ClampMagnitude(d,.045f)+Vector3.down*.04f);
            }
            Assert.Fail("Blocked at "+cc.transform.position+" walking to "+target);
        }
    }
}
