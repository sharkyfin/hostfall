using System.Collections;
using System.Linq;
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
        [UnityTest]
        public IEnumerator FirmwareAuditRestoresProbeAndTurnsOnTheThreeArenaMarkers()
        {
            yield return EnterGame();
            var campaign = Root.Simulation.ComputerCampaign;
            var motor = Object.FindAnyObjectByType<PlayerMotor>();
            var hardware = Object.FindAnyObjectByType<IntegrityTelemetryController>();
            Assert.That(hardware, Is.Not.Null);
            Assert.That(campaign.BeginIntegrity(), Is.False);
            Assert.That(hardware.VisibleMarkers, Is.Zero);
            foreach (var watcher in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None)) watcher.enabled = false;
            yield return CompletePhysicalQuarantine(campaign, motor);

            var station = Station(ComputerCampaign.IntegrityStation);
            Assert.That(station.transform.position.z, Is.LessThan(126f));
            Assert.That(FindDoor("door.sec10.super").IsUnlocked, Is.False);
            motor.Restore(new PlayerStateData { position = new Vector3(30, .08f, 121.8f) });
            Physics.SyncTransforms();
            Walk(motor.GetComponent<CharacterController>(), new Vector3(34.27f, 0, 121.8f));
            Desktop.Open(station);
            yield return new WaitForSecondsRealtime(.7f);
            var view = Desktop.PhysicalDisplay.View;
            Assert.That(view.Q<Button>("field-files"), Is.Not.Null);
            Assert.That(view.Q<Button>("field-case"), Is.Null);
            var audit = campaign.Integrity;
            for (int bank = 0; bank < IntegrityAuditSession.StageCount; bank++)
            {
                Assert.That(campaign.BeginIntegrity(), Is.True);
                for (int pass = 0; pass < 5 && !audit.CanIsolate; pass++)
                {
                    var remaining = Enumerable.Range(0, IntegrityAuditSession.PageCount).Where(audit.IsCandidate).ToArray();
                    audit.SelectPage(remaining[0]);
                    audit.SelectPage(remaining[remaining.Length / 2 - 1]);
                    Assert.That(audit.Probe(), Is.True);
                    Assert.That(audit.Reports.Count, Is.EqualTo(pass + 1));
                    if (bank == 0 && pass == 0)
                    {
                        Assert.That(Root.Persistence.Save("manual1"), Is.True);
                        var library = new SaveLibrary(WorldSaveController.TestStorageDirectory, null, true);
                        Assert.That(library.Read("manual1", out var saved, out var error), Is.True, error);
                        var restored = ComputerCampaign.Restore(saved.simulation.computerCampaign);
                        Assert.That(restored.Integrity.Reports.Count, Is.EqualTo(1));
                        Assert.That(restored.Integrity.Reports.Single().mismatch, Is.True);
                        Assert.That(restored.Integrity.IsCandidate(16), Is.False);
                    }
                }
                Assert.That(audit.CanIsolate, Is.True);
                Assert.That(audit.Isolate(), Is.True);
            }
            Assert.That(audit.Complete && hardware.Active, Is.True);
            Assert.That(hardware.VisibleMarkers, Is.EqualTo(3));
            Assert.That(campaign.Actions.Count(action => action.target == ComputerCampaign.IntegrityTarget), Is.EqualTo(1));
            Assert.That(campaign.Home.TryRead(ComputerCampaign.IntegrityLog, out _), Is.True);
            Assert.That(FindDoor("door.sec10.super").IsUnlocked, Is.False);
            Desktop.Close();

            Assert.That(Root.Persistence.Save("manual1"), Is.True);
            bool restoredTelemetry = false;
            void Loaded(Scene _, LoadSceneMode mode)
            {
                restoredTelemetry = Root.Simulation.ComputerCampaign.Integrity.Complete &&
                    Object.FindAnyObjectByType<IntegrityTelemetryController>().VisibleMarkers == 3;
            }
            SceneManager.sceneLoaded += Loaded;
            try
            {
                Root.Persistence.Load("manual1");
                yield return null;
                yield return new WaitForSecondsRealtime(.1f);
            }
            finally { SceneManager.sceneLoaded -= Loaded; }
            Assert.That(restoredTelemetry, Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
