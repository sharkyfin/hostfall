using System;
using Hostfall.Simulation.Computers;
using NUnit.Framework;

namespace Hostfall.Tests
{
    public sealed class RecoveryProtocolTests
    {
        [Test]
        public void VerifiedNetworkCredentialsAndSequenceFinishWithOneActivation()
        {
            var session = new RecoveryProtocolSession();
            Assert.That(session.Verify(), Is.EqualTo(RecoveryIssue.Inactive));
            Assert.That(session.Start(), Is.True);
            session.ToggleLink(1);
            Assert.That(session.Verify(), Is.EqualTo(RecoveryIssue.Link));
            session.ToggleLink(1);
            foreach (int link in new[] { 0, 2, 4, 6, 7, 9 }) session.ToggleLink(link);
            Assert.That(session.Verify(), Is.EqualTo(RecoveryIssue.None));
            Assert.That(session.Stage, Is.EqualTo(RecoveryStage.Access));
            session = RecoveryProtocolSession.Restore(session.Capture());
            for (int service = 0; service < RecoveryProtocolSession.AccessRows; service++)
                session.SetCredential(service, (RecoveryCredential)((service + 1) % RecoveryProtocolSession.AccessRows));
            Assert.That(session.Verify(), Is.EqualTo(RecoveryIssue.Credential));
            for (int service = 0; service < RecoveryProtocolSession.AccessRows; service++)
                session.SetCredential(service, (RecoveryCredential)service);
            Assert.That(session.Verify(), Is.EqualTo(RecoveryIssue.None));
            session.Append(RecoveryOperation.SecureBoundary);
            session.Append(RecoveryOperation.Authenticate);
            session.Append(RecoveryOperation.TakeClockReference);
            session.Append(RecoveryOperation.RestoreBus);
            session.Append(RecoveryOperation.Commit);
            Assert.That(session.Verify(), Is.EqualTo(RecoveryIssue.Order));
            session = RecoveryProtocolSession.Restore(session.Capture());
            session.Move(1, 1);
            int activations = 0;
            session.Completed += () => activations++;
            Assert.That(session.Verify(), Is.EqualTo(RecoveryIssue.None));
            Assert.That(session.Verify(), Is.EqualTo(RecoveryIssue.Inactive));
            Assert.That(activations, Is.EqualTo(1));
            Assert.That(RecoveryProtocolSession.Restore(session.Capture()).Complete, Is.True);
        }

        [Test]
        public void ShuttleKeepsShaftsClosedInMotionAndRestoresItsExactSample()
        {
            var shuttle = new RecoveryTransit();
            Assert.That(shuttle.GateOpening(TransitStop.Shelter), Is.EqualTo(1));
            Assert.That(shuttle.Dispatch(TransitStop.Ring, true), Is.True);
            Assert.That(shuttle.Dispatch(TransitStop.Shelter, false), Is.False);
            for (int i = 0; i < 58; i++) shuttle.Advance(.08);
            Assert.That(shuttle.Pose.Y, Is.EqualTo(RecoveryTransit.TunnelY));
            Assert.That(shuttle.GateOpening(TransitStop.Shelter), Is.Zero);
            Assert.That(shuttle.GateOpening(TransitStop.Ring), Is.Zero);
            var saved = shuttle.Capture();
            Assert.Throws<ArgumentException>(() => RecoveryTransit.Restore(saved, false));
            var restored = RecoveryTransit.Restore(saved, true);
            Assert.That(restored.Passenger, Is.True);
            Assert.That(restored.Pose.Z, Is.EqualTo(shuttle.Pose.Z));
            while (restored.Moving) restored.Advance(.08);
            Assert.That(restored.Passenger, Is.False);
            Assert.That(restored.Pose.Z, Is.EqualTo(RecoveryTransit.Ring.Z));
            Assert.That(restored.GateOpening(TransitStop.Ring), Is.EqualTo(1));
            Assert.That(restored.Dispatch(TransitStop.Shelter, false), Is.True);
            while (restored.Moving) restored.Advance(.08);
            Assert.That(restored.Pose.Z, Is.EqualTo(RecoveryTransit.Shelter.Z));
        }

        [Test]
        public void RestoreRejectsPrematureCompletionAndFutureStageAssignments()
        {
            Assert.Throws<ArgumentException>(() =>
                RecoveryProtocolSession.Restore(new RecoveryProtocolData { started = true, stage = RecoveryStage.Complete }));
            var data = new RecoveryProtocolData { started = true };
            data.credentials[0] = (int)RecoveryCredential.ArchiveKey;
            Assert.Throws<ArgumentException>(() => RecoveryProtocolSession.Restore(data));
            data.credentials[0] = -1;
            data.sequence = new[] { (int)RecoveryOperation.Authenticate };
            Assert.Throws<ArgumentException>(() => RecoveryProtocolSession.Restore(data));
        }
    }
}
