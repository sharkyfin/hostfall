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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Hostfall.Tests
{
    public sealed partial class ComputerWorldTests
    {
        private static IEnumerator CompletePhysicalQuarantine(ComputerCampaign campaign, PlayerMotor motor)
        {
            EarnContainmentKey(campaign, motor);
            Assert.That(campaign.BeginContainment(), Is.True);
            var sentry = Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None)
                .Single(w => w.Id == "assembly-sentry-r26");
            sentry.GetComponent<RobotCombatController>().Hit(10000, RobotHitRegion.Core, sentry.transform.position);
            Assert.That(sentry.IsDestroyed, Is.True);
            var session = campaign.Containment;
            session.Toggle(ContainmentShutter.South);
            session.Toggle(ContainmentShutter.North);
            yield return ObserveContainment(() => session.CanIsolate, 6, () => "Outside capsule must allow quarantine clearance");
            Assert.That(session.Isolate(), Is.True);
        }

        [UnityTest]
        public IEnumerator WitnessScheduleRejectsEarlySignatureAndUnlocksThePhysicalWestLoop()
        {
            yield return EnterGame();
            var campaign = Root.Simulation.ComputerCampaign;
            var motor = Object.FindAnyObjectByType<PlayerMotor>();
            var shortcut = FindDoor(ComputerCampaign.QuorumShortcut);
            Assert.That(shortcut.IsUnlocked || campaign.BeginQuorum(), Is.False);
            foreach (var watcher in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None))
                watcher.enabled = false;
            yield return CompletePhysicalQuarantine(campaign, motor);

            motor.Restore(new PlayerStateData { position = new Vector3(53, .08f, 112) });
            Physics.SyncTransforms();
            var capsule = motor.GetComponent<CharacterController>();
            Walk(capsule, new Vector3(56, 0, 112));
            Walk(capsule, new Vector3(58.9f, 0, 112.4f));
            Desktop.Open(Station(ComputerCampaign.QuorumStation));
            yield return new WaitForSecondsRealtime(.7f);
            var view = Desktop.PhysicalDisplay.View;
            Assert.That(view.Q<Button>("field-files"), Is.Not.Null);
            Assert.That(view.Q<Button>("field-case"), Is.Null);
            Assert.That(campaign.BeginQuorum(), Is.True);

            var schedule = new[] { (0, 0, 0), (1, 1, 2), (2, 2, 0), (3, 2, 2), (4, 1, 4), (5, 0, 4) };
            foreach (var (job, lane, slot) in schedule)
            {
                campaign.Quorum.Select(job);
                Assert.That(campaign.Quorum.Schedule(lane, slot), Is.EqualTo(QuorumIssue.None));
            }
            Assert.That(campaign.Quorum.Dispatch(out _), Is.EqualTo(QuorumIssue.Dependency));
            Assert.That(shortcut.IsUnlocked, Is.False);
            Assert.That(Root.Persistence.Save("manual1"), Is.True);
            var library = new SaveLibrary(WorldSaveController.TestStorageDirectory, null, true);
            Assert.That(library.Read("manual1", out var saved, out var error), Is.True, error);
            var restored = ComputerCampaign.Restore(saved.simulation.computerCampaign);
            Assert.That(restored.Quorum.Inspect(out int jobIndex), Is.EqualTo(QuorumIssue.Dependency));
            Assert.That(jobIndex, Is.EqualTo(5));
            Assert.That(restored.Quorum.Selected, Is.EqualTo(5));

            Assert.That(campaign.Quorum.Schedule(0, 5), Is.EqualTo(QuorumIssue.None));
            Assert.That(campaign.Quorum.Dispatch(out _), Is.EqualTo(QuorumIssue.None));
            Assert.That(campaign.Quorum.Complete, Is.True);
            Assert.That(shortcut.IsUnlocked, Is.True);
            Assert.That(campaign.Actions.Count(a => a.target == ComputerCampaign.QuorumShortcut), Is.EqualTo(1));
            Assert.That(campaign.Home.TryRead(ComputerCampaign.QuorumLog, out _), Is.True);
            Desktop.Close();
            motor.Restore(new PlayerStateData { position = new Vector3(19.4f, .08f, 108) });
            shortcut.Interact();
            yield return new WaitForSecondsRealtime(2.15f);
            Physics.SyncTransforms();
            Walk(capsule, new Vector3(13f, 0, 108));
            Walk(capsule, new Vector3(13f, 0, 93.5f));
            Walk(capsule, new Vector3(13f, 0, 108));
            Walk(capsule, new Vector3(19.4f, 0, 108));

            Assert.That(Root.Persistence.Save("manual1"), Is.True);
            bool restoredShortcut = false;
            void Loaded(Scene _, LoadSceneMode mode)
            {
                restoredShortcut = Root.Simulation.ComputerCampaign.Quorum.Complete &&
                    FindDoor(ComputerCampaign.QuorumShortcut).IsUnlocked;
            }
            SceneManager.sceneLoaded += Loaded;
            try
            {
                Root.Persistence.Load("manual1");
                yield return null;
                yield return new WaitForSecondsRealtime(.1f);
            }
            finally { SceneManager.sceneLoaded -= Loaded; }
            Assert.That(restoredShortcut, Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
