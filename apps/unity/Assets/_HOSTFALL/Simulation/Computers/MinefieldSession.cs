using System;

namespace Hostfall.Simulation.Computers
{
    public enum MinefieldPhase { Ready, Playing, Failed, Complete }

    [Serializable]
    public sealed class MinefieldData
    {
        public int version = 1, stage, hit = -1;
        public MinefieldPhase phase;
        public long revealed, flags;
        public double alarmRemaining;
    }

    // Rows increase to the north from the control desk. The same cell IDs address
    // the screen, saved board, and authored laser/sensor tiles in the world.
    public sealed class MinefieldSession
    {
        public const int Side = 6, CellCount = Side * Side, StartCell = 2, LevelCount = 3;
        public const long CellMask = (1L << CellCount) - 1;
        public const double AlarmDuration = 6;
        private static readonly long[] layouts =
        {
            Mask(11,19,22,31,35),
            Mask(5,16,20,23,24,26,35),
            Mask(6,10,17,18,20,23,24,25,33)
        };
        private static readonly long[] neighbors = BuildNeighbors();

        public int Stage { get; private set; }
        public int Level => Math.Min(Stage, LevelCount - 1);
        public MinefieldPhase Phase { get; private set; }
        public int HitCell { get; private set; } = -1;
        public long Revealed { get; private set; }
        public long Flags { get; private set; }
        public double AlarmRemaining { get; private set; }
        public int MineCount => CountBits(Mines);
        public int FlagCount => CountBits(Flags);
        private long Mines => layouts[Level];
        public event Action Changed;
        public event Action<int> StageCompleted;
        public event Action<int> AlarmTriggered;

        public static long Bit(int cell) => cell >= 0 && cell < CellCount ? 1L << cell : 0;
        public static long Neighbors(int cell) => Bit(cell) != 0 ? neighbors[cell] : 0;

        public static int CountBits(long value)
        {
            int count = 0;
            while (value != 0)
            {
                value &= value - 1;
                count++;
            }
            return count;
        }

        public bool IsRevealed(int cell) => (Revealed & Bit(cell)) != 0;
        public bool IsFlagged(int cell) => (Flags & Bit(cell)) != 0;
        public int Clue(int cell) => IsRevealed(cell) ? CountBits(Mines & Neighbors(cell)) : -1;
        public bool IsArmed(int cell) => Phase != MinefieldPhase.Complete && (Mines & ~Flags & Bit(cell)) != 0;

        public bool Begin()
        {
            if ((Phase != MinefieldPhase.Ready && Phase != MinefieldPhase.Failed) || AlarmRemaining > 0)
                return false;

            Phase = MinefieldPhase.Playing;
            Revealed = Flags = 0;
            HitCell = -1;
            Flood(Bit(StartCell));
            Changed?.Invoke();
            return true;
        }

        public bool Reveal(int cell)
        {
            long bit = Bit(cell);
            if (Phase != MinefieldPhase.Playing || bit == 0 || ((Flags | Revealed) & bit) != 0)
                return false;
            return Expose(bit);
        }

        public bool ToggleFlag(int cell)
        {
            long bit = Bit(cell);
            if (Phase != MinefieldPhase.Playing || bit == 0 || (Revealed & bit) != 0)
                return false;

            Flags ^= bit;
            CheckCompleted();
            Changed?.Invoke();
            return true;
        }

        public bool OpenNeighbors(int cell)
        {
            int clue = Clue(cell);
            long adjacent = Neighbors(cell);
            long hidden = adjacent & ~(Flags | Revealed);
            if (Phase != MinefieldPhase.Playing || clue < 0 || CountBits(adjacent & Flags) != clue || hidden == 0)
                return false;
            return Expose(hidden);
        }

        public bool TriggerSensor(int cell)
        {
            if (!IsArmed(cell) || AlarmRemaining > 0)
                return false;

            RaiseAlarm(cell);
            Changed?.Invoke();
            return true;
        }

        private bool Expose(long cells)
        {
            long detonated = cells & Mines;
            if (detonated != 0)
            {
                for (int cell = 0; cell < CellCount; cell++)
                    if ((detonated & Bit(cell)) != 0)
                    {
                        HitCell = cell;
                        break;
                    }
                Phase = MinefieldPhase.Failed;
                RaiseAlarm(HitCell);
            }
            else
            {
                Flood(cells);
                CheckCompleted();
            }

            Changed?.Invoke();
            return true;
        }

