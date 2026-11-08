using System.Collections;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Player;
using Hostfall.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Hostfall.Tests
{
    public sealed class FacilitySmokeTests
    {
        [UnityTest]
        public IEnumerator AuthoredSceneBootsWithThreeBoundTerminalsAndSolidFloor()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility");
            yield return null;
            var root = Object.FindAnyObjectByType<FacilityRoot>();
            Assert.That(root, Is.Not.Null); Assert.That(root.Simulation, Is.Not.Null);
            var terminals = Object.FindObjectsByType<ComputerTerminal>();
            Assert.That(terminals.Length, Is.EqualTo(3));
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            Assert.That(Physics.Raycast(player.transform.position + Vector3.up * .3f, Vector3.down, 1, CollisionLayers.WorldMask), Is.True);
            var document = Object.FindAnyObjectByType<UIDocument>();
            Assert.That(document.rootVisualElement.Q<TextField>("command"), Is.Null);
            var controls = Object.FindAnyObjectByType<ControlState>();
            foreach (var id in new[] { DeviceId.Ws07,DeviceId.Edge01,DeviceId.Core01 })
            {
                var terminal=System.Linq.Enumerable.Single(terminals,t=>t.Id==id);
                terminal.Interact(); yield return null;
                Assert.That(controls.Mode, Is.EqualTo(ControlMode.Terminal));
                Assert.That(document.rootVisualElement.Q<Label>("host").text, Does.Contain(terminal.Id.Hostname().ToUpperInvariant()));
                MenuFlowTests.FinishRoutine(root.Simulation,id);
                controls.SetMode(ControlMode.Exploring);
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
