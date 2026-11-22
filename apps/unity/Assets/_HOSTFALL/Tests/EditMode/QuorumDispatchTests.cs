using System;
using Hostfall.Simulation.Computers;
using NUnit.Framework;

namespace Hostfall.Tests
{
    public sealed class QuorumDispatchTests
    {
        private static void Reserve(QuorumDispatchSession session, int task, int witness, int start)
        {
            session.Select(task);
            Assert.That(session.Schedule(witness, start), Is.EqualTo(QuorumIssue.None), QuorumDispatchSession.Tasks[task].Title);
        }

        [Test]
        public void WitnessScheduleHasValidOrderAndIssuanceIsIdempotent()
        {
            var schedule = new QuorumDispatchSession();
            Assert.That(schedule.Start(), Is.True);
            Reserve(schedule, 0, 0, 0);
            Reserve(schedule, 1, 1, 2);
            Reserve(schedule, 2, 2, 0);
            Reserve(schedule, 3, 2, 2);
            Reserve(schedule, 4, 1, 4);
            Reserve(schedule, 5, 0, 4);
            Assert.That(schedule.Dispatch(out int blocked), Is.EqualTo(QuorumIssue.Dependency));
            Assert.That(blocked, Is.EqualTo(5));
            Reserve(schedule, 5, 0, 5);
            int dispatches = 0;
            schedule.Completed += () => dispatches++;
            Assert.That(schedule.Dispatch(out _), Is.EqualTo(QuorumIssue.None));
            Assert.That(schedule.Dispatch(out _), Is.EqualTo(QuorumIssue.Inactive));
            Assert.That(dispatches, Is.EqualTo(1));
            Assert.That(QuorumDispatchSession.Restore(schedule.Capture()).Complete, Is.True);
        }

        [Test]
        public void SharedPowerWitnessAndWindowRejectionsPreservePriorSlots()
        {
            var schedule = new QuorumDispatchSession();
            schedule.Start();
            Reserve(schedule, 0, 0, 0);
            Reserve(schedule, 2, 2, 0);
            schedule.Select(1);
            Assert.That(schedule.Schedule(2, 1), Is.EqualTo(QuorumIssue.Witness));
            Assert.That(schedule.Schedule(1, 3), Is.EqualTo(QuorumIssue.TimeWindow));
            Assert.That(schedule.Schedule(1, 1), Is.EqualTo(QuorumIssue.SharedLimit));
            Reserve(schedule, 1, 1, 2);
            Assert.That(schedule.Schedule(1, 1), Is.EqualTo(QuorumIssue.SharedLimit));
            var restored = QuorumDispatchSession.Restore(schedule.Capture());
            Assert.That(restored.Selected, Is.EqualTo(1));
            Assert.That(restored.Time(1), Is.EqualTo(2));
            Assert.That(restored.JobAt(1, 3), Is.EqualTo(1));
            var invalid = restored.Capture();
            invalid.times[1] = 1;
            Assert.Throws<ArgumentException>(() => QuorumDispatchSession.Restore(invalid));
        }
    }
}
