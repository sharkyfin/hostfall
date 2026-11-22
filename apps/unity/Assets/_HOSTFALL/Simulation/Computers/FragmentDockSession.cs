using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation.Computers
{
    public enum FragmentPlacement { Placed, Inactive, OutsidePartition, Reserved, Occupied }

    [Serializable]
    public sealed class FragmentDockData
    {
        public bool started, committed;
        public int selected, rotation;
        public int[] anchors = Enumerable.Repeat(-1, FragmentDockSession.FragmentCount).ToArray();
        public int[] turns = new int[FragmentDockSession.FragmentCount];
    }

    public sealed class FragmentDockSession
    {
        public const int Columns = 7, Rows = 5, DataColumns = 5, FragmentCount = 5;
        public const int CellCount = Columns * Rows;
        private static readonly int[] reserved = { 0, 2, 7, 16, 32 };
        private static readonly (int x, int y)[][] originals =
        {
            new[] { (0, 0), (1, 0), (2, 0), (3, 0) },
            new[] { (0, 0), (1, 0), (0, 1), (1, 1) },
            new[] { (0, 0), (0, 1), (0, 2), (1, 2) },
            new[] { (0, 0), (1, 0), (2, 0), (1, 1) },
            new[] { (1, 0), (2, 0), (0, 1), (1, 1) }
        };
        private static readonly (int x, int y)[][][] rotations = originals.Select(DescribeRotations).ToArray();
        private FragmentDockData state = new();

        public bool Started => state.started;
        public bool Complete => state.committed;
        public int Selected => state.selected;
        public int Rotation => state.rotation;
        public bool ReadyToCommit => Started && !Complete && state.anchors.All(a => a >= 0);
        public event Action Completed;
        public event Action Changed;

        public int Anchor(int fragment) => state.anchors[fragment];
        public int Turns(int fragment) => state.turns[fragment];
        public static bool IsReserved(int cell) => Array.IndexOf(reserved, cell) >= 0;
        public static IReadOnlyList<(int x, int y)> Shape(int fragment, int turn) => rotations[fragment][turn];

        public bool Start()
        {
            if (Started) return false;
            state.started = true;
            Changed?.Invoke();
            return true;
        }

        public void Select(int fragment)
        {
            if (!Started || Complete || fragment < 0 || fragment >= FragmentCount || fragment == Selected) return;
            state.selected = fragment;
            state.rotation = Turns(fragment);
            Changed?.Invoke();
        }

        public void Rotate()
        {
            if (!Started || Complete) return;
            state.rotation = (Rotation + 1) % 4;
            Changed?.Invoke();
        }

        public bool Remove()
        {
            if (!Started || Complete || Anchor(Selected) < 0) return false;
            state.anchors[Selected] = -1;
            state.turns[Selected] = 0;
            Changed?.Invoke();
            return true;
        }

        public int FragmentAt(int cell)
        {
            for (int piece = 0; piece < FragmentCount; piece++)
            {
                int anchor = Anchor(piece);
                if (anchor < 0) continue;
                foreach (var point in rotations[piece][Turns(piece)])
                    if (Cell(anchor, point) == cell) return piece;
            }
            return -1;
        }

        public FragmentPlacement Place(int anchor)
        {
            if (!Started || Complete) return FragmentPlacement.Inactive;
            var verdict = CheckPlacement(Selected, Rotation, anchor);
            if (verdict != FragmentPlacement.Placed) return verdict;
            state.anchors[Selected] = anchor;
            state.turns[Selected] = Rotation;
            Changed?.Invoke();
            return verdict;
        }

        public FragmentPlacement CheckPlacement(int fragment, int turn, int anchor)
        {
            if (anchor < 0 || anchor >= CellCount || fragment < 0 || fragment >= FragmentCount || turn < 0 || turn > 3)
                return FragmentPlacement.OutsidePartition;
            foreach (var point in rotations[fragment][turn])
            {
                int cell = Cell(anchor, point);
                if (cell < 0) return FragmentPlacement.OutsidePartition;
                if (IsReserved(cell)) return FragmentPlacement.Reserved;
                int occupier = FragmentAt(cell);
                if (occupier >= 0 && occupier != fragment) return FragmentPlacement.Occupied;
            }
            return FragmentPlacement.Placed;
        }

        public static int Cell(int anchor, (int x, int y) offset)
        {
            int x = anchor % Columns + offset.x;
            int y = anchor / Columns + offset.y;
            return x >= 0 && x < DataColumns && y >= 0 && y < Rows ? y * Columns + x : -1;
        }

        public bool Commit()
        {
            if (!ReadyToCommit) return false;
            state.committed = true;
            Completed?.Invoke();
            Changed?.Invoke();
            return true;
        }

        private static (int x, int y)[][] DescribeRotations((int x, int y)[] source)
        {
            var result = new (int, int)[4][];
            for (int rotation = 0; rotation < result.Length; rotation++)
            {
                var ordered = source.OrderBy(c => c.y).ThenBy(c => c.x).ToArray();
                var first = ordered[0];
                result[rotation] = ordered.Select(p => (p.x - first.x, p.y - first.y)).ToArray();
                source = source.Select(p => (-p.y, p.x)).ToArray();
            }
            return result;
        }

        public FragmentDockData Capture() => new()
        {
            started = Started, committed = Complete, selected = Selected, rotation = Rotation,
            anchors = (int[])state.anchors.Clone(), turns = (int[])state.turns.Clone()
        };

        public static FragmentDockSession Restore(FragmentDockData data)
        {
            SaveValidation.Require(data != null && data.selected >= 0 && data.selected < FragmentCount &&
                data.rotation >= 0 && data.rotation < 4 && data.anchors?.Length == FragmentCount &&
                data.turns?.Length == FragmentCount && data.turns.All(t => t >= 0 && t < 4), "Invalid fragment placement state");
            var restored = new FragmentDockSession();
            for (int fragment = 0; fragment < FragmentCount; fragment++)
            {
                int anchor = data.anchors[fragment];
                if (anchor == -1)
                {
                    SaveValidation.Require(data.turns[fragment] == 0 && !data.committed, "Unpacked fragment in finalized volume");
                    continue;
                }
                SaveValidation.Require(data.started && restored.CheckPlacement(fragment, data.turns[fragment], anchor) ==
                    FragmentPlacement.Placed, "Fragment overlaps or is outside partition");
                restored.state.anchors[fragment] = anchor;
                restored.state.turns[fragment] = data.turns[fragment];
            }
            SaveValidation.Require(data.started || !data.committed && data.selected == 0 && data.rotation == 0, "Inactive fragment controls");
            restored.state.selected = data.selected;
            restored.state.rotation = data.rotation;
            restored.state.started = data.started;
            restored.state.committed = data.committed;
            return restored;
        }
    }
}
