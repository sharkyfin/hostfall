using System;
using System.Linq;
using Hostfall.Simulation.Computers;
using NUnit.Framework;

namespace Hostfall.Tests
{
    public sealed class FragmentDockTests
    {
        private static bool Pack(FragmentDockSession disk, int piece)
        {
            if (piece == FragmentDockSession.FragmentCount) return true;
            disk.Select(piece);
            for (int turn = 0; turn < 4; turn++)
            {
                for (int cell = 0; cell < FragmentDockSession.CellCount; cell++)
                {
                    if (disk.CheckPlacement(piece, turn, cell) != FragmentPlacement.Placed) continue;
                    Assert.That(disk.Place(cell), Is.EqualTo(FragmentPlacement.Placed));
                    if (Pack(disk, piece + 1)) return true;
                    disk.Select(piece);
                    Assert.That(disk.Remove(), Is.True);
                }
                disk.Rotate();
            }
            return false;
        }

        [Test]
        public void AllFragmentsFitWhileBootCellsAndContiguousScratchStayFree()
        {
            var disk = new FragmentDockSession();
            Assert.That(disk.Start(), Is.True);
            Assert.That(Pack(disk, 0), Is.True);
            Assert.That(disk.ReadyToCommit, Is.True);
            int signal = 0;
            disk.Completed += () => signal++;
            Assert.That(disk.Commit(), Is.True);
            Assert.That(disk.Commit(), Is.False);
            Assert.That(signal, Is.EqualTo(1));
            var restored = FragmentDockSession.Restore(disk.Capture());
            for (int cell = 0; cell < FragmentDockSession.CellCount; cell++)
            {
                bool used = restored.FragmentAt(cell) >= 0;
                Assert.That(used, Is.EqualTo(cell % FragmentDockSession.Columns < FragmentDockSession.DataColumns &&
                    !FragmentDockSession.IsReserved(cell)));
            }
        }

        [Test]
        public void ConflictPreservesPlacementAndCurrentRotationSurvivesRestore()
        {
            var disk = new FragmentDockSession();
            disk.Start();
            disk.Rotate();
            Assert.That(disk.Place(1), Is.EqualTo(FragmentPlacement.Placed));
            Assert.That(disk.Place(0), Is.EqualTo(FragmentPlacement.Reserved));
            Assert.That(disk.Anchor(0), Is.EqualTo(1));
            disk.Select(1);
            Assert.That(disk.CheckPlacement(1, 0, 22), Is.EqualTo(FragmentPlacement.Occupied));
            disk.Rotate();
            var restored = FragmentDockSession.Restore(disk.Capture());
            Assert.That(restored.Selected, Is.EqualTo(1));
            Assert.That(restored.Rotation, Is.EqualTo(1));
            Assert.That(restored.Turns(0), Is.EqualTo(1));
            Assert.That(restored.Anchor(0), Is.EqualTo(1));
            var badSave = restored.Capture();
            badSave.anchors[1] = 22;
            Assert.Throws<ArgumentException>(() => FragmentDockSession.Restore(badSave));
        }
    }
}
