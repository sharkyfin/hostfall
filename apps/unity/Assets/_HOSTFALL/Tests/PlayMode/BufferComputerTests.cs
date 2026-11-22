using System.Collections;
using System.Collections.Generic;
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
        [UnityTest]
        public IEnumerator BufferRecordsResumeFromPrivateSaveAndCabinetRewardsOnlyOnce()
        {
            yield return EnterGame();
            var campaign = Root.Simulation.ComputerCampaign;
            var motor = Object.FindAnyObjectByType<PlayerMotor>();
            var cabinet = FindDoor(ComputerCampaign.BufferCache);
            var kit = Object.FindAnyObjectByType<BufferCachePickup>();
            Assert.That(kit, Is.Not.Null);
            foreach (var actor in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None)) actor.enabled = false;

            motor.Restore(new PlayerStateData { position = new Vector3(.05f, .08f, 4.04f), yaw = 90 });
            Desktop.Open(Station(ComputerCampaign.BufferStation));
            yield return new WaitForSecondsRealtime(.7f);
            var view = Desktop.PhysicalDisplay.View;
            Assert.That(view.Q<Button>("field-files"), Is.Not.Null);
            Assert.That(view.Q<Button>("field-case"), Is.Null);
            Assert.That(view.Q<Button>("field-knowledge"), Is.Null);
            Assert.That(campaign.ShowBuffer(), Is.False);
            Assert.That(cabinet.IsUnlocked, Is.False);

            CompleteFirstComputer(campaign);
            var session = campaign.Buffer;
            Assert.That(campaign.ShowBuffer(), Is.True);
            session.Advance(.1);
            var privateClock = session.Capture().watchTime;
            yield return new WaitForSecondsRealtime(.26f);
            Assert.That(session.Capture().watchTime, Is.EqualTo(privateClock));
            Assert.That(Root.Persistence.Save("manual1"), Is.True);
            var library = new SaveLibrary(WorldSaveController.TestStorageDirectory, null, true);
            Assert.That(library.Read("manual1", out var saved, out var error), Is.True, error);
            var partial = ComputerCampaign.Restore(saved.simulation.computerCampaign);
            Assert.That(partial.Buffer.Phase, Is.EqualTo(BufferPhase.Observe));
            Assert.That(partial.Buffer.Capture().watchTime, Is.EqualTo(privateClock).Within(1e-12));

            // Drive the archived save model without mounting its retired UI.
            for (int stage = 0; stage < BufferRecallSession.StageCount; stage++)
            {
                if (session.Phase != BufferPhase.Observe)
                    Assert.That(campaign.ShowBuffer(), Is.True);
                var cells = new List<int>();
                while (session.Phase == BufferPhase.Observe)
                {
                    if (session.Position == cells.Count && session.Primary >= 0)
                        cells.Add(session.Primary);
                    session.Advance(.05);
                }
                Assert.That(cells.Count, Is.EqualTo(session.Size));
                foreach (int cell in cells) Assert.That(session.Append(cell), Is.True);
                Assert.That(session.Confirm(), Is.True);
            }

            Assert.That(cabinet.IsUnlocked, Is.True);
            Assert.That(campaign.Actions.Count(a => a.target == ComputerCampaign.BufferCache), Is.EqualTo(1));
            Assert.That(campaign.Home.TryRead(ComputerCampaign.BufferLog, out _), Is.True);
            Desktop.Close();
            yield return new WaitForSecondsRealtime(.75f);

            var sentry = Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None).Single(actor => actor.Id == "watcher-r17");
            sentry.GetComponent<RobotCombatController>().Hit(10000, RobotHitRegion.Core, sentry.transform.position);
            Assert.That(sentry.IsDestroyed, Is.True);
            yield return null;
            var capsule = motor.GetComponent<CharacterController>();
            Walk(capsule, new Vector3(0, 0, 5.8f));
            Walk(capsule, new Vector3(0, 0, 4.22f));
            Walk(capsule, new Vector3(0, 0, 5.22f));
            var health = motor.GetComponent<PlayerHealth>();
            kit.Interact();
            Assert.That(session.SupplyClaimed, Is.False, "Full health does not consume the reserved package");
            health.TakeDamage(50);
            Physics.SyncTransforms();
            Vector3 direction = (kit.transform.position - motor.Eye).normalized;
            Assert.That(Physics.Raycast(motor.Eye, direction, out var hit, 2f, CollisionLayers.InteractionMask, QueryTriggerInteraction.Ignore), Is.True);
            Assert.That(hit.collider.GetComponentInParent<BufferCachePickup>(), Is.SameAs(kit));
            kit.Interact();
            Assert.That(health.CurrentHealth, Is.EqualTo(85));
            Assert.That(session.SupplyClaimed && !kit.gameObject.activeSelf, Is.True);

            Assert.That(Root.Persistence.Save("manual1"), Is.True);
            bool restoredClaim = false;
            void Loaded(Scene _, LoadSceneMode mode)
            {
                var replacement = Object.FindAnyObjectByType<BufferCachePickup>(FindObjectsInactive.Include);
                restoredClaim = Root.Simulation.ComputerCampaign.Buffer.SupplyClaimed && replacement != null && !replacement.gameObject.activeSelf;
            }
            SceneManager.sceneLoaded += Loaded;
            try
            {
                Root.Persistence.Load("manual1");
                yield return null;
                yield return new WaitForSecondsRealtime(.1f);
            }
            finally { SceneManager.sceneLoaded -= Loaded; }
            Assert.That(restoredClaim, Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
