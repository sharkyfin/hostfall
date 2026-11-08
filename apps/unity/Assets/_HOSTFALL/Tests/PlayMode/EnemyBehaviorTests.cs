using System.Collections;
using System.IO;
using System.Linq;
using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Hostfall.Tests
{
    public sealed class EnemyBehaviorTests
    {
        private static WatcherController Enemy(string id)=>Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).Single(r=>r.Id==id);
        private static void Pose(WatcherController robot,Vector3 position,float yaw=0)
        { var data=robot.Capture(); data.position=position; data.bodyYaw=yaw; data.sensorEuler=Vector3.zero; data.scan=0; data.direction=1; data.contactCooldown=0; robot.Restore(data); }
        private static void PlayerAt(Vector3 position)
        { var p=Object.FindAnyObjectByType<PlayerMotor>(); var data=p.Capture(false); data.position=position; data.planarVelocity=Vector3.zero; p.Restore(data); }

        [UnityTest] public IEnumerator DetectionAndMovementRespectWallsAndContactIsRateLimited()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var controls=Object.FindAnyObjectByType<ControlState>(); var root=Object.FindAnyObjectByType<FacilityRoot>(); var robot=Enemy("watcher-r17");
            Pose(robot,new Vector3(0,0,2.2f)); PlayerAt(new Vector3(0,0,3.5f)); Physics.SyncTransforms();
            Assert.That(robot.Sees(Object.FindAnyObjectByType<PlayerMotor>().Eye),Is.True);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name="Enemy QA wall"; wall.transform.position=new Vector3(0,1,3); wall.transform.localScale=new Vector3(2,2,.15f); Physics.SyncTransforms();
            robot.Tick(0); Assert.That(robot.State.Mode,Is.EqualTo(WatcherMode.Patrol),"A wall must block detection");
            controls.SetMode(ControlMode.Exploring); robot.Tick(1); Physics.SyncTransforms();
            Assert.That(robot.transform.position.z,Is.LessThan(2.6f),"Body sweep must stop before the wall");
            int contacts=root.Simulation.WatcherContacts;
            Object.Destroy(wall); yield return null;
            Pose(robot,new Vector3(0,0,2.2f)); PlayerAt(new Vector3(0,0,2.85f)); Physics.SyncTransforms(); robot.Tick(.02f);
            Assert.That(root.Simulation.WatcherContacts,Is.EqualTo(contacts+1));
            Assert.That(Object.FindAnyObjectByType<PlayerMotor>().transform.position.z,Is.LessThan(0));
            PlayerAt(new Vector3(0,0,2.85f)); Physics.SyncTransforms(); robot.Tick(.02f);
            Assert.That(root.Simulation.WatcherContacts,Is.EqualTo(contacts+1),"Repeated contact must respect cooldown");
            Assert.That(robot.AnimationState,Is.EqualTo("Attack"));
            controls.SetMode(ControlMode.Paused); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator TwoRobotsAnimateAndObeyTheirOwnControlProcesses()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var controls=Object.FindAnyObjectByType<ControlState>(); var world=Object.FindAnyObjectByType<FacilityRoot>().Simulation;
            var relay=Enemy("watcher-r17"); var core=Enemy("sentry-r18");
            Assert.That(Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).Length,Is.GreaterThanOrEqualTo(2));
            foreach(var robot in new[] { relay,core })
            {
                var animator=robot.GetComponentInChildren<Animator>(); Assert.That(animator.runtimeAnimatorController,Is.Not.Null); Assert.That(animator.applyRootMotion,Is.False);
                Assert.That(robot.GetComponentsInChildren<SkinnedMeshRenderer>().Length,Is.GreaterThan(0));
                animator.speed=1; animator.Play("Walk",0,0); animator.Update(0);
                var bones=animator.GetComponentsInChildren<Transform>(); var rotations=bones.Select(b=>b.localRotation).ToArray(); var position=robot.transform.position;
                animator.Update(.2f);
                Assert.That(bones.Where((b,i)=>Quaternion.Angle(b.localRotation,rotations[i])>1).Any(),Is.True,"Imported walk must deform the rig");
                Assert.That(robot.transform.position,Is.EqualTo(position),"Animation must not displace the physical root");
            }
            Pose(relay,new Vector3(0,0,2.2f)); PlayerAt(new Vector3(-2.65f,.05f,-1.7f));
            controls.SetMode(ControlMode.Exploring); world.SetPower(DeviceId.Edge01,false); var before=relay.transform.position; relay.Tick(.5f); core.Tick(0);
            Assert.That(relay.State.Mode,Is.Not.EqualTo(WatcherMode.Offline),"Autonomous robot survives loss of network power");
            Assert.That(core.State.Mode,Is.Not.EqualTo(WatcherMode.Offline),"CORE has its own power/control");
            world.SetPower(DeviceId.Edge01,true); var shell=new TerminalSession(world,DeviceId.Ws07); shell.Execute("ps"); shell.Execute("systemctl disable cache-sync"); shell.Execute("systemctl stop cache-sync");
            relay.Tick(0); core.Tick(0); Assert.That(relay.State.Mode,Is.Not.EqualTo(WatcherMode.Maintenance),"Stopping the relay does not remove the autonomous enemy"); Assert.That(core.State.Mode,Is.Not.EqualTo(WatcherMode.Maintenance));
            new TerminalSession(world,DeviceId.Core01).Execute("kill 666"); core.Tick(0); Assert.That(core.State.Mode,Is.EqualTo(WatcherMode.Maintenance));
            Assert.That(core.AnimationState,Is.EqualTo("TurnOff")); controls.SetMode(ControlMode.Paused); before=core.transform.position; core.Tick(1);
            Assert.That(core.transform.position,Is.EqualTo(before)); Assert.That(core.GetComponentInChildren<Animator>().speed,Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator BothEnemyPosesRoundTripAndOldSavesWithoutSecondaryRemainValid()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var root=Object.FindAnyObjectByType<FacilityRoot>(); var data=root.Persistence.Capture();
            var sentryTarget=data.enemies.Single(e=>e.id=="sentry-r18");
            sentryTarget.position=new Vector3(.8f,0,10.8f); sentryTarget.bodyYaw=225; sentryTarget.direction=-1; sentryTarget.scan=3; sentryTarget.contactCooldown=1.5f;
            string path=Path.Combine(Path.GetTempPath(),"hostfall-enemy-"+System.Guid.NewGuid().ToString("N"));
            try
            {
                var store=new WorldSaveStore(path); store.Write(data); Assert.That(store.TryLoad(out var saved,out var error),Is.True,error);
                yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
                Enemy("watcher-r17").Restore(saved.watcher); Enemy("sentry-r18").Restore(saved.enemies.Single(e=>e.id=="sentry-r18"));
                var restored=Enemy("sentry-r18").Capture(); Assert.That(restored.position,Is.EqualTo(sentryTarget.position)); Assert.That(restored.bodyYaw,Is.EqualTo(225).Within(.001f));
                Assert.That(restored.direction,Is.EqualTo(-1)); Assert.That(restored.scan,Is.EqualTo(3)); Assert.That(restored.contactCooldown,Is.EqualTo(1.5f));
                data.enemies=null; store.Write(data); Assert.That(store.TryLoad(out saved,out error),Is.True,error);
                Assert.That(saved.enemies==null||saved.enemies.Length==0,Is.True);
                data.enemies=new[] { new RobotStateData { id="unknown" } }; Assert.Throws<System.ArgumentException>(()=>GameSaveData.Validate(data));
            }
            finally { if(Directory.Exists(path)) Directory.Delete(path,true); }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator AuthoredPatrolsCompleteBothDirectionsWithoutFurnitureBlockingThem()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var controls=Object.FindAnyObjectByType<ControlState>();
            PlayerAt(new Vector3(-2.65f,.05f,-1.7f));
            var relay=Enemy("watcher-r17"); var core=Enemy("sentry-r18");
            // This old saved pose sat just in front of EDGE's tabletop corner.
            Pose(relay,new Vector3(-.25f,0,4.92f)); Pose(core,new Vector3(.7f,0,10.2f));
            controls.SetMode(ControlMode.Exploring);
            Physics.SyncTransforms(); relay.Tick(.5f);
            Assert.That(relay.transform.position.x,Is.LessThan(-.38f),"Rejoin the clear lane before passing the desk corner");
            float relayMin=100,relayMax=-100,coreMin=100,coreMax=-100;
            for(int i=0;i<240;i++)
            {
                Physics.SyncTransforms(); relay.Tick(.1f); core.Tick(.1f);
                relayMin=Mathf.Min(relayMin,relay.transform.position.z); relayMax=Mathf.Max(relayMax,relay.transform.position.z);
                coreMin=Mathf.Min(coreMin,core.transform.position.z); coreMax=Mathf.Max(coreMax,core.transform.position.z);
            }
            Assert.That(relayMin,Is.LessThan(2.35f)); Assert.That(relayMax,Is.GreaterThan(5.45f));
            Assert.That(coreMin,Is.LessThan(9.25f)); Assert.That(coreMax,Is.GreaterThan(11.65f));
            controls.SetMode(ControlMode.Paused); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator TargetLossTransitionsIntoSearchAtLastKnownPosition()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var controls=Object.FindAnyObjectByType<ControlState>(); var robot=Enemy("watcher-r17");
            Pose(robot,new Vector3(0,0,2.2f)); PlayerAt(new Vector3(0,0,4.3f)); Physics.SyncTransforms();
            controls.SetMode(ControlMode.Exploring);

            // Initial glance starts suspicion buildup
            robot.Tick(.1f);
            Assert.That(robot.State.Mode,Is.EqualTo(WatcherMode.Suspicious),"Brief glance at distance triggers suspicion");

            // Sustained gaze enters full alert
            for(int i=0;i<4;i++) { Physics.SyncTransforms(); robot.Tick(.1f); }
            Assert.That(robot.State.Mode,Is.EqualTo(WatcherMode.Alert),"Sustained line of sight alerts the robot");
            Assert.That(robot.LastKnownPosition.z,Is.EqualTo(4.3f).Within(.1f));

            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name="Search test wall";
            wall.transform.position=new Vector3(0,1,3.2f); wall.transform.localScale=new Vector3(2,2,.15f); Physics.SyncTransforms();
            robot.Tick(.1f);
            Assert.That(robot.State.Mode,Is.EqualTo(WatcherMode.Search),"Breaking sight must enter Search instead of instant Patrol");
            Assert.That(robot.SearchTimer,Is.GreaterThan(0));

            for(int i=0;i<45;i++) { Physics.SyncTransforms(); robot.Tick(.1f); }
            Assert.That(robot.State.Mode,Is.EqualTo(WatcherMode.Patrol),"Expired search returns to patrol");
            Object.Destroy(wall); controls.SetMode(ControlMode.Paused); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator AttackWindupTelegraphsAndAllowsDodge()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var controls=Object.FindAnyObjectByType<ControlState>(); var root=Object.FindAnyObjectByType<FacilityRoot>();
            var robot=Enemy("watcher-r17");
            Pose(robot,new Vector3(0,0,2.2f)); PlayerAt(new Vector3(0,0,3.5f)); Physics.SyncTransforms();
            controls.SetMode(ControlMode.Exploring);
            int contactsBefore=root.Simulation.WatcherContacts;

            // Distance is 1.3m: within windup telegraph range (0.7m < dist <= 1.65m)
            robot.Tick(.05f);
            Assert.That(robot.State.Mode,Is.EqualTo(WatcherMode.AttackWindup),"Approaching target initiates telegraph windup");
            Assert.That(root.Simulation.WatcherContacts,Is.EqualTo(contactsBefore),"Windup phase does not cause premature contact");
            Assert.That(robot.WindupTimer,Is.GreaterThan(0));

            // Player dashes away to safe distance
            PlayerAt(new Vector3(0,0,5.2f)); Physics.SyncTransforms();
            robot.Tick(.45f);
            Assert.That(root.Simulation.WatcherContacts,Is.EqualTo(contactsBefore),"Successful dodge avoids contact");
            Assert.That(robot.State.Mode,Is.Not.EqualTo(WatcherMode.AttackWindup));

            controls.SetMode(ControlMode.Paused); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator ArcShotgunDestroysRobotsInTwoShots()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var robot = Enemy("watcher-r17");
            Assert.That(robot.Health, Is.EqualTo(100f));
            Assert.That(robot.IsDestroyed, Is.False);

            // First shot deals 55 damage
            robot.TakeDamage(55f);
            Assert.That(robot.Health, Is.EqualTo(45f));
            Assert.That(robot.IsDestroyed, Is.False);
            Assert.That(robot.State.Mode, Is.EqualTo(WatcherMode.Alert), "First shot alerts the damaged robot");

            // Second shot deals 55 damage -> total 110 dmg -> destroys 100 HP robot!
            robot.TakeDamage(55f);
            Assert.That(robot.Health, Is.EqualTo(0f));
            Assert.That(robot.IsDestroyed, Is.True, "Second shot destroys the robot completely");
            Assert.That(robot.State.Mode, Is.EqualTo(WatcherMode.Offline));

            var collider = robot.GetComponent<CapsuleCollider>();
            Assert.That(collider.enabled, Is.False, "Destroyed robot collider is disabled");

            // Tick does not revive or move destroyed robot
            robot.Tick(0.5f);
            Assert.That(robot.IsDestroyed, Is.True);
            Assert.That(robot.State.Mode, Is.EqualTo(WatcherMode.Offline));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
