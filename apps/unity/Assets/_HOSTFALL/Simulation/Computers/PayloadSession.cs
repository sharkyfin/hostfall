using System;
using System.Collections.Generic;

namespace Hostfall.Simulation.Computers
{
    public enum PayloadPhase { Ready, Running, Complete }
    public enum PayloadObjectKind { Platform, MovingPlatform, Conveyor, Laser, Sentinel, Packet, Checkpoint, Exit }
    public readonly struct PayloadObject
    {
        public readonly PayloadObjectKind Kind;
        public readonly double X, Y, Width, Height;
        public readonly bool Active;
        public PayloadObject(PayloadObjectKind kind, double x, double y, double width, double height, bool active = true)
        { Kind = kind; X = x; Y = y; Width = width; Height = height; Active = active; }
    }
    [Serializable] public sealed class PayloadData
    {
        public int version = 1, stage, ticks, deaths, checkpoint, packets;
        public PayloadPhase phase;
        public double x = 1, y = 1, vx, vy, elapsed, coyote, jumpBuffer;
    }

    // A deterministic platformer. Its clock advances only while its desktop application is active.
    // Coordinates use metres, Y up; no Unity physics, rendering or world rewards live here.
    public sealed class PayloadSession
    {
        public const double Width = 34, Height = 12, ActorWidth = .65, ActorHeight = .9, StepSeconds = 1.0 / 120;
        private double elapsed, coyote, jumpBuffer;
        private int checkpoint, packets, ticks;
        private readonly List<PayloadObject> objects = new(20);
        public int Stage { get; private set; }
        public int Deaths { get; private set; }
        public PayloadPhase Phase { get; private set; }
        public double X { get; private set; } = 1;
        public double Y { get; private set; } = 1;
        public double VelocityX { get; private set; }
        public double VelocityY { get; private set; }
        public int PacketMask => packets;
        public int Checkpoint => checkpoint;
        public double Time => ticks * StepSeconds;
        public event Action<int> StageCompleted;
        public event Action Changed;
        public IReadOnlyList<PayloadObject> Objects { get { BuildObjects(Time); return objects; } }

