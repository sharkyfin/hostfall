using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation.Computers
{
    public enum BufferPhase { Ready, Observe, Rebuild, Mismatch, Complete }

    [Serializable]
    public sealed class BufferRecallData
    {
        public BufferPhase phase;
        public int stage;
        public double watchTime;
        public int[] input = Array.Empty<int>();
        public bool supplyClaimed;
    }

    public sealed class BufferRecallSession
    {
        public const int Width = 4, Height = 3, CellCount = Width * Height, StageCount = 3;
        private const double Beat = .78, VisibleTime = .52;
        private static readonly int[][] Recordings =
        {
            new[] { 1, 8, 6, 3 },
            new[] { 7, 4, 10, 0, 5 },
            new[] { 2, 11, 1, 9, 2, 4 }
        };

        private readonly List<int> input = new(6);
        private double watchTime;
        public int Stage { get; private set; }
        public BufferPhase Phase { get; private set; }
        public bool Complete => Phase == BufferPhase.Complete;
        public bool SupplyClaimed { get; private set; }
        public int Size => Recordings[Math.Min(Stage, StageCount - 1)].Length;
        public int Position => Phase == BufferPhase.Observe ? Math.Min((int)(watchTime / Beat), Size - 1) : -1;
        public int Primary => Position >= 0 && watchTime - Position * Beat < VisibleTime ? Recordings[Stage][Position] : -1;
        public int Interference => Primary >= 0 && Stage >= 1 ? (Primary + 7 + Position * 3) % CellCount : -1;
        public int SecondaryInterference => Primary >= 0 && Stage == 2 ? (Primary + 2 + Position * 3) % CellCount : -1;
        public IReadOnlyList<int> Input => input;
        public event Action Completed;
        public event Action Changed;

        public bool Show()
        {
            if (Complete) return false;
            watchTime = 0;
            Phase = BufferPhase.Observe;
            Changed?.Invoke();
            return true;
        }

        public void Advance(double seconds)
        {
            if (Phase != BufferPhase.Observe || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) return;
            int previousBeat = Position, previousPulse = Primary;
            watchTime += Math.Min(seconds, .1);
            if (watchTime >= Size * Beat)
            {
                Phase = BufferPhase.Rebuild;
                watchTime = 0;
            }
            if (previousBeat != Position || previousPulse != Primary) Changed?.Invoke();
        }

        public bool Append(int cell)
        {
            if (Phase != BufferPhase.Rebuild || cell < 0 || cell >= CellCount || input.Count >= Size) return false;
            input.Add(cell);
            Changed?.Invoke();
            return true;
        }

        public bool Erase()
        {
            if (Phase is not (BufferPhase.Rebuild or BufferPhase.Mismatch) || input.Count == 0) return false;
            input.RemoveAt(input.Count - 1);
            Phase = BufferPhase.Rebuild;
            Changed?.Invoke();
            return true;
        }

        public void Clear()
        {
            if (Phase is not (BufferPhase.Rebuild or BufferPhase.Mismatch)) return;
            input.Clear();
            Phase = BufferPhase.Rebuild;
            Changed?.Invoke();
        }

        public bool Confirm()
        {
            if (Phase != BufferPhase.Rebuild || input.Count != Size) return false;
            if (!input.SequenceEqual(Recordings[Stage]))
            {
                Phase = BufferPhase.Mismatch;
                Changed?.Invoke();
                return false;
            }

            Stage++;
            input.Clear();
            Phase = Stage == StageCount ? BufferPhase.Complete : BufferPhase.Ready;
            if (Complete) Completed?.Invoke();
            Changed?.Invoke();
            return true;
        }

        public bool ClaimSupply()
        {
            if (!Complete || SupplyClaimed) return false;
            SupplyClaimed = true;
            Changed?.Invoke();
            return true;
        }

        public BufferRecallData Capture() => new()
        {
            phase = Phase, stage = Stage, watchTime = watchTime, input = input.ToArray(), supplyClaimed = SupplyClaimed
        };

        public static BufferRecallSession Restore(BufferRecallData data)
        {
            SaveValidation.Require(data != null && Enum.IsDefined(typeof(BufferPhase), data.phase) && data.stage >= 0 &&
                data.stage <= StageCount && (data.stage == StageCount) == (data.phase == BufferPhase.Complete) &&
                (!data.supplyClaimed || data.phase == BufferPhase.Complete) && data.input != null, "Invalid recall progress");
            int length = Recordings[Math.Min(data.stage, StageCount - 1)].Length;
            SaveValidation.Require(data.watchTime >= 0 && data.watchTime < length * Beat &&
                (data.phase == BufferPhase.Observe || data.watchTime == 0) && data.input.Length <= length &&
                data.input.All(cell => cell >= 0 && cell < CellCount) &&
                (data.phase is not (BufferPhase.Ready or BufferPhase.Complete) || data.input.Length == 0) &&
                (data.phase != BufferPhase.Mismatch || data.input.Length == length), "Invalid recall board");
            var restored = new BufferRecallSession
            {
                Phase = data.phase, Stage = data.stage, watchTime = data.watchTime, SupplyClaimed = data.supplyClaimed
            };
            restored.input.AddRange(data.input);
            return restored;
        }
    }
}
