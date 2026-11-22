using System.Collections;
using System.Linq;
using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using Hostfall.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hostfall.Tests
{
    public sealed partial class ComputerWorldTests
    {
        [UnityTest]
        public IEnumerator ComputeVaultGalleriesConnectThroughTheServiceBridgeBothWays()
        {
            yield return EnterGame();
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            player.Restore(new PlayerStateData { position = new Vector3(4.6f, .08f, 16), yaw = 0 });
            var capsule = player.GetComponent<CharacterController>();
            Physics.SyncTransforms();
            WalkRoute(capsule, (4.6f, 27), (-4.6f, 27));
            Assert.That(capsule.transform.position.y, Is.EqualTo(3.2f).Within(.15f));
            WalkRoute(capsule, (4.6f, 27), (4.6f, 16));
            Assert.That(capsule.transform.position.y, Is.LessThan(.15f));
        }

        [UnityTest]
        public IEnumerator EnergyScreenHasAVisibleFaceFromNormalApproach()
        {
            var station = Station(ComputerCampaign.CircuitStation);
            var glass = station.Surface;
            var eye = new Vector3(-16.5f, 1.64f, 34.1f);
            Physics.SyncTransforms();
            Assert.That(Vector3.Dot(glass.Normal, Vector3.back), Is.GreaterThan(.9f));
            Assert.That(Physics.Linecast(eye, glass.Centre + Vector3.forward * .025f, out var hit,
                CollisionLayers.InteractionMask, QueryTriggerInteraction.Ignore), Is.True);
            Assert.That(hit.collider.name, Is.EqualTo("Screen interaction surface"));
            Assert.That(Object.FindObjectsByType<TrackedSentryController>().Any(actor => actor.DamageableId == "sentry-crawler-e06"), Is.False);
            Assert.That(Object.FindObjectsByType<WatcherController>().Any(actor => actor.Id == "aux-watch-energy-r36"), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CorridorWatcherFollowsThePlayerThroughTheOfficeAirlock()
        {
            yield return EnterGame();
            CompleteFirstComputer(Root.Simulation.ComputerCampaign);
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            var door = FindDoor("door.soc.corridor");
            var robot = Object.FindObjectsByType<WatcherController>().Single(actor => actor.Id == "watcher-r17");
            player.enabled = false;
            robot.enabled = false;
            player.Restore(new PlayerStateData { position = new Vector3(0, .04f, -2.65f), yaw = 0 });
            robot.transform.SetPositionAndRotation(new Vector3(0, 0, 2.55f), Quaternion.Euler(0, 180, 0));
            Physics.SyncTransforms();
            Assert.That(door.Capture().opened, Is.False);
            Assert.That(robot.Sees(player.Eye), Is.False, "The closed leaf must occlude the player");

            door.Interact();
            yield return new WaitForSeconds(1.9f);
            Physics.SyncTransforms();
            Assert.That(robot.Sees(player.Eye), Is.True, "The player is visible through the open doorway");
            robot.enabled = true;
            yield return ObserveContainment(() => robot.transform.position.z < -.65f, 6f,
                () => $"The robot saw the player but stopped in {robot.State.Mode} at {robot.transform.position:F3}");
            Assert.That(robot.LastKnownPosition.z, Is.LessThan(door.transform.position.z));

            player.Restore(new PlayerStateData { position = new Vector3(0, .04f, 3.35f), yaw = 180 });
            robot.transform.SetPositionAndRotation(new Vector3(0, 0, -1.65f), Quaternion.identity);
            Physics.SyncTransforms();
            Assert.That(robot.Sees(player.Eye), Is.True, "The player is visible from the office side");
            yield return ObserveContainment(() => robot.transform.position.z > 1.8f, 6f,
                () => $"The robot did not return to the corridor. Position is {robot.transform.position:F3}");
        }

        [UnityTest]
        public IEnumerator VentCrawlerRunsThroughTheCutOfficeWallBeforeEnteringTheRoom()
        {
            yield return EnterGame();
            var vent = Object.FindAnyObjectByType<VentReleaseController>();
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            player.Restore(new PlayerStateData { position = new Vector3(0, .04f, -3), yaw = 90 });
            player.enabled = false;
            Physics.SyncTransforms();
            var origin = vent.ExitPosition + Vector3.right * 1.10f + Vector3.up * .73f;
            Assert.That(Physics.Raycast(origin, Vector3.right, out var wall, 6,
                CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore), Is.True);
            Assert.That(wall.collider.name, Is.EqualTo("Bend rear panel"), "The shaft must extend past the original wall");

            var campaign = Root.Simulation.ComputerCampaign;
            campaign.BeginSnake();
            for (int i = 0; i < 20; i++) campaign.AdvanceWorld(.25);
            yield return ObserveContainment(() => campaign.ThreatPhase == VentThreatPhase.Opening &&
                vent.Actor.transform.position.x < 4.85f && vent.Actor.transform.position.x > 3.38f, 2,
                () => $"The crawler must pass the east wall while visible. Position is {vent.Actor.transform.position:F3}");
            Assert.That(vent.Actor.GetComponentsInChildren<Renderer>().Any(renderer => renderer.enabled), Is.True);
            yield return ObserveContainment(() => vent.Actor.enabled, 2, () => "The crawler did not reach the room");

            CompleteFirstComputer(campaign);
            Physics.SyncTransforms();
            Assert.That(Physics.Raycast(origin, Vector3.right, out var valve, 6,
                CollisionLayers.WorldMask, QueryTriggerInteraction.Ignore), Is.True);
            Assert.That(valve.collider.name, Is.EqualTo("Isolation damper V-14"), "Sealing ventilation closes a physical damper");
        }

        [UnityTest]
        public IEnumerator VaultWatcherDetectsNearbyMovementAndFollowsThroughTheOpenEnergyDoor()
        {
            yield return EnterGame();
            var campaign = Root.Simulation.ComputerCampaign;
            ReachEnergyComputer(campaign);
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            var capsule = player.GetComponent<CharacterController>();
            var robot = Object.FindObjectsByType<WatcherController>().Single(actor => actor.Id == "sentinel-r21");
            foreach (var watcher in Object.FindObjectsByType<WatcherController>())
                watcher.enabled = false;
            robot.transform.SetPositionAndRotation(new Vector3(0, 0, 20), Quaternion.identity);
            player.Restore(new PlayerStateData { position = new Vector3(0, .08f, 19.2f), yaw = 0 });
            Physics.SyncTransforms();
            Assert.That(robot.Sees(player.Eye), Is.True, "Nearby player outside the front sensor cone");
            robot.transform.SetPositionAndRotation(new Vector3(-5.65f, 0, 26), Quaternion.Euler(0, -90, 0));
            player.Restore(new PlayerStateData { position = new Vector3(-5.65f, .08f, 26), yaw = 270 });
            Physics.SyncTransforms();
            Assert.That(robot.Sees(new Vector3(-8.5f, 1.2f, 26)), Is.False, "The closed bulkhead occludes the adjacent room");
            yield return OpenRouteDoor(capsule, "door.sec03.energy");
            player.Restore(new PlayerStateData { position = new Vector3(-13.25f, .08f, 26), yaw = 90 });
            Physics.SyncTransforms();
            robot.HearShot(player.transform.position);
            robot.enabled = true;
            yield return ObserveContainment(() => robot.transform.position.x < -12.05f, 8,
                () => "The robot stopped before entering the room beyond its patrol bounds: " + robot.transform.position);
        }
    }
}
