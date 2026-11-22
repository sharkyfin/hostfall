using System;

namespace Hostfall.Simulation.Computers
{
    public enum SentryOrder { Patrol, Calibration, Capture }
    public enum ContainmentShutter { South, North }
    public enum ContainmentCamera { South, Bay, North }
    public enum SentryZone { South, Bay, North, Transit, Offline }
    public enum ContainmentClearance { Pending, Captured, Offline }

    public readonly struct ContainmentSensors
    {
        public readonly SentryZone Zone;
        public readonly bool SouthBlocked, NorthBlocked, PlayerInside;

        public ContainmentSensors(SentryZone zone, bool southBlocked, bool northBlocked, bool playerInside)
        {
            Zone = zone;
            SouthBlocked = southBlocked;
            NorthBlocked = northBlocked;
            PlayerInside = playerInside;
        }
    }

    [Serializable]
    public sealed class ContainmentData
    {
        public bool started, southOpen = true, northOpen = true;
        public SentryOrder order;
        public ContainmentCamera camera;
        public ContainmentClearance clearance;
        public double southPosition = 1, northPosition = 1, verification;
    }

    public sealed class ContainmentSession
    {
        public const double VerificationSeconds = 1.2;
        public const double CycleSeconds = .85;
        private ContainmentData state = new();

        public bool Started => state.started;
        public bool Complete => Clearance != ContainmentClearance.Pending;
        public SentryOrder Order => state.order;
        public ContainmentCamera Camera => state.camera;
        public ContainmentClearance Clearance => state.clearance;
        public double Verification => state.verification;
        public ContainmentSensors Sensors { get; private set; } = new(SentryZone.Transit, false, false, false);
        public bool CanIsolate => Started && !Complete && Verification >= VerificationSeconds && ReadyForIsolation;
        public event Action Isolated;
        public event Action Changed;

        public bool RequestedOpen(ContainmentShutter shutter) =>
            shutter == ContainmentShutter.South ? state.southOpen : state.northOpen;

        public double Opening(ContainmentShutter shutter) =>
            shutter == ContainmentShutter.South ? state.southPosition : state.northPosition;

        public void Start()
        {
            if (Started) return;
            state.started = true;
            Changed?.Invoke();
        }

        public void Send(SentryOrder order)
        {
            if (!Started || Complete || !Enum.IsDefined(typeof(SentryOrder), order) || Order == order) return;
            state.order = order;
            state.verification = 0;
            Changed?.Invoke();
        }

        public void SelectCamera(ContainmentCamera camera)
        {
            if (!Started || !Enum.IsDefined(typeof(ContainmentCamera), camera) || Camera == camera) return;
            state.camera = camera;
            Changed?.Invoke();
        }

        public void Toggle(ContainmentShutter shutter)
        {
            if (!Started || !Enum.IsDefined(typeof(ContainmentShutter), shutter)) return;
            SetShutter(shutter, !RequestedOpen(shutter));
        }

        // Local egress uses the same shutter request as the console.
        public void Release(ContainmentShutter shutter)
        {
            if (!Started || !Enum.IsDefined(typeof(ContainmentShutter), shutter)) return;
            SetShutter(shutter, true);
        }

        private void SetShutter(ContainmentShutter shutter, bool open)
        {
            if (RequestedOpen(shutter) == open) return;
            if (shutter == ContainmentShutter.South) state.southOpen = open;
            else state.northOpen = open;
            state.verification = 0;
            Changed?.Invoke();
        }

        private bool ReadyForIsolation => !Sensors.PlayerInside && !Sensors.SouthBlocked && !Sensors.NorthBlocked &&
            !state.southOpen && !state.northOpen && state.southPosition == 0 && state.northPosition == 0 &&
            (Sensors.Zone == SentryZone.Offline || Order == SentryOrder.Capture && Sensors.Zone == SentryZone.Bay);

        public void Observe(ContainmentSensors sensors, double seconds)
        {
            Sensors = sensors;
            if (!Started || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) return;
            double tick = Math.Min(.1, seconds);
            double motion = tick / CycleSeconds;
            state.southPosition = Move(state.southPosition, state.southOpen || sensors.SouthBlocked || sensors.PlayerInside, motion);
            state.northPosition = Move(state.northPosition, state.northOpen || sensors.NorthBlocked || sensors.PlayerInside, motion);
            state.verification = !Complete && ReadyForIsolation ? Math.Min(VerificationSeconds, state.verification + tick) : 0;
        }

        public bool Isolate()
        {
            if (!CanIsolate) return false;
            state.clearance = Sensors.Zone == SentryZone.Offline ? ContainmentClearance.Offline : ContainmentClearance.Captured;
            state.verification = 0;
            Isolated?.Invoke();
            Changed?.Invoke();
            return true;
        }

        private static double Move(double opening, bool open, double step) =>
            open ? Math.Min(1, opening + step) : Math.Max(0, opening - step);

        public ContainmentData Capture() => Copy(state);

        public static ContainmentSession Restore(ContainmentData data)
        {
            SaveValidation.Require(data != null && Enum.IsDefined(typeof(SentryOrder), data.order) &&
                Enum.IsDefined(typeof(ContainmentCamera), data.camera) &&
                Enum.IsDefined(typeof(ContainmentClearance), data.clearance) &&
                InBounds(data.southPosition, 1) && InBounds(data.northPosition, 1) &&
                InBounds(data.verification, VerificationSeconds), "Invalid containment control state");
            SaveValidation.Require(data.started || data.southOpen && data.northOpen && data.southPosition == 1 &&
                data.northPosition == 1 && data.order == SentryOrder.Patrol && data.camera == ContainmentCamera.South &&
                data.clearance == ContainmentClearance.Pending && data.verification == 0, "Inactive containment has live settings");
            SaveValidation.Require(data.clearance != ContainmentClearance.Captured || data.order == SentryOrder.Capture,
                "Captured sentry has no transfer order");
            SaveValidation.Require(data.verification == 0 || data.clearance == ContainmentClearance.Pending && !data.southOpen &&
                !data.northOpen && data.southPosition == 0 && data.northPosition == 0, "Containment verification without sealed shutters");
            return new ContainmentSession { state = Copy(data) };
        }

        private static bool InBounds(double value, double upper) => value >= 0 && value <= upper;

        private static ContainmentData Copy(ContainmentData data) => new()
        {
            started = data.started, southOpen = data.southOpen, northOpen = data.northOpen, order = data.order,
            camera = data.camera, clearance = data.clearance, southPosition = data.southPosition,
            northPosition = data.northPosition, verification = data.verification
        };
    }
}
