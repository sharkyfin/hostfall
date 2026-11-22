using System;
using Hostfall.Simulation.Computers;
using NUnit.Framework;

namespace Hostfall.Tests
{
    public sealed class ContainmentTests
    {
        private static void Advance(ContainmentSession session, SentryZone zone, int ticks, bool southBlocked = false,
            bool northBlocked = false, bool playerInside = false)
        {
            var sensors = new ContainmentSensors(zone, southBlocked, northBlocked, playerInside);
            for (int tick = 0; tick < ticks; tick++) session.Observe(sensors, .05);
        }

        private static ContainmentSession Seal()
        {
            var session = new ContainmentSession();
            session.Start();
            session.Toggle(ContainmentShutter.South);
            session.Toggle(ContainmentShutter.North);
            return session;
        }

        [Test]
        public void IsolationRequiresTransferSealedShuttersAndContinuousPresence()
        {
            var session = Seal();
            Advance(session, SentryZone.Bay, 100);
            Assert.That(session.Isolate(), Is.False);
            session.Send(SentryOrder.Capture);
            Advance(session, SentryZone.Bay, 20);
            Assert.That(session.Isolate(), Is.False);
            Advance(session, SentryZone.Transit, 1, northBlocked: true);
            Assert.That(session.Verification, Is.Zero);
            Advance(session, SentryZone.Bay, 40);

            int awards = 0;
            session.Isolated += () => awards++;
            Assert.That(session.Isolate(), Is.True);
            Assert.That(session.Clearance, Is.EqualTo(ContainmentClearance.Captured));
            session.Send(SentryOrder.Patrol);
            Assert.That(session.Order, Is.EqualTo(SentryOrder.Capture));
            Assert.That(session.Isolate(), Is.False);
            Assert.That(awards, Is.EqualTo(1));
        }

        [Test]
        public void PlayerAndBlockedBeamOverrideShutterRequestsAcrossRestore()
        {
            var session = Seal();
            session.Send(SentryOrder.Capture);
            Advance(session, SentryZone.Bay, 4, southBlocked: true);
            Assert.That(session.Opening(ContainmentShutter.South), Is.EqualTo(1));
            Assert.That(session.Opening(ContainmentShutter.North), Is.InRange(.1, .9));
            session.SelectCamera(ContainmentCamera.Bay);
            session = ContainmentSession.Restore(session.Capture());
            Assert.That(session.Camera, Is.EqualTo(ContainmentCamera.Bay));
            Advance(session, SentryZone.Bay, 80, playerInside: true);
            Assert.That(session.Opening(ContainmentShutter.South), Is.EqualTo(1));
            Assert.That(session.Opening(ContainmentShutter.North), Is.EqualTo(1));
            Assert.That(session.CanIsolate, Is.False);
            session.Release(ContainmentShutter.North);
            Advance(session, SentryZone.Bay, 80);
            Assert.That(session.Opening(ContainmentShutter.North), Is.EqualTo(1));
            Assert.That(session.Opening(ContainmentShutter.South), Is.Zero);
            Assert.That(session.CanIsolate, Is.False);
        }

        [Test]
        public void DestroyedSentryStillAllowsIndependentQuarantineClearance()
        {
            var session = Seal();
            session.Send(SentryOrder.Calibration);
            Advance(session, SentryZone.Offline, 50);
            Assert.That(session.Isolate(), Is.True);
            Assert.That(ContainmentSession.Restore(session.Capture()).Clearance, Is.EqualTo(ContainmentClearance.Offline));
        }

        [Test]
        public void InvalidRestoreAndMissingArchiveKeyCannotStartQuarantine()
        {
            var campaign = new ComputerCampaign();
            Assert.That(campaign.BeginContainment(), Is.False);
            Assert.Throws<ArgumentException>(() => ContainmentSession.Restore(new ContainmentData { southPosition = double.NaN }));
            Assert.Throws<ArgumentException>(() => ContainmentSession.Restore(new ContainmentData { started = true, verification = .1 }));
            Assert.Throws<ArgumentException>(() => ContainmentSession.Restore(new ContainmentData { started = true, clearance = ContainmentClearance.Captured }));
            var current = campaign.Capture();
            current.containment = null;
            Assert.That(ComputerCampaign.Restore(current).Containment.Started, Is.False);
            current.containment = new ContainmentData { started = true };
            Assert.Throws<ArgumentException>(() => ComputerCampaign.Restore(current));
        }
    }
}
