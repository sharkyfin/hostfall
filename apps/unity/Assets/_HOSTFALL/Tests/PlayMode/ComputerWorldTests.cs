using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Field;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using Hostfall.UI;
using Hostfall.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace Hostfall.Tests
{
    public sealed partial class ComputerWorldTests
    {
        private string storage;
        [UnitySetUp] public IEnumerator StartFresh()
        {
            storage=Path.Combine(Path.GetTempPath(),"hostfall2-play-"+Guid.NewGuid().ToString("N"));
            WorldSaveController.TestStorageDirectory=storage;WorldSaveController.FreshStartRequested=true;
            yield return SceneManager.LoadSceneAsync("Hostfall2Facility");yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Object.FindAnyObjectByType<ControlState>()?.SetMode(ControlMode.MainMenu);
            WorldSaveController.TestStorageDirectory=null;
            yield return null;
            if(Directory.Exists(storage))Directory.Delete(storage,true);
        }
        private static FacilityRoot Root => Object.FindAnyObjectByType<FacilityRoot>();
        private static ControlState Controls => Object.FindAnyObjectByType<ControlState>();
        private static ComputerRuntimeUI Desktop => Object.FindAnyObjectByType<ComputerRuntimeUI>();
        private static ComputerStation Station(string id) => Object.FindObjectsByType<ComputerStation>(FindObjectsSortMode.None).Single(s=>s.StationId==id);
        private static IEnumerator EnterGame()
        {
            var ui=Object.FindAnyObjectByType<FacilityUI>();ui.BeginSession();ui.Opening.Skip();
            Root.Field.AcknowledgeIntroduction();Controls.SetMode(ControlMode.Exploring);yield return null;
        }
        private static IEnumerator Click(Button button, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Assert.That(button, Is.Not.Null, $"Missing control in {caller}, station {Desktop.ActiveStation?.StationId}, stage {Root.Simulation.ComputerCampaign.CurrentStage}");
            button.Focus();using(var submit=NavigationSubmitEvent.GetPooled())button.SendEvent(submit);yield return null;
        }
        [UnityTest] public IEnumerator HomeScreenKnowledgeAndWorldClockArePhysicalAndSynchronized()
        {
            Assert.That(Root.Simulation.InteractiveComputers,Is.True);
            Assert.That(Root.Simulation.AutomatedRoute,Is.False);
            Assert.That(Object.FindAnyObjectByType<PlayerMotor>().transform.position.z,Is.LessThan(-9));
            Assert.That(Object.FindObjectsByType<ComputerStation>(FindObjectsSortMode.None).Length,Is.EqualTo(13));
            Assert.That(Object.FindObjectsByType<ComputerTerminal>(FindObjectsSortMode.None),Is.Empty);
            Assert.That(Object.FindObjectsByType<DoorTerminalConsole>(FindObjectsSortMode.None),Is.Empty);
            yield return EnterGame();
            Object.FindAnyObjectByType<FieldSystemUI>().OpenArchive();Assert.That(Controls.Mode,Is.EqualTo(ControlMode.Exploring));
            Desktop.Open(Station(ComputerCampaign.HomeStation));yield return new WaitForSecondsRealtime(.65f);
            Assert.That(Controls.Mode,Is.EqualTo(ControlMode.Computer));Assert.That(Time.timeScale,Is.EqualTo(1));
            Assert.That(Desktop.PhysicalDisplay.Interactive,Is.True);
            var physical=Desktop.PhysicalDisplay;Assert.That(physical.Surface,Is.EqualTo(Station(ComputerCampaign.HomeStation).Surface));
            var view=physical.View;
            var command = view.Q<TextField>("home-command");
            var focused = view.focusController.focusedElement as VisualElement;
            Assert.That(focused == command || focused != null && command.Contains(focused), Is.True,
                "The physical keyboard focus must start on the command input before any pointer click");
            Assert.That(view.Q<Button>("home-knowledge"), Is.Null);
            Assert.That(view.Q<Button>("home-messages"), Is.Null);
            var outside = Desktop.HomeControls;
            yield return Click(outside.Q<Button>("home-knowledge"));
            Assert.That(outside.Q<Button>("guide-start"),Is.Not.Null);
            outside.Q<TextField>("home-guide-search").value="grep";yield return null;
            Assert.That(outside.Q<Button>("guide-start"),Is.Not.Null);
            Assert.That(outside.Q<Button>("guide-network"),Is.Null, "A dormant station should not expose story notes");
            outside.Q<TextField>("home-guide-search").value = "";
            yield return Click(outside.Q<Button>("archive-evacuation"));
            Assert.That(Root.Simulation.ComputerCampaign.HasRead(ComputerCampaign.EvacuationPath), Is.True);
            Assert.That(outside.Query<Label>().ToList().Any(label => label.text.Contains("Пункты сбора пусты")), Is.True);
            yield return Click(outside.Q<Button>("home-knowledge"));
            yield return new WaitForSecondsRealtime(.08f);
            Assert.That(Root.Simulation.ComputerCampaign.HomeTab, Is.EqualTo("terminal"));
            command = view.Q<TextField>("home-command");
            focused = view.focusController.focusedElement as VisualElement;
            Assert.That(focused == command || focused != null && command.Contains(focused), Is.True,
                "Closing Knowledge must restore shell focus");
            // The runtime dispatcher owns keyboard focus across the HUD and the render-texture panel.
            // InputSystem text packets do not feed the engine's InputForUI provider in the test Editor.
            var runtime = typeof(PanelSettings).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility", true);
            const System.Reflection.BindingFlags instanceFlags = System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var eventSystem = runtime.GetProperty("defaultEventSystem", System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).GetValue(null);
            var dispatcher = eventSystem.GetType();
            var dispatch = dispatcher.GetMethod("SendFocusBasedEvent", instanceFlags).MakeGenericMethod(typeof(char));
            Func<char, EventBase> keyEvent = c => KeyDownEvent.GetPooled(c, KeyCode.None, EventModifiers.None);
            using ((IDisposable)dispatcher.GetMethod("FocusBasedEventSequence", instanceFlags).Invoke(eventSystem, null))
                foreach (char character in "cat ~/status.txt") dispatch.Invoke(eventSystem, new object[] { keyEvent, character });
            Assert.That(command.value, Is.EqualTo("cat ~/status.txt"), "Typing needs no preceding pointer click");
            using(var key=KeyDownEvent.GetPooled('\n',KeyCode.Return,EventModifiers.None))command.SendEvent(key);
            yield return null;
            Assert.That(Root.Simulation.ComputerCampaign.Home.Terminal.History.Last(),Is.EqualTo("cat ~/status.txt"));
            Assert.That(view.Q<Label>(className:"hf2-transcript").text,Does.Contain("контур восстановления"));
            CapturePanel("home-live",physical.Texture);
            Controls.SetMode(ControlMode.Paused);Assert.That(Controls.WorldRuns,Is.False);
            Object.FindAnyObjectByType<FacilityUI>().BeginSession();yield return new WaitForSecondsRealtime(.6f);
            Assert.That(Controls.Mode,Is.EqualTo(ControlMode.Computer));
            Assert.That(Desktop.PhysicalDisplay,Is.SameAs(physical));
            Desktop.Close();yield return null;
            Assert.That(Controls.CanExplore,Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator OpeningKeyMustBeReleasedBeforeTheHomeShellTakesFocus()
        {
            yield return EnterGame();
            var physicalKeyboards = InputSystem.devices.OfType<Keyboard>().Where(device => device.enabled).ToArray();
            foreach (var device in physicalKeyboards) InputSystem.DisableDevice(device);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var behavior = InputSystem.settings.backgroundBehavior;
            var editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
                yield return null;
                Assert.That(Keyboard.current, Is.SameAs(keyboard));
                Assert.That(keyboard.eKey.isPressed, Is.True);
                Desktop.Open(Station(ComputerCampaign.HomeStation));
                yield return ObserveContainment(() => Desktop.PhysicalDisplay.Interactive, 4,
                    () => "The camera did not settle at the home terminal");
                var view = Desktop.PhysicalDisplay.View;
                var command = view.Q<TextField>("home-command");
                bool ShellFocused() => view.focusController.focusedElement is VisualElement element &&
                    (element == command || command.Contains(element));

                Assert.That(ShellFocused(), Is.False, "A held interaction key must never enter the shell");
                Assert.That(command.value, Is.Empty);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return ObserveContainment(ShellFocused, 2, () => "Release did not restore automatic shell focus");
                Assert.That(command.value, Is.Empty);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                foreach (var device in physicalKeyboards) InputSystem.EnableDevice(device);
                InputSystem.settings.backgroundBehavior = behavior;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorBehavior;
            }
        }

        [UnityTest]
        public IEnumerator VentflowArchiveOpensPhotographAndRestoresTheHiddenServiceFile()
        {
            yield return EnterGame();
            var motor = Object.FindAnyObjectByType<PlayerMotor>();
            motor.Restore(new PlayerStateData { position = new Vector3(-2.65f, .08f, -2.6f), yaw = 180 });
            Desktop.Open(Station(ComputerCampaign.SnakeStation));
            yield return new WaitForSecondsRealtime(.65f);
            var view = Desktop.PhysicalDisplay.View;
            yield return Click(view.Q<Button>("desktop-files"));
            var files = view.Q("app-files");
            Assert.That(files.Q<Button>("ventflow-file-handover-letter"), Is.Not.Null);
            Assert.That(files.Q<Button>("ventflow-file-unsent-letter"), Is.Not.Null);
            Assert.That(files.Q("ventflow-hidden-files").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            yield return Click(files.Q<Button>("ventflow-file-" + VentflowArchive.PhotographId));
            Assert.That(view.Q<Image>("ventflow-archive-photo").image, Is.Not.Null);
            CapturePanel("ventflow-handover-desktop", Desktop.PhysicalDisplay.Texture);
            yield return Click(view.Q<Button>("close-document"));
            yield return Click(files.Q<Button>("ventflow-show-hidden"));
            Assert.That(files.Q("ventflow-hidden-files").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            yield return Click(files.Q<Button>("ventflow-file-relay-service"));
            Assert.That(view.Q("app-document").Query<Label>().ToList().Any(label => label.text.Contains("Состояние рабочего места: пусто")), Is.True);
            Assert.That(Root.Persistence.Save("manual1"), Is.True);
            var library = new SaveLibrary(storage, null, true);
            Assert.That(library.Read("manual1", out var saved, out var error), Is.True, error);
            var restored = ComputerCampaign.Restore(saved.simulation.computerCampaign);
            Assert.That(restored.Desktop(ComputerCampaign.SnakeStation).document, Is.EqualTo("relay-service"));
            Assert.That(restored.Desktop(ComputerCampaign.SnakeStation).focusedApp, Is.EqualTo("document"));
            Assert.That(restored.Snake.Stage, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator VentBreaksOutWhileSnakeCanBeExitedAndPartialSaveLoads()
        {
            yield return EnterGame();
            var motor=Object.FindAnyObjectByType<PlayerMotor>();
            motor.Restore(new PlayerStateData{position=new Vector3(-2.65f,.08f,-2.6f),yaw=180});
            Desktop.Open(Station(ComputerCampaign.SnakeStation));yield return new WaitForSecondsRealtime(.65f);
            var view=Desktop.PhysicalDisplay.View;
            Assert.That(view.Q<Button>("desktop-snake"),Is.Not.Null,"Desktop was replaced: "+Desktop.ActiveStation?.StationId);
            yield return Click(view.Q<Button>("desktop-snake"));
            Assert.That(view.Q<Button>("snake-start"),Is.Not.Null,"App did not open: "+string.Join(",",view.Query<Button>().ToList().Select(b=>b.name)));
            yield return Click(view.Q<Button>("snake-start"));
            var campaign=Root.Simulation.ComputerCampaign;
            campaign.Snake.Turn(SnakeDirection.Up);campaign.Snake.Advance(.035);
            Assert.That(Root.Persistence.Save("manual1"),Is.True);
            var before=campaign.Capture();
            Assert.That(new SaveLibrary(storage,null,true).Read("manual1",out var saved,out string error),Is.True,error);
            var read=saved.simulation.computerCampaign;
            Assert.That(read.snake.elapsed,Is.EqualTo(before.snake.elapsed).Within(1e-12));
            Assert.That(read.threat.elapsed,Is.EqualTo(before.threat.elapsed).Within(1e-12));
            Assert.That(read.networkSecurity.relayClock,Is.EqualTo(before.networkSecurity.relayClock).Within(1e-12));
            read.snake.elapsed=before.snake.elapsed;read.threat.elapsed=before.threat.elapsed;read.networkSecurity.relayClock=before.networkSecurity.relayClock;
            Assert.That(JsonUtility.ToJson(read),Is.EqualTo(JsonUtility.ToJson(before)),"Non-clock state remains exact; JSON double clocks allow 1ps");
            CapturePanel("ventflow-live",Desktop.PhysicalDisplay.Texture);
            Desktop.Close();Assert.That(Controls.CanExplore,Is.True);
            // World time, not app time, releases the robot. No scripted reward or hidden kill.
            for(int i=0;i<28;i++)campaign.AdvanceWorld(.25);
            yield return null;
            var vent=Object.FindAnyObjectByType<VentReleaseController>();
            Assert.That(campaign.ThreatPhase,Is.EqualTo(VentThreatPhase.Released));
            Assert.That(vent.Actor.enabled,Is.True);
            Assert.That(Vector3.Distance(vent.Actor.transform.position,vent.ExitPosition),Is.LessThan(.8f));
            var obstacles=Physics.OverlapCapsule(vent.ExitPosition+Vector3.up*.35f,vent.ExitPosition+Vector3.up, .28f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore)
                .Where(c=>!c.transform.IsChildOf(vent.Actor.transform)).ToArray();
            Assert.That(obstacles,Is.Empty,"Vent exit contains external obstacles: "+string.Join(",",obstacles.Select(c=>c.name)));
            Root.Persistence.Load("manual1");yield return null;yield return null;
            var loaded = Root.Simulation.ComputerCampaign.Snake.Capture();
            Assert.That(loaded.elapsed, Is.EqualTo(before.snake.elapsed).Within(1e-12), "Scene reload parses the partial clock again");
            loaded.elapsed = before.snake.elapsed;
            Assert.That(JsonUtility.ToJson(loaded), Is.EqualTo(JsonUtility.ToJson(before.snake)));
            Assert.That(Root.Simulation.ComputerCampaign.ThreatPhase,Is.EqualTo(saved.simulation.computerCampaign.threat.phase));
            Assert.That(Desktop.ActiveStation,Is.Null,"Load restores app state without capturing player input");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator FirstTaskUnlocksRealDoorsAndPublishesHomeFileWithoutAutoplay()
        {
            yield return EnterGame();var campaign=Root.Simulation.ComputerCampaign;
            var corridor=Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Single(d=>d.DoorId=="door.soc.corridor");
            var server=Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Single(d=>d.DoorId=="door.corridor.server");
            Assert.That(corridor.IsUnlocked||server.IsUnlocked,Is.False);
            campaign.BeginSnake();
            for(int i=0;i<2500&&campaign.Snake.Phase!=SnakePhase.Complete;i++)Steer(campaign.Snake);
            Assert.That(campaign.FirstTaskComplete,Is.True);Assert.That(corridor.IsUnlocked&&server.IsUnlocked,Is.True);
            Assert.That(campaign.VentSealed,Is.True);Assert.That(campaign.ThreatPhase,Is.EqualTo(VentThreatPhase.Sealed));
            corridor.Interact();yield return new WaitForSeconds(.5f);
            Assert.That(corridor.Capture().offset,Is.GreaterThan(.3f));
            Assert.That(Root.Simulation.Terminals.Capture().All(r=>!r.completed),Is.True);
            Assert.That(campaign.Home.TryRead(ComputerCampaign.IncidentPath,out _),Is.True);
            campaign.Shell.Execute("cat "+ComputerCampaign.IncidentPath);
            Assert.That(campaign.HasRead(ComputerCampaign.IncidentPath),Is.True);
            Assert.That(Root.Persistence.Save(),Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator VentClawHitsOnceAtItsVisibleReach()
        {
            yield return EnterGame();
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            var health = player.GetComponent<PlayerHealth>();
            var vent = Object.FindAnyObjectByType<VentReleaseController>();
            player.Restore(new PlayerStateData { position = vent.ExitPosition - Vector3.right * .85f + Vector3.up * .04f, yaw = 90 });
            player.enabled = false;
            player.ScareIntensity = 0;
            var campaign = Root.Simulation.ComputerCampaign;
            campaign.BeginSnake();
            for (int i = 0; i < 20; i++) campaign.AdvanceWorld(.25);
            yield return ObserveContainment(() => campaign.VentGrabResolved, 2, () => "The claw did not finish its reach");
            Assert.That(health.CurrentHealth, Is.EqualTo(84));
            Assert.That(player.Startle, Is.Zero);
            yield return ObserveContainment(() => vent.Actor.enabled, 2, () => "The robot stayed in the duct");
            Assert.That(health.CurrentHealth, Is.EqualTo(84), "The claw only closes once");
            Assert.That(ComputerCampaign.Restore(campaign.Capture()).VentGrabResolved, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator FacingMeleeCounterDeflectsVentGrab()
        {
            yield return EnterGame();
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            var vent = Object.FindAnyObjectByType<VentReleaseController>();
            var health = player.GetComponent<PlayerHealth>();
            var weapon = player.GetComponent<PlayerWeapon>();
            player.Restore(new PlayerStateData { position = vent.ExitPosition - Vector3.right * .65f + Vector3.up * .04f, yaw = 90 });
            player.enabled = false;
            var campaign = Root.Simulation.ComputerCampaign;
            campaign.BeginSnake();
            for (int i = 0; i < 22; i++) campaign.AdvanceWorld(.25);
            Assert.That(weapon.TryMelee(), Is.True);
            Assert.That(weapon.TryMelee(), Is.False, "Counters have a cooldown");
            campaign.AdvanceWorld(.25);
            campaign.AdvanceWorld(.10);
            yield return ObserveContainment(() => campaign.VentGrabResolved, 1, () => "The claw did not finish its reach");
            Assert.That(health.CurrentHealth, Is.EqualTo(100));
            Assert.That(weapon.TryCounter(vent.transform.position), Is.False, "The counter window was consumed");
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CharacterControllerMakesTheHomeOfficeRoundTripThroughActualDoors()
        {
            yield return EnterGame();
            var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.enabled=false;
            var cc=motor.GetComponent<CharacterController>();
            Walk(cc,new Vector3(0,0,-14));Walk(cc,new Vector3(0,0,-10));
            var door=Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Single(d=>d.DoorId=="door.shelter.blast");
            door.Interact();yield return new WaitForSeconds(2.2f);
            Walk(cc,new Vector3(0,0,-5));Walk(cc,new Vector3(-2.65f,0,-2.3f));
            Assert.That(Vector3.Distance(motor.Eye,Station(ComputerCampaign.SnakeStation).Surface.Centre),Is.LessThan(2.8f));
            var campaign=Root.Simulation.ComputerCampaign;campaign.BeginSnake();
            for(int i=0;i<2500&&campaign.Snake.Phase!=SnakePhase.Complete;i++)Steer(campaign.Snake);
            Assert.That(campaign.FirstTaskComplete,Is.True);
            Walk(cc,new Vector3(0,0,-4));Walk(cc,new Vector3(0,0,-10));Walk(cc,new Vector3(-2.6f,0,-14.3f));
            Assert.That(cc.transform.position.y,Is.GreaterThan(-.1f));
            Desktop.Open(Station(ComputerCampaign.HomeStation));yield return new WaitForSecondsRealtime(.6f);
            campaign.Shell.Execute("cat "+ComputerCampaign.IncidentPath);
            Assert.That(campaign.HasRead(ComputerCampaign.IncidentPath),Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NewVentDoesNotOccupyTheSecurityShortcutOrIntersectProps()
        {
            yield return EnterGame();var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.enabled=false;
            var door=Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Single(d=>d.DoorId=="door.sec07.soc");
            door.UnlockForComputer();door.Interact();yield return new WaitForSeconds(2.2f);
            var cc=motor.GetComponent<CharacterController>();cc.enabled=false;cc.transform.position=new Vector3(2.6f,.08f,-.95f);cc.enabled=true;Physics.SyncTransforms();
            Walk(cc,new Vector3(6.2f,0,-.95f));Walk(cc,new Vector3(2.6f,0,-.95f));
            var vent=Object.FindAnyObjectByType<VentReleaseController>();
            var obstacles=Physics.OverlapBox(vent.transform.position+Vector3.up*.85f,new Vector3(.7f,.8f,.88f),Quaternion.identity,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore)
                .Where(c=>!c.transform.IsChildOf(vent.transform)&&!c.transform.IsChildOf(vent.Actor.transform)).ToArray();
            Assert.That(obstacles,Is.Empty,string.Join(",",obstacles.Select(c=>c.name)));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PhysicalPanelReceivesMouseClicksAtProjectedGlassCoordinates()
        {
            var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;var oldBackground=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            // Unity's own pinned-version test hook: batch Editor has no focused Game View.
            var runtime=typeof(PanelSettings).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility",true);
            var updateMode=runtime.GetProperty("eventSystemUpdateMode",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            var previousMode=updateMode.GetValue(null);updateMode.SetValue(null,Enum.Parse(updateMode.PropertyType,"Always"));
            var physicalMice=InputSystem.devices.OfType<Mouse>().Where(m=>m.enabled).ToArray();
            foreach(var physical in physicalMice)InputSystem.DisableDevice(physical);
            var mouse=InputSystem.AddDevice<Mouse>();InputSystem.EnableDevice(mouse);
            try
            {
                yield return EnterGame();Desktop.Open(Station(ComputerCampaign.HomeStation));yield return new WaitForSecondsRealtime(.8f);
                yield return PointerClick(mouse,Desktop.HomeControls.Q<Button>("home-knowledge"));
                Assert.That(Root.Simulation.ComputerCampaign.HomeTab,Is.EqualTo("knowledge"),"Mouse must reach the button outside the terminal glass");
                Desktop.Close();Object.FindAnyObjectByType<PlayerMotor>().Restore(new PlayerStateData{position=new Vector3(-2.65f,.08f,-2.3f),yaw=180});
                Desktop.Open(Station(ComputerCampaign.SnakeStation));yield return new WaitForSecondsRealtime(.8f);
                yield return PointerClick(mouse,Desktop.PhysicalDisplay.View.Q<Button>("desktop-snake"));
                var start=Desktop.PhysicalDisplay.View.Q<Button>("snake-start");Assert.That(start,Is.Not.Null,"Mouse must open desktop app");
                yield return PointerClick(mouse,start);
                Assert.That(Root.Simulation.ComputerCampaign.Snake.Phase,Is.EqualTo(SnakePhase.Running));
                Assert.That(Object.FindAnyObjectByType<FacilityInput>().Fire.enabled,Is.False,"UI click cannot fire weapon");
            }
            finally
            {
                updateMode.SetValue(null,previousMode);InputSystem.RemoveDevice(mouse);
                foreach(var physical in physicalMice)if(physical.added)InputSystem.EnableDevice(physical);InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;InputSystem.settings.backgroundBehavior=oldBackground;
            }
        }
        private static IEnumerator PointerClick(Mouse mouse,VisualElement target)
        {
            Assert.That(target,Is.Not.Null);
            var point = target.worldBound.center;
            Vector2 position;
            if (target.panel == Desktop.HomeControls.panel)
            {
                var bounds = target.panel.visualTree.worldBound;
                position = new Vector2((point.x - bounds.xMin) * Screen.width / bounds.width,
                    Screen.height - (point.y - bounds.yMin) * Screen.height / bounds.height);
            }
            else
            {
                var glass = Desktop.PhysicalDisplay.Surface;
                var world = glass.TopLeft + (glass.TopRight - glass.TopLeft) * (point.x / NearbyTerminalDisplay.Width) +
                    (glass.BottomLeft - glass.TopLeft) * (point.y / NearbyTerminalDisplay.Height) + glass.Normal * .012f;
                var projected = UnityEngine.Camera.main.WorldToScreenPoint(world);
                position = new Vector2(projected.x, projected.y);
            }
            var topLeft=new Vector2(position.x,Screen.height-position.y);
            var mapped=RuntimePanelUtils.ScreenToPanel(target.panel,topLeft);
            Assert.That(Vector2.Distance(mapped,point),Is.LessThan(2),"Mapping "+mapped+" expected "+point+" screen "+position);
            var picked = target.panel.Pick(mapped);
            Assert.That(picked == target || picked != null && target.Contains(picked), Is.True,
                "A different element covers the control: " + picked?.name);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=position});yield return new WaitForSecondsRealtime(.055f);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=position,buttons=1});yield return new WaitForSecondsRealtime(.055f);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=position});yield return new WaitForSecondsRealtime(.08f);
        }
        [UnityTest] public IEnumerator CargoUtilityStartsThePhysicalMechanismsAndQuarantine()
        {
            yield return EnterGame();
            foreach(var enemy in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None))enemy.enabled=false;
            foreach(var combat in Object.FindObjectsByType<RobotCombatController>(FindObjectsSortMode.None))combat.enabled=false;
            var campaign=Root.Simulation.ComputerCampaign;
            CompleteFirstComputer(campaign);
            var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.Restore(new PlayerStateData{position=new Vector3(8.5f,1.08f,73.5f),yaw=180});
            Desktop.Open(Station(ComputerCampaign.PayloadStation));yield return new WaitForSecondsRealtime(.8f);
            yield return RunFieldUtility(FieldUtilityId.CargoTransfer);
            Assert.That(campaign.CargoReleased, Is.False);
            CalibrateLogisticsEntry(campaign);
            yield return RunFieldUtility(FieldUtilityId.CargoTransfer);
            Assert.That(campaign.CargoReleased, Is.True);
            yield return ClearLogisticsDefence(campaign);
            yield return new WaitForSecondsRealtime(3.2f);
            var mechanism=Object.FindAnyObjectByType<LogisticsMechanisms>();
            Assert.That(mechanism,Is.Not.Null);Assert.That(Quaternion.Angle(mechanism.Bridge.rotation,Quaternion.identity),Is.LessThan(.1f));
            Assert.That(Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Single(d=>d.DoorId=="door.sec06.robo").IsUnlocked,Is.True);
            Assert.That(campaign.Logistics.AlarmEnabled,Is.True);
            var map=Object.FindAnyObjectByType<FacilityUI>().MapWorld;map.Refresh();
            Assert.That(map.Markers.Count(m=>m.Source is ComputerStation),Is.EqualTo(13),"Modern PCs need actual radar/map markers");
            Assert.That(map.Shapes.Single(s=>s.Collider.transform==mechanism.Lift).Moving,Is.True);
            var gallery=map.Rooms.Single(r=>r.Id=="logistics.gallery");
            Assert.That(gallery.Floors.Count,Is.EqualTo(4));Assert.That(gallery.Floors.All(f=>!f.Moving),Is.True,"Static slabs must not be scheduled as movers");
            Assert.That(gallery.Contains(new Vector3(12,1,68.5f)),Is.False,"Map must preserve the actual lift opening");
            Assert.That(map.Rooms.Any(r=>r.Id=="logistics.bridge"),Is.True);
            Assert.That(Root.Persistence.Save("manual1"),Is.True);
            var library=new SaveLibrary(WorldSaveController.TestStorageDirectory,null,true);
            Assert.That(library.Read("manual1",out var saved,out var error),Is.True,error);
            Assert.That(ComputerCampaign.Restore(saved.simulation.computerCampaign).CargoReleased, Is.True);
            Desktop.Close();
            motor.Restore(new PlayerStateData{position=new Vector3(8.5f,1.05f,77.7f),yaw=0});
            Walk(motor.GetComponent<CharacterController>(),new Vector3(8.5f,1.05f,84));
            Assert.That(motor.transform.position.y,Is.GreaterThan(.85f),"Deployed bridge must support the real controller");
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ActivatedFreightLiftCarriesCharacterAndSaveCapturesMatchingPose()
        {
            yield return EnterGame();
            foreach(var enemy in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None))enemy.enabled=false;
            foreach(var combat in Object.FindObjectsByType<RobotCombatController>(FindObjectsSortMode.None))combat.enabled=false;
            var campaign=Root.Simulation.ComputerCampaign;
            CalibrateLogisticsEntry(campaign);
            campaign.BeginPayload();for(int i=0;i<8000 && campaign.Payload.Stage==0;i++)DrivePayload(campaign.Payload);
            Assert.That(campaign.Payload.Stage,Is.EqualTo(1));
            var mechanism=Object.FindAnyObjectByType<LogisticsMechanisms>();
            for(int i=0;i<60 && campaign.Logistics.Capture().liftClock>=1;i++)campaign.AdvanceWorld(.25);
            mechanism.RefreshState();Physics.SyncTransforms();
            var motor=Object.FindAnyObjectByType<PlayerMotor>();var position=mechanism.Lift.position;position.y=mechanism.Lift.GetComponent<Collider>().bounds.max.y+.03f;
            motor.Restore(new PlayerStateData{position=position,yaw=180});
            float initial=motor.transform.position.y, highest=initial;
            for(int i=0;i<95;i++)
            {
                campaign.AdvanceWorld(.1);yield return null;
                highest=Mathf.Max(highest,motor.transform.position.y);
                Assert.That(Mathf.Abs(motor.transform.position.y-mechanism.Lift.GetComponent<Collider>().bounds.max.y),Is.LessThan(.2f),"Passenger must stay on the lift through its entire cycle");
            }
            Assert.That(highest-initial,Is.GreaterThan(.7f),"Lift must carry passenger instead of passing through them");
            var snapshot=Root.Persistence.Capture();
            Assert.That(Mathf.Abs(snapshot.player.position.y-mechanism.Lift.GetComponent<Collider>().bounds.max.y),Is.LessThan(.15f));
            Assert.That(snapshot.simulation.computerCampaign.logistics.liftClock,Is.EqualTo(campaign.Logistics.Capture().liftClock));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator LegacyPayloadClockStaysFrozenAndWorkstationDisablesWorldInput()
        {
            yield return EnterGame();
            var campaign=Root.Simulation.ComputerCampaign;
            CalibrateLogisticsEntry(campaign);
            var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.Restore(new PlayerStateData{position=new Vector3(8.5f,1.08f,73.5f),yaw=180});
            Desktop.Open(Station(ComputerCampaign.PayloadStation));yield return new WaitForSecondsRealtime(.8f);
            Assert.That(campaign.BeginPayload(), Is.True);
            Assert.That(Desktop.PhysicalDisplay.View.Q<Button>("desktop-payload"), Is.Null);
            var oldBackground=InputSystem.settings.backgroundBehavior;var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var physical=InputSystem.devices.OfType<Keyboard>().Where(k=>k.enabled).ToArray();foreach(var key in physical)InputSystem.DisableDevice(key);
            var keyboard=InputSystem.AddDevice<Keyboard>();var before=motor.transform.position;
            try
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(motor.transform.position, Is.EqualTo(before));
                Assert.That(Object.FindAnyObjectByType<FacilityInput>().Fire.enabled, Is.False);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                var frozen=campaign.Payload.Capture();yield return new WaitForSecondsRealtime(.25f);
                Assert.That(campaign.Payload.Capture().ticks,Is.EqualTo(frozen.ticks));
                Assert.That(campaign.Payload.X,Is.EqualTo(frozen.x));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);foreach(var key in physical)if(key.added)InputSystem.EnableDevice(key);
                InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
            }
            LogAssert.NoUnexpectedReceived();
        }
        private static void DrivePayload(PayloadSession game)
        {
            int input=1;var laser=game.Objects.FirstOrDefault(o=>o.Kind==PayloadObjectKind.Laser&&o.X>game.X&&o.X-game.X<1.3);
            if(laser.Width>0&&laser.Active&&game.Y<2.8)input=0;
            if((game.X>6.9&&game.X<7.6||game.X>17&&game.X<17.7||
                game.Objects.Any(o=>o.Kind==PayloadObjectKind.Sentinel&&o.X>game.X&&o.X-game.X<1.3))&&Math.Abs(game.VelocityY)<.01)game.QueueJump();
            game.Advance(1.0/60,input);
        }
        private static void Walk(CharacterController cc,Vector3 target)
        {
            for(int i=0;i<1800;i++)
            {
                var delta=target-cc.transform.position;delta.y=0;
                if(delta.magnitude<.07f){for(int n=0;n<10;n++)cc.Move(Vector3.down*.03f);return;}
                cc.Move(Vector3.ClampMagnitude(delta,.04f)+Vector3.down*.03f);
            }
            var nearby=Physics.OverlapSphere(cc.transform.position+Vector3.up,.6f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore);
            Assert.Fail("Blocked from home/office at "+cc.transform.position+" toward "+target+": "+string.Join(",",nearby.Select(c=>c.name)));
        }
        private static void Steer(SnakeSession snake)
        {
            if(snake.Phase!=SnakePhase.Running)snake.Start();
            int head=snake.Body[0],target=snake.Food;
            var q=new Queue<int>();var parents=new Dictionary<int,int>{{head,-1}};q.Enqueue(head);
            while(q.Count>0&&!parents.ContainsKey(target))
            {
                int c=q.Dequeue();foreach(int n in new[]{c+1,c-1,c+SnakeSession.Width,c-SnakeSession.Width})
                {
                    if(n<0||n>=SnakeSession.Width*SnakeSession.Height||Math.Abs(n%SnakeSession.Width-c%SnakeSession.Width)+Math.Abs(n/SnakeSession.Width-c/SnakeSession.Width)!=1||parents.ContainsKey(n)||snake.IsObstacle(n)||snake.Body.Contains(n))continue;
                    parents[n]=c;q.Enqueue(n);
                }
            }
            if(parents.ContainsKey(target))
            {
                int n=target;while(parents[n]!=head&&parents[n]>=0)n=parents[n];
                snake.Turn(n==head+1?SnakeDirection.Right:n==head-1?SnakeDirection.Left:n>head?SnakeDirection.Down:SnakeDirection.Up);
            }
            snake.Advance(snake.StepSeconds);
        }
        private static void CapturePanel(string name,RenderTexture texture)
        {
            var previous=RenderTexture.active;RenderTexture.active=texture;
            var image=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);image.Apply();RenderTexture.active=previous;
            var directory=new DirectoryInfo(Application.dataPath);while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"AGENTS.md")))directory=directory.Parent;
            File.WriteAllBytes(Path.Combine(directory.FullName,".artifacts/hostfall2-20261117/"+name+".png"),image.EncodeToPNG());Object.Destroy(image);
        }
    }
}
