using System.Collections;
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
        [UnityTest, Timeout(120000)]
        public IEnumerator CoreAvatarUsesPhysicalWeakPointsAndOnlyDefeatUnlocksTrust()
        {
            yield return EnterGame();
            var avatar = Object.FindAnyObjectByType<CoreAvatarEncounter>();
            Assert.That(avatar, Is.Not.Null);
            var trust = FindDoor("door.sec10.trust");
            Assert.That(trust.IsUnlocked, Is.False);
            yield return FinishHomeRecovery();

            var player = Object.FindAnyObjectByType<PlayerMotor>();
            player.Restore(new PlayerStateData { position = new Vector3(10, .04f, 136), yaw = 0 });
            player.enabled = false;
            yield return FightCoreAndReachEpilogue(player);
        }

        private IEnumerator FightCoreAndReachEpilogue(PlayerMotor player)
        {
            var avatar = Object.FindAnyObjectByType<CoreAvatarEncounter>();
            var trust = FindDoor("door.sec10.trust");
            var health = player.GetComponent<PlayerHealth>();
            health.ResetHealth();
            Physics.SyncTransforms();
            var battle = avatar.Battle;
            yield return ObserveContainment(() => battle.Clock > .55f, 3, () => "The real combat did not arm");
            Assert.That(battle.Attack, Is.EqualTo(CoreAvatarAttack.Beam));

            var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                cover.transform.position = new Vector3(12.6f, 1.15f, 143.2f);
                cover.transform.localScale = new Vector3(3.4f, 2.3f, .45f);
                Physics.SyncTransforms();
                yield return ObserveContainment(() => battle.GuardOpen, 3, () => "The beam never fired");
                Assert.That(health.CurrentHealth, Is.EqualTo(100), "Solid cover must block the energy beam");
            }
            finally { Object.Destroy(cover); }
            yield return null;

            var reactor = GameObject.Find("Low reactor lens / exposed during recovery").transform;
            var relay = GameObject.Find("Power relay / target under field protection").transform;
            HitCorePart(reactor, 120);
            Assert.That(battle.Phase, Is.EqualTo(2));
            var riser = GameObject.Find("Core floor riser 1").transform;
            yield return ObserveContainment(() => riser.position.y > -.3f, 3, () => "A real cover barrier never rose");
            Assert.That(trust.IsUnlocked, Is.False);
            HitCorePart(relay, 170);
            Assert.That(battle.Phase, Is.EqualTo(3));
            Assert.That(battle.Attack, Is.EqualTo(CoreAvatarAttack.Rush));
            Assert.That(trust.IsUnlocked, Is.False);
            var oppositeRiser = GameObject.Find("Core floor riser 2").transform;
            yield return ObserveContainment(() => Mathf.Abs(oppositeRiser.position.y - riser.position.y) > .82f,
                2.4f, () => "The last phase did not move physical cover to one side");
            Assert.That(Root.Persistence.Save("manual2"), Is.True);
            HitCorePart(relay, 250);
            Assert.That(avatar.IsDestroyed, Is.True);
            Assert.That(trust.IsUnlocked, Is.True);
            Assert.That(Root.Persistence.Save("manual1"), Is.True);
            Assert.That(new SaveLibrary(storage, null, true).Read("manual1", out var saved, out var error), Is.True, error);
            Assert.That(ComputerCampaign.Restore(saved.simulation.computerCampaign).CoreAvatar.Defeated, Is.True);
            var terminal = Object.FindAnyObjectByType<CoreRevealSequence>();
            var exit = FindDoor("door.sec10.evac");
            Assert.That(exit.IsUnlocked, Is.False);
            Assert.That(terminal, Is.Not.Null);
            var capsule = player.GetComponent<CharacterController>();
            WalkRoute(capsule, (19, 136), (19, 168), (10, 171), (10, 175.5f));
            yield return OpenRouteDoor(capsule, trust.DoorId);
            WalkRoute(capsule, (10, 183.35f));
            terminal.Interact();
            Assert.That(Controls.Mode, Is.EqualTo(ControlMode.Cinematic));
            Assert.That(terminal.Playing, Is.True);
            yield return new WaitForSecondsRealtime(5.1f);
            Assert.That(terminal.Playing, Is.True);
            Assert.That(exit.IsUnlocked, Is.False);
            yield return ObserveContainment(() => !terminal.Playing, 11, () => "The closing shot did not finish");
            Assert.That(exit.IsUnlocked, Is.True);
            Assert.That(battle.RevealSeen, Is.True);
            Assert.That(Controls.Mode, Is.EqualTo(ControlMode.Exploring));
            Assert.That(Object.FindAnyObjectByType<WatcherController>().enabled, Is.False);
            yield return ObserveContainment(() => exit.FullyOpen, 12, () => "Evacuation door did not reach its open position");
            WalkRoute(capsule, (11.65f, 183.35f), (11.65f, 187), (10, 189), (10, 193), (11.9f, 195.9f), (11.9f, 201));
            Assert.That(Root.Persistence.Save("manual3"), Is.True);
            Root.Persistence.Load("manual3");
            yield return null;
            yield return null;
            Assert.That(Root.Simulation.ComputerCampaign.CoreAvatar.RevealSeen, Is.True);
            Assert.That(FindDoor("door.sec10.evac").IsUnlocked, Is.True);
            Assert.That(Object.FindAnyObjectByType<CoreRevealSequence>().Playing, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        private static void HitCorePart(Transform target, float damage)
        {
            var origin = target.position + Vector3.back * 6;
            Assert.That(RobotShot.Fire(new Ray(origin, (target.position - origin).normalized), 8, damage, out var hit), Is.True);
            Assert.That(hit.collider.transform, Is.EqualTo(target), hit.collider.name);
        }
    }
}
