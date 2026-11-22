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
        [UnityTest]
        public IEnumerator ArmoryUtilityRequiresCameraClearanceAndUnlocksExistingArmory()
        {
            yield return EnterGame();
            var campaign = Root.Simulation.ComputerCampaign;
            foreach (var actor in Object.FindObjectsByType<WatcherController>(FindObjectsSortMode.None)) actor.enabled = false;
            var gate = FindDoor(ComputerCampaign.FragmentArmory);
            var motor = Object.FindAnyObjectByType<PlayerMotor>();
            Assert.That(gate.IsUnlocked, Is.False);
            motor.Restore(new PlayerStateData { position = new Vector3(0, .08f, 24.4f) });
            Desktop.Open(Station(ComputerCampaign.FragmentStation));
            yield return new WaitForSecondsRealtime(.7f);
            yield return RunFieldUtility(FieldUtilityId.ArmoryPermit);
            Assert.That(campaign.ArmoryAccessible || gate.IsUnlocked, Is.False);
            ReachEnergyComputer(campaign);
            yield return RunFieldUtility(FieldUtilityId.ArmoryPermit);
            yield return RunFieldUtility(FieldUtilityId.ArmoryPermit);
            Assert.That(campaign.ArmoryAccessible, Is.True);
            Assert.That(gate.IsUnlocked, Is.True);
            Assert.That(campaign.Actions.Count(a => a.target == ComputerCampaign.FragmentArmory), Is.EqualTo(1));
            Assert.That(campaign.Home.TryRead(ComputerCampaign.FragmentLog, out _), Is.True);

            Desktop.Close();
            var capsule = motor.GetComponent<CharacterController>();
            Walk(capsule, new Vector3(2.15f, 0, 24.4f));
            Walk(capsule, new Vector3(2.15f, 0, 26.67f));
            Walk(capsule, new Vector3(6.3f, 0, 26.67f));
            Walk(capsule, new Vector3(6.3f, 0, 26));
            gate.Interact();
            yield return new WaitForSecondsRealtime(2.05f);
            Physics.SyncTransforms();
            Walk(capsule, new Vector3(9.2f, 0, 26));
            Walk(capsule, new Vector3(6.3f, 0, 26));
            Assert.That(Root.Persistence.Save("manual1"), Is.True);
            bool reloadPassed = false;
            void Loaded(Scene _, LoadSceneMode mode)
            {
                reloadPassed = Root.Simulation.ComputerCampaign.ArmoryAccessible &&
                    FindDoor(ComputerCampaign.FragmentArmory).IsUnlocked;
            }
            SceneManager.sceneLoaded += Loaded;
            try
            {
                Root.Persistence.Load("manual1");
                yield return null;
                yield return new WaitForSecondsRealtime(.1f);
            }
            finally { SceneManager.sceneLoaded -= Loaded; }
            Assert.That(reloadPassed, Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
