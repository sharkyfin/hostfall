using System.Collections;
using Hostfall.Core;
using Hostfall.Devices;
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

        [UnityTest]
        public IEnumerator ExpandedFacilitySectorsAreUnlockedByTerminalsAndFormContinuousLoop()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility");
            yield return null;
            var root = Object.FindAnyObjectByType<FacilityRoot>();
            Assert.That(root, Is.Not.Null);

            var doors = Object.FindObjectsByType<FacilityDoor>();
            Assert.That(doors.Length, Is.GreaterThanOrEqualTo(5));

            var vaultDoor = System.Linq.Enumerable.FirstOrDefault(doors, d => d.name == "Vault Gate (Z=15)");
            Assert.That(vaultDoor, Is.Not.Null);
            Assert.That(vaultDoor.IsUnlocked, Is.False, "Vault Gate must be locked before CORE-01 completion");

            // Complete WS-07, EDGE-01, CORE-01 routines in natural sequence
            MenuFlowTests.FinishRoutine(root.Simulation, DeviceId.Ws07);
            MenuFlowTests.FinishRoutine(root.Simulation, DeviceId.Edge01);
            MenuFlowTests.FinishRoutine(root.Simulation, DeviceId.Core01);
            Assert.That(vaultDoor.IsUnlocked, Is.True, "Vault Gate must unlock when CORE-01 completes");

            // Locate the 3 door terminal consoles
            var consoles = Object.FindObjectsByType<DoorTerminalConsole>();
            Assert.That(consoles.Length, Is.EqualTo(3), "Expected DATA-01, PWR-01, and QNT-01 consoles");

            var dataConsole = System.Linq.Enumerable.FirstOrDefault(consoles, c => c.name.Contains("DATA-01"));
            var pwrConsole = System.Linq.Enumerable.FirstOrDefault(consoles, c => c.name.Contains("PWR-01"));
            var qntConsole = System.Linq.Enumerable.FirstOrDefault(consoles, c => c.name.Contains("QNT-01"));

            Assert.That(dataConsole, Is.Not.Null);
            Assert.That(pwrConsole, Is.Not.Null);
            Assert.That(qntConsole, Is.Not.Null);

            var energyDoor = System.Linq.Enumerable.FirstOrDefault(doors, d => d.name == "Airlock to Energy Center");
            var cryoDoor = System.Linq.Enumerable.FirstOrDefault(doors, d => d.name == "Cryo Gate (Z=38)");
            var execDoor = System.Linq.Enumerable.FirstOrDefault(doors, d => d.name == "Executive Shortcut Door");

            Assert.That(energyDoor, Is.Not.Null);
            Assert.That(cryoDoor, Is.Not.Null);
            Assert.That(execDoor, Is.Not.Null);

            Assert.That(energyDoor.IsUnlocked, Is.False, "Energy Airlock must be locked initially");
            Assert.That(cryoDoor.IsUnlocked, Is.False, "Cryo Gate must be locked initially");
            Assert.That(execDoor.IsUnlocked, Is.False, "Executive Shortcut must be locked initially");

            var controls = Object.FindAnyObjectByType<ControlState>();

            var ui = Object.FindAnyObjectByType<FacilityUI>();
            Assert.That(ui, Is.Not.Null);

            // Interacting with DATA-01 unlocks Energy Airlock
            dataConsole.Interact();
            yield return null;
            for (int i = 0; i < 200 && !dataConsole.Completed; i++)
            {
                ui.TickCustomSession(0.1f);
            }
            Assert.That(dataConsole.Completed, Is.True, "DATA-01 session must complete");
            Assert.That(energyDoor.IsUnlocked, Is.True, "Energy Airlock must unlock after DATA-01");
            controls.SetMode(ControlMode.Exploring);
            yield return null;

            // Interacting with PWR-01 unlocks Cryo Gate
            pwrConsole.Interact();
            yield return null;
            for (int i = 0; i < 200 && !pwrConsole.Completed; i++)
            {
                ui.TickCustomSession(0.1f);
            }
            Assert.That(pwrConsole.Completed, Is.True, "PWR-01 session must complete");
            Assert.That(cryoDoor.IsUnlocked, Is.True, "Cryo Gate must unlock after PWR-01");
            controls.SetMode(ControlMode.Exploring);
            yield return null;

            // Interacting with QNT-01 unlocks Executive Shortcut Door
            qntConsole.Interact();
            yield return null;
            for (int i = 0; i < 200 && !qntConsole.Completed; i++)
            {
                ui.TickCustomSession(0.1f);
            }
            Assert.That(qntConsole.Completed, Is.True, "QNT-01 session must complete");
            Assert.That(execDoor.IsUnlocked, Is.True, "Executive Shortcut must unlock after QNT-01");
            controls.SetMode(ControlMode.Exploring);
            yield return null;

            // Verify all autonomous patrol robots exist across sectors
            var watchers = Object.FindObjectsByType<WatcherController>();
            Assert.That(watchers.Length, Is.GreaterThanOrEqualTo(6), "Facility must have at least 6 patrol robots across all sectors");

            // Verify mission objectives progress across sectors
            Assert.That(root.Simulation.Mission.CompletedObjectives, Is.EqualTo(6), "All 6 sector objectives must be completed");
            Assert.That(root.Simulation.Mission.IsComplete, Is.True, "Full facility loop mission must be complete");

            // Floor colliders check across all sectors
            Assert.That(Physics.Raycast(new Vector3(0, 1f, 26f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 02 floor solid");
            Assert.That(Physics.Raycast(new Vector3(-16.5f, 1f, 28f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 03 floor solid");
            Assert.That(Physics.Raycast(new Vector3(0, 1f, 50f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 04 bridge solid");
            Assert.That(Physics.Raycast(new Vector3(0, 0f, 45f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 04 trench floor solid");
            Assert.That(Physics.Raycast(new Vector3(-16.5f, 1f, 8f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Bypass corridor floor solid");
            Assert.That(Physics.Raycast(new Vector3(-10.75f, 1f, -1.75f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Connector corridor floor solid");

            LogAssert.NoUnexpectedReceived();
        }
    }
}
