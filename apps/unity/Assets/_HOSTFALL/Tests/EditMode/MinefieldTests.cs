using System;
using System.Collections.Generic;
using NUnit.Framework;
using Hostfall.Simulation.Computers;

namespace Hostfall.Tests
{
    public sealed class MinefieldTests
    {
        [Test]
        public void ThreeBoardsCanBeExhaustedByVisibleCluesWithoutAnyGuess()
        {
            var board = new MinefieldSession();
            for (int stage = 0; stage < MinefieldSession.LevelCount; stage++)
            {
                Assert.That(board.Begin(), Is.True);
                Assert.That(board.Clue(MinefieldSession.StartCell), Is.Zero);
                Assert.That(SolveCurrentLevel(board), Is.True, "Logical solver exhausted the visible clues");
                Assert.That(board.Stage, Is.EqualTo(stage + 1));
            }
            Assert.That(board.Phase, Is.EqualTo(MinefieldPhase.Complete));
            for (int cell = 0; cell < MinefieldSession.CellCount; cell++)
                Assert.That(board.IsArmed(cell), Is.False);
            Assert.That(MinefieldSession.Restore(board.Capture()).Capture().flags, Is.EqualTo(board.Flags));
        }

        [Test]
        public void MineStrikeTriggersAlarmAndRetryKeepsEarnedLevels()
        {
            var board = new MinefieldSession();
            board.Begin();
            Assert.That(SolveCurrentLevel(board), Is.True);
            board.Begin();
            int triggerCell = -1;
            board.AlarmTriggered += cell => triggerCell = cell;
            Assert.That(board.Reveal(5), Is.True);
            Assert.That(board.Phase, Is.EqualTo(MinefieldPhase.Failed));
            Assert.That(triggerCell, Is.EqualTo(5));
            Assert.That(board.Begin(), Is.False);

            board.AdvanceWorld(.135);
            var restored = MinefieldSession.Restore(board.Capture());
            Assert.That(restored.HitCell, Is.EqualTo(5));
            Assert.That(restored.AlarmRemaining, Is.EqualTo(board.AlarmRemaining));
            for (int frame = 0; frame < 24; frame++)
                restored.AdvanceWorld(.25);
            Assert.That(restored.Begin(), Is.True);
            Assert.That(restored.Stage, Is.EqualTo(1));
            Assert.That(restored.HitCell, Is.EqualTo(-1));
        }

        [Test]
        public void FlagsChangeOnlyTheirMatchingSensorAndSaveTheirExactBoard()
        {
            var board = new MinefieldSession();
            board.Begin();
            Assert.That(board.IsArmed(11), Is.True);
            Assert.That(board.ToggleFlag(11), Is.True);
            Assert.That(board.Reveal(11), Is.False);
            Assert.That(board.IsArmed(11), Is.False);
            Assert.That(board.TriggerSensor(11), Is.False);
            Assert.That(board.TriggerSensor(31), Is.True);
            Assert.That(board.TriggerSensor(35), Is.False, "An existing alarm must rate limit floor sensors");
            Assert.That(board.Phase, Is.EqualTo(MinefieldPhase.Playing));
            Assert.That(board.ToggleFlag(MinefieldSession.StartCell), Is.False);
            board.AdvanceWorld(.12);
            var restored = MinefieldSession.Restore(board.Capture());
            Assert.That(restored.Flags, Is.EqualTo(board.Flags));
            Assert.That(restored.Revealed, Is.EqualTo(board.Revealed));
            Assert.That(restored.IsArmed(11), Is.False);
            Assert.That(restored.IsArmed(35), Is.True);
        }

        [Test]
        public void ChordingWithIncorrectFlagsCanTriggerAHiddenMine()
        {
            var board = new MinefieldSession();
            board.Begin();
            int clue = 10;
            Assert.That(board.Reveal(clue) || board.IsRevealed(clue), Is.True);
            int missing = board.Clue(clue);
            for (int cell = 0; cell < MinefieldSession.CellCount && missing > 0; cell++)
            {
                if (cell != 11 && (MinefieldSession.Neighbors(clue) & MinefieldSession.Bit(cell)) != 0 && board.ToggleFlag(cell))
                    missing--;
            }
            Assert.That(missing, Is.Zero);
            Assert.That(board.OpenNeighbors(clue), Is.True);
            Assert.That(board.Phase, Is.EqualTo(MinefieldPhase.Failed));
        }

