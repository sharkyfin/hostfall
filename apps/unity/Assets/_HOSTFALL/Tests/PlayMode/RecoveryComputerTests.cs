using System;
using System.Collections;
using System.Linq;
using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.Player;
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
        private static IEnumerator FinishHomeRecovery()
        {
            var campaign = Root.Simulation.ComputerCampaign;
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            Assert.That(campaign.RecoveryOffered || campaign.BeginRecovery(), Is.False);
            Assert.That(campaign.DispatchTransit(TransitStop.Ring, true), Is.False);
            ReachSignalComputer(campaign);
            CalibrateSignals(campaign);
            yield return CompletePhysicalQuarantine(campaign, player);
            Assert.That(campaign.RecoveryOffered, Is.True);
            Assert.That(campaign.RecoveryAccessible || campaign.BeginRecovery(), Is.False);
            Assert.That(campaign.Buffer.Complete || campaign.Fragments.Complete || campaign.Quorum.Complete || campaign.Integrity.Complete, Is.False);

            player.Restore(new PlayerStateData { position = new Vector3(-2.6f, .08f, -14.9f), yaw = 180 });
            Desktop.Open(Station(ComputerCampaign.HomeStation));
            yield return new WaitForSecondsRealtime(.7f);
            yield return CompleteRecoveryAtOpenHome();
        }

        private static IEnumerator CompleteRecoveryAtOpenHome()
        {
            var campaign = Root.Simulation.ComputerCampaign;
            var view = Desktop.PhysicalDisplay.View;
            Assert.That(view.Q<Button>("home-recovery"), Is.Not.Null,
                $"Home authorization entry missing. Tab {campaign.HomeTab}, offered {campaign.RecoveryOffered}, signed {campaign.RecoveryAuthorized}");
            yield return Click(view.Q<Button>("home-recovery"));
            var sign = view.Q<Button>("recovery-authorize");
            Assert.That(sign.enabledInHierarchy, Is.False);
            for (int record = 0; record < ComputerCampaign.RecoveryRecords.Count; record++)
            {
                yield return Click(view.Q<Button>("recovery-record-" + record));
                Assert.That(view.Q<Label>("recovery-evidence").text, Does.Contain(ComputerCampaign.RecoveryRecords[record].Source));
            }
            Assert.That(sign.enabledInHierarchy, Is.True);
            Assert.That(Root.Persistence.Save("manual1"), Is.True);
            var library = new SaveLibrary(WorldSaveController.TestStorageDirectory, null, true);
            Assert.That(library.Read("manual1", out var saved, out var error), Is.True, error);
            var restored = ComputerCampaign.Restore(saved.simulation.computerCampaign);
            Assert.That(restored.HomeTab, Is.EqualTo(ComputerCampaign.RecoveryTab));
            Assert.That(restored.RecoveryAccessible && !restored.RecoveryAuthorized, Is.True);
            yield return Click(sign);
            Assert.That(campaign.RecoveryAuthorized, Is.True);
            Assert.That(ComputerCampaign.Restore(campaign.Capture()).RecoveryAuthorized, Is.True);
            Assert.That(campaign.Actions.Count(a => a.target == ComputerCampaign.RecoveryTransitTarget), Is.EqualTo(1));
            Assert.That(campaign.Home.TryRead(ComputerCampaign.RecoveryLog, out _), Is.True);
            Assert.That(view.Q<Label>("recovery-status").text, Does.Contain("Кабина готова"));
            Desktop.Close();
        }

        [UnityTest]
        public IEnumerator RecoveryUnlocksRealShuttleThenRestoresAboardAndReachesCoreInBothDirections()
        {
            yield return EnterGame();
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            var shuttle = Object.FindAnyObjectByType<RecoveryTransitController>();
            Assert.That(shuttle, Is.Not.Null);
            Assert.That(shuttle.Powered, Is.False);
            var gate = FindDoor(ComputerCampaign.RecoveryCoreGate);
            Assert.That(gate.IsUnlocked, Is.False);
            foreach (var actor in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None)) actor.enabled = false;
            yield return FinishHomeRecovery();
            Assert.That(shuttle.Powered && gate.IsUnlocked, Is.True);
            var capsule = player.GetComponent<CharacterController>();
            Physics.SyncTransforms();
            Walk(capsule, new Vector3(0, 0, -14.9f));
            Walk(capsule, new Vector3(0, 0, -18.3f));
            Walk(capsule, new Vector3(0, 0, RecoveryTransit.Shelter.Z));

            var cabButton = CabinButton(shuttle);
            PointToCabButton(player, cabButton);
            cabButton.Interact();
            Assert.That(Controls.RidingTransit && shuttle.Riding, Is.True);
            yield return ObserveContainment(() => shuttle.Cabin.position.y < -9f && shuttle.Cabin.position.z > -11f, 9,
                () => "Expected travel at tunnel depth. Actual " + shuttle.Cabin.position);
            var oldPose = player.transform.position;
            Controls.SetMode(ControlMode.Paused);
            yield return new WaitForSecondsRealtime(.18f);
            Assert.That(player.transform.position, Is.EqualTo(oldPose));
            Assert.That(Root.Persistence.Save("manual2"), Is.True);

            bool attached = false;
            void Loaded(Scene _, LoadSceneMode mode)
            {
                var restoredShuttle = Object.FindAnyObjectByType<RecoveryTransitController>();
                attached = restoredShuttle != null && restoredShuttle.Riding && Controls.RidingTransit &&
                    Vector3.Distance(Object.FindAnyObjectByType<PlayerMotor>().transform.position, oldPose) < .025f;
            }
            SceneManager.sceneLoaded += Loaded;
            try
            {
                Root.Persistence.Load("manual2");
                yield return null;
                yield return new WaitForSecondsRealtime(.1f);
            }
            finally { SceneManager.sceneLoaded -= Loaded; }
            Assert.That(attached, Is.True);
            player = Object.FindAnyObjectByType<PlayerMotor>();
            shuttle = Object.FindAnyObjectByType<RecoveryTransitController>();
            capsule = player.GetComponent<CharacterController>();
            Controls.SetMode(ControlMode.Exploring);
            yield return ObserveContainment(() => !Controls.RidingTransit, 14, () => "Restored shuttle did not reach dispatch");
            Assert.That(player.transform.position.z, Is.EqualTo(RecoveryTransit.Ring.Z).Within(.14f));
            Physics.SyncTransforms();
            Walk(capsule, new Vector3(38, 0, 124.34f));
            Walk(capsule, new Vector3(30, 0, 124.34f));
            Walk(capsule, new Vector3(30, 0, 129.5f));
            Walk(capsule, new Vector3(30, 0, 124.34f));
            Walk(capsule, new Vector3(38, 0, 124.34f));
            Walk(capsule, new Vector3(38, 0, RecoveryTransit.Ring.Z));
            CabinButton(shuttle).Interact();
            Assert.That(Controls.RidingTransit, Is.True);
            yield return ObserveContainment(() => !Controls.RidingTransit, 17, () => "Return shuttle did not reach the shelter");
            Assert.That(player.transform.position.z, Is.EqualTo(RecoveryTransit.Shelter.Z).Within(.14f));
            Physics.SyncTransforms();
            Walk(capsule, new Vector3(0, 0, -17.85f));
            Assert.That(Root.Simulation.ComputerCampaign.RecoveryAuthorized, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        private static RecoveryTransitPanel CabinButton(RecoveryTransitController controller) =>
            controller.Cabin.GetComponentsInChildren<RecoveryTransitPanel>().Single();

        private static void PointToCabButton(PlayerMotor player, RecoveryTransitPanel button)
        {
            Physics.SyncTransforms();
            var direction = (button.transform.position - player.Eye).normalized;
            Assert.That(Physics.Raycast(player.Eye, direction, out var hit, 2.7f, CollisionLayers.InteractionMask,
                QueryTriggerInteraction.Ignore), Is.True);
            Assert.That(hit.collider.GetComponentInParent<RecoveryTransitPanel>(), Is.SameAs(button));
        }
    }
}
