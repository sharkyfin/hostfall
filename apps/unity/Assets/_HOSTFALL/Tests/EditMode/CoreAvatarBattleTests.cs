using System;
using Hostfall.Simulation.Computers;
using NUnit.Framework;

namespace Hostfall.Tests
{
    public sealed class CoreAvatarBattleTests
    {
        [Test]
        public void AimLocksAndRestoredDischargeNeverRepeats()
        {
            var battle = new CoreAvatarBattle();
            Assert.That(battle.Begin(7, 136), Is.True);
            for (int i = 0; i < 4; i++) battle.Step(.125f, 8, 137, _ => Assert.Fail());
            Assert.That(battle.TargetX, Is.EqualTo(8));
            int shots = 0;
            for (int i = 0; i < 8; i++) battle.Step(.125f, 11, 138, attack =>
            {
                Assert.That(attack, Is.EqualTo(CoreAvatarAttack.Beam));
                shots++;
            });
            Assert.That(shots, Is.EqualTo(1));
            Assert.That(battle.TargetX, Is.EqualTo(8));
            Assert.That(battle.GuardOpen, Is.True);
            var resumed = CoreAvatarBattle.Restore(battle.Capture(), true);
            for (int i = 0; i < 16; i++) resumed.Step(.125f, 7, 136, _ => Assert.Fail());
            Assert.That(resumed.Attack, Is.EqualTo(CoreAvatarAttack.TwinBeam));
            Assert.That(resumed.Clock, Is.LessThan(resumed.FireAt));
        }

        [Test]
        public void ArmorReactorAndRelaysMatterAcrossPhasesAndEmpCancelsOneAttack()
        {
            var battle = new CoreAvatarBattle();
            Assert.That(battle.Hit(100, CoreAvatarPart.Reactor, 0), Is.False);
            battle.Begin(3, 140);
            Assert.That(battle.Interrupt(), Is.True);
            Assert.That(battle.Interrupt(), Is.False);
            battle.Hit(120, CoreAvatarPart.Reactor, 0);
            Assert.That(battle.Phase, Is.EqualTo(2));
            Assert.That(battle.Attack, Is.EqualTo(CoreAvatarAttack.FieldPulse));
            float previous = battle.Health;
            battle.Hit(100, CoreAvatarPart.Shell, 0);
            Assert.That(previous - battle.Health, Is.EqualTo(12).Within(.01f));
            battle.Hit(165, CoreAvatarPart.Relay, 0);
            Assert.That(battle.Phase, Is.EqualTo(3));
            Assert.That(battle.Attack, Is.EqualTo(CoreAvatarAttack.Rush));
            Assert.That(battle.CompleteReveal(), Is.False);
            battle.Hit(300, CoreAvatarPart.Relay, 0);
            Assert.That(battle.Defeated, Is.True);
            Assert.That(battle.CompleteReveal(), Is.True);
            Assert.That(battle.CompleteReveal(), Is.False);
            Assert.That(CoreAvatarBattle.Restore(battle.Capture(), true).RevealSeen, Is.True);
            Assert.Throws<ArgumentException>(() => CoreAvatarBattle.Restore(battle.Capture(), false));
            Assert.That(CoreAvatarBattle.Restore(new CoreAvatarBattleData(), false).Health, Is.EqualTo(CoreAvatarBattle.Maximum));
        }
    }
}
