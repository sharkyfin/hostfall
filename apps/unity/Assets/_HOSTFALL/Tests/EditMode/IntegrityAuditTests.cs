using System;
using System.Linq;
using Hostfall.Simulation.Computers;
using NUnit.Framework;

namespace Hostfall.Tests
{
    public sealed class IntegrityAuditTests
    {
        [Test]
        public void AllThreeBanksAreSolvableWithinBudgetUsingOnlyReturnedChecksums()
        {
            var truth = new IntegrityAuditSession();
            int rewards = 0;
            truth.Completed += () => rewards++;
            for (int bank = 0; bank < IntegrityAuditSession.StageCount; bank++)
            {
                truth.Start();
                Assert.That(truth.Isolate(), Is.False);
                while (!truth.CanIsolate)
                {
                    int[] candidates = Enumerable.Range(0, IntegrityAuditSession.PageCount).Where(truth.IsCandidate).ToArray();
                    truth.SelectPage(candidates[0]);
                    truth.SelectPage(candidates[candidates.Length / 2 - 1]);
                    Assert.That(truth.Probe(), Is.True);
                }
                Assert.That(truth.Reports.Count, Is.EqualTo(5));
                Assert.That(truth.Isolate(), Is.True);
            }
            Assert.That(truth.Complete, Is.True);
            Assert.That(truth.Start() || truth.Isolate(), Is.False);
            Assert.That(rewards, Is.EqualTo(1));
            Assert.That(IntegrityAuditSession.Restore(truth.Capture()).Complete, Is.True);
        }

        [Test]
        public void ExhaustedBudgetRetriesAndSavedReportCannotFabricateEvidence()
        {
            var audit = new IntegrityAuditSession();
            audit.Start();
            audit.SelectPage(0);
            audit.SelectPage(15);
            Assert.That(audit.Probe(), Is.True);
            audit.SelectPage(5);
            var restored = IntegrityAuditSession.Restore(audit.Capture());
            restored.SelectPage(9);
            Assert.That(restored.First, Is.EqualTo(5));
            Assert.That(restored.Last, Is.EqualTo(9));
            var invalid = audit.Capture();
            invalid.probes[0].mismatch = false;
            Assert.Throws<ArgumentException>(() => IntegrityAuditSession.Restore(invalid));
            while (restored.Phase == IntegrityPhase.Audit)
                Assert.That(restored.Probe(), Is.True);
            Assert.That(restored.Phase, Is.EqualTo(IntegrityPhase.OutOfProbes));
            Assert.That(restored.Isolate(), Is.False);
            Assert.That(restored.Start(), Is.True);
            Assert.That(restored.Stage, Is.Zero);
            Assert.That(restored.Reports, Is.Empty);
        }
    }
}
