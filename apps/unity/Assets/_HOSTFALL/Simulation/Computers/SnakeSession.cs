using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation.Computers
{
    public enum SnakeDirection { Right, Down, Left, Up }
    public enum SnakePhase { Ready, Running, Failed, Complete }

    [Serializable]
    public sealed class SnakeData
    {
        public int version = 1, stage, score, direction, queuedDirection, food;
        public int[] body;
        public bool hasQueuedTurn;
        public SnakePhase phase;
        public uint random;
        public double elapsed;
    }

    // Fixed-step grid rules, independent of UI, framerate and scene lifetime.
    public sealed class SnakeSession
    {
        public const int Width = 16, Height = 12, PickupsPerStage = 4, StageCount = 3;
        private readonly List<int> body = new();
        private readonly bool[] blocked = new bool[Width * Height];
        private readonly bool[] reachable = new bool[Width * Height];
        private readonly Queue<int> search = new();
        private uint random;
        private SnakeDirection queued;
        private bool hasQueuedTurn;
        private double elapsed;

        public int Stage { get; private set; }
        public int Score { get; private set; }
        public int Food { get; private set; }
        public SnakeDirection Direction { get; private set; }
        public SnakePhase Phase { get; private set; }
        public IReadOnlyList<int> Body => body;
        public double StepSeconds => Stage == 0 ? .20 : Stage == 1 ? .165 : .13;
        public double PartialStep => elapsed;
        public bool DiagnosticFood => Score % PickupsPerStage == PickupsPerStage - 1;
        public event Action<int> StageCompleted;
        public event Action Changed;

        public SnakeSession(uint seed = 0x4e4c0398)
        {
            random = seed == 0 ? 1u : seed;
            ResetBoard();
        }

        public bool IsObstacle(int cell) => cell >= 0 && cell < blocked.Length && blocked[cell];
        public void Start()
        {
            if (Phase == SnakePhase.Complete || Phase == SnakePhase.Running) return;
            if (Phase == SnakePhase.Failed) { Score = Stage * PickupsPerStage; ResetBoard(); }
            Phase = SnakePhase.Running;
            Changed?.Invoke();
        }

        public bool Turn(SnakeDirection next)
        {
            if (Phase != SnakePhase.Running || hasQueuedTurn || !Enum.IsDefined(typeof(SnakeDirection), next) ||
                next == Direction || ((int)next + 2) % 4 == (int)Direction) return false;
            queued = next;
            hasQueuedTurn = true;
            return true;
        }

        // Call only while this application is active; leaving it freezes its clock, not the world.
        public void Advance(double seconds)
        {
            if (Phase != SnakePhase.Running || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) return;
            elapsed += Math.Min(seconds, .5);
            int ticks = 0;
            while (Phase == SnakePhase.Running && elapsed + 1e-9 >= StepSeconds && ticks++ < 8)
            {
                elapsed = Math.Max(0, elapsed - StepSeconds);
                Step();
            }
        }

        private void Step()
        {
            if (hasQueuedTurn) Direction = queued;
            hasQueuedTurn = false;
            int head = body[0], x = head % Width, y = head / Width;
            switch (Direction)
            {
                case SnakeDirection.Right: x++; break;
                case SnakeDirection.Down: y++; break;
                case SnakeDirection.Left: x--; break;
                case SnakeDirection.Up: y--; break;
            }
            int next = y * Width + x;
            bool eats = next == Food;
            // The tail vacates its cell on a non-growth tick.
            if (x < 0 || x >= Width || y < 0 || y >= Height || blocked[next] ||
                body.Take(body.Count - (eats ? 0 : 1)).Contains(next))
            {
                Phase = SnakePhase.Failed;
                elapsed = 0;
                Changed?.Invoke();
                return;
            }
            body.Insert(0, next);
            if (!eats) body.RemoveAt(body.Count - 1);
            else
            {
                Score++;
                if (Score % PickupsPerStage == 0)
                {
                    Stage++;
                    if (Stage == StageCount) { Phase = SnakePhase.Complete; Food = -1; elapsed = 0; }
                    else ResetBoard();
                    // New board and earned stage are committed together before publishing the reward.
                    StageCompleted?.Invoke(Stage);
                }
                else PlaceFood();
            }
            Changed?.Invoke();
        }

        private void ResetBoard()
        {
            ConfigureObstacles();
            body.Clear();
            for (int x = 5; x >= 2; x--) body.Add(5 * Width + x);
            Direction = queued = SnakeDirection.Right;
            hasQueuedTurn = false;
            elapsed = 0;
            Phase = SnakePhase.Ready;
            PlaceFood();
            // Completing a stage continues without requiring an extra click.
            if (Stage > 0) Phase = SnakePhase.Running;
        }

        private void ConfigureObstacles()
        {
            Array.Clear(blocked, 0, blocked.Length);
            if (Stage >= 1)
                for (int x = 5; x <= 10; x++) { blocked[2 * Width + x] = true; blocked[9 * Width + x] = true; }
            if (Stage >= 2)
                for (int y = 4; y <= 7; y++) { blocked[y * Width + 1] = true; blocked[y * Width + 14] = true; }
        }

        private uint NextRandom()
        {
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            return random;
        }

        private void PlaceFood()
        {
            // Food never appears in an isolated pocket or inside the body/obstacles.
            Array.Clear(reachable, 0, reachable.Length);
            search.Clear(); search.Enqueue(body[0]); reachable[body[0]] = true;
            while (search.Count > 0)
            {
                int cell = search.Dequeue(), x = cell % Width, y = cell / Width;
                if (x > 0) Visit(cell - 1);
                if (x + 1 < Width) Visit(cell + 1);
                if (y > 0) Visit(cell - Width);
                if (y + 1 < Height) Visit(cell + Width);
            }
            int count = 0;
            for (int i = 0; i < reachable.Length; i++) if (reachable[i] && !body.Contains(i)) count++;
            // Authored boards always have room; reject an invalid restored board rather than hang.
            SaveValidation.Require(count > 0, "Snake board has no reachable food cell");
            int choice = (int)(NextRandom() % (uint)count);
            for (int i = 0; i < reachable.Length; i++)
                if (reachable[i] && !body.Contains(i) && choice-- == 0) { Food = i; return; }
        }

        private void Visit(int cell)
        {
            if (reachable[cell] || blocked[cell] || body.Contains(cell)) return;
            reachable[cell] = true; search.Enqueue(cell);
        }

        public SnakeData Capture() => new()
        {
            stage = Stage, score = Score, body = body.ToArray(), food = Food,
            direction = (int)Direction, queuedDirection = (int)queued, hasQueuedTurn = hasQueuedTurn,
            random = random, phase = Phase, elapsed = elapsed
        };

        public static SnakeSession Restore(SnakeData data)
        {
            SaveValidation.Require(data != null && data.version == 1, "Invalid Snake version");
            SaveValidation.Require(Enum.IsDefined(typeof(SnakePhase), data.phase), "Invalid Snake phase");
            SaveValidation.Require(data.stage >= 0 && data.stage <= StageCount && data.score >= data.stage * PickupsPerStage &&
                data.score <= StageCount * PickupsPerStage && (data.stage == StageCount || data.score < (data.stage + 1) * PickupsPerStage), "Invalid Snake progress");
            SaveValidation.Require((data.phase == SnakePhase.Complete) == (data.stage == StageCount), "Inconsistent Snake completion");
            SaveValidation.Require(data.body != null && data.body.Length == 4 + (data.stage == StageCount ? PickupsPerStage : data.score % PickupsPerStage), "Invalid Snake length");
            SaveValidation.Require(data.direction >= 0 && data.direction < 4 && data.queuedDirection >= 0 && data.queuedDirection < 4 &&
                (!data.hasQueuedTurn || (data.queuedDirection != data.direction && (data.queuedDirection + 2) % 4 != data.direction)), "Invalid Snake turn");
            var session = new SnakeSession(1) { Stage = data.stage };
            session.ConfigureObstacles();
            var unique = new HashSet<int>();
            int previous = -1;
            foreach (int cell in data.body)
            {
                SaveValidation.Require(cell >= 0 && cell < Width * Height && !session.blocked[cell] && unique.Add(cell), "Invalid Snake body");
                if (previous >= 0) SaveValidation.Require(Math.Abs(cell % Width - previous % Width) + Math.Abs(cell / Width - previous / Width) == 1, "Disconnected Snake body");
                previous = cell;
            }
            SaveValidation.Require(data.stage == StageCount ? data.food == -1 : data.food >= 0 && data.food < Width * Height && !unique.Contains(data.food) && !session.blocked[data.food], "Invalid Snake food");
            SaveValidation.Require(data.random != 0 && !double.IsNaN(data.elapsed) && !double.IsInfinity(data.elapsed) && data.elapsed >= 0 && data.elapsed < session.StepSeconds &&
                ((data.phase == SnakePhase.Running) || data.elapsed == 0), "Invalid Snake timing");
            if (data.phase == SnakePhase.Ready) SaveValidation.Require(data.stage == 0 && data.score == 0, "Invalid ready stage");
            session.body.Clear(); session.body.AddRange(data.body);
            session.Score = data.score; session.Direction = (SnakeDirection)data.direction;
            session.queued = (SnakeDirection)data.queuedDirection; session.hasQueuedTurn = data.hasQueuedTurn;
            session.random = data.random; session.elapsed = data.elapsed; session.Phase = data.phase; session.Food = data.food;
            return session;
        }
    }
}