        public void Start()
        {
            if (Phase != PayloadPhase.Ready) return;
            Phase = PayloadPhase.Running; Changed?.Invoke();
        }
        public void QueueJump()
        {
            if (Phase == PayloadPhase.Running) jumpBuffer = .12;
        }
        public void Advance(double seconds, int horizontal)
        {
            if (Phase != PayloadPhase.Running || !Finite(seconds) || seconds <= 0) return;
            elapsed += Math.Min(seconds, .25); horizontal = Math.Max(-1, Math.Min(1, horizontal));
            while (elapsed + 1e-12 >= StepSeconds && Phase == PayloadPhase.Running)
            { elapsed = Math.Max(0, elapsed - StepSeconds); Tick(horizontal); }
            Changed?.Invoke();
        }
        private void Tick(int input)
        {
            double oldTime = Time; ticks++;
            BuildObjects(oldTime);
            // A standing passenger inherits only platform displacement, never wall penetration.
            foreach (var item in objects)
                if (Solid(item) && Math.Abs(Y - (item.Y + item.Height)) < .025 &&
                    X + ActorWidth > item.X && X < item.X + item.Width && VelocityY <= 0)
                {
                    if (item.Kind == PayloadObjectKind.MovingPlatform)
                    { X += MovingX(Time) - MovingX(oldTime); }
                    if (item.Kind == PayloadObjectKind.Conveyor) X += StepSeconds * 1.1;
                }
            VelocityX = Approach(VelocityX, input * 4.6, StepSeconds * (input == 0 ? 32 : 26));
            if (jumpBuffer > 0 && coyote > 0)
            { VelocityY = 8.7; jumpBuffer = coyote = 0; }
            else { jumpBuffer = Math.Max(0, jumpBuffer - StepSeconds); coyote = Math.Max(0, coyote - StepSeconds); }
            VelocityY = Math.Max(-15, VelocityY - 21 * StepSeconds);
            double oldY = Y;
            X = Math.Max(0, Math.Min(Width - ActorWidth, X + VelocityX * StepSeconds));
            Y += VelocityY * StepSeconds;
            BuildObjects(Time);
            if (VelocityY <= 0)
                foreach (var item in objects)
                    if (Solid(item) && oldY >= item.Y + item.Height - .03 && Y <= item.Y + item.Height &&
                        X + ActorWidth > item.X + .015 && X < item.X + item.Width - .015)
                    { Y = item.Y + item.Height; VelocityY = 0; coyote = .1; }
            if (X >= 13 && checkpoint == 0 && Y >= .9) checkpoint = 1;
            for (int i = 0; i < 3; i++)
                if (Overlaps(X, Y, ActorWidth, ActorHeight, PacketX(i), 1.15, .55, .65)) packets |= 1 << i;
            foreach (var item in objects)
                if (item.Active && (item.Kind == PayloadObjectKind.Laser || item.Kind == PayloadObjectKind.Sentinel) &&
                    Overlaps(X + .08, Y + .06, ActorWidth - .16, ActorHeight - .12, item.X, item.Y, item.Width, item.Height))
                { Respawn(); return; }
            if (Y < -2) { Respawn(); return; }
            if (X >= 32.5 && packets == 7)
            {
                Stage++;
                if (Stage == 3) { Phase = PayloadPhase.Complete; VelocityX = VelocityY = elapsed = jumpBuffer = coyote = 0; }
                else { ResetLevel(); Phase = PayloadPhase.Ready; }
                StageCompleted?.Invoke(Stage);
            }
        }
        private void Respawn()
        {
            Deaths++; X = checkpoint == 0 ? 1 : 13.1; Y = 1;
            VelocityX = VelocityY = jumpBuffer = 0; coyote = .1;
        }
        private void ResetLevel()
        { checkpoint = packets = ticks = 0; X = Y = 1; VelocityX = VelocityY = elapsed = jumpBuffer = coyote = 0; }
        private void BuildObjects(double time)
        {
            objects.Clear();
            objects.Add(new PayloadObject(PayloadObjectKind.Platform, 0, 0, 8, 1));
            objects.Add(new PayloadObject(Stage == 1 ? PayloadObjectKind.Conveyor : PayloadObjectKind.Platform, 10, 0, 8, 1));
            objects.Add(new PayloadObject(PayloadObjectKind.Platform, 20, 0, 14, 1));
            objects.Add(new PayloadObject(PayloadObjectKind.MovingPlatform, MovingX(time), 1.65, 2.1, .25));
            objects.Add(new PayloadObject(PayloadObjectKind.Platform, 18.15, 1.6, 1.8, .25));
            if (Stage >= 1) objects.Add(new PayloadObject(PayloadObjectKind.Laser, 24.5, 1, .25, 2.2, time % 3.4 < 1.7));
            if (Stage >= 2) objects.Add(new PayloadObject(PayloadObjectKind.Laser, 11, 1, .25, 1.3, (time + 1) % 3.1 < 1.4));
            objects.Add(new PayloadObject(PayloadObjectKind.Sentinel, 21 + Math.Sin(time * 1.6) * .9, 1, .7, .65));
            for (int i = 0; i < 3; i++) objects.Add(new PayloadObject(PayloadObjectKind.Packet, PacketX(i), 1.15, .55, .65, (packets & (1 << i)) == 0));
            objects.Add(new PayloadObject(PayloadObjectKind.Checkpoint, 13, 1, .2, 1.7, checkpoint > 0));
            objects.Add(new PayloadObject(PayloadObjectKind.Exit, 32.5, 1, .8, 2));
        }
        private double MovingX(double time) => 7.6 + Math.Sin(time * 1.4) * .8;
        private static double PacketX(int i) => i == 0 ? 5 : i == 1 ? 15 : 29;
        private static bool Solid(PayloadObject item) => item.Kind == PayloadObjectKind.Platform || item.Kind == PayloadObjectKind.MovingPlatform || item.Kind == PayloadObjectKind.Conveyor;
        private static bool Overlaps(double x, double y, double w, double h, double bx, double by, double bw, double bh) => x < bx + bw && x + w > bx && y < by + bh && y + h > by;
        private static double Approach(double value, double target, double delta) => value < target ? Math.Min(target, value + delta) : Math.Max(target, value - delta);
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public PayloadData Capture() => new()
        {
            stage = Stage, phase = Phase, ticks = ticks, deaths = Deaths, checkpoint = checkpoint, packets = packets,
            x = X, y = Y, vx = VelocityX, vy = VelocityY, elapsed = elapsed, coyote = coyote, jumpBuffer = jumpBuffer
        };
        public static PayloadSession Restore(PayloadData data)
        {
            SaveValidation.Require(data != null && data.version == 1 && Enum.IsDefined(typeof(PayloadPhase), data.phase) &&
                data.stage >= 0 && data.stage <= 3 && (data.phase == PayloadPhase.Complete) == (data.stage == 3), "Invalid Payload progress");
            SaveValidation.Require(data.ticks >= 0 && data.ticks <= 120 * 60 * 60 * 24 && data.deaths >= 0 && data.deaths <= 100000 &&
                data.checkpoint >= 0 && data.checkpoint <= 1 && data.packets >= 0 && data.packets <= 7, "Invalid Payload counters");
            SaveValidation.Require(Finite(data.x) && Finite(data.y) && Finite(data.vx) && Finite(data.vy) &&
                data.x >= 0 && data.x <= Width - ActorWidth && data.y >= -2 && data.y <= Height &&
                Math.Abs(data.vx) <= 4.600001 && data.vy >= -15 && data.vy <= 8.7, "Invalid Payload pose");
            SaveValidation.Require(Finite(data.elapsed) && data.elapsed >= 0 && data.elapsed < StepSeconds &&
                Finite(data.coyote) && data.coyote >= 0 && data.coyote <= .1 && Finite(data.jumpBuffer) && data.jumpBuffer >= 0 && data.jumpBuffer <= .12,
                "Invalid Payload clock");
            SaveValidation.Require(data.phase != PayloadPhase.Ready || data.x == 1 && data.y == 1 && data.vx == 0 && data.vy == 0 &&
                data.packets == 0 && data.checkpoint == 0 && data.ticks == 0 && data.elapsed == 0, "Invalid Payload checkpoint");
            SaveValidation.Require(data.phase != PayloadPhase.Complete || data.packets == 7 && data.x >= 32.5 && data.vx == 0 && data.vy == 0 && data.elapsed == 0,
                "Invalid completed Payload transfer");
            return new PayloadSession { Stage = data.stage, Phase = data.phase, ticks = data.ticks, Deaths = data.deaths,
                checkpoint = data.checkpoint, packets = data.packets, X = data.x, Y = data.y, VelocityX = data.vx, VelocityY = data.vy,
                elapsed = data.elapsed, coyote = data.coyote, jumpBuffer = data.jumpBuffer };
        }
    }
}
