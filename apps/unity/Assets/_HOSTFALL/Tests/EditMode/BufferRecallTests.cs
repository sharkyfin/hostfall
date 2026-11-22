using System;
using System.Collections.Generic;
using Hostfall.Simulation.Computers;
using NUnit.Framework;

namespace Hostfall.Tests
{
    public sealed class BufferRecallTests
    {
        private static int[] Watch(BufferRecallSession session)
        {
            session.Show();
            var pulses = new List<int>();
            while (session.Phase == BufferPhase.Observe)
            {
                if (session.Position == pulses.Count && session.Primary >= 0) pulses.Add(session.Primary);
                session.Advance(.075);
            }
            return pulses.ToArray();
        }

        [Test]
        public void ThreeRecordingsRebuildFromVisiblePulsesAndReleaseOneSupply()
        {
            var session = new BufferRecallSession();
            int rewards = 0;
            session.Completed += () => rewards++;
            Assert.That(session.ClaimSupply(), Is.False);
            for (int length = 4; length <= 6; length++)
            {
                var positions = Watch(session);
                Assert.That(positions.Length, Is.EqualTo(length));
                foreach (int cell in positions) Assert.That(session.Append(cell), Is.True);
                Assert.That(session.Confirm(), Is.True);
            }
            Assert.That(rewards, Is.EqualTo(1));
            Assert.That(session.Show() || session.Confirm(), Is.False);
            Assert.That(session.ClaimSupply(), Is.True);
            Assert.That(BufferRecallSession.Restore(session.Capture()).ClaimSupply(), Is.False);
        }

        [Test]
        public void FailedAnswerAllowsCorrectionAndPreviewRestoresExactPulse()
        {
            var session = new BufferRecallSession();
            var positions = Watch(session);
            foreach (int cell in positions) session.Append((cell + 1) % BufferRecallSession.CellCount);
            Assert.That(session.Confirm(), Is.False);
            Assert.That(session.Phase, Is.EqualTo(BufferPhase.Mismatch));
            session.Clear();
            session.Append(positions[0]);
            session.Show();
            session.Advance(.1);
            session.Advance(.1);
            var restored = BufferRecallSession.Restore(session.Capture());
            Assert.That(restored.Primary, Is.EqualTo(positions[0]));
            Assert.That(restored.Input, Is.EqualTo(new[] { positions[0] }));
            Assert.That(restored.Capture().watchTime, Is.EqualTo(session.Capture().watchTime));
            Assert.Throws<ArgumentException>(() => BufferRecallSession.Restore(new BufferRecallData { stage = 0, phase = BufferPhase.Complete }));
        }
    }
}
