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
        public IEnumerator LegacyHealthDataDoesNotDestroyPlayer()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility");
            yield return null;
            var health = Object.FindAnyObjectByType<PlayerHealth>();
            var legacy = JsonUtility.FromJson<Hostfall.Persistence.GameSaveData>("{\"version\":2}");
            health.Restore(legacy.health);
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));
        }

        [UnityTest]
        public IEnumerator PlayableSectorsDoNotTriggerOutOfBoundsRecovery()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility");
            yield return null;
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            var controls = Object.FindAnyObjectByType<ControlState>();
            var controller = player.GetComponent<CharacterController>();
            controls.SetMode(ControlMode.Exploring);
            foreach (var position in new[] { new Vector3(0, 0.1f, -17f), new Vector3(0, 0.1f, 80f), new Vector3(30f, 0.1f, 80f), new Vector3(10f, 0.1f, 199f) })
            {
                controller.enabled = false;
                player.transform.position = position;
                controller.enabled = true;
                yield return null;
                Assert.That(Vector3.Distance(player.transform.position, position), Is.LessThan(1f),
                    "The shelter and expanded sectors are inside the playable world.");
            }
        }

        [UnityTest]
        public IEnumerator EdgeCorridorHasRoomForThePlayer()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility");
            yield return null;
            var hits = Physics.CapsuleCastAll(new Vector3(-0.55f, 0.35f, 1.4f),
                new Vector3(-0.55f, 1.5f, 1.4f), 0.3f, Vector3.forward, 5.0f,
                CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<WatcherController>() != null) continue;
                Assert.Fail("EDGE corridor is obstructed by " + hit.collider.name + " at " + hit.point);
            }
        }

        [UnityTest]
        public IEnumerator DeathScreenOwnsFocusInsteadOfPauseMenu()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility");
            yield return null;
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            var document = Object.FindAnyObjectByType<UIDocument>();
            player.GetComponent<DeathController>().TriggerDeath("test");
            yield return new WaitForSecondsRealtime(0.1f);
            var root = document.rootVisualElement;
            Assert.That(root.Q("death-screen").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q("pause").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(root.panel.focusController.focusedElement, Is.EqualTo(root.Q<Button>("death-restart")));
        }

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

            // Locate door terminal consoles
            var consoles = Object.FindObjectsByType<DoorTerminalConsole>();
            Assert.That(consoles.Length, Is.GreaterThanOrEqualTo(9), "Expected DATA-01, PWR-01, QNT-01, CARGO-WIN, RBT-01, SEC-07, NEURO-MAC, CLOUD-09, and SUPER-CORE consoles");

            var dataConsole = System.Linq.Enumerable.FirstOrDefault(consoles, c => c.name.Contains("DATA-01"));
            var pwrConsole = System.Linq.Enumerable.FirstOrDefault(consoles, c => c.name.Contains("PWR-01"));
            var qntConsole = System.Linq.Enumerable.FirstOrDefault(consoles, c => c.name.Contains("QNT-01"));
            var cargoConsole = System.Linq.Enumerable.FirstOrDefault(consoles, c => c.name.Contains("CARGO-WIN"));
            var roboConsole = System.Linq.Enumerable.FirstOrDefault(consoles, c => c.name.Contains("RBT-01"));
            var secConsole = System.Linq.Enumerable.FirstOrDefault(consoles, c => c.name.Contains("SEC-07"));
            var neuroConsole = System.Linq.Enumerable.FirstOrDefault(consoles, c => c.name.Contains("NEURO-MAC"));
            var cloudConsole = System.Linq.Enumerable.FirstOrDefault(consoles, c => c.name.Contains("CLOUD-09"));
            var superConsole = System.Linq.Enumerable.FirstOrDefault(consoles, c => c.name.Contains("SUPER-CORE"));

            Assert.That(dataConsole, Is.Not.Null);
            Assert.That(pwrConsole, Is.Not.Null);
            Assert.That(qntConsole, Is.Not.Null);
            Assert.That(cargoConsole, Is.Not.Null);
            Assert.That(roboConsole, Is.Not.Null);
            Assert.That(secConsole, Is.Not.Null);
            Assert.That(neuroConsole, Is.Not.Null);
            Assert.That(cloudConsole, Is.Not.Null);
            Assert.That(superConsole, Is.Not.Null);

            var energyDoor = System.Linq.Enumerable.FirstOrDefault(doors, d => d.name == "Airlock to Energy Center");
            var cryoDoor = System.Linq.Enumerable.FirstOrDefault(doors, d => d.name == "Cryo Gate (Z=38)");
            var execDoor = System.Linq.Enumerable.FirstOrDefault(doors, d => d.name == "Executive Shortcut Door");
            var logisticsDoor = System.Linq.Enumerable.FirstOrDefault(doors, d => d.name == "Logistics Gate (Z=62)");
            var roboDoor = System.Linq.Enumerable.FirstOrDefault(doors, d => d.name == "Airlock to Robotics Plant");
            var socSecDoor = System.Linq.Enumerable.FirstOrDefault(doors, d => d.name.Contains("Security Shortcut to SOC Gate"));
            var memVaultDoor = System.Linq.Enumerable.FirstOrDefault(doors, d => d.name.Contains("Deep Memory Vault Gate"));
            var s10Door = System.Linq.Enumerable.FirstOrDefault(doors, d => d.name.Contains("Super Core Transition Gate"));
            var evacDoor = System.Linq.Enumerable.FirstOrDefault(doors, d => d.name.Contains("Evacuation Overlook Portal"));

            Assert.That(energyDoor, Is.Not.Null);
            Assert.That(cryoDoor, Is.Not.Null);
            Assert.That(execDoor, Is.Not.Null);
            Assert.That(logisticsDoor, Is.Not.Null);
            Assert.That(roboDoor, Is.Not.Null);
            Assert.That(socSecDoor, Is.Not.Null);
            Assert.That(memVaultDoor, Is.Not.Null);
            Assert.That(s10Door, Is.Not.Null);
            Assert.That(evacDoor, Is.Not.Null);

            Assert.That(energyDoor.IsUnlocked, Is.False, "Energy Airlock must be locked initially");
            Assert.That(cryoDoor.IsUnlocked, Is.False, "Cryo Gate must be locked initially");
            Assert.That(execDoor.IsUnlocked, Is.False, "Executive Shortcut must be locked initially");
            Assert.That(logisticsDoor.IsUnlocked, Is.False, "Logistics Gate must be locked initially");
            Assert.That(roboDoor.IsUnlocked, Is.False, "Robotics Airlock must be locked initially");
            Assert.That(socSecDoor.IsUnlocked, Is.False, "Security Shortcut to SOC must be locked initially");
            Assert.That(memVaultDoor.IsUnlocked, Is.False, "Deep Memory Vault Gate must be locked initially");
            Assert.That(s10Door.IsUnlocked, Is.False, "Super Core Transition Gate must be locked initially");
            Assert.That(evacDoor.IsUnlocked, Is.False, "Evacuation Overlook Portal must be locked initially");

            var controls = Object.FindAnyObjectByType<ControlState>();

            var ui = Object.FindAnyObjectByType<FacilityUI>();
            Assert.That(ui, Is.Not.Null);

            // Interacting with DATA-01 before cartridge inserted keeps it locked
            dataConsole.Interact();
            yield return null;
            Assert.That(dataConsole.Completed, Is.False, "DATA-01 must require physical data cartridge");

            // Insert cartridge and re-engage DATA-01
            DataCartridgeState.HasCartridge = true;
            DataCartridgeState.IsInserted = true;
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

            // Interacting with PWR-01 before cooling valve is opened keeps it locked
            pwrConsole.Interact();
            yield return null;
            Assert.That(pwrConsole.Completed, Is.False, "PWR-01 must require coolant valve bypass");

            // Turn coolant loop valve V-102 and re-engage PWR-01
            CoolingValveState.IsOpen = true;
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
            Assert.That(logisticsDoor.IsUnlocked, Is.True, "Logistics Gate must unlock after QNT-01");
            controls.SetMode(ControlMode.Exploring);
            yield return null;

            // Interacting with CARGO-WIN unlocks Robotics Plant Airlock Door
            cargoConsole.Interact();
            yield return null;
            for (int i = 0; i < 200 && !cargoConsole.Completed; i++)
            {
                ui.TickCustomSession(0.1f);
            }
            Assert.That(cargoConsole.Completed, Is.True, "CARGO-WIN session must complete");
            Assert.That(roboDoor.IsUnlocked, Is.True, "Robotics Airlock must unlock after CARGO-WIN");
            controls.SetMode(ControlMode.Exploring);
            yield return null;

            // Interacting with SEC-07 unlocks Security Shortcut to SOC
            secConsole.Interact();
            yield return null;
            for (int i = 0; i < 200 && !secConsole.Completed; i++)
            {
                ui.TickCustomSession(0.1f);
            }
            Assert.That(secConsole.Completed, Is.True, "SEC-07 session must complete");
            Assert.That(socSecDoor.IsUnlocked, Is.True, "Security Shortcut Gate to SOC must unlock after SEC-07");
            controls.SetMode(ControlMode.Exploring);
            yield return null;

            // Interacting with NEURO-MAC unlocks Deep Memory Vault Gate
            neuroConsole.Interact();
            yield return null;
            for (int i = 0; i < 200 && !neuroConsole.Completed; i++)
            {
                ui.TickCustomSession(0.1f);
            }
            Assert.That(neuroConsole.Completed, Is.True, "NEURO-MAC session must complete");
            Assert.That(memVaultDoor.IsUnlocked, Is.True, "Deep Memory Vault Gate must unlock after NEURO-MAC");
            controls.SetMode(ControlMode.Exploring);
            yield return null;

            // Interacting with CLOUD-09 unlocks Super Core Transition Gate to Sector 10
            cloudConsole.Interact();
            yield return null;
            for (int i = 0; i < 200 && !cloudConsole.Completed; i++)
            {
                ui.TickCustomSession(0.1f);
            }
            Assert.That(cloudConsole.Completed, Is.True, "CLOUD-09 session must complete");
            Assert.That(s10Door.IsUnlocked, Is.True, "Super Core Transition Gate must unlock after CLOUD-09");
            controls.SetMode(ControlMode.Exploring);
            yield return null;

            // Interacting with SUPER-CORE unlocks Evacuation Overlook Portal
            superConsole.Interact();
            yield return null;
            for (int i = 0; i < 200 && !superConsole.Completed; i++)
            {
                ui.TickCustomSession(0.1f);
            }
            Assert.That(superConsole.Completed, Is.True, "SUPER-CORE session must complete");
            Assert.That(evacDoor.IsUnlocked, Is.True, "Evacuation Overlook Portal must unlock after SUPER-CORE");
            controls.SetMode(ControlMode.Exploring);
            yield return null;

            // Verify all autonomous patrol robots exist across sectors
            var watchers = Object.FindObjectsByType<WatcherController>();
            Assert.That(watchers.Length, Is.GreaterThanOrEqualTo(8), "Facility must have at least 8 patrol robots across all sectors");

            // Verify combat entities and pickups
            var sentries = Object.FindObjectsByType<TrackedSentryController>();
            Assert.That(sentries.Length, Is.GreaterThanOrEqualTo(2), "Facility must have at least 2 heavy tracked sentries");

            var bulwarks = Object.FindObjectsByType<BulwarkSentryController>();
            Assert.That(bulwarks.Length, Is.GreaterThanOrEqualTo(1), "Facility must have E08 Bulwark Sentry in Sector 07");

            var jammers = Object.FindObjectsByType<JammerRobotController>();
            Assert.That(jammers.Length, Is.GreaterThanOrEqualTo(1), "Facility must have E09 RF Jammer in Sector 07");

            var hounds = Object.FindObjectsByType<TraceHoundController>();
            Assert.That(hounds.Length, Is.GreaterThanOrEqualTo(1), "Facility must have E10 Trace Hound in Sector 08");

            var railEyes = Object.FindObjectsByType<RailEyeTurretController>();
            Assert.That(railEyes.Length, Is.GreaterThanOrEqualTo(1), "Facility must have E12 Rail-Eye Turret in Sector 09");

            var bastions = Object.FindObjectsByType<BastionSentryController>();
            Assert.That(bastions.Length, Is.GreaterThanOrEqualTo(1), "Facility must have E15 Bastion-4 Assault Sentry in Sector 09");

            var atlases = Object.FindObjectsByType<AtlasBossController>();
            Assert.That(atlases.Length, Is.GreaterThanOrEqualTo(1), "Facility must have BOSS ATLAS-0 in Sector 10");

            var pulsePickups = Object.FindObjectsByType<PulseSidearmPickup>();
            Assert.That(pulsePickups.Length, Is.GreaterThanOrEqualTo(1), "Facility must have CIRRUS pulse sidearm pickup");

            var titanPickups = Object.FindObjectsByType<TitanbreakerPickup>();
            Assert.That(titanPickups.Length, Is.GreaterThanOrEqualTo(1), "Facility must have W10 TITANBREAKER pickup");

            var ammoCrates = Object.FindObjectsByType<AmmoBoxPickup>();
            Assert.That(ammoCrates.Length, Is.GreaterThanOrEqualTo(10), "Facility must have at least 10 staged ammo crates");

            // Floor colliders check across all sectors
            Assert.That(Physics.Raycast(new Vector3(0, 1f, 26f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 02 floor solid");
            Assert.That(Physics.Raycast(new Vector3(-16.5f, 1f, 28f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 03 floor solid");
            Assert.That(Physics.Raycast(new Vector3(0, 1f, 50f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 04 bridge solid");
            Assert.That(Physics.Raycast(new Vector3(0, 0f, 45f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 04 trench floor solid");
            Assert.That(Physics.Raycast(new Vector3(-5f, 1f, 78.5f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 05 logistics floor solid");
            Assert.That(Physics.Raycast(new Vector3(33.5f, 1f, 80f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 06 robotics floor solid");
            Assert.That(Physics.Raycast(new Vector3(33.5f, 5f, 91.5f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 06 observation catwalk solid");
            Assert.That(Physics.Raycast(new Vector3(28f, 1f, 57.5f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 07 foyer floor solid");
            Assert.That(Physics.Raycast(new Vector3(31f, 1f, 35.0f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 07 atrium floor solid");
            Assert.That(Physics.Raycast(new Vector3(34f, 4f, 35.0f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 07 catwalk solid");
            Assert.That(Physics.Raycast(new Vector3(48f, 1f, 27.5f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 07 archive floor solid");
            Assert.That(Physics.Raycast(new Vector3(8.5f, 1f, 9.0f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 07 shortcut corridor solid");
            Assert.That(Physics.Raycast(new Vector3(62f, 1f, 75.0f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 08 gallery floor solid");
            Assert.That(Physics.Raycast(new Vector3(79f, 1f, 75.0f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 08 pods floor solid");
            Assert.That(Physics.Raycast(new Vector3(79f, 1f, 52.5f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 08 chassis floor solid");
            Assert.That(Physics.Raycast(new Vector3(62f, 1f, 52.5f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 08 office floor solid");
            Assert.That(Physics.Raycast(new Vector3(71f, 1f, 29.0f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 08 memory vault solid");
            Assert.That(Physics.Raycast(new Vector3(30f, 1f, 93.5f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 09 ring floor solid");
            Assert.That(Physics.Raycast(new Vector3(36f, 1f, 113.5f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 09 dispatch floor solid");
            Assert.That(Physics.Raycast(new Vector3(36f, 4.5f, 121.0f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 09 mezzanine solid");
            Assert.That(Physics.Raycast(new Vector3(59f, 1f, 112.0f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 09 witness floor solid");
            Assert.That(Physics.Raycast(new Vector3(10f, 1f, 153.0f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 10 arena floor solid");
            Assert.That(Physics.Raycast(new Vector3(10f, 1.5f, 153.0f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 10 central dais solid");
            Assert.That(Physics.Raycast(new Vector3(-11f, 4.5f, 153.0f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 10 catwalk solid");
            Assert.That(Physics.Raycast(new Vector3(10f, 1f, 185.0f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 10 trust core floor solid");
            Assert.That(Physics.Raycast(new Vector3(10f, 1f, 199.0f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Sector 10 evac overlook floor solid");
            Assert.That(Physics.Raycast(new Vector3(-16.5f, 1f, 8f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Bypass corridor floor solid");
            Assert.That(Physics.Raycast(new Vector3(-10.75f, 1f, -1.75f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Connector corridor floor solid");
            Assert.That(Physics.Raycast(new Vector3(-16.5f, 1f, 8f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Bypass corridor floor solid");
            Assert.That(Physics.Raycast(new Vector3(-10.75f, 1f, -1.75f), Vector3.down, 2f, CollisionLayers.WorldMask), Is.True, "Connector corridor floor solid");

            LogAssert.NoUnexpectedReceived();
        }
    }
}
