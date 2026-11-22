using System.Collections;
using System.Linq;
using Hostfall.Core;
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
    public sealed class AwakeningTests
    {
        [UnityTest] public IEnumerator NewGameRevealsRealBodyAndLoadingNeverReplaysAwakening()
        {
            WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
            var root=Object.FindAnyObjectByType<FacilityRoot>();var ui=Object.FindAnyObjectByType<FacilityUI>();var player=Object.FindAnyObjectByType<PlayerMotor>();var controls=Object.FindAnyObjectByType<ControlState>();
            Assert.That(player.transform.position.z,Is.LessThan(0));var spawn=player.transform.position;
            ui.BeginSession();Assert.That(controls.Mode,Is.EqualTo(ControlMode.Cinematic));Assert.That(Time.timeScale,Is.Zero);Assert.That(ui.Opening.Playing,Is.True);
            yield return new WaitForSecondsRealtime(.15f);Assert.That(Object.FindAnyObjectByType<OperatorVisual>(FindObjectsInactive.Include).gameObject.activeInHierarchy,Is.True);
            var view=ui.GetComponent<UIDocument>().rootVisualElement;Assert.That(view.Q<Label>("awakening-subtitle").text,Does.Contain("Что со мной"));Assert.That(Resources.Load<AudioClip>("Story/Awakening"),Is.Not.Null);
            ui.Opening.Skip();Assert.That(controls.Mode,Is.EqualTo(ControlMode.Introduction));Assert.That(Vector3.Distance(spawn,player.transform.position),Is.LessThan(.001f));
            root.Field.AcknowledgeIntroduction();controls.SetMode(ControlMode.Exploring);var save=root.Persistence.Capture();save.player.position=new Vector3(-2,.04f,-2.5f);
            var oldDirectory=WorldSaveController.TestStorageDirectory;WorldSaveController.TestStorageDirectory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"hostfall-awakening-"+System.Guid.NewGuid().ToString("N"));
            try
            {
                WorldSaveController.PendingLoad=save;WorldSaveController.FreshStartRequested=false;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
                ui=Object.FindAnyObjectByType<FacilityUI>();ui.BeginSession();Assert.That(ui.Opening.Playing,Is.False);Assert.That(Object.FindAnyObjectByType<ControlState>().Mode,Is.EqualTo(ControlMode.Exploring));
                Assert.That(Vector3.Distance(Object.FindAnyObjectByType<PlayerMotor>().transform.position,save.player.position),Is.LessThan(.1f));
            }
            finally{foreach(var store in Object.FindObjectsByType<WorldSaveController>(FindObjectsSortMode.None))store.enabled=false;WorldSaveController.TestStorageDirectory=oldDirectory;WorldSaveController.PendingLoad=null;WorldSaveController.FreshStartRequested=true;}
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ApproachingAndFocusingKeepTheSamePhysicalTranscriptAndTexture()
        {
            WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
            var ui=Object.FindAnyObjectByType<FacilityUI>();var root=Object.FindAnyObjectByType<FacilityRoot>();var controls=Object.FindAnyObjectByType<ControlState>();var motor=Object.FindAnyObjectByType<PlayerMotor>();
            var surface=Object.FindObjectsByType<WorkstationSurface>(FindObjectsSortMode.None).Single(s=>s.Device==DeviceId.Data01);
            var state=motor.Capture(false);state.position=surface.Centre+surface.Normal*1.5f-Vector3.up*1.65f;state.yaw=Quaternion.LookRotation(-surface.Normal).eulerAngles.y;state.pitch=0;state.targetPitch=0;motor.Restore(state);motor.enabled=false;controls.SetMode(ControlMode.Exploring);
            var scenario=TerminalScenarioCatalog.GetScenario(DeviceId.Data01);var session=root.Simulation.Screens.Present(scenario);for(int i=0;i<5000;i++)session.Tick(.1f);
            yield return new WaitForSecondsRealtime(.15f);Assert.That(ui.PhysicalDisplay.Surface,Is.SameAs(surface));
            var texture=ui.PhysicalDisplay.Texture;var dom=ui.TerminalView;string before=string.Join("\n",dom.Q<ScrollView>("output").Query<Label>().ToList().Select(l=>l.text));
            Assert.That(before,Does.Contain("VAULT"));Assert.That(dom.Q<Label>("host").text,Is.EqualTo(scenario.GetOsTitle()));
            ui.OpenTerminalScenario(scenario);yield return new WaitForSecondsRealtime(.6f);
            Assert.That(ui.PhysicalDisplay.Texture,Is.SameAs(texture));Assert.That(ui.TerminalView,Is.SameAs(dom));Assert.That(string.Join("\n",dom.Q<ScrollView>("output").Query<Label>().ToList().Select(l=>l.text)),Is.EqualTo(before));
            var native=ui.GetComponent<UIDocument>().rootVisualElement.Q<Image>("terminal-glass-image");
            Assert.That(native.image,Is.SameAs(texture));Assert.That(native.style.display.value,Is.EqualTo(DisplayStyle.Flex));
            Assert.That(native.worldBound.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(native.worldBound.xMax,Is.LessThanOrEqualTo(ui.GetComponent<UIDocument>().rootVisualElement.worldBound.width));
            var screenPoint=Camera.main.WorldToScreenPoint(surface.Centre);var panelPoint=RuntimePanelUtils.ScreenToPanel(dom.panel,new Vector2(screenPoint.x,Screen.height-screenPoint.y));
            Assert.That(panelPoint.x,Is.EqualTo(NearbyTerminalDisplay.Width*.5f).Within(1));Assert.That(panelPoint.y,Is.EqualTo(NearbyTerminalDisplay.Height*.5f).Within(1));
            Assert.That(ui.PhysicalDisplay.ScreenToPanel(new Vector2(-1000,-1000)).x,Is.LessThan(0));
            controls.SetMode(ControlMode.Exploring);yield return new WaitForSecondsRealtime(.6f);Assert.That(ui.TerminalView,Is.SameAs(dom));Assert.That(Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None).Count(d=>d.panelSettings.targetTexture!=null),Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CorridorDoorStartsClosedOpensAfterWs07AndNotesAreWallMounted()
        {
            WorldSaveController.FreshStartRequested=true;yield return SceneManager.LoadSceneAsync("NorthlineFacility");yield return null;
            var root=Object.FindAnyObjectByType<FacilityRoot>();var controls=Object.FindAnyObjectByType<ControlState>();controls.SetMode(ControlMode.Exploring);
            var door=Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Single(d=>d.DoorId=="door.soc.corridor");Physics.SyncTransforms();Assert.That(door.IsUnlocked,Is.False);
            foreach(int side in new[]{-1,1})Assert.That(Physics.Raycast(new Vector3(0,1.2f,1+side),Vector3.back*side,out var hit,1.5f,CollisionLayers.WorldMask)&&hit.transform.IsChildOf(door.transform),Is.True,"Door blocks both directions");
            door.Interact();Assert.That(door.Capture().opened,Is.False);MenuFlowTests.FinishRoutine(root.Simulation,DeviceId.Ws07);Assert.That(door.IsUnlocked,Is.True);Assert.That(door.Capture().opened,Is.False);
            door.Interact();yield return new WaitForSeconds(2);Assert.That(door.Capture().offset,Is.GreaterThan(2.4f));var entry=door.CaptureEntry();Assert.That(entry.id,Is.EqualTo("door.soc.corridor"));
            foreach(int side in new[]{-1,1})Assert.That(Physics.Raycast(new Vector3(0,1.2f,1+side),Vector3.back*side,1.5f,CollisionLayers.WorldMask),Is.False,"Opened leaf clears doorway");
            var notes=Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Where(t=>t.name=="Evacuation message").ToArray();Assert.That(notes.Length,Is.EqualTo(4));Assert.That(notes.Any(t=>t.text.Contains("СПАСИ МИР")),Is.True);Assert.That(notes.Any(t=>t.text.Contains("ЗАХВАТИЛ")),Is.True);
            foreach(var note in notes){Assert.That(note.GetComponent<Collider>(),Is.Null);Assert.That(Mathf.Abs(note.transform.forward.y),Is.LessThan(.01f));}
            LogAssert.NoUnexpectedReceived();
        }
    }
}
