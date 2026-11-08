using System.Collections;
using System.Linq;
using Hostfall.CameraSystem;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Player;
using Hostfall.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Hostfall.Tests
{
    public sealed class InputFlowTests
    {
        [UnityTest]
        public IEnumerator KeyboardAndMouseDriveMovementCameraAndTerminalMission()
        {
            var background = InputSystem.settings.backgroundBehavior;
            var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.EnableDevice(keyboard); InputSystem.EnableDevice(mouse);
            ControlState controls = null;
            try
            {
                yield return SceneManager.LoadSceneAsync("NorthlineFacility");
                var bindings = Object.FindAnyObjectByType<FacilityInput>();
                controls = Object.FindAnyObjectByType<ControlState>();
                bindings.Move.actionMap.devices = new InputDevice[] { keyboard, mouse };
                bindings.Escape.actionMap.devices = new InputDevice[] { keyboard, mouse };
                Object.FindAnyObjectByType<Hostfall.Interaction.InteractionScanner>().enabled=false;
                controls.SetMode(ControlMode.Exploring);
                Assert.That(controls.CanExplore, Is.True, "The test player needs focus to exercise exploration input.");
                yield return new WaitForSeconds(.1f);
                var player = Object.FindAnyObjectByType<PlayerMotor>();
                var controller = player.GetComponent<CharacterController>();
                Vector3 initial = player.transform.position;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                float moveDeadline = Time.realtimeSinceStartup + 2;
                while (initial.z - player.transform.position.z < .35f && Time.realtimeSinceStartup < moveDeadline) yield return null;
                Assert.That(initial.z - player.transform.position.z, Is.GreaterThan(.3f), "W must move: key=" + keyboard.wKey.isPressed + ", enabled=" + bindings.Move.enabled + ", value=" + bindings.Move.ReadValue<Vector2>() + ", mode=" + controls.Mode);
                yield return new WaitForSeconds(.8f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Assert.That(player.transform.position.z, Is.GreaterThan(-3.4f), "The console collider must stop forward movement.");
                float ground = player.transform.position.y;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                yield return new WaitForSeconds(.16f);
                Assert.That(player.transform.position.y, Is.GreaterThan(ground + .1f), "Space must jump.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return new WaitForSeconds(1.2f);
                Assert.That(Mathf.Abs(player.transform.position.y - ground), Is.LessThan(.08f), "After jumping, the capsule must settle back onto the floor.");
                Vector3 beforeSwitch = player.transform.position;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.V));
                yield return null; yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                var rig = Object.FindAnyObjectByType<PlayerCameraRig>();
                Assert.That(rig.ThirdPerson, Is.True);
                Assert.That(Vector3.Distance(beforeSwitch, player.transform.position), Is.LessThan(.02f));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
                yield return new WaitForSeconds(1.2f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Assert.That(UnityEngine.Camera.main.transform.position.z, Is.LessThan(.9f), "TPS must retract at the SOC wall.");
                Assert.That(Physics.CheckSphere(UnityEngine.Camera.main.transform.position, .06f, CollisionLayers.WorldMask), Is.False);

                // Reset the scene so the UI path always starts at the authored workstation approach.
                yield return SceneManager.LoadSceneAsync("NorthlineFacility");
                bindings = Object.FindAnyObjectByType<FacilityInput>();
                controls = Object.FindAnyObjectByType<ControlState>();
                bindings.Move.actionMap.devices = new InputDevice[] { keyboard, mouse };
                bindings.Escape.actionMap.devices = new InputDevice[] { keyboard, mouse };
                bindings.Interact.actionMap.devices = new InputDevice[] { keyboard, mouse };
                controls.SetMode(ControlMode.Exploring);
                yield return new WaitForSeconds(.1f);
                InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(0, -100));
                yield return new WaitForSeconds(.15f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                float connectionDeadline=Time.realtimeSinceStartup+3;
                while(controls.Mode!=ControlMode.Terminal&&Time.realtimeSinceStartup<connectionDeadline) yield return null;
                Assert.That(controls.Mode,Is.EqualTo(ControlMode.Terminal),"Pressing E while targeting the reachable terminal must start connection");
                Assert.That(bindings.Move.enabled,Is.False);
                var document=Object.FindAnyObjectByType<UIDocument>(); var view=document.rootVisualElement;
                Assert.That(view.Q<TextField>("command"),Is.Null,"No manual command surface may remain");
                Assert.That(view.Q<Button>("execute"),Is.Null);
                var simulation=Object.FindAnyObjectByType<FacilityRoot>().Simulation;
                var ui=Object.FindAnyObjectByType<Hostfall.UI.FacilityUI>();
                foreach(var id in new[] { DeviceId.Ws07,DeviceId.Edge01,DeviceId.Core01 })
                { ui.OpenTerminal(id); MenuFlowTests.FinishRoutine(simulation,id); yield return null; }
                Assert.That(simulation.Mission.CompletedObjectives,Is.GreaterThanOrEqualTo(3));
                var fieldUI=Object.FindAnyObjectByType<Hostfall.Field.FieldSystemUI>();
                fieldUI.OpenArticle("process"); yield return null;
                Assert.That(controls.Mode,Is.EqualTo(ControlMode.Field));
                Assert.That(view.Q<Label>("field-breadcrumb"),Is.Null);
                fieldUI.Close(); yield return null;
                Assert.That(controls.Mode,Is.EqualTo(ControlMode.Terminal));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape)); yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
                Assert.That(controls.Mode,Is.EqualTo(ControlMode.Exploring));
                Assert.That(bindings.Move.enabled,Is.True);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.K)); yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                Assert.That(controls.Mode, Is.EqualTo(ControlMode.Field));
                Assert.That(Time.timeScale, Is.Zero);
                var search = document.rootVisualElement.Q<TextField>("knowledge-search");
                search.schedule.Execute(() => search.Focus()).ExecuteLater(50); yield return new WaitForSecondsRealtime(.2f);
                Assert.That(search.focusController.focusedElement, Is.Not.Null, "Search must receive focus: " + string.Join(" / ", search.GetClasses()) + " layout=" + search.layout + " enabled=" + search.enabledInHierarchy + " visible=" + search.visible + " ancestors=" + string.Join(" > ", search.GetFirstAncestorOfType<ScrollView>()?.GetClasses() ?? new string[0]));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.K)); yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                Assert.That(controls.Mode, Is.EqualTo(ControlMode.Field), "Typing K in archive search must not close the archive.");
                fieldUI.SelectDomain("Инфраструктура");
                fieldUI.Session.Study("container");
                fieldUI.SelectDomain("Инфраструктура");
                search.value = "kubectl";
                Assert.That(document.rootVisualElement.Q<ScrollView>("knowledge-results").Query<Button>().ToList().Count, Is.EqualTo(1));
                Assert.That(document.rootVisualElement.Q<Hostfall.Field.KnowledgeGraphView>(), Is.Null);
                fieldUI.Close();
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                if (controls != null) controls.SetMode(ControlMode.Paused);
                Time.timeScale = 1;
                InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse);
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            }
        }
    }
}
