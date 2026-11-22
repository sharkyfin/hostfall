using System;

namespace Hostfall.Simulation.Computers
{
    [Serializable] public sealed class LogisticsData
    {
        public int version = 1;
        public bool lift, conveyor, bridge, alarm;
        public double liftClock, conveyorClock, bridgeClock, alarmClock;
    }
    // Desired mechanism state and exact clocks belong to the campaign, not scene Time.time.
    public sealed class LogisticsSystem
    {
        private LogisticsData state = new();
        public bool LiftEnabled => state.lift;
        public bool ConveyorEnabled => state.conveyor;
        public bool BridgeEnabled => state.bridge;
        public bool AlarmEnabled => state.alarm;
        public double LiftHeight
        {
            get
            {
                double t = state.liftClock;
                return !state.lift || t < 2 ? 0 : t < 5 ? Smooth((t - 2) / 3) : t < 8 ? 1 : 1 - Smooth((t - 8) / 3);
            }
        }
        public double ConveyorPhase => state.conveyorClock / 8;
        public double BridgeFraction => Smooth(Math.Min(1, state.bridgeClock / 3));
        public double AlarmPhase => state.alarmClock;
        public void Activate(int stage)
        { if (stage >= 1) state.lift = true; if (stage >= 2) state.conveyor = true; if (stage >= 3) state.bridge = state.alarm = true; }
        public void Advance(double seconds)
        {
            if (!Finite(seconds) || seconds <= 0) return;
            seconds = Math.Min(.25, seconds);
            if (state.lift) state.liftClock = (state.liftClock + seconds) % 11;
            if (state.conveyor) state.conveyorClock = (state.conveyorClock + seconds) % 8;
            if (state.bridge) state.bridgeClock = Math.Min(3, state.bridgeClock + seconds);
            if (state.alarm) state.alarmClock = (state.alarmClock + seconds) % 2;
        }
        private static double Smooth(double t) => t * t * (3 - 2 * t);
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public LogisticsData Capture() => new() { lift = state.lift, conveyor = state.conveyor, bridge = state.bridge, alarm = state.alarm,
            liftClock = state.liftClock, conveyorClock = state.conveyorClock, bridgeClock = state.bridgeClock, alarmClock = state.alarmClock };
        public static LogisticsSystem Restore(LogisticsData data, int stage)
        {
            if (data == null) { var initial = new LogisticsSystem(); initial.Activate(stage); return initial; }
            SaveValidation.Require(data.version == 1 && data.lift == (stage >= 1) && data.conveyor == (stage >= 2) &&
                data.bridge == (stage == 3) && data.alarm == (stage == 3), "Invalid logistics activation");
            double[] clocks = { data.liftClock, data.conveyorClock, data.bridgeClock, data.alarmClock };
            double[] limits = { 11, 8, 3.000000001, 2 };
            bool[] active = { data.lift, data.conveyor, data.bridge, data.alarm };
            for (int i = 0; i < clocks.Length; i++) SaveValidation.Require(Finite(clocks[i]) && clocks[i] >= 0 && clocks[i] < limits[i] &&
                (active[i] || clocks[i] == 0), "Invalid logistics clock");
            return new LogisticsSystem { state = new LogisticsData { lift = data.lift, conveyor = data.conveyor, bridge = data.bridge, alarm = data.alarm,
                liftClock = data.liftClock, conveyorClock = data.conveyorClock, bridgeClock = data.bridgeClock, alarmClock = data.alarmClock } };
        }
    }
}