        private void RaiseAlarm(int cell)
        {
            bool silent = AlarmRemaining == 0;
            AlarmRemaining = AlarmDuration;
            if (silent)
                AlarmTriggered?.Invoke(cell);
        }

        public void AdvanceWorld(double seconds)
        {
            if (AlarmRemaining <= 0 || !double.IsFinite(seconds) || seconds <= 0)
                return;

            int previousSecond = (int)Math.Ceiling(AlarmRemaining);
            double next = AlarmRemaining - Math.Min(seconds, .25);
            AlarmRemaining = next < 1e-12 ? 0 : next;
            if ((int)Math.Ceiling(AlarmRemaining) != previousSecond)
                Changed?.Invoke();
        }

        private void Flood(long pending)
        {
            while (pending != 0)
            {
                long batch = pending & ~(Revealed | Flags | Mines);
                Revealed |= batch;
                pending = 0;
                for (int cell = 0; cell < CellCount; cell++)
                    if ((batch & Bit(cell)) != 0 && (Mines & Neighbors(cell)) == 0)
                        pending |= Neighbors(cell) & ~(Revealed | Flags);
            }
        }

        private void CheckCompleted()
        {
            if (Revealed != (CellMask & ~Mines) || Flags != Mines)
                return;

            Stage++;
            if (Stage == LevelCount)
                Phase = MinefieldPhase.Complete;
            else
            {
                Phase = MinefieldPhase.Ready;
                Revealed = Flags = 0;
            }
            StageCompleted?.Invoke(Stage);
        }

        public MinefieldData Capture() => new()
        {
            stage = Stage, phase = Phase, hit = HitCell, revealed = Revealed,
            flags = Flags, alarmRemaining = AlarmRemaining
        };

        public static MinefieldSession Restore(MinefieldData data)
        {
            SaveValidation.Require(data != null && data.version == 1 &&
                data.stage >= 0 && data.stage <= LevelCount &&
                Enum.IsDefined(typeof(MinefieldPhase), data.phase), "Invalid minefield stage");

            var board = new MinefieldSession
            {
                Stage = data.stage, Phase = data.phase, HitCell = data.hit,
                Revealed = data.revealed, Flags = data.flags, AlarmRemaining = data.alarmRemaining
            };
            long mines = board.Mines;
            bool solved = board.Revealed == (CellMask & ~mines) && board.Flags == mines;
            SaveValidation.Require(((board.Revealed | board.Flags) & ~CellMask) == 0 &&
                (board.Revealed & (board.Flags | mines)) == 0 &&
                double.IsFinite(board.AlarmRemaining) && board.AlarmRemaining >= 0 &&
                board.AlarmRemaining <= AlarmDuration, "Invalid minefield cells or clock");
            SaveValidation.Require((board.Stage == LevelCount) == (board.Phase == MinefieldPhase.Complete) &&
                solved == (board.Phase == MinefieldPhase.Complete) &&
                (board.Phase == MinefieldPhase.Ready ? (board.Revealed | board.Flags) == 0 : board.IsRevealed(StartCell)),
                "Inconsistent minefield progress");
            SaveValidation.Require(board.Phase == MinefieldPhase.Failed
                ? (mines & ~board.Flags & Bit(board.HitCell)) != 0
                : board.HitCell == -1, "Invalid minefield failure");
            return board;
        }

        private static long Mask(params int[] cells)
        {
            long mask = 0;
            foreach (int cell in cells)
                mask |= Bit(cell);
            return mask;
        }

        private static long[] BuildNeighbors()
        {
            var result = new long[CellCount];
            for (int cell = 0; cell < CellCount; cell++)
            {
                int x = cell % Side, row = cell / Side;
                for (int z = Math.Max(0, row - 1); z <= Math.Min(Side - 1, row + 1); z++)
                    for (int column = Math.Max(0, x - 1); column <= Math.Min(Side - 1, x + 1); column++)
                        result[cell] |= Bit(z * Side + column);
                result[cell] &= ~Bit(cell);
            }
            return result;
        }
    }
}
