using System.Collections;
using System.Linq;
using Hostfall.Core;
using Hostfall.Interaction;
using Hostfall.Devices;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.UI;
using Hostfall.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Hostfall.Tests
{
    public sealed class ComputeVaultTests
    {
        [UnitySetUp] public IEnumerator LoadFreshScene()
        {
            WorldSaveController.FreshStartRequested=true;
            TerminalScenarioCatalog.ResetExecutedEvents();
            yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
        }
        [TearDown] public void ResetStatics()
        {
            DataCartridgeState.Reset();PhysicalMediaInventory.Reset();TerminalScenarioCatalog.ResetExecutedEvents();
            WorldSaveController.FreshStartRequested=false;
        }
        [UnityTest] public IEnumerator BothStairsAndUpperGalleriesAreWalkableInBothDirections()
        {
            var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.enabled=false;
            var controller=motor.GetComponent<CharacterController>();
            Assert.That(Physics.Raycast(new Vector3(4.6f,1.5f,20.8f),Vector3.up,out var underside,2,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore),Is.True);
            Assert.That(underside.collider.name,Is.EqualTo("East Stairs Walk Surface"),"Stair underside must block a jumping head");
            foreach(var route in new[]{(4.6f,16f,35f),(-4.6f,16.6f,30.8f)})
            {
                Place(controller,new Vector3(0,.08f,16));
                WalkTo(controller,new Vector3(route.Item1,0,route.Item2));
                WalkTo(controller,new Vector3(route.Item1,0,route.Item3));
                Assert.That(controller.transform.position.y,Is.EqualTo(3.2f).Within(.12f),"Reach the upper deck on foot");
                WalkTo(controller,new Vector3(route.Item1,0,route.Item2));
                Assert.That(controller.transform.position.y,Is.LessThan(.15f),"Return to ground on foot");
            }
            yield return null;
        }
        [UnityTest] public IEnumerator BridgeSwitchAndBothLandingsConnectTheUpperRoutes()
        {
            var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.enabled=false;
            var controller=motor.GetComponent<CharacterController>();
            Place(controller,new Vector3(0,.08f,16));
            WalkTo(controller,new Vector3(4.6f,0,16));
            WalkTo(controller,new Vector3(4.6f,0,26.4f));
            var bridge=Object.FindAnyObjectByType<RetractableBridgeController>();
            AssertVisible(motor.Eye,bridge.transform.position,bridge);
            Object.FindAnyObjectByType<ControlState>().SetMode(ControlMode.Exploring);
            bridge.Interact();
            yield return new WaitForSeconds(1.6f);
            Assert.That(bridge.IsExtended,Is.True);
            WalkTo(controller,new Vector3(4.6f,0,27));
            WalkTo(controller,new Vector3(-4.6f,0,27));
            Assert.That(controller.transform.position.y,Is.EqualTo(3.2f).Within(.15f));
            WalkTo(controller,new Vector3(-4.6f,0,16.6f));
            Assert.That(controller.transform.position.y,Is.LessThan(.15f));
        }
        [UnityTest] public IEnumerator CartridgeCanBeReachedCollectedInsertedAndUsedToUnlockBothExits()
        {
            var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.enabled=false;
            var controller=motor.GetComponent<CharacterController>();
            var ui=Object.FindAnyObjectByType<FacilityUI>();
            var root=Object.FindAnyObjectByType<FacilityRoot>();
            var console=Object.FindObjectsByType<DoorTerminalConsole>(FindObjectsSortMode.None).Single(c=>c.DeviceId==DeviceId.Data01);
            var doors=Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None)
                .Where(d=>d.DoorId=="door.sec03.energy"||d.DoorId=="door.sec02.armory").ToArray();
            Assert.That(doors.Length,Is.EqualTo(2));
            console.Interact();Finish(ui);
            Assert.That(console.Completed,Is.False);
            Assert.That(doors.All(d=>!d.IsUnlocked),Is.True,"Diagnosis must not grant access");
            var status=ui.TerminalView.Q<Label>("routine-status");
            Assert.That(status,Is.Not.Null);
            Assert.That(status.text,Does.Contain("Доступ закрыт"));
            Assert.That(TerminalScenarioCatalog.GetScenario(DeviceId.Data01).ScenarioId,Is.EqualTo("t07_data"));
            Place(controller,new Vector3(0,.08f,16));
            WalkTo(controller,new Vector3(4.6f,0,16));
            WalkTo(controller,new Vector3(4.6f,0,35f));
            var cartridge=Object.FindAnyObjectByType<DataCartridgeItem>();
            AssertVisible(motor.Eye,cartridge.transform.position,cartridge);
            cartridge.Interact();
            Assert.That(DataCartridgeState.HasCartridge,Is.True);
            Assert.That(cartridge.gameObject.activeSelf,Is.False);
            Assert.That(PhysicalMediaInventory.HasItem("item.cartridge.nl044"),Is.True);
            WalkTo(controller,new Vector3(4.6f,0,16f));
            WalkTo(controller,new Vector3(1.8f,0,16));
            WalkTo(controller,new Vector3(1.8f,0,24.4f));
            WalkTo(controller,new Vector3(0,0,24.4f));
            var slot=Object.FindAnyObjectByType<DataCartridgeSlot>();
            AssertVisible(motor.Eye,slot.transform.position,slot);
            slot.Interact();Assert.That(slot.IsInserted,Is.True);
            // Earlier terminal objectives are established without bypassing this physical puzzle.
            foreach(var id in new[]{DeviceId.Ws07,DeviceId.Edge01,DeviceId.Core01})root.Simulation.RecordTerminalObjective(id);
            console.Interact();Finish(ui);
            Assert.That(console.Completed,Is.True);
            Assert.That(doors.All(d=>d.IsUnlocked),Is.True);
            Assert.That(root.Simulation.Mission.CompletedObjectives,Is.EqualTo(4));
            // Replay is idempotent.
            console.Interact();Finish(ui);
            Assert.That(root.Simulation.Mission.CompletedObjectives,Is.EqualTo(4));
            yield return null;
        }
        private static void Finish(FacilityUI ui)
        { for(int i=0;i<1000;i++)ui.TickCustomSession(.1f); }
        private static void Place(CharacterController c,Vector3 p)
        {c.enabled=false;c.transform.position=p;c.enabled=true;Physics.SyncTransforms();}
        private static void WalkTo(CharacterController c,Vector3 target)
        {
            var p=c.transform.position;
            Assert.That(Physics.OverlapCapsule(p+Vector3.up*.4f,p+Vector3.up*1.4f,.25f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore),Is.Empty,"Route must begin outside static geometry");
            int steps=0;
            while(Vector2.Distance(new Vector2(c.transform.position.x,c.transform.position.z),new Vector2(target.x,target.z))>.08f && steps++<1500)
            {
                var move=Vector3.ProjectOnPlane(target-c.transform.position,Vector3.up).normalized*.068f;
                c.Move(move+Vector3.down*.04f);
            }
            Assert.That(steps,Is.LessThan(1500),"Walking blocked at "+c.transform.position+" towards "+target);
            for(int i=0;i<12;i++)c.Move(Vector3.down*.04f);
        }
        private static void AssertVisible(Vector3 eye,Vector3 target,IInteractable component)
        {
            Assert.That(Vector3.Distance(eye,target),Is.LessThan(2.8f));
            Assert.That(Physics.Raycast(eye,(target-eye).normalized,out var hit,2.8f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore),Is.True);
            Assert.That(hit.collider.GetComponentInParent<IInteractable>(),Is.SameAs(component),"Interaction blocked by "+hit.collider.name);
        }
    }
}
