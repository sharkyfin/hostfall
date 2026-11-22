using System.Collections;
using System.IO;
using System.Linq;
using Hostfall.Core;
using Hostfall.UI;
using Hostfall.Devices;
using Hostfall.CameraSystem;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace Hostfall.Tests
{
    public sealed class WorkstationTests
    {
        [UnitySetUp] public IEnumerator Load()
        {WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;}
        [UnityTest] public IEnumerator PhysicalScreenFocusIsSmoothAndTranscriptSurvivesLeaving()
        {
            var ui=Object.FindAnyObjectByType<FacilityUI>();var rig=Object.FindAnyObjectByType<PlayerCameraRig>();var motor=Object.FindAnyObjectByType<PlayerMotor>();
            var surface=Object.FindObjectsByType<WorkstationSurface>(FindObjectsSortMode.None).Single(s=>s.Device==DeviceId.Data01);
            var cc=motor.GetComponent<CharacterController>();cc.enabled=false;motor.transform.position=surface.Centre+surface.Normal*1.5f;cc.enabled=true;motor.enabled=false;
            var controls=Object.FindAnyObjectByType<ControlState>();controls.SetMode(ControlMode.Exploring);yield return null;
            var before=Camera.main.transform.position;var scenario=TerminalScenarioCatalog.GetScenario(DeviceId.Data01);int rewards=0;
            ui.OpenTerminalScenario(scenario,()=>rewards++);yield return null;
            Assert.That(rig.FocusReady,Is.False);Assert.That(Vector3.Distance(before,Camera.main.transform.position),Is.LessThan(.4f));
            yield return new WaitForSecondsRealtime(.7f);Assert.That(rig.FocusReady,Is.True);
            surface.FocusPose(Camera.main.aspect,out var expected,out _,out _);Assert.That(Vector3.Distance(Camera.main.transform.position,expected),Is.LessThan(.02f));
            var doc=ui.GetComponent<UIDocument>();var terminal=ui.TerminalView;
            var tl=Camera.main.WorldToViewportPoint(surface.TopLeft);var br=Camera.main.WorldToViewportPoint(surface.BottomRight);
            Assert.That(tl.x,Is.LessThan(br.x));Assert.That(tl.y,Is.GreaterThan(br.y));
            Assert.That(ui.PhysicalDisplay.Surface,Is.SameAs(surface));
            Assert.That(ui.PhysicalDisplay.Texture.width,Is.EqualTo(NearbyTerminalDisplay.TextureWidth));
            Assert.That(terminal.resolvedStyle.width,Is.EqualTo(NearbyTerminalDisplay.Width));
            Assert.That(terminal.ClassListContains("terminal-world"),Is.True);
            var world=Object.FindAnyObjectByType<FacilityRoot>().Simulation;var session=world.Screens.Get(scenario);
            Assert.That(session.Memory.Draft.Length+session.Memory.History.Count,Is.GreaterThan(0));
            controls.SetMode(ControlMode.Exploring);int typed=session.Typed;yield return new WaitForSecondsRealtime(.2f);Assert.That(session.Typed,Is.EqualTo(typed));
            ui.OpenTerminalScenario(scenario,()=>rewards++);for(int i=0;i<5000;i++)ui.TickCustomSession(.1f);
            Assert.That(rewards,Is.EqualTo(1));ui.OpenTerminalScenario(scenario,()=>rewards++);ui.TickCustomSession(.1f);Assert.That(rewards,Is.EqualTo(1));
            var field=Object.FindAnyObjectByType<Hostfall.Field.FieldSystemUI>();field.OpenArticle("terminal");yield return null;field.Close();yield return new WaitForSecondsRealtime(.6f);
            Assert.That(rig.FocusedSurface,Is.SameAs(surface));Assert.That(rig.FocusReady,Is.True,"Returning from archive must refocus the actual screen");
            Assert.That(surface.GetComponent<ComputerScreen>().DisplayedText,Is.EqualTo(TerminalDisplay.TailText(string.Join("\n",session.Memory.Lines.Select(l=>l.text)),62,15)));
            controls.SetMode(ControlMode.Exploring);yield return new WaitForSecondsRealtime(.55f);Assert.That(rig.FocusedSurface,Is.Null);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SharedWallsAreClosedAndSwitchesUseExistingLights()
        {
            Physics.SyncTransforms();
            foreach(float x in new[]{-4f,-2f,0f,2f,4f})
                Assert.That(Physics.Raycast(new Vector3(x,4.5f,17),Vector3.back,out _,3,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore),Is.True,"Upper wall gap at "+x);
            Assert.That(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.name.StartsWith("Armory West Wall")),Is.False,"No duplicate shared wall");
            var surfaces=Object.FindObjectsByType<WorkstationSurface>(FindObjectsSortMode.None);Assert.That(surfaces.Length,Is.EqualTo(12));Assert.That(surfaces.All(s=>s.Valid),Is.True);
            foreach(var surface in surfaces){Assert.That(Vector3.Dot(surface.Up,Vector3.up),Is.GreaterThan(.95f));Assert.That(Vector3.Distance(surface.TopLeft,surface.TopRight),Is.InRange(.4f,.9f));}
            foreach(var panelCollider in Object.FindObjectsByType<ServiceButtonPanel>(FindObjectsSortMode.None)){var b=panelCollider.GetComponent<BoxCollider>().bounds;Assert.That(b.size.magnitude,Is.LessThan(.35f),"No FBX-scaled invisible blocker");}
            Assert.That(Object.FindAnyObjectByType<DataCartridgeItem>().GetComponent<Collider>(),Is.Not.Null);
            var panel=Object.FindAnyObjectByType<ServiceButtonPanel>();string prompt=panel.Prompt;panel.Interact();Assert.That(panel.Prompt,Is.Not.EqualTo(prompt));panel.Interact();Assert.That(panel.Prompt,Is.EqualTo(prompt));
            yield return null;LogAssert.NoUnexpectedReceived();
        }
    }
}
