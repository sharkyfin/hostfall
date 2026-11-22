using System;

namespace Hostfall.Simulation.Computers
{
    [Serializable] public sealed class LogisticsDefenseData
    {
        public int phase;
        public float time;
    }

    public sealed class LogisticsDefense
    {
        public const int Warning = 1, FirstWave = 2, BetweenWaves = 3, SecondWave = 4, Cleared = 5;
        public int Phase { get; private set; }
        public float TimeInPhase { get; private set; }
        public bool Active => Phase is >= Warning and < Cleared;
        public int Released => Phase >= SecondWave ? 3 : Phase >= FirstWave ? 1 : 0;

        public bool Begin()
        {
            if (Phase != 0) return false;
            Phase = Warning;
            return true;
        }

        public bool Advance(float seconds, bool areaClear)
        {
            if (!Active || !float.IsFinite(seconds) || seconds <= 0) return false;
            int next = Phase;
            switch (Phase)
            {
                case Warning:
                    TimeInPhase = Math.Min(1.5f, TimeInPhase + seconds);
                    if (TimeInPhase >= 1.5f) next = FirstWave;
                    break;
                case FirstWave:
                    if (areaClear) next = BetweenWaves;
                    break;
                case BetweenWaves:
                    TimeInPhase = Math.Min(1.8f, TimeInPhase + seconds);
                    if (TimeInPhase >= 1.8f) next = SecondWave;
                    break;
                case SecondWave:
                    if (areaClear) next = Cleared;
                    break;
            }
            if (next == Phase) return false;
            Phase = next;
            TimeInPhase = 0;
            return true;
        }

        public LogisticsDefenseData Capture() => new() { phase = Phase, time = TimeInPhase };

        public static LogisticsDefense Restore(LogisticsDefenseData data, bool logisticsComplete)
        {
            if (data == null) return new LogisticsDefense();
            float latest = data.phase == Warning ? 1.5f : data.phase == BetweenWaves ? 1.8f : 0;
            SaveValidation.Require(data.phase is >= 0 and <= Cleared && (data.phase == 0 || logisticsComplete) &&
                data.time >= 0 && (latest == 0 ? data.time == 0 : data.time < latest), "Invalid logistics defence");
            return new LogisticsDefense { Phase = data.phase, TimeInPhase = data.time };
        }
    }
}
