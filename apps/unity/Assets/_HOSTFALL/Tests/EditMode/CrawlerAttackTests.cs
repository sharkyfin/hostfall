using System;
using System.Collections.Generic;
using Hostfall.Simulation;
using NUnit.Framework;

namespace Hostfall.Tests
{
    public sealed class CrawlerAttackTests
    {
        [Test]
        public void SavingInCannonBurstKeepsCommittedAimAndDoesNotRepeatShots()
        {
            var robot = new RobotCombatState("guardian", true, 780);
            var original = new CrawlerAttack(robot.Data.crawlerAttack = new CrawlerAttackData());
            Assert.That(original.Commit(CrawlerAttackKind.CannonBurst, 247, -6, false), Is.True);
            var shots = new List<int>();
            for (int i = 0; i < 8; i++) original.Step(.1f, shots.Add, _ => Assert.Fail());
            Assert.That(shots, Is.Empty);
            for (int i = 0; i < 8 && shots.Count == 0; i++) original.Step(.1f, shots.Add, _ => Assert.Fail());
            Assert.That(shots, Is.EqualTo(new[] { 0 }));
            var clone = new RobotCombatState("guardian", true, 780);
            clone.Restore(robot.Capture());
            var resumed = new CrawlerAttack(clone.Data.crawlerAttack);
            Assert.That(resumed.Heading, Is.EqualTo(247));
            Assert.That(resumed.Pitch, Is.EqualTo(-6));
            shots.Clear();
            for (int i = 0; i < 40; i++) resumed.Step(.1f, shots.Add, _ => Assert.Fail());
            Assert.That(shots, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(resumed.Phase, Is.EqualTo(CrawlerAttackPhase.Idle));
            Assert.That(robot.Data.crawlerAttack.emitted, Is.EqualTo(1));
        }

        [Test]
        public void ChargeContactCannotRepeatAndBlockedTracksNeedLongerRecovery()
        {
            var data = new CrawlerAttackData();
            var attack = new CrawlerAttack(data);
            Assert.That(attack.Select(7, 1, false), Is.EqualTo(CrawlerAttackKind.HydraulicCharge));
            Assert.That(attack.Commit(CrawlerAttackKind.HydraulicCharge, 0, 0, false), Is.True);
            for (int i = 0; i < 14 && attack.Phase != CrawlerAttackPhase.Active; i++)
                attack.Step(.1f, _ => Assert.Fail(), _ => Assert.Fail());
            Assert.That(attack.Phase, Is.EqualTo(CrawlerAttackPhase.Active));
            attack.Step(.04f, _ => Assert.Fail(), time =>
            {
                Assert.That(time, Is.EqualTo(.04f));
                Assert.That(attack.SpendContact(), Is.True);
                Assert.That(attack.SpendContact(), Is.False);
                attack.StopCharge(hitWall: true);
            });
            Assert.That(data.phase, Is.EqualTo(CrawlerAttackPhase.Recovery));
            Assert.That(data.remaining, Is.EqualTo(2.6f));
            Assert.That(attack.Select(7, .38f, true), Is.EqualTo(CrawlerAttackKind.Suppression));
            data.remaining = float.PositiveInfinity;
            Assert.Throws<ArgumentException>(() => CrawlerAttack.Validate(data));
        }

        [TestCase(CrawlerAttackKind.GroundSlam, false, 1)]
        [TestCase(CrawlerAttackKind.Suppression, false, 4)]
        [TestCase(CrawlerAttackKind.Suppression, true, 5)]
        public void SlamAndSuppressionHaveFiniteImpactWindows(CrawlerAttackKind kind, bool overheated, int expected)
        {
            var attack = new CrawlerAttack(new CrawlerAttackData());
            Assert.That(attack.Commit(kind, 0, 0, overheated), Is.True);
            int pulses = 0;
            for (int i = 0; i < 65; i++) attack.Step(.1f, _ => pulses++, _ => Assert.Fail());
            Assert.That(attack.Phase, Is.EqualTo(CrawlerAttackPhase.Idle));
            Assert.That(pulses, Is.EqualTo(expected));
        }

        [Test]
        public void OverheatExposesCoreWithOneCoolantGoneWithoutChangingLegacyDamage()
        {
            var combat = new RobotCombatState("guardian", true, 780);
            var legacy = new RobotCombatState("legacy", true, 780);
            combat.Data.crawlerAttack = new CrawlerAttackData { version = CrawlerAttack.SaveVersion };
            foreach (var model in new[] { combat, legacy })
            {
                model.ApplyHit(4000, RobotHitRegion.Hull);
                Assert.That(model.CoreExposed, Is.False);
                model.ApplyHit(140, RobotHitRegion.LeftCoolant);
            }
            Assert.That(combat.CoreExposed, Is.True);
            Assert.That(legacy.CoreExposed, Is.False);
            combat.ApplyHit(75, RobotHitRegion.Core);
            Assert.That(combat.IsDead, Is.True);
        }

        [Test]
        public void EmptyLegacyAttackRecordStaysAbsentAfterRestore()
        {
            var normal = new RobotCombatState("watcher", false, 260);
            var snapshot = normal.Capture();
            // Unity's serializer materializes an empty object for inline null fields.
            snapshot.crawlerAttack = new CrawlerAttackData();
            normal.Restore(snapshot);
            Assert.That(normal.Data.crawlerAttack, Is.Null);
            snapshot.crawlerAttack.version = CrawlerAttack.SaveVersion;
            Assert.Throws<ArgumentException>(() => normal.Restore(snapshot));
        }
    }
}
