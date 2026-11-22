using System;
using System.Collections;
using System.Linq;
using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.Simulation.Computers;
using Hostfall.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Hostfall.Tests
{
    public sealed partial class ComputerWorldTests
    {
        private static void EarnContainmentKey(ComputerCampaign campaign, PlayerMotor motor)
        {
            EarnNeuroarchiveEntry(campaign);
            VerifyInvestigation(campaign.Investigation);
            motor.Restore(new PlayerStateData { position = new Vector3(66.3f, .08f, 47.75f) });
            Object.FindAnyObjectByType<NeuroInvestigationController>().Interact();
            Assert.That(campaign.ContainmentAccessible, Is.True);
        }

        private static void VerifyInvestigation(DetectiveSession investigation)
        {
            var sources = new[] { DetectiveEvidence.EngineerMail, DetectiveEvidence.ShiftNotebook,
                DetectiveEvidence.SensorRegister, DetectiveEvidence.CustodyMemo };
            foreach (var id in sources.Concat(new[] { DetectiveEvidence.ArchivePlayback, DetectiveEvidence.PublicReport }))
            {
                Assert.That(investigation.Open(id), Is.True);
                Assert.That(investigation.Collect(id), Is.True);
            }
            for (int index = 0; index < sources.Length; index++)
            {
                Assert.That(investigation.Place(index, (DetectiveEvent)index), Is.True);
                Assert.That(investigation.Cite(index, sources[index]), Is.True);
            }
            Assert.That(investigation.SetDate(17, DetectiveEvidence.ArchivePlayback, DetectiveEvidence.PublicReport), Is.True);
            Assert.That(investigation.Submit(), Is.EqualTo(DetectiveResult.Verified));
        }

        private static IEnumerator OpenContainmentDesk(PlayerMotor motor)
        {
            motor.Restore(new PlayerStateData { position = new Vector3(33.5f, 4.31f, 90.90f) });
            var station = Station(ComputerCampaign.ContainmentStation);
            Desktop.Open(station);
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(Desktop.ActiveStation, Is.SameAs(station));
        }

        private static IEnumerator ObserveContainment(Func<bool> predicate, float seconds, Func<string> context)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!predicate() && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(predicate(), Is.True, context());
        }

        [UnityTest]
        public IEnumerator LiveR26FollowsServiceRecallUntilPhysicalCaptureOpensCentralRing()
        {
            yield return EnterGame();
            var campaign = Root.Simulation.ComputerCampaign;
            var world = Object.FindAnyObjectByType<ContainmentWorldController>();
            var motor = Object.FindAnyObjectByType<PlayerMotor>();
            var ring = FindDoor(ComputerCampaign.ContainmentAccess);
            var sentry = Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).Single(w => w.Id == "assembly-sentry-r26");
            Assert.That(world, Is.Not.Null);
            Assert.That(ring.IsUnlocked || FindDoor("door.sec10.super").IsUnlocked, Is.False);

            yield return OpenContainmentDesk(motor);
            yield return RunFieldUtility(FieldUtilityId.QuarantineRecall);
            Assert.That(campaign.Containment.Started, Is.False);
            Desktop.Close();
            EarnContainmentKey(campaign, motor);
            yield return OpenContainmentDesk(motor);
            yield return CompleteReachedContainment(campaign);
            var session = campaign.Containment;
            Assert.That(session.Clearance, Is.EqualTo(ContainmentClearance.Captured));
            Assert.That(ring.IsUnlocked, Is.True);
            Assert.That(sentry.State.Mode, Is.EqualTo(WatcherMode.Maintenance));
            Assert.That(FindDoor("door.sec10.super").IsUnlocked, Is.False);
            Assert.That(campaign.Actions.Count(a => a.target == ComputerCampaign.ContainmentAccess), Is.EqualTo(1));
            Assert.That(campaign.Home.TryRead(ComputerCampaign.ContainmentLog, out _), Is.True);
            Desktop.Close();
            int finalFeed = world.FrameCount;
            yield return ObserveContainment(() => ring.Capture().offset >= 3.19f, 5,
                () => "Ring leaf failed to finish opening");
            motor.Restore(new PlayerStateData { position = new Vector3(30, .08f, 92.55f) });
            Physics.SyncTransforms();
            var capsule = motor.GetComponent<CharacterController>();
            Walk(capsule, new Vector3(30, 0, 96.6f));
            Walk(capsule, new Vector3(30, 0, 92.55f));
            Assert.That(world.FrameCount, Is.EqualTo(finalFeed));
            Assert.That(Root.Persistence.Save("manual1"), Is.True);
            bool restored = false;
            void Loaded(Scene _, LoadSceneMode mode)
            {
                var model = Root.Simulation.ComputerCampaign;
                restored = model.Containment.Clearance == ContainmentClearance.Captured && FindDoor(ComputerCampaign.ContainmentAccess).IsUnlocked;
            }
            SceneManager.sceneLoaded += Loaded;
            try
            {
                Root.Persistence.Load("manual1");
                yield return null;
                yield return new WaitForSecondsRealtime(.1f);
            }
            finally { SceneManager.sceneLoaded -= Loaded; }
            Assert.That(restored, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator OccupiedCaptureBayAllowsEgressAndOfflineR26StillClearsQuarantine()
        {
            yield return EnterGame();
            var campaign = Root.Simulation.ComputerCampaign;
            var motor = Object.FindAnyObjectByType<PlayerMotor>();
            var world = Object.FindAnyObjectByType<ContainmentWorldController>();
            var sentry = Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).Single(w => w.Id == "assembly-sentry-r26");
            EarnContainmentKey(campaign, motor);
            Assert.That(campaign.BeginContainment(), Is.True);
            var session = campaign.Containment;
            sentry.GetComponent<RobotCombatController>().Hit(10000, RobotHitRegion.Core, sentry.transform.position);
            Assert.That(sentry.IsDestroyed, Is.True);
            session.Toggle(ContainmentShutter.South);
            session.Toggle(ContainmentShutter.North);
            motor.Restore(new PlayerStateData { position = new Vector3(42, .08f, 81.4f) });
            Physics.SyncTransforms();
            yield return ObserveContainment(() => session.Sensors.PlayerInside && session.Sensors.Zone == SentryZone.Offline, 2, () => "Bay did not observe player or offline sentry");
            Assert.That(session.Opening(ContainmentShutter.North), Is.EqualTo(1));
            Assert.That(session.Isolate(), Is.False);
            Walk(motor.GetComponent<CharacterController>(), new Vector3(42, 0, 85.6f));
            yield return ObserveContainment(() => session.CanIsolate, 6, () => "Offline sentry did not confirm after player exited");
            Assert.That(session.Isolate(), Is.True);
            Assert.That(session.Clearance, Is.EqualTo(ContainmentClearance.Offline));
            Assert.That(FindDoor(ComputerCampaign.ContainmentAccess).IsUnlocked, Is.True);
            Assert.That(world.BayBounds.Contains(motor.transform.position), Is.False);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
