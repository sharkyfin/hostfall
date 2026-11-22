using System;
using System.Collections.Generic;
using Hostfall.Simulation.Computers;
using NUnit.Framework;

namespace Hostfall.Tests
{
    public sealed class FacilityMinibossTests
    {
        [Test]
        public void EachBossKeepsDifferentAttacksAndAStableClockThroughReentry()
        {
            var seen = new HashSet<MinibossMove>();
            foreach (FacilityMinibossKind kind in Enum.GetValues(typeof(FacilityMinibossKind)))
            {
                var boss = new FacilityMinibossState(kind);
                var emitted = new HashSet<MinibossMove>();
                for (int i = 0; i < 100; i++)
                {
                    boss.Step(.2f, 10, 30, move => emitted.Add(move));
                    if (i == 9) boss = FacilityMinibossState.Restore(boss.Capture(), kind);
                }
                Assert.That(emitted.Count, Is.EqualTo(kind == FacilityMinibossKind.SecurityWarden ? 5 : 4));
                foreach (var move in emitted) Assert.That(seen.Add(move), Is.True, move.ToString());
            }
        }

        [Test]
        public void ShieldDisconnectAndRepairInterruptChangeTheOutcome()
        {
            var warden = new FacilityMinibossState(FacilityMinibossKind.SecurityWarden);
            warden.Hit(100, false, true);
            Assert.That(warden.Health, Is.EqualTo(warden.Maximum - 16));
            Assert.That(warden.CutPower(), Is.True);
            Assert.That(warden.CutPower(), Is.False);
            warden.Hit(100, false, true);
            Assert.That(warden.Health, Is.EqualTo(warden.Maximum - 78));

            var harvester = new FacilityMinibossState(FacilityMinibossKind.Harvester);
            for (int i = 0; i < 70 && harvester.Move != MinibossMove.Repair; i++) harvester.Step(.2f, 36, 112, _ => { });
            Assert.That(harvester.Move, Is.EqualTo(MinibossMove.Repair));
            harvester.Hit(49, true, true);
            float damaged = harvester.Health;
            for (int i = 0; i < 8; i++) harvester.Step(.2f, 36, 112, _ => { });
            Assert.That(harvester.Health, Is.EqualTo(damaged));
        }

        [Test]
        public void LockdownUsesTwoWavesAndDoesNotRestartAfterSave()
        {
            var state = new LogisticsDefense();
            Assert.That(state.Begin(), Is.True);
            state.Advance(.75f, true);
            state = LogisticsDefense.Restore(state.Capture(), true);
            Assert.That(state.Released, Is.Zero);
            state.Advance(.75f, true);
            Assert.That(state.Released, Is.EqualTo(1));
            Assert.That(state.Advance(3, false), Is.False);
            state.Advance(.04f, true);
            state.Advance(2f, true);
            Assert.That(state.Released, Is.EqualTo(3));
            state.Advance(.04f, true);
            Assert.That(state.Phase, Is.EqualTo(LogisticsDefense.Cleared));
            Assert.That(LogisticsDefense.Restore(state.Capture(), true).Begin(), Is.False);
        }
    }
}
