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
        private static void EarnNeuroarchiveEntry(ComputerCampaign campaign)
        {
            EarnSecurityEntry(campaign);
            var board = campaign.Minefield;
            for (int level = 0; level < MinefieldSession.LevelCount; level++)
            {
                Assert.That(campaign.BeginMinefield(), Is.True);
                var armed = Enumerable.Range(0, MinefieldSession.CellCount).Where(board.IsArmed).ToArray();
                foreach (int cell in armed)
                    board.ToggleFlag(cell);
                for (int cell = 0; cell < MinefieldSession.CellCount && board.Stage == level; cell++)
                    if (!armed.Contains(cell))
                        board.Reveal(cell);
            }
            Assert.That(campaign.InvestigationAccessible, Is.True);
        }

        private static IEnumerator OpenInvestigationDesk()
        {
            var motor = Object.FindAnyObjectByType<PlayerMotor>();
            motor.Restore(new PlayerStateData { position = new Vector3(62, .08f, 54.6f) });
            Desktop.Open(Station(ComputerCampaign.DetectiveStation));
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(Desktop.ActiveStation, Is.SameAs(Station(ComputerCampaign.DetectiveStation)));
        }

        [UnityTest]
        public IEnumerator LegacyDetectiveSaveRestoresPlaybackAndWindowsBehindNewDesktop()
        {
            yield return EnterGame();
            var campaign = Root.Simulation.ComputerCampaign;
            EarnNeuroarchiveEntry(campaign);
            yield return OpenInvestigationDesk();
            var session = campaign.Investigation;
            Assert.That(session.Open(DetectiveEvidence.CorridorPhoto), Is.True);
            Assert.That(session.Open(DetectiveEvidence.ArchivePlayback), Is.True);
            session.SeekRecording(.8);
            session.SetHighContrast(true);
            session.ChooseDay(17);
            var playback = session.PlaybackTime;
            campaign.SaveDesktop(new ComputerDesktopData
            {
                station = ComputerCampaign.DetectiveStation,
                focusedApp = "detective",
                windows = new[] { new DesktopWindowData { app = "detective" } }
            });
            Assert.That(Desktop.PhysicalDisplay.View.Q<Button>("field-case"), Is.Not.Null);
            Assert.That(Desktop.PhysicalDisplay.View.Q<Button>("desktop-detective"), Is.Null);
            Assert.That(Root.Persistence.Save("manual1"), Is.True);
            var library = new SaveLibrary(WorldSaveController.TestStorageDirectory, null, true);
            Assert.That(library.Read("manual1", out var saved, out var error), Is.True, error);
            var restored = ComputerCampaign.Restore(saved.simulation.computerCampaign);
            Assert.That(restored.Investigation.PlaybackTime, Is.EqualTo(playback).Within(1e-12));
            Assert.That(restored.Investigation.HighContrast, Is.True);
            Assert.That(restored.Investigation.IncidentDay, Is.EqualTo(17));
            Assert.That(restored.Investigation.Selected(DetectiveSource.Photos), Is.EqualTo((int)DetectiveEvidence.CorridorPhoto));
            Assert.That(restored.Desktop(ComputerCampaign.DetectiveStation).focusedApp, Is.EqualTo("detective"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SecondVoiceOpensPhysicalSafeAndArchiveKeyUnlocksTheVault()
        {
            yield return EnterGame();
            var campaign = Root.Simulation.ComputerCampaign;
            EarnNeuroarchiveEntry(campaign);
            foreach (var actor in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None))
                actor.enabled = false;

            var safe = Object.FindAnyObjectByType<NeuroInvestigationController>();
            var gate = FindDoor(ComputerCampaign.DetectiveVault);
            Assert.That(safe, Is.Not.Null);
            Assert.That(safe.HasPhysicalKey || gate.IsUnlocked, Is.False);
            yield return OpenInvestigationDesk();
            yield return SolveFieldCase(2);
            Assert.That(safe.SafeOpen && safe.HasPhysicalKey, Is.True);
            Assert.That(gate.IsUnlocked, Is.False, "Submitting the timeline only releases the key");
            Assert.That(safe.ShownFrame, Is.GreaterThanOrEqualTo(0));
            Desktop.Close();
            var motor = Object.FindAnyObjectByType<PlayerMotor>();
            motor.Restore(new PlayerStateData { position = new Vector3(66.3f, .08f, 47.75f) });
            CollectKeyAtSafe(motor, safe);
            Assert.That(gate.IsUnlocked, Is.True);
            yield return new WaitForSecondsRealtime(2.1f);
            Physics.SyncTransforms();
            var cc = motor.GetComponent<CharacterController>();
            Walk(cc, new Vector3(69, 0, 46.7f));
            Walk(cc, new Vector3(71.2f, 0, 46.7f));
            Walk(cc, new Vector3(71.2f, 0, 38.4f));
            Walk(cc, new Vector3(71.2f, 0, 46.7f));
            Walk(cc, new Vector3(68.4f, 0, 46.7f));

            Assert.That(Root.Persistence.Save("manual1"), Is.True);
            bool restoredKeyState = false;
            void Loaded(Scene scene, LoadSceneMode mode)
            {
                var recovered = Object.FindAnyObjectByType<NeuroInvestigationController>();
                restoredKeyState = recovered.SafeOpen && !recovered.HasPhysicalKey && FindDoor(ComputerCampaign.DetectiveVault).IsUnlocked;
            }
            SceneManager.sceneLoaded += Loaded;
            try
            {
                Root.Persistence.Load("manual1");
                yield return null;
                yield return new WaitForSecondsRealtime(.1f);
            }
            finally
            {
                SceneManager.sceneLoaded -= Loaded;
            }
            Assert.That(restoredKeyState, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        private static void CollectKeyAtSafe(PlayerMotor player, NeuroInvestigationController safe)
        {
            Physics.SyncTransforms();
            Vector3 origin = player.transform.position + Vector3.up * 1.35f;
            Vector3 aim = new Vector3(66.3f, .87f, 49f);
            Assert.That(Physics.Raycast(origin, (aim - origin).normalized, out var hit, 2.4f,
                CollisionLayers.InteractionMask, QueryTriggerInteraction.Ignore), Is.True);
            Assert.That(hit.collider.GetComponentInParent<NeuroInvestigationController>(), Is.SameAs(safe));
            safe.Interact();
            Assert.That(safe.HasPhysicalKey, Is.False);
        }
    }
}
