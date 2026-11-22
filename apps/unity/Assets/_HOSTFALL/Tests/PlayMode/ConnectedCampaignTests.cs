using System.Collections;
using System.Linq;
using Hostfall.Core;
using Hostfall.Field;
using Hostfall.Player;
using Hostfall.Simulation.Computers;
using Hostfall.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Hostfall.Tests
{
    public sealed partial class ComputerWorldTests
    {
        [UnityTest]
        public IEnumerator FreshCampaignWalksFromShelterThroughQuantumToLogistics()
        {
            yield return WalkFromShelterToLogistics();
            Assert.That(Root.Simulation.ComputerCampaign.Payload.Stage, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator FreshCampaignWalksAcrossLogisticsBridgeAndReachesTheSensorDesk()
        {
            yield return WalkFromShelterToSecurity();
            var campaign = Root.Simulation.ComputerCampaign;
            Assert.That(campaign.MinefieldAccessible, Is.True);
            Assert.That(campaign.InvestigationAccessible, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator FreshCampaignReachesCoreThroughAllMandatoryComputersAndHomeRecovery()
        {
            yield return WalkFromShelterToSecurity(keepQuarantineSentry: true);
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            var capsule = player.GetComponent<CharacterController>();
            var campaign = Root.Simulation.ComputerCampaign;
            Assert.That(campaign.CurrentStage, Is.EqualTo(CampaignStage.Minefield));

            yield return RunFieldUtility(FieldUtilityId.FloorService);
            Assert.That(campaign.FloorServiced, Is.True);
            Desktop.Close();
            Assert.That(FindDoor(ComputerCampaign.MinefieldExit).IsUnlocked, Is.True);
            Assert.That(FindDoor(ComputerCampaign.MinefieldNeuroPass).IsUnlocked, Is.True);
            yield return new WaitForSecondsRealtime(2.1f);
            WalkRoute(capsule, (46.2f, 23.5f), (46.2f, 27.05f), (47.35f, 27.05f),
                (47.35f, 36.5f), (47.35f, 43), (56, 43), (60, 52.5f), (62, 54.6f));
            yield return OpenReachedStation(player, ComputerCampaign.DetectiveStation);
            yield return SolveFieldCase(2);
            Assert.That(campaign.VoiceVerified, Is.True);
            Desktop.Close();
            var safe = Object.FindAnyObjectByType<NeuroInvestigationController>();
            Assert.That(safe.HasPhysicalKey, Is.True);
            WalkRoute(capsule, (64, 54.6f), (64, 47.75f), (66.3f, 47.75f));
            CollectKeyAtSafe(player, safe);
            Assert.That(campaign.ContainmentAccessible, Is.True);

            WalkRoute(capsule, (64, 47.75f), (64, 52.5f), (60, 52.5f), (56, 43), (47.35f, 43),
                (47.35f, 36.5f), (47.35f, 27.05f), (46.2f, 27.05f), (46.2f, 23.5f), (42, 24), (31, 26.5f), (30, 54),
                (27.9f, 58), (30, 61), (30, 66.4f), (17, 66.4f), (17, 77), (17.9f, 91.5f), (23.5f, 91.5f), (33.5f, 90.9f));
            Assert.That(capsule.transform.position.y, Is.EqualTo(4.3f).Within(.15f));
            yield return OpenReachedStation(player, ComputerCampaign.ContainmentStation);
            yield return CompleteReachedContainment(campaign);
            Desktop.Close();
            Assert.That(campaign.RecoveryOffered, Is.True);
            Assert.That(campaign.CurrentStage, Is.EqualTo(CampaignStage.Recovery));
            Assert.That(FindDoor(ComputerCampaign.RecoveryCoreGate).IsUnlocked, Is.False);

            WalkRoute(capsule, (23.5f, 91.5f), (17.9f, 91.5f), (17, 77), (14.25f, 77),
                (14.25f, 86), (4, 86), (2, 84), (2, 65), (1.5f, 63.3f));
            yield return UseNorthReturnActuator(capsule);
            WalkRoute(capsule, (0, 63.3f), (0, 60.6f), (1.25f, 60.6f), (1.25f, 40), (0, 40),
                (0, 36.6f), (-1.95f, 32.5f), (-1.95f, 20.5f), (0, 20.5f), (0, 14.1f),
                (1.8f, 14.1f), (1.8f, 11.4f), (0, 11.4f), (0, -5), (0, -10), (-2.6f, -14.9f));
            yield return OpenReachedStation(player, ComputerCampaign.HomeStation);
            yield return CompleteRecoveryAtOpenHome();

            var shuttle = Object.FindAnyObjectByType<RecoveryTransitController>();
            Assert.That(shuttle.Powered, Is.True);
            WalkRoute(capsule, (0, -14.9f), (0, -18.3f), (0, RecoveryTransit.Shelter.Z));
            var panel = CabinButton(shuttle);
            PointToCabButton(player, panel);
            panel.Interact();
            Assert.That(shuttle.Riding, Is.True);
            yield return ObserveContainment(() => !shuttle.Riding, 17, () => "Shuttle did not reach the Ring");
            Assert.That(capsule.transform.position.z, Is.EqualTo(RecoveryTransit.Ring.Z).Within(.14f));
            Physics.SyncTransforms();
            WalkRoute(capsule, (38, 124.34f), (30, 124.34f), (30, 129.5f));
            Assert.That(campaign.RecoveryAuthorized, Is.True);
            Assert.That(FindDoor(ComputerCampaign.RecoveryCoreGate).IsUnlocked, Is.True);
            Assert.That(player.GetComponent<PlayerHealth>().CurrentHealth, Is.GreaterThan(0));
            WalkRoute(capsule, (30, 133.1f), (18.8f, 133.1f), (10, 136));
            yield return FightCoreAndReachEpilogue(player);
            Assert.That(Root.Simulation.ComputerCampaign.CurrentStage, Is.EqualTo(CampaignStage.Epilogue));
            LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator WalkFromShelterToSecurity(bool keepQuarantineSentry = false)
        {
            yield return WalkFromShelterToLogistics(keepQuarantineSentry);
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            var capsule = player.GetComponent<CharacterController>();
            var campaign = Root.Simulation.ComputerCampaign;

            WalkRoute(capsule, (4, 65), (4, 68.2f), (5.5f, 68.2f), (5.5f, 73.5f), (8.5f, 73.5f));
            Assert.That(capsule.transform.position.y, Is.EqualTo(1).Within(.15f));
            yield return OpenReachedStation(player, ComputerCampaign.PayloadStation);
            yield return RunFieldUtility(FieldUtilityId.CargoTransfer);
            yield return ClearLogisticsDefence(campaign);
            yield return new WaitForSecondsRealtime(3.2f);
            WalkRoute(capsule, (8.5f, 84));
            Assert.That(capsule.transform.position.y, Is.EqualTo(1).Within(.15f));
            WalkRoute(capsule, (11.2f, 84), (14.25f, 84), (14.25f, 77));
            yield return OpenRouteDoor(capsule, "door.sec06.robo");
            WalkRoute(capsule, (17, 77), (17, 66.4f), (30, 66.4f));
            yield return OpenRouteDoor(capsule, "door.sec07.citadel");
            WalkRoute(capsule, (30, 61), (27.9f, 58), (30, 54), (31, 26.5f), (42, 24), (48, 23.5f));
            yield return OpenReachedStation(player, ComputerCampaign.MinefieldStation);
        }

        private static IEnumerator ClearLogisticsDefence(ComputerCampaign campaign)
        {
            var state = campaign.LogisticsDefense;
            Assert.That(state.Active, Is.True, "Finishing the physical PAYLOAD station must trigger the lockdown");
            Assert.That(Desktop.ActiveStation, Is.Null);
            var robots = Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None)
                .Where(actor => actor.Id.StartsWith("aux-watch-logistics-")).OrderBy(actor => actor.Id).ToArray();
            Assert.That(robots.Length, Is.EqualTo(3));
            var behindShutter = robots[0].transform.position;
            yield return ObserveContainment(() => state.Phase == LogisticsDefense.Warning &&
                behindShutter.x - robots[0].transform.position.x > .45f, 2,
                () => "The reserve must approach from behind the shutter before release");
            Assert.That(robots[0].transform.position.y, Is.EqualTo(1.025f).Within(.04f));
            Assert.That(robots[0].GetComponentsInChildren<Renderer>().Any(renderer => renderer.enabled), Is.True);
            yield return ObserveContainment(() => state.Phase == LogisticsDefense.FirstWave, 3, () => "The first wave did not deploy");
            Assert.That(robots.Count(enemy => enemy.isActiveAndEnabled), Is.EqualTo(1));
            robots[0].GetComponent<RobotCombatController>().Model.Defeat();
            yield return ObserveContainment(() => state.Phase == LogisticsDefense.SecondWave, 4, () => "The second wave did not deploy");
            foreach (var robot in robots.Skip(1))
            {
                Assert.That(robot.isActiveAndEnabled, Is.True);
                robot.GetComponent<RobotCombatController>().Model.Defeat();
            }
            yield return ObserveContainment(() => state.Phase == LogisticsDefense.Cleared, 2, () => "The quarantine gates did not acknowledge the wave defeat");
        }

        private static IEnumerator WalkFromShelterToLogistics(bool keepQuarantineSentry = false)
        {
            yield return EnterGame();
            var journal = Object.FindAnyObjectByType<FieldSystemUI>();
            journal.OpenJournal();
            var document = Object.FindAnyObjectByType<Hostfall.UI.FacilityUI>().GetComponent<UIDocument>().rootVisualElement;
            Assert.That(document.Q<Button>("campaign-task-current").text, Does.Contain("Чужое дыхание"));
            Assert.That(document.Q("quest-body").Query<Label>().ToList().Any(l => l.text.Contains("четыре пакета")), Is.True);
            Assert.That(document.Q<Button>("campaign-task-14"), Is.Null, "The final report is first shown after reaching it");
            journal.Close();
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            player.enabled = false;
            // Keep static colliders, gates and power authorities live while isolating route clearance.
            foreach (var actor in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(component => component is IDamageable && component is not (PlayerHealth or CoreAvatarEncounter)))
            {
                if (keepQuarantineSentry && actor is WatcherController sentry && sentry.Id == "assembly-sentry-r26")
                    continue;
                actor.enabled = false;
                foreach (var collider in actor.GetComponentsInChildren<Collider>())
                    collider.enabled = false;
            }
            var capsule = player.GetComponent<CharacterController>();
            var campaign = Root.Simulation.ComputerCampaign;

            WalkRoute(capsule, (0, -14), (0, -10));
            yield return OpenRouteDoor(capsule, "door.shelter.blast");
            WalkRoute(capsule, (0, -5), (-2.65f, -2.3f));
            yield return OpenReachedStation(player, ComputerCampaign.SnakeStation);
            yield return Click(Desktop.PhysicalDisplay.View.Q<Button>("desktop-snake"));
            yield return Click(Desktop.PhysicalDisplay.View.Q<Button>("snake-start"));
            CompleteFirstComputer(campaign);
            Desktop.Close();

            WalkRoute(capsule, (0, -4), (0, -10), (-2.6f, -14.3f));
            yield return OpenReachedStation(player, ComputerCampaign.HomeStation);
            campaign.Shell.Execute("cat " + ComputerCampaign.IncidentPath);
            Assert.That(campaign.HasRead(ComputerCampaign.IncidentPath), Is.True);
            Desktop.Close();

            WalkRoute(capsule, (0, -14.3f), (0, -10), (0, -.45f));
            yield return OpenRouteDoor(capsule, "door.soc.corridor");
            WalkRoute(capsule, (0, 5.5f));
            yield return OpenRouteDoor(capsule, "door.corridor.server");
            WalkRoute(capsule, (0, 11.4f));
            yield return OpenReachedStation(player, ComputerCampaign.NetworkStation);
            yield return SolveFieldCase(1);
            Assert.That(campaign.CameraIsolated, Is.True);
            Desktop.Close();

            WalkRoute(capsule, (1.8f, 11.4f), (1.8f, 14.1f), (0, 14.1f));
            yield return OpenRouteDoor(capsule, "door.sec02.vault");
            WalkRoute(capsule, (0, 20.5f), (-1.95f, 20.5f), (-1.95f, 26), (-5.65f, 26));
            yield return OpenRouteDoor(capsule, "door.sec03.energy");
            WalkRoute(capsule, (-10.5f, 26), (-11.3f, 33.5f), (-16.5f, 34.1f));
            yield return OpenReachedStation(player, ComputerCampaign.CircuitStation);
            yield return RunFieldUtility(FieldUtilityId.PowerGuard);
            Assert.That(campaign.Circuit.Powered(CircuitLoad.Turret), Is.True);
            Assert.That(campaign.Circuit.Powered(CircuitLoad.Lighting), Is.False);
            yield return RunFieldUtility(FieldUtilityId.PowerLight);
            Assert.That(campaign.Circuit.Powered(CircuitLoad.Lighting), Is.True);
            Assert.That(campaign.Circuit.Powered(CircuitLoad.Shortcut), Is.True);
            Assert.That(campaign.Circuit.Powered(CircuitLoad.MainDoor), Is.True);
            Desktop.Close();

            WalkRoute(capsule, (-11.3f, 33.5f), (-10.5f, 26), (-1.95f, 26), (-1.95f, 32.5f), (0, 36.6f));
            yield return OpenRouteDoor(capsule, "door.sec04.cryo");
            WalkRoute(capsule, (0, 48.4f));
            yield return OpenReachedStation(player, ComputerCampaign.SignalStation);
            yield return RunFieldUtility(FieldUtilityId.PhaseBypass);
            Assert.That(campaign.PayloadAccessible, Is.True);
            Desktop.Close();

            yield return new WaitForSecondsRealtime(2.3f);
            WalkRoute(capsule, (1.25f, 48.4f), (1.25f, 60.6f), (0, 61.15f), (0, 64));
            Assert.That(player.transform.position.y, Is.GreaterThan(-.2f));
        }

        private static IEnumerator OpenRouteDoor(CharacterController capsule, string id)
        {
            var door = FindDoor(id);
            Assert.That(door.IsUnlocked, Is.True, id);
            Assert.That(Vector3.Distance(capsule.transform.position, door.transform.position), Is.LessThan(2.8f), id);
            if (!door.Capture().opened)
                door.Interact();
            yield return ObserveContainment(() => door.FullyOpen, 12, () => id + " did not clear the opening. Offset " + door.Capture().offset);
            Physics.SyncTransforms();
        }

        private static IEnumerator OpenReachedStation(PlayerMotor player, string id)
        {
            var station = Station(id);
            Assert.That(Vector3.Distance(player.Eye, station.Surface.Centre), Is.LessThan(2.8f), id);
            station.Interact();
            Assert.That(Desktop.ActiveStation, Is.SameAs(station));
            yield return null;
            yield return ObserveContainment(() => Desktop.PhysicalDisplay.Interactive, 4, () => "Camera did not reach " + id);
        }

        private static void WalkRoute(CharacterController capsule, params (float x, float z)[] route)
        {
            foreach (var (x, z) in route)
                Walk(capsule, new Vector3(x, 0, z));
        }

        private static IEnumerator CompleteReachedContainment(ComputerCampaign campaign)
        {
            yield return RunFieldUtility(FieldUtilityId.QuarantineRecall);
            var session = campaign.Containment;
            yield return ObserveContainment(() => session.Sensors.Zone == SentryZone.Bay, 16,
                () => "R26 did not enter the capture bay");
            yield return RunFieldUtility(FieldUtilityId.QuarantineSeal);
            yield return ObserveContainment(() => session.CanIsolate, 7, () => "The capture bay rejected isolation");
            yield return RunFieldUtility(FieldUtilityId.QuarantineSeal);
            Assert.That(session.Clearance, Is.EqualTo(ContainmentClearance.Captured));
            Assert.That(FindDoor(ComputerCampaign.ContainmentAccess).IsUnlocked, Is.True);
        }

        private static IEnumerator RunFieldUtility(FieldUtilityId utility)
        {
            var panel = Desktop.PhysicalDisplay.View;
            Assert.That(panel.Q<Button>("field-knowledge"), Is.Not.Null);
            var script = panel.Q<Button>("field-script-" + utility);
            Assert.That(script, Is.Not.Null, $"Missing script file for {utility}");
            yield return Click(script);
            var run = panel.Q<Button>("field-run-" + utility);
            Assert.That(run, Is.Not.Null, $"Script did not open for {utility}");
            yield return Click(run);
        }

        private static IEnumerator SolveFieldCase(int suspect)
        {
            var panel = Desktop.PhysicalDisplay.View;
            var campaign = Root.Simulation.ComputerCampaign;
            var station = Desktop.ActiveStation.StationId;
            Assert.That(panel.Q<Button>("field-knowledge"), Is.Null);
            Assert.That(panel.Q<Button>("field-mark-0").enabledInHierarchy, Is.False);
            yield return Click(panel.Q<Button>("field-evidence-0"));
            if (suspect == 1)
            {
                yield return Click(panel.Q<Button>("field-frame-late"));
                Assert.That(panel.Query<Label>().ToList().Count(l => l.text.Contains("Кадр 06313")), Is.EqualTo(1));
                Assert.That(panel.Query<Image>().ToList().Count(frame => frame.image != null), Is.EqualTo(3));
                CapturePanel("empty-camera-buffer", Desktop.PhysicalDisplay.Texture);
            }
            else
            {
                Assert.That(Resources.Load<AudioClip>("Investigation/Voice0317"), Is.Not.Null);
                yield return Click(panel.Q<Button>("field-play-voice"));
                CapturePanel("second-voice-recording", Desktop.PhysicalDisplay.Texture);
            }
            yield return Click(panel.Q<Button>("field-evidence-1"));
            yield return Click(panel.Q<Button>("field-verdict"));
            yield return Click(panel.Q<Button>("field-mark-0"));
            yield return Click(panel.Q<Button>("field-mark-1"));
            yield return Click(panel.Q<Button>("field-suspect-0"));
            yield return Click(panel.Q<Button>("field-submit"));
            Assert.That(campaign.CaseConfirmed(station), Is.False);
            yield return Click(panel.Q<Button>("field-suspect-" + suspect));
            yield return Click(panel.Q<Button>("field-submit"));
            Assert.That(campaign.CaseConfirmed(station), Is.True);
            var restored = ComputerCampaign.Restore(campaign.Capture());
            Assert.That(restored.CaseReadMask(station), Is.EqualTo(3));
            Assert.That(restored.CaseConfirmed(station), Is.True);
        }

        private static IEnumerator UseNorthReturnActuator(CharacterController capsule)
        {
            var barrier = Object.FindAnyObjectByType<SignalBarrierController>();
            Physics.SyncTransforms();
            var origin = capsule.transform.position + Vector3.up * 1.4f;
            var target = new Vector3(2.7f, 1.25f, 62.47f);
            Assert.That(Physics.Raycast(origin, (target - origin).normalized, out var hit, 2.5f,
                CollisionLayers.InteractionMask, QueryTriggerInteraction.Ignore), Is.True);
            Assert.That(hit.collider.GetComponentInParent<SignalBarrierController>(), Is.SameAs(barrier));
            barrier.Interact();
            Assert.That(Root.Simulation.ComputerCampaign.SignalPlant.ReturnWindow, Is.EqualTo(8));
            yield return new WaitForSecondsRealtime(2.3f);
        }
    }
}
