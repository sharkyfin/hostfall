using System.Collections;
using System.Linq;
using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.UI;
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
        public IEnumerator CrawlerCannonWarnsFreezesOnPauseAndCannotShootThroughCover()
        {
            yield return EnterGame();
            var guardian = ArrangeFreightGuardian(new Vector3(0, 0, 82.6f), new Vector3(0, .04f, 70f));
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            var health = player.GetComponent<PlayerHealth>();
            var combat = guardian.GetComponent<RobotCombatController>();
            float fullHealth = health.CurrentHealth;
            yield return ObserveContainment(() => guardian.AttackPhase == CrawlerAttackPhase.Telegraph, 5,
                () => "Freight guardian failed to choose the first attack");
            Assert.That(guardian.CurrentAttack, Is.EqualTo(CrawlerAttackKind.CannonBurst));
            Assert.That(guardian.GetComponentInChildren<LineRenderer>().enabled, Is.True);
            var ui = Object.FindAnyObjectByType<FacilityUI>().GetComponent<UIDocument>().rootVisualElement;
            var bossBar = ui.Q("guardian-health");
            yield return ObserveContainment(() => bossBar.resolvedStyle.display == DisplayStyle.Flex,
                1, () => "Encounter health bar did not appear");
            Assert.That(bossBar.worldBound.yMax, Is.LessThan(ui.Q("health-badge").worldBound.yMin));

            Controls.SetMode(ControlMode.Paused);
            float remaining = combat.Model.Data.crawlerAttack.remaining;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(combat.Model.Data.crawlerAttack.remaining, Is.EqualTo(remaining));
            Controls.SetMode(ControlMode.Exploring);
            var cover = GuardianTestBarrier(77.1f);
            try
            {
                yield return ObserveContainment(() => guardian.AttackPhase == CrawlerAttackPhase.Recovery, 5,
                    () => "Expected three emitted cannon shots and recovery");
                Assert.That(combat.Model.Data.crawlerAttack.emitted, Is.EqualTo(3));
                Assert.That(health.CurrentHealth, Is.EqualTo(fullHealth));
                var restored = RobotCombatStateForTest(combat.Model);
                Assert.That(restored.crawlerAttack.emitted, Is.EqualTo(3));
                Assert.That(restored.crawlerAttack.phase, Is.EqualTo(CrawlerAttackPhase.Recovery));
            }
            finally { Object.Destroy(cover); }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CrawlerChargeStopsShortOfAnActualWallAfterTelegraph()
        {
            yield return EnterGame();
            var guardian = ArrangeFreightGuardian(new Vector3(0, 0, 78f), new Vector3(0, .04f, 70.8f));
            var combat = guardian.GetComponent<RobotCombatController>();
            yield return ObserveContainment(() => guardian.AttackPhase == CrawlerAttackPhase.Telegraph, 5,
                () => "Freight guardian failed to engage at charge range");
            Assert.That(guardian.CurrentAttack, Is.EqualTo(CrawlerAttackKind.HydraulicCharge));
            var wheel = guardian.GetComponentsInChildren<Transform>().First(t => t.name == "MANTIS wheel");
            Quaternion wheelBefore = wheel.localRotation;
            var wall = GuardianTestBarrier(74.2f);
            try
            {
                yield return ObserveContainment(() => guardian.AttackPhase == CrawlerAttackPhase.Recovery, 5,
                    () => "The blocked charge stayed active");
                Assert.That(guardian.transform.position.z, Is.LessThan(77.9f));
                Assert.That(guardian.transform.position.z, Is.GreaterThan(76f));
                Assert.That(combat.Model.Data.crawlerAttack.remaining, Is.GreaterThan(2.35f));
                Assert.That(combat.Model.Data.crawlerAttack.contact, Is.False);
                Assert.That(Quaternion.Angle(wheel.localRotation, wheelBefore), Is.GreaterThan(1));
            }
            finally { Object.Destroy(wall); }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CrawlerTurretAimsAndRecoilsWithoutTurningTheHull()
        {
            yield return EnterGame();
            var guardian = ArrangeFreightGuardian(new Vector3(0, 0, 82.6f), new Vector3(2.6f, .04f, 70f));
            var rig = guardian.GetComponentsInChildren<SkinnedMeshRenderer>();
            Assert.That(rig.Length, Is.EqualTo(6));
            Assert.That(rig.Sum(part => (int)part.sharedMesh.GetIndexCount(0)), Is.EqualTo(52008));
            var parts = guardian.GetComponentsInChildren<Transform>();
            var turret = parts.Single(t => t.name == "MANTIS turret");
            var recoil = parts.Single(t => t.name == "MANTIS recoil");
            var rest = recoil.localPosition;
            Quaternion hull = guardian.transform.rotation;
            yield return ObserveContainment(() => guardian.AttackPhase == CrawlerAttackPhase.Telegraph, 5,
                () => "Rigged turret did not acquire the off-axis target");
            Assert.That(Quaternion.Angle(hull, guardian.transform.rotation), Is.LessThan(1));
            Assert.That(Quaternion.Angle(turret.rotation, hull), Is.GreaterThan(5));
            yield return ObserveContainment(() => (recoil.localPosition - rest).sqrMagnitude > .005f, 4,
                () => "A cannon pulse did not drive the actual recoil slide");
            Assert.That(Quaternion.Angle(hull, guardian.transform.rotation), Is.LessThan(1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CrawlerSlamShowsItsRadiusAndCanBeDodgedOnFoot()
        {
            yield return EnterGame();
            var guardian = ArrangeFreightGuardian(new Vector3(0, 0, 78f), new Vector3(0, .04f, 74.35f));
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            var health = player.GetComponent<PlayerHealth>();
            float before = health.CurrentHealth;
            yield return ObserveContainment(() => guardian.AttackPhase == CrawlerAttackPhase.Telegraph, 5,
                () => "Freight guardian failed to telegraph the close-range slam");
            Assert.That(guardian.CurrentAttack, Is.EqualTo(CrawlerAttackKind.GroundSlam));
            var guide = guardian.GetComponentInChildren<LineRenderer>();
            Assert.That(guide.enabled && guide.loop && guide.positionCount == 48, Is.True);
            Walk(player.GetComponent<CharacterController>(), new Vector3(-3, 0, 74.35f));
            yield return ObserveContainment(() => guardian.AttackPhase == CrawlerAttackPhase.Recovery, 5,
                () => "The slam did not resolve");
            Assert.That(health.CurrentHealth, Is.EqualTo(before));
            LogAssert.NoUnexpectedReceived();
        }

        private static CrawlerMinibossController ArrangeFreightGuardian(Vector3 guardianPosition, Vector3 playerPosition)
        {
            var sentry = Object.FindObjectsByType<TrackedSentryController>()
                .Single(robot => robot.DamageableId == "crawler-logistics-r35");
            foreach (var actor in Object.FindObjectsByType<MonoBehaviour>()
                .Where(component => component is IDamageable && component is not PlayerHealth && component != sentry))
            {
                actor.enabled = false;
                foreach (var collider in actor.GetComponentsInChildren<Collider>())
                    collider.enabled = false;
            }
            sentry.transform.SetPositionAndRotation(guardianPosition, Quaternion.LookRotation(Vector3.back));
            var player = Object.FindAnyObjectByType<PlayerMotor>();
            player.Restore(new PlayerStateData { position = playerPosition, yaw = 0 });
            player.enabled = false;
            Physics.SyncTransforms();
            return sentry.GetComponent<CrawlerMinibossController>();
        }

        private static GameObject GuardianTestBarrier(float z)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Guardian test cover";
            wall.transform.position = new Vector3(0, 1.5f, z);
            wall.transform.localScale = new Vector3(5, 3, .20f);
            Physics.SyncTransforms();
            return wall;
        }

        private static RobotCombatData RobotCombatStateForTest(RobotCombatState original)
        {
            var loaded = new RobotCombatState(original.Id, true, original.Maximum);
            loaded.Restore(original.Capture());
            return loaded.Data;
        }
    }
}
