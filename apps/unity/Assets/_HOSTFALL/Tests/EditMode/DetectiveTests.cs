using System;
using NUnit.Framework;
using Hostfall.Simulation.Computers;

namespace Hostfall.Tests
{
    public sealed class DetectiveTests
    {
        [Test]
        public void ChronologyRequiresCollectedPrimaryRecordsAndSeparatesBothDates()
        {
            var game = new DetectiveSession();
            Assert.That(game.Submit(), Is.EqualTo(DetectiveResult.Incomplete));
            Assert.That(game.Collect(DetectiveEvidence.PublicReport), Is.False);
            Assert.That(game.Open(DetectiveEvidence.IdentityProtocol), Is.False);
            ReadAll(game);
            FillChronology(game);
            game.SetDate(18, DetectiveEvidence.SensorRegister, DetectiveEvidence.PublicReport);
            Assert.That(game.Submit(), Is.EqualTo(DetectiveResult.DateConflict));
            game.SetDate(17, DetectiveEvidence.PublicReport, DetectiveEvidence.CustodyMemo);
            Assert.That(game.Submit(), Is.EqualTo(DetectiveResult.DateConflict));
            game.SetDate(17, DetectiveEvidence.ArchivePlayback, DetectiveEvidence.PublicReport);
            Assert.That(game.Submit(), Is.EqualTo(DetectiveResult.Verified));
            Assert.That(game.Open(DetectiveEvidence.IdentityProtocol), Is.True);
        }

        [Test]
        public void MisorderedEventsAndIrrelevantCitationsFailSeparately()
        {
            var game = new DetectiveSession();
            ReadAll(game);
            FillChronology(game);
            game.SetDate(17, DetectiveEvidence.SensorRegister, DetectiveEvidence.PublicReport);

            game.Place(0, DetectiveEvent.Quarantine);
            game.Place(1, DetectiveEvent.SealArchive);
            game.Cite(0, DetectiveEvidence.ShiftNotebook);
            game.Cite(1, DetectiveEvidence.EngineerMail);
            Assert.That(game.Submit(), Is.EqualTo(DetectiveResult.ChronologyConflict));

            FillChronology(game);
            game.Cite(1, DetectiveEvidence.CorridorPhoto);
            Assert.That(game.Submit(), Is.EqualTo(DetectiveResult.UnsupportedEvidence));
            game.Cite(1, DetectiveEvidence.ShiftNotebook);
            Assert.That(game.Submit(), Is.EqualTo(DetectiveResult.Verified));
        }

        [Test]
        public void ReadingMovesCitationsAndPhysicalKeyRestoreWithoutDuplicateEvents()
        {
            var game = new DetectiveSession();
            ReadAll(game);
            game.Place(2, DetectiveEvent.PublishReport);
            Assert.That(game.Cite(2, DetectiveEvidence.PublicReport), Is.True);
            game.Place(1, DetectiveEvent.PublishReport);
            var restored = DetectiveSession.Restore(game.Capture());
            Assert.That(restored.TimelineAt(2), Is.EqualTo(-1));
            Assert.That(restored.CitationAt(2), Is.EqualTo(-1));
            Assert.That(restored.TimelineAt(1), Is.EqualTo((int)DetectiveEvent.PublishReport));
            Assert.That(restored.IsCollected(DetectiveEvidence.PublicReport), Is.True);
            Assert.That(restored.RecoverKey(), Is.False);
            FillChronology(restored);
            restored.SetDate(17, DetectiveEvidence.SensorRegister, DetectiveEvidence.PublicReport);

            int cases = 0, keys = 0;
            restored.CaseVerified += () => cases++;
            restored.KeyCollected += () => keys++;
            restored.Submit();
            restored.Submit();
            Assert.That(restored.RecoverKey(), Is.True);
            Assert.That(restored.RecoverKey(), Is.False);
            Assert.That(cases, Is.EqualTo(1));
            Assert.That(keys, Is.EqualTo(1));
            Assert.That(DetectiveSession.Restore(restored.Capture()).KeyRecovered, Is.True);
        }

        [Test]
        public void CorruptInvestigationCannotUnlockASecretOrAKey()
        {
            var initial = new DetectiveSession();
            var invalid = initial.Capture();
            invalid.keyRecovered = true;
            Assert.Throws<ArgumentException>(() => DetectiveSession.Restore(invalid));
            invalid = initial.Capture();
            invalid.opened = 1 << (int)DetectiveEvidence.IdentityProtocol;
            Assert.Throws<ArgumentException>(() => DetectiveSession.Restore(invalid));
            invalid = initial.Capture();
            invalid.verified = true;
            invalid.result = DetectiveResult.Verified;
            Assert.Throws<ArgumentException>(() => DetectiveSession.Restore(invalid));
        }

        private static void ReadAll(DetectiveSession game)
        {
            foreach (var document in DetectiveSession.Documents)
            {
                if (!game.Open(document.Id))
                    continue;
                Assert.That(game.Collect(document.Id), Is.True);
            }
        }

        private static void FillChronology(DetectiveSession game)
        {
            game.Place(0, DetectiveEvent.SealArchive);
            game.Place(1, DetectiveEvent.Quarantine);
            game.Place(2, DetectiveEvent.RemoteSignature);
            game.Place(3, DetectiveEvent.PublishReport);
            game.Cite(0, DetectiveEvidence.EngineerMail);
            game.Cite(1, DetectiveEvidence.ArchivePlayback);
            game.Cite(2, DetectiveEvidence.SensorRegister);
            game.Cite(3, DetectiveEvidence.CustodyMemo);
        }
    }
}
