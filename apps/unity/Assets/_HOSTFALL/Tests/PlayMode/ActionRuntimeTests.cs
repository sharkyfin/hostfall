using System.Collections;
using System.Linq;
using Hostfall.Core;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Hostfall.Tests
{
    public sealed class ActionRuntimeTests
    {
        private static void PosePlayer(Vector3 position,float pitch=0)
        {
            var motor=Object.FindAnyObjectByType<PlayerMotor>(); var state=motor.Capture(false);
            state.position=position; state.yaw=0; state.pitch=pitch; state.targetPitch=pitch; state.planarVelocity=Vector3.zero; state.verticalSpeed=0; motor.Restore(state);
        }
        [UnityTest] public IEnumerator PulseRequiresUnlockAndSightStopsContactAndExpiresWithoutReloadReset()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var root=Object.FindAnyObjectByType<FacilityRoot>(); var controls=Object.FindAnyObjectByType<ControlState>();
            var actions=Object.FindAnyObjectByType<FacilityActionController>();
            Object.FindAnyObjectByType<Hostfall.Interaction.InteractionScanner>().enabled=false;
            var robot=Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).Single(r=>r.Id=="watcher-r17");
            var pose=robot.Capture(); pose.position=new Vector3(0,0,3); pose.bodyYaw=180; pose.contactCooldown=0; robot.Restore(pose);
            PosePlayer(new Vector3(0,.05f,1.4f),20); controls.SetMode(ControlMode.Exploring); Physics.SyncTransforms();
            Assert.That(actions.TryPulse(),Is.False); Assert.That(root.Simulation.Actions.PulseCooldown,Is.Zero);
            MenuFlowTests.FinishRoutine(root.Simulation,DeviceId.Ws07);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position=new Vector3(0,1,2.2f); wall.transform.localScale=new Vector3(2,2,.15f); Physics.SyncTransforms();
            Assert.That(actions.TryPulse(),Is.False,"Wall must block the ability"); Assert.That(root.Simulation.Actions.PulseCooldown,Is.Zero);
            Object.Destroy(wall); yield return null; Physics.SyncTransforms();
            Assert.That(actions.TryPulse(),Is.True); Assert.That(actions.TryPulse(),Is.False,"Cooldown is shared across targets");
            var before=robot.transform.position; int contacts=root.Simulation.WatcherContacts;
            robot.Tick(1); Assert.That(robot.State.Mode,Is.EqualTo(WatcherMode.Jammed)); Assert.That(robot.transform.position,Is.EqualTo(before));
            Assert.That(root.Simulation.WatcherContacts,Is.EqualTo(contacts));
            controls.SetMode(ControlMode.Paused); var cooldown=root.Simulation.Actions.PulseCooldown;
            yield return new WaitForSecondsRealtime(.15f); Assert.That(root.Simulation.Actions.PulseCooldown,Is.EqualTo(cooldown));
            var restored=FacilitySimulation.Restore(root.Persistence.Capture().simulation);
            Assert.That(restored.Actions.PulseCooldown,Is.EqualTo(cooldown));
            root.Simulation.Actions.Tick(3.1f); robot.Tick(0);
            Assert.That(robot.State.Mode,Is.Not.EqualTo(WatcherMode.Jammed)); Assert.That(root.Simulation.Actions.Slow(robot.Id),Is.GreaterThan(0));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DashSweepsIntoWallsAndServiceStepsHaveStandingClearance()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            Object.FindAnyObjectByType<Hostfall.Interaction.InteractionScanner>().enabled=false;
            var root=Object.FindAnyObjectByType<FacilityRoot>(); var controls=Object.FindAnyObjectByType<ControlState>();
            var player=Object.FindAnyObjectByType<PlayerMotor>();
            PosePlayer(new Vector3(0,.05f,-1)); controls.SetMode(ControlMode.Exploring);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position=new Vector3(0,1,-.15f); wall.transform.localScale=new Vector3(2,2,.15f); Physics.SyncTransforms();
            Assert.That(player.TryDash(Vector3.forward),Is.True);
            yield return new WaitForSeconds(.25f);
            Assert.That(player.transform.position.z,Is.LessThan(-.48f),"Dash cannot tunnel through the wall");
            Assert.That(player.TryDash(Vector3.forward),Is.False);
            var saved=root.Persistence.Capture(); Assert.That(saved.simulation.actions.dashCooldown,Is.GreaterThan(0));
            Object.Destroy(wall);
            var steps=GameObject.Find("Action Route / Service Steps"); Assert.That(steps,Is.Not.Null);
            Assert.That(steps.GetComponentsInChildren<BoxCollider>().Length,Is.EqualTo(3));
            foreach(var step in steps.GetComponentsInChildren<BoxCollider>())
            {
                var top=step.bounds.center; top.y=step.bounds.max.y;
                Assert.That(Physics.CheckCapsule(top+Vector3.up*.34f,top+Vector3.up*1.5f,.28f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore),Is.False,step.name);
            }
            controls.SetMode(ControlMode.Paused); LogAssert.NoUnexpectedReceived();
        }
    }
}