        [Test]
        public void RestoreRejectsInvalidCellsAndProgress()
        {
            var board = new MinefieldSession();
            board.Begin();
            var data = board.Capture();
            data.revealed |= MinefieldSession.Bit(11);
            Assert.Throws<ArgumentException>(() => MinefieldSession.Restore(data));
            data = board.Capture();
            data.flags = 1L << 60;
            Assert.Throws<ArgumentException>(() => MinefieldSession.Restore(data));
            data = board.Capture();
            data.stage = 3;
            Assert.Throws<ArgumentException>(() => MinefieldSession.Restore(data));
        }

        [Test]
        public void CampaignRestoresPartialBoardAndAwardsNeuroPassageOnce()
        {
            var campaign = new ComputerCampaign();
            Assert.That(campaign.BeginMinefield(), Is.False);
            SignalTests.CommissionSignalPath(campaign);
            for (int frame = 0; frame < 24000 && campaign.Payload.Phase != PayloadPhase.Complete; frame++)
            {
                campaign.BeginPayload();
                ComputerGameTests.DrivePayload(campaign.Payload);
            }
            Assert.That(campaign.BeginMinefield(), Is.True);
            campaign.Minefield.ToggleFlag(11);
            var resumed = ComputerCampaign.Restore(campaign.Capture());
            Assert.That(resumed.Minefield.IsFlagged(11), Is.True);
            Assert.That(SolveCurrentLevel(resumed.Minefield), Is.True);
            for (int level = 1; level < MinefieldSession.LevelCount; level++)
            {
                Assert.That(resumed.BeginMinefield(), Is.True);
                Assert.That(SolveCurrentLevel(resumed.Minefield), Is.True);
            }
            var final = ComputerCampaign.Restore(resumed.Capture());
            Assert.That(final.BeginMinefield(), Is.False);
            Assert.That(final.Home.TryRead(ComputerCampaign.MinefieldIncidentPath, out _), Is.True);
            Assert.That(System.Linq.Enumerable.Count(final.Actions, a => a.target == ComputerCampaign.MinefieldNeuroPass), Is.EqualTo(1));
            var older = new ComputerCampaign().Capture();
            older.minefield = null;
            Assert.That(ComputerCampaign.Restore(older).Minefield.Phase, Is.EqualTo(MinefieldPhase.Ready));
        }

        // Uses only evidence visible to the player: clues, flags, and grid adjacency.
        // No mine lookups or traversal of hypothetical board solutions.
        private static bool SolveCurrentLevel(MinefieldSession board)
        {
            int stage = board.Stage;
            while (board.Stage == stage && board.Phase == MinefieldPhase.Playing)
            {
                var constraints = new List<(long cells, int mines)>();
                for (int cell = 0; cell < MinefieldSession.CellCount; cell++)
                {
                    int clue = board.Clue(cell);
                    if (clue < 0)
                        continue;
                    long adjacent = MinefieldSession.Neighbors(cell);
                    long hidden = adjacent & ~(board.Revealed | board.Flags);
                    if (hidden != 0)
                        constraints.Add((hidden, clue - MinefieldSession.CountBits(adjacent & board.Flags)));
                }

                long safe = 0, danger = 0;
                foreach (var rule in constraints)
                {
                    Deduce(rule.cells, rule.mines);
                    foreach (var smaller in constraints)
                        if (smaller.cells != rule.cells && (smaller.cells & rule.cells) == smaller.cells)
                            Deduce(rule.cells & ~smaller.cells, rule.mines - smaller.mines);
                }

                if ((safe | danger) == 0)
                    return false;
                Assert.That(safe & danger, Is.Zero);
                for (int cell = 0; cell < MinefieldSession.CellCount && board.Stage == stage; cell++)
                {
                    long bit = MinefieldSession.Bit(cell);
                    if ((danger & bit) != 0)
                        board.ToggleFlag(cell);
                    else if ((safe & bit) != 0)
                        board.Reveal(cell);
                }

                void Deduce(long cells, int mines)
                {
                    if (mines == 0)
                        safe |= cells;
                    else if (mines == MinefieldSession.CountBits(cells))
                        danger |= cells;
                }
            }
            return board.Stage == stage + 1;
        }
    }
}
