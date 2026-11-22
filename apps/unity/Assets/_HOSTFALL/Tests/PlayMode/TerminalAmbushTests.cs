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
using Object = UnityEngine.Object;

namespace Hostfall.Tests
{
    public sealed partial class ComputerWorldTests
    {
        [UnityTest] public IEnumerator BufferAmbushReleasesTheScreenBeforeItsPhysicalStrikeAndCanBeParried()
        {
            yield return EnterGame();
            var campaign = Root.Simulation.ComputerCampaign;
            CompleteFirstComputer(campaign);
            foreach (var actor in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None)) actor.enabled = false;
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            var health = player.GetComponent<PlayerHealth>();
            var weapon = player.GetComponent<PlayerWeapon>();
            var ambush = player.GetComponent<TerminalAmbush>();
            var watcher = Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).Single(enemy => enemy.Id == "watcher-r17");
            player.Restore(new PlayerStateData { position = new Vector3(.05f, .08f, 4.04f), yaw = 90 });
            watcher.transform.SetPositionAndRotation(new Vector3(-.16f, 0, 3.02f), Quaternion.identity);
            Physics.SyncTransforms();
            var station = Station(ComputerCampaign.BufferStation);
            Desktop.Open(station);
            yield return new WaitForSecondsRealtime(.62f);
            Assert.That(ambush.OnContact(watcher, true), Is.True);
            Assert.That(Desktop.ActiveStation, Is.Null);
            Assert.That(Controls.Mode, Is.EqualTo(ControlMode.Exploring));
            Assert.That(health.CurrentHealth, Is.EqualTo(100));
            Desktop.Open(station);
            Assert.That(Desktop.ActiveStation, Is.Null, "Reopening the screen cannot bypass the leap");
            yield return new WaitForSeconds(.63f);
            Assert.That(ambush.Busy, Is.True, "The robot should still be in the close-contact counter window");
            Assert.That(health.CurrentHealth, Is.EqualTo(100), "The player can shove the robot away before taking the bite");
            var grapplePosition = watcher.transform.position;
            Assert.That(weapon.TryMelee(), Is.True);
            yield return ObserveContainment(() => !ambush.Busy, 2, () => "Parry did not finish the ambush");
            Assert.That(health.CurrentHealth, Is.EqualTo(100));
            Assert.That(Vector3.Distance(grapplePosition, watcher.transform.position), Is.GreaterThan(.2f),
                "The counter should physically displace the attacker");
            Assert.That(watcher.IsStunned, Is.True);
            Assert.That(weapon.TryCounter(watcher.transform.position), Is.False);
            var saved = ComputerCampaign.Restore(campaign.Capture());
            Assert.That(saved.TrySpendTerminalAmbush(ComputerCampaign.BufferStation), Is.False);
            Assert.That(saved.TrySpendTerminalAmbush(ComputerCampaign.SnakeStation), Is.False);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
