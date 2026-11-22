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
        private static void EarnSecurityEntry(ComputerCampaign campaign)
        {
            CalibrateLogisticsEntry(campaign);
            for (int frame = 0; frame < 24000 && campaign.Payload.Phase != PayloadPhase.Complete; frame++)
            {
                campaign.BeginPayload();
                DrivePayload(campaign.Payload);
            }
            Assert.That(campaign.MinefieldAccessible, Is.True);
        }

        private static FacilityDoor FindDoor(string id) =>
            Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).Single(d => d.DoorId == id);

        [UnityTest]
        public IEnumerator LegacyFlagDisarmsItsPhysicalCellAndRestoresIt()
        {
            yield return EnterGame();
            var campaign = Root.Simulation.ComputerCampaign;
            var floor = Object.FindAnyObjectByType<MinefieldFloorController>();
            Assert.That(floor, Is.Not.Null);
            Assert.That(floor.Entry.IsUnlocked, Is.False);
            EarnSecurityEntry(campaign);
            Assert.That(floor.Entry.IsUnlocked, Is.True);
            var motor = Object.FindAnyObjectByType<PlayerMotor>();
            motor.Restore(new PlayerStateData { position = new Vector3(48, .08f, 23.5f) });

            Desktop.Open(Station(ComputerCampaign.MinefieldStation));
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(campaign.BeginMinefield(), Is.True);
            Assert.That(floor.Sensor(11).enabled && floor.Beam(11).enabled, Is.True);
            Assert.That(floor.Sensor(11).transform.position.x, Is.EqualTo(51.25f).Within(.01f));
            Assert.That(floor.Sensor(11).transform.position.z, Is.EqualTo(28.35f).Within(.01f));

            Assert.That(campaign.Minefield.ToggleFlag(11), Is.True);
            Assert.That(campaign.Minefield.IsFlagged(11), Is.True);
            Assert.That(floor.Sensor(11).enabled || floor.Beam(11).enabled, Is.False);
            Assert.That(floor.Sensor(31).enabled && floor.Beam(31).enabled, Is.True);

            Assert.That(Root.Persistence.Save("manual1"), Is.True);
            Desktop.Close();
            bool observed = false;
            void Loaded(Scene scene, LoadSceneMode mode)
            {
                var restored = Object.FindAnyObjectByType<MinefieldFloorController>();
                observed = Root.Simulation.ComputerCampaign.Minefield.IsFlagged(11) &&
                    !restored.Sensor(11).enabled && !restored.Beam(11).enabled && restored.Sensor(31).enabled;
            }
            SceneManager.sceneLoaded += Loaded;
            try
            {
                Root.Persistence.Load("manual1");
                yield return null;
                yield return new WaitForSecondsRealtime(.12f);
            }
            finally
            {
                SceneManager.sceneLoaded -= Loaded;
            }
            Assert.That(observed, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator FloorSensorTriggersLocalAlarmThenSolvedRouteReachesNeuroarchive()
        {
            yield return EnterGame();
            var campaign = Root.Simulation.ComputerCampaign;
            EarnSecurityEntry(campaign);
            foreach (var actor in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None))
                actor.enabled = false;

            var floor = Object.FindAnyObjectByType<MinefieldFloorController>();
            var motor = Object.FindAnyObjectByType<PlayerMotor>();
            var health = motor.GetComponent<PlayerHealth>();
            var cc = motor.GetComponent<CharacterController>();
            var exit = FindDoor(ComputerCampaign.MinefieldExit);
            var passage = FindDoor(ComputerCampaign.MinefieldNeuroPass);
            Assert.That(exit.IsUnlocked || passage.IsUnlocked, Is.False);
            var position = floor.Sensor(11).transform.position + Vector3.up * .08f;
            motor.Restore(new PlayerStateData { position = position });
            float before = health.CurrentHealth;
            yield return null;
            yield return null;
            Assert.That(health.CurrentHealth, Is.LessThan(before));
            Assert.That(campaign.Minefield.AlarmRemaining, Is.GreaterThan(0));

            motor.Restore(new PlayerStateData { position = new Vector3(47.35f, .08f, 27.05f) });
            for (int i = 0; i < 24; i++)
                campaign.AdvanceWorld(.25);
            var board = campaign.Minefield;
            for (int level = 0; level < MinefieldSession.LevelCount; level++)
            {
                Assert.That(campaign.BeginMinefield(), Is.True);
                // Physical integration fixture. The independent Edit Mode solver proves deduction.
                var mines = Enumerable.Range(0, MinefieldSession.CellCount).Where(board.IsArmed).ToArray();
                foreach (int cell in mines)
                    board.ToggleFlag(cell);
                for (int cell = 0; cell < MinefieldSession.CellCount && board.Stage == level; cell++)
                    if (!mines.Contains(cell))
                        board.Reveal(cell);
            }
            Assert.That(board.Phase, Is.EqualTo(MinefieldPhase.Complete));
            Assert.That(exit.IsUnlocked && passage.IsUnlocked, Is.True);
            Assert.That(Enumerable.Range(0, MinefieldSession.CellCount)
                .All(cell => !floor.Sensor(cell).enabled && !floor.Beam(cell).enabled), Is.True);
            yield return new WaitForSecondsRealtime(1.8f);
            Physics.SyncTransforms();

            Walk(cc, new Vector3(47.35f, 0, 36.5f));
            Walk(cc, new Vector3(47.35f, 0, 43f));
            Walk(cc, new Vector3(56f, 0, 43f));
            Walk(cc, new Vector3(60f, 0, 52.5f));
            Walk(cc, new Vector3(56f, 0, 43f));
            Walk(cc, new Vector3(47.35f, 0, 43f));
            Walk(cc, new Vector3(47.35f, 0, 27.05f));
            Assert.That(campaign.Home.TryRead(ComputerCampaign.MinefieldIncidentPath, out _), Is.True);
            Assert.That(Root.Persistence.Save("manual1"), Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
